using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable InconsistentNaming

namespace Fantasy
{
    /// <summary>
    /// TCP客户端网络类，用于管理TCP客户端网络连接。
    /// </summary>
    public sealed class TCPClientNetwork : AClientNetwork
    {
        #region 逻辑线程

        private bool _isInit;
        private long _connectTimeoutId;
        /// <summary>
        /// 在网络通道被销毁时触发的事件。
        /// </summary>
        public override event Action OnDispose;
        /// <summary>
        /// 在连接失败时触发的事件。
        /// </summary>
        public override event Action OnConnectFail;
        /// <summary>
        /// 在连接成功时触发的事件。
        /// </summary>
        public override event Action OnConnectComplete;
        /// <summary>
        /// 在连接断开时触发的事件。
        /// </summary>
        public override event Action OnConnectDisconnect;
        /// <summary>
        /// 在通道 ID 发生变化时触发的事件，参数为新的通道 ID。
        /// </summary>
        public override event Action<uint> OnChangeChannelId = channelId => { };
        /// <summary>
        /// 在接收到内存流数据包时触发的事件，参数为解析后的数据包信息。
        /// </summary>
        public override event Action<APackInfo> OnReceiveMemoryStream;

        /// <summary>
        /// 创建一个 TCP协议客户端网络实例。
        /// </summary>
        /// <param name="scene">所属场景。</param>
        /// <param name="networkTarget">网络目标。</param>
        public TCPClientNetwork(Scene scene, NetworkTarget networkTarget) : base(scene, NetworkType.Client, NetworkProtocolType.TCP, networkTarget)
        {
            NetworkThread.Instance.AddNetwork(this);
        }

        /// <summary>
        /// 连接到远程服务器。
        /// </summary>
        /// <param name="remoteEndPoint">远程服务器的终端点。</param>
        /// <param name="onConnectComplete">连接成功时的回调。</param>
        /// <param name="onConnectFail">连接失败时的回调。</param>
        /// <param name="onConnectDisconnect">连接断开时的回调。</param>
        /// <param name="connectTimeout">连接超时时间，单位：毫秒。</param>
        /// <returns>连接的通道ID。</returns>
        public override uint Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000)
        {
            // 如果已经初始化过一次，抛出异常，要求重新实例化
            if (_isInit)
            {
                throw new NotSupportedException($"KCPClientNetwork Id:{Id} Has already been initialized. If you want to call Connect again, please re instantiate it.");
            }
            
            _isInit = true;
            OnConnectFail = onConnectFail;
            OnConnectComplete = onConnectComplete;
            OnConnectDisconnect = onConnectDisconnect;

            // 生成随机的 Channel ID
            ChannelId = 0xC0000000 | (uint) new Random().Next();

            // 设置发送操作的委托
            _sendAction = (rpcId, routeTypeOpCode, routeId, memoryStream, message) =>
            {
                if (IsDisposed)
                {
                    return;
                }

                _messageCache.Enqueue(new MessageCacheInfo()
                {
                    RpcId = rpcId,
                    RouteId = routeId,
                    RouteTypeOpCode = routeTypeOpCode,
                    Message = message,
                    MemoryStream = memoryStream
                });
            };

            // 创建数据包解析器
            _packetParser = APacketParser.CreatePacketParser(NetworkTarget);

            // 设置异步操作完成时的回调函数
            _outArgs.Completed += OnComplete;
            _innArgs.Completed += OnComplete;

            // 设置连接超时定时器
            _connectTimeoutId = TimerScheduler.Instance.Core.OnceTimer(connectTimeout, () =>
            {
                OnConnectFail?.Invoke();
                Dispose();
            });

            // 将连接操作放入网络主线程的同步上下文中执行
            NetworkThread.Instance.SynchronizationContext.Post(() =>
            {
                // 创建异步操作参数
                var outArgs = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = remoteEndPoint
                };
                
                outArgs.Completed += OnComplete;

                // 创建套接字并设置参数
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {NoDelay = true};
                _socket.SetSocketBufferToOsLimit();
                // 如果连接成功，直接返回；否则继续执行连接操作
                if (_socket.ConnectAsync(outArgs))
                {
                    return;
                }
                // 手动触发连接完成事件
                OnNetworkConnectComplete(outArgs);
            });

            return ChannelId;
        }

        /// <summary>
        /// 释放资源并断开网络连接。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            IsDisposed = true;
            
            NetworkThread.Instance.SynchronizationContext.Post(() =>
            {
                if (_socket.Connected)
                {
                    if (OnConnectDisconnect != null)
                    {
                        ThreadSynchronizationContext.Main.Post(OnConnectDisconnect);
                    }

                    _socket.Disconnect(true);
                    _socket.Close();
                }

                _outArgs?.Dispose();
                _innArgs?.Dispose();
                _sendBuffer?.Dispose();
                _receiveBuffer?.Dispose();
                _packetParser?.Dispose();

                _sendAction = null;
                _packetParser = null;
                _isSending = false;

                if (_messageCache != null)
                {
                    _messageCache.Clear();
                    _messageCache = null;
                }
                
                ThreadSynchronizationContext.Main.Post(OnDispose);
                base.Dispose();
            });
        }
        
        #endregion

        #region 网络主线程
        
        private Socket _socket; // 用于通信的套接字。
        private bool _isSending; // 表示是否正在发送数据。
        private APacketParser _packetParser; // 数据包解析器。
        private Action<uint, long, long, MemoryStream, object> _sendAction; // 发送数据的回调方法。
        private readonly CircularBuffer _sendBuffer = new CircularBuffer(); // 发送数据的环形缓冲区。
        private readonly CircularBuffer _receiveBuffer = new CircularBuffer(); // 接收数据的环形缓冲区。
        private readonly SocketAsyncEventArgs _outArgs = new SocketAsyncEventArgs(); // 发送数据异步操作的参数。
        private readonly SocketAsyncEventArgs _innArgs = new SocketAsyncEventArgs(); // 接收数据异步操作的参数。
        private Queue<MessageCacheInfo> _messageCache = new Queue<MessageCacheInfo>(); // 数据消息缓存队列。

        /// <summary>
        /// 在网络连接成功时的回调方法。
        /// </summary>
        /// <param name="asyncEventArgs"></param>
        private void OnNetworkConnectComplete(SocketAsyncEventArgs asyncEventArgs)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (IsDisposed)
            {
                return;
            }

            if (asyncEventArgs.SocketError != SocketError.Success)
            {
                Log.Error($"Unable to connect to the target server asyncEventArgs:{asyncEventArgs.SocketError}");
                
                if (OnConnectFail != null)
                {
                    ThreadSynchronizationContext.Main.Post(OnConnectFail);
                }
                
                Dispose();
                return;
            }
            
            Receive();
            ClearConnectTimeout(ref _connectTimeoutId);

            _sendAction = (rpcId, routeTypeOpCode, routeId, memoryStream, message) =>
            {
                if (IsDisposed)
                {
                    return;
                }

                memoryStream = Pack(rpcId, routeTypeOpCode, routeId, memoryStream, message);
                Send(memoryStream);
            };

            while (_messageCache.TryDequeue(out var messageCacheInfo))
            {
                _sendAction(
                    messageCacheInfo.RpcId, 
                    messageCacheInfo.RouteTypeOpCode, 
                    messageCacheInfo.RouteId,
                    messageCacheInfo.MemoryStream, 
                    messageCacheInfo.Message);
            }

            _messageCache.Clear();
            _messageCache = null;

            if (OnConnectComplete != null)
            {
                ThreadSynchronizationContext.Main.Post(OnConnectComplete);
            }
        }

        /// <summary>
        /// 发送数据到指定的网络通道。
        /// </summary>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="message">要发送的消息。</param>
        public override void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, object message)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (channelId != ChannelId || IsDisposed)
            {
                return;
            }

            _sendAction(rpcId, routeTypeOpCode, routeId, null, message);
        }

        /// <summary>
        /// 发送数据到指定的网络通道。
        /// </summary>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="memoryStream">要发送的内存流。</param>
        public override void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (channelId != ChannelId || IsDisposed)
            {
                return;
            }

            _sendAction(rpcId, routeTypeOpCode, routeId, memoryStream, null);
        }

        private void Send(MemoryStream memoryStream)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            _sendBuffer.Write(memoryStream);
            
            // 因为memoryStream对象池出来的、所以需要手动回收下
            
            memoryStream.Dispose();
            
            if (_isSending)
            {
                return;
            }
            
            Send();
        }
        
        private void Send()
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (_isSending || IsDisposed)
            {
                return;
            }

            for (;;)
            {
                try
                {
                    if (_sendBuffer.Length == 0)
                    {
                        _isSending = false;
                        return;
                    }
                    
                    _isSending = true;
                    
                    var sendSize = CircularBuffer.ChunkSize - _sendBuffer.FirstIndex;
                    
                    if (sendSize > _sendBuffer.Length)
                    {
                        sendSize = (int) _sendBuffer.Length;
                    }

                    _outArgs.SetBuffer(_sendBuffer.First, _sendBuffer.FirstIndex, sendSize);
                    
                    if (_socket.SendAsync(_outArgs))
                    {
                        return;
                    }
                    
                    SendCompletedHandler(_outArgs);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        private void SendCompletedHandler(SocketAsyncEventArgs asyncEventArgs)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (asyncEventArgs.SocketError != SocketError.Success || asyncEventArgs.BytesTransferred == 0)
            {
                return;
            }
            
            _sendBuffer.FirstIndex += asyncEventArgs.BytesTransferred;
        
            if (_sendBuffer.FirstIndex == CircularBuffer.ChunkSize)
            {
                _sendBuffer.FirstIndex = 0;
                _sendBuffer.RemoveFirst();
            }
        }

        private void OnSendComplete(SocketAsyncEventArgs asyncEventArgs)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (IsDisposed)
            {
                return;
            }
            
            _isSending = false;
            SendCompletedHandler(asyncEventArgs);
            
            if (_sendBuffer.Length > 0)
            {
                Send();
            }
        }
        
        private void Receive()
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            for (;;)
            {
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    
                    var size = CircularBuffer.ChunkSize - _receiveBuffer.LastIndex;
                    _innArgs.SetBuffer(_receiveBuffer.Last, _receiveBuffer.LastIndex, size);
                    
                    if (_socket.ReceiveAsync(_innArgs))
                    {
                        return;
                    }
                    
                    ReceiveCompletedHandler(_innArgs);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        private void ReceiveCompletedHandler(SocketAsyncEventArgs asyncEventArgs)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (asyncEventArgs.SocketError != SocketError.Success)
            {
                return;
            }

            if (asyncEventArgs.BytesTransferred == 0)
            {
                Dispose();
                return;
            }
            
            _receiveBuffer.LastIndex += asyncEventArgs.BytesTransferred;
                
            if (_receiveBuffer.LastIndex >= CircularBuffer.ChunkSize)
            {
                _receiveBuffer.AddLast();
                _receiveBuffer.LastIndex = 0;
            }

            for (;;)
            {
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    
                    if (!_packetParser.UnPack(_receiveBuffer,out var packInfo))
                    {
                        break;
                    }

                    ThreadSynchronizationContext.Main.Post(() =>
                    {
                        if (IsDisposed)
                        {
                            return;
                        }
                        
                        // ReSharper disable once PossibleNullReferenceException
                        OnReceiveMemoryStream(packInfo);
                    });
                }
                catch (ScanException e)
                {
                    Log.Debug($"{e}");
                    Dispose();
                }
                catch (Exception e)
                {
                    Log.Error($"{e}");
                    Dispose();
                }
            }
        }
        
        private void OnReceiveComplete(SocketAsyncEventArgs asyncEventArgs)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            ReceiveCompletedHandler(asyncEventArgs);
            Receive();
        }

        /// <summary>
        /// 从网络中移除指定通道。
        /// </summary>
        /// <param name="channelId">要移除的通道 ID。</param>
        public override void RemoveChannel(uint channelId)
        {
            Dispose();
        }

        private void ClearConnectTimeout(ref long connectTimeoutId)
        {
            if (connectTimeoutId == 0)
            {
                return;
            }
            
            if (NetworkThread.Instance.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                var timeoutId = connectTimeoutId;
                ThreadSynchronizationContext.Main.Post(() => { TimerScheduler.Instance.Core.Remove(timeoutId); });
                connectTimeoutId = 0;
                return;
            }
            
            TimerScheduler.Instance.Core.RemoveByRef(ref connectTimeoutId);
        }

        #endregion
        
        #region 网络线程（由Socket底层产生的线程）

        private void OnComplete(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            if (IsDisposed)
            {
                return;
            }

            switch (asyncEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                {
                    NetworkThread.Instance.SynchronizationContext.Post(() => OnNetworkConnectComplete(asyncEventArgs));
                    break;
                }
                case SocketAsyncOperation.Receive:
                {
                    NetworkThread.Instance.SynchronizationContext.Post(() => OnReceiveComplete(asyncEventArgs));
                    break;
                }
                case SocketAsyncOperation.Send:
                {
                    NetworkThread.Instance.SynchronizationContext.Post(() => OnSendComplete(asyncEventArgs));
                    break;
                }
                case SocketAsyncOperation.Disconnect:
                {
                    NetworkThread.Instance.SynchronizationContext.Post(Dispose);
                    break;
                }
            }
        }

        #endregion
    }
}