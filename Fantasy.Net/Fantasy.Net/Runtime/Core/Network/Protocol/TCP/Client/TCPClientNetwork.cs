using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable InconsistentNaming

namespace Fantasy
{
    /// <summary>
    /// 消息缓存信息结构。
    /// </summary>
    public struct MessageQueue
    {
        /// <summary>
        /// 获取或设置 RPC ID。
        /// </summary>
        public uint RpcId;
        /// <summary>
        /// 获取或设置路由 ID。
        /// </summary>
        public long RouteId;
        /// <summary>
        /// 获取或设置路由类型与操作码。
        /// </summary>
        public long RouteTypeOpCode;
        /// <summary>
        /// 获取或设置消息对象。
        /// </summary>
        public object Message;
        /// <summary>
        /// 获取或设置内存流。
        /// </summary>
        public MemoryStream MemoryStream;
    }
    
    /// <summary>
    /// TCP客户端网络类，用于管理TCP客户端网络连接。
    /// </summary>
    public sealed class TCPClientNetwork : AClientNetwork
    {
        #region 跟随Scene线程
        
        private bool _connectionSuccessful;
        private long _connectTimeoutId;
        private Socket _socket; // 用于通信的套接字。
        private bool _isSending; // 表示是否正在发送数据。
        private APacketParser _packetParser; // 数据包解析器。
        private readonly CircularBuffer _sendBuffer = new CircularBuffer(); // 发送数据的环形缓冲区。
        private readonly CircularBuffer _receiveBuffer = new CircularBuffer(); // 接收数据的环形缓冲区。
        private readonly SocketAsyncEventArgs _outArgs = new SocketAsyncEventArgs(); // 发送数据异步操作的参数。
        private readonly SocketAsyncEventArgs _innArgs = new SocketAsyncEventArgs(); // 接收数据异步操作的参数。
        private Queue<MessageQueue> _messageCache = new Queue<MessageQueue>(); // 数据消息缓存队列。
        
        private Action OnConnectFail;
        private Action OnConnectComplete;
        private Action OnConnectDisconnect;

        /// <summary>
        /// 创建一个 TCP协议客户端网络实例。
        /// </summary>
        /// <param name="scene">所属场景。</param>
        /// <param name="networkTarget">网络目标。</param>
        public TCPClientNetwork(Scene scene, NetworkTarget networkTarget) : base(scene, NetworkType.Client, NetworkProtocolType.TCP, networkTarget)
        {
            // 设置异步操作完成时的回调函数
            _outArgs.Completed += OnComplete;
            _innArgs.Completed += OnComplete;
            // 创建数据包解析器
            _packetParser = APacketParser.CreatePacketParser(scene, NetworkTarget);
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
        public override Session Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000)
        {
            // 如果已经初始化过一次，抛出异常，要求重新实例化
            
            if (IsInit)
            {
                throw new NotSupportedException($"TCPClientNetwork Id:{Id} Has already been initialized. If you want to call Connect again, please re instantiate it.");
            }
            
            IsInit = true;
            OnConnectFail = onConnectFail;
            OnConnectComplete = onConnectComplete;
            OnConnectDisconnect = onConnectDisconnect;
            // 设置连接超时定时器
            _connectTimeoutId = Scene.TimerComponent.Core.OnceTimer(connectTimeout, () =>
            {
                OnConnectFail?.Invoke();
                Dispose();
            });
            // 创建异步操作参数
            var outArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = remoteEndPoint
            };
            outArgs.Completed += OnComplete;
            // 创建套接字并设置参数
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true;
            _socket.SetSocketBufferToOsLimit();
            // 如果连接成功，直接返回；否则继续执行连接操作
            if (!_socket.ConnectAsync(outArgs))
            {
                // 手动触发连接完成事件
                OnNetworkConnectComplete(outArgs);
            }
            // 创建一个新的Session对象
            Session = Session.Create(this, remoteEndPoint);
            return Session;
        }

        /// <summary>
        /// 在网络连接成功时的回调方法。
        /// </summary>
        /// <param name="asyncEventArgs"></param>
        private void OnNetworkConnectComplete(SocketAsyncEventArgs asyncEventArgs)
        {
            if (IsDisposed)
            {
                return;
            }
            
            if (asyncEventArgs.SocketError != SocketError.Success)
            {
                Log.Error($"Unable to connect to the target server asyncEventArgs:{asyncEventArgs.SocketError}");
                
                OnConnectFail?.Invoke();
                Dispose();
                return;
            }
            // 发送缓存中的消息
            while (_messageCache.TryDequeue(out var messageQueue))
            {
                var stream = _packetParser.Pack(
                    messageQueue.RpcId, messageQueue.RouteTypeOpCode, messageQueue.RouteId,
                    messageQueue.MemoryStream, messageQueue.Message);
                Send(stream);
            }
            _connectionSuccessful = true;
            ClearConnectTimeout();
            OnConnectComplete?.Invoke();
            Receive();
        }

        #region Send

        public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
        {
            if (IsDisposed)
            {
                return;
            }
            
            // 在连接没有创建完成之前，需要将消息放进队列中，等待连接成功后再发送

            if (!_connectionSuccessful)
            {
                _messageCache.Enqueue(new MessageQueue()
                {
                    RpcId = rpcId,
                    RouteId = routeId,
                    RouteTypeOpCode = routeTypeOpCode,
                    Message = message,
                    MemoryStream = memoryStream
                });
                
                return;
            }
            
            Send(_packetParser.Pack(rpcId, routeTypeOpCode, routeId, memoryStream, message));
        }

        public override void Send(MemoryStream memoryStream)
        {
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

        #endregion

        #region Receive

        private void Receive()
        {
            while (true)
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
            
            while (true)
            {
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    if (!_packetParser.UnPack(_receiveBuffer, out var packInfo))
                    {
                        break;
                    }

                    Session.Receive(packInfo);
                }
                catch (ScanException e)
                {
                    Log.Warning($"{e}");
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
            ReceiveCompletedHandler(asyncEventArgs);
            Receive();
        }

        #endregion
        
        /// <summary>
        /// 从网络中移除指定通道。
        /// </summary>
        /// <param name="channelId">要移除的通道 ID。</param>
        public override void RemoveChannel(uint channelId)
        {
            Dispose();
        }

        private void ClearConnectTimeout()
        {
            if (_connectTimeoutId == 0)
            {
                return;
            }

            Scene.TimerComponent.Core.Remove(ref _connectTimeoutId);
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
            
            if (_socket.Connected)
            {
                OnConnectDisconnect?.Invoke();
                _socket.Disconnect(true);
                _socket.Close();
            }

            _outArgs?.Dispose();
            _innArgs?.Dispose();
            _sendBuffer?.Dispose();
            _receiveBuffer?.Dispose();
            _packetParser?.Dispose();
            
            ClearConnectTimeout();

            ChannelId = 0;
            _connectionSuccessful = false;
            _packetParser = null;
            _isSending = false;

            if (_messageCache != null)
            {
                _messageCache.Clear();
                _messageCache = null;
            }

            base.Dispose();
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
                    ThreadSynchronizationContext.Post(() => OnNetworkConnectComplete(asyncEventArgs));
                    break;
                }
                case SocketAsyncOperation.Receive:
                {
                    ThreadSynchronizationContext.Post(() => OnReceiveComplete(asyncEventArgs));
                    break;
                }
                case SocketAsyncOperation.Send:
                {
                    ThreadSynchronizationContext.Post(() => OnSendComplete(asyncEventArgs));
                    break;
                }
                case SocketAsyncOperation.Disconnect:
                {
                    ThreadSynchronizationContext.Post(Dispose);
                    break;
                }
            }
        }

        #endregion
    }
}