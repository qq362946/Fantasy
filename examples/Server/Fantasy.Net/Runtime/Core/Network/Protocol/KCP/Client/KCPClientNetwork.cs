using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using kcp2k;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// KCP协议客户端网络类，用于管理KCP客户端网络连接。
    /// </summary>
    public class KCPClientNetwork : AClientNetwork
    {
        #region 跟随Scene线程

        private bool _isInit;
        private Socket _socket;
        private bool _isDisconnect;
        private Thread _networkThread;
        private long _connectTimeoutId;
        private readonly long _startTime;
        private bool _connectionSuccessful;
        private APacketParser _packetParser;
        
        /// <summary>
        /// 当连接失败时触发的事件。
        /// </summary>
        public override event Action OnConnectFail;
        /// <summary>
        /// 当连接成功建立时触发的事件。
        /// </summary>
        public override event Action OnConnectComplete;
        /// <summary>
        /// 当连接断开时触发的事件。
        /// </summary>
        public override event Action OnConnectDisconnect;
        /// <summary>
        /// 数据消息缓存队列。
        /// </summary>
        private ConcurrentQueue<MessageQueue> _messageCache = new ConcurrentQueue<MessageQueue>();  
#if FANTASY_NET
        private readonly ArrayPool<byte> _byteArrayPool = ArrayPool<byte>.Create(2048, 3000);
#else
        private readonly ArrayPool<byte> _byteArrayPool = ArrayPool<byte>.Create(2048,200);
#endif
        
        public KCPClientNetwork(Scene scene, NetworkTarget networkTarget) : base(scene, NetworkType.Client, NetworkProtocolType.KCP, networkTarget)
        {
            _startTime = TimeHelper.Now;
            _packetParser = APacketParser.CreatePacketParser(Scene, NetworkTarget);
        }
        
        /// <summary>
        /// 连接到指定的远程终结点。
        /// </summary>
        /// <param name="remoteEndPoint">远程终结点。</param>
        /// <param name="onConnectComplete">连接成功回调。</param>
        /// <param name="onConnectFail">连接失败回调。</param>
        /// <param name="onConnectDisconnect">连接断开回调。</param>
        /// <param name="connectTimeout">连接超时时间（毫秒），默认为 5000 毫秒。</param>
        /// <returns>新建的通道 ID。</returns>
        /// <exception cref="NotSupportedException">如果已经初始化，则抛出该异常。</exception>
        public override Session Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000)
        {
            if (_isInit)
            {
                throw new NotSupportedException($"KCPClientNetwork Id:{Id} Has already been initialized. If you want to call Connect again, please re instantiate it.");
            }
            
            _isInit = true;
            OnConnectFail = onConnectFail;
            OnConnectComplete = onConnectComplete;
            OnConnectDisconnect = onConnectDisconnect;
            ChannelId = CreateChannelId();
            _kcpSettings = KCPSettings.Create(NetworkTarget);
            _maxSndWnd = _kcpSettings.MaxSendWindowSize;
            _rawReceiveBuffer = new byte[_kcpSettings.Mtu + 5];
            _receiveMemoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
            // 设置连接超时定时器
            _connectTimeoutId = Scene.TimerComponent.Core.OnceTimer(connectTimeout, () =>
            {
                OnConnectFail?.Invoke();
                Dispose();
            });
            // 创建网络线程
            _networkThread = new Thread(() =>
            {
                // 创建一个新的网络套接字
                _socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketBufferToOsLimit();
                _socket.SetSioUdpConnReset();
                _socket.Connect(remoteEndPoint);
                SendHeader(KcpHeader.RequestConnection);
                Loop();
            });
            _networkThread.Start();
            // 创建一个新的Session对象
            Session = Session.Create(this, remoteEndPoint);
            return Session;
        }
        
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
            if (IsDisposed)
            {
                return;
            }
            
            if (_networkThread == Thread.CurrentThread)
            {
                // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
            
                var waitSendSize = _kcp.WaitSnd;

                if (waitSendSize > _maxSndWnd)
                {
                    Log.Warning($"ERR_KcpWaitSendSizeTooLarge {waitSendSize} > {_maxSndWnd}");
                    Dispose();
                    return;
                }

                _kcp.Send(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                memoryStream.Dispose();
                AddToUpdate(0);
                return;
            }
            
            _networkThreadSynchronizationContext.Post(() =>
            {
                Send(memoryStream);
            });
        }
        
        /// <summary>
        /// 移除指定通道的网络连接。
        /// </summary>
        /// <param name="channelId"></param>
        public override void RemoveChannel(uint channelId)
        {
            Dispose();
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_networkThread != Thread.CurrentThread)
            {
                _networkThreadSynchronizationContext.Post(Dispose);
                return;
            }

            IsDisposed = true;
            
            if (!_isDisconnect)
            {
                SendHeader(KcpHeader.Disconnect);
            }
            
            if (_socket.Connected)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (OnConnectDisconnect != null)
                {
                    ThreadSynchronizationContext.Post(OnConnectDisconnect);
                }
                
                _socket.Disconnect(false);
                _socket.Close();
            }
            
            _kcp = null;
            _maxSndWnd = 0;
            _updateMinTime = 0;
            _receiveMemoryStream.Dispose();
            _receiveMemoryStream = null;
            _rawReceiveBuffer = null;
            _packetParser?.Dispose();
            ClearConnectTimeout();
            _messageCache.Clear();
            _messageCache = null;
            _updateTimer.Clear();
            _updateTimeOutTime.Clear();
            base.Dispose();
        }

        #endregion

        #region 网络线程

        private Kcp _kcp;
        private int _maxSndWnd;
        private long _updateMinTime;
        private KCPSettings _kcpSettings;
        private byte[] _rawReceiveBuffer;
        private MemoryStream _receiveMemoryStream;
        private readonly SortedSet<uint> _updateTimer = new SortedSet<uint>();
        private readonly Queue<uint> _updateTimeOutTime = new Queue<uint>();
        private EndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private ThreadSynchronizationContext _networkThreadSynchronizationContext;
        private uint TimeNow => (uint) (TimeHelper.Now - _startTime);
        
        private void Loop()
        {
            _networkThreadSynchronizationContext = new ThreadSynchronizationContext(_networkThread.ManagedThreadId);
            SynchronizationContext.SetSynchronizationContext(_networkThreadSynchronizationContext);
            
            while (true)
            {
                if (IsDisposed)
                {
                    return;
                }
                
                Receive();
                _networkThreadSynchronizationContext.Update();
                var nowTime = TimeNow;
            
                if (_updateTimer.Count == 0 || nowTime < _updateMinTime)
                {
                    continue;
                }
            
                foreach (var timeId in _updateTimer)
                {
                    if (timeId > nowTime)
                    {
                        _updateMinTime = timeId;
                        break;
                    }
            
                    _updateTimeOutTime.Enqueue(timeId);
                }
            
                while (_updateTimeOutTime.TryDequeue(out var time))
                {
                    KcpUpdate();
                    _updateTimer.Remove(time);
                }
                
                Thread.Yield();
            }
        }
        
        private void Receive()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            while (_socket != null && _socket.Poll(0, SelectMode.SelectRead))
            {
                try
                {
                    var receiveLength = _socket.ReceiveFrom(_rawReceiveBuffer, ref _clientEndPoint);
                    
                    if (receiveLength > _rawReceiveBuffer.Length)
                    {
                        Log.Error($"KCP ClientConnection: message of size {receiveLength} does not fit into buffer of size {_rawReceiveBuffer.Length}. The excess was silently dropped. Disconnecting.");
                        Dispose();
                        return;
                    }
                    
                    var header = (KcpHeader) _rawReceiveBuffer[0];
                    var channelId = BitConverter.ToUInt32(_rawReceiveBuffer, 1);

                    switch (header)
                    {
                        case KcpHeader.RepeatChannelId:
                        {
                            if (receiveLength != 5 || channelId != ChannelId)
                            {
                                break;
                            }
                            
                            // 到这里是客户端的channelId再服务器上已经存在、需要重新生成一个再次尝试连接
                            ChannelId = CreateChannelId();
                            SendHeader(KcpHeader.RequestConnection);
                            // 这里要处理入如果有发送的消息的问题、后面处理
                            break;
                        }
                        case KcpHeader.WaitConfirmConnection:
                        {
                            if (receiveLength != 5 || channelId != ChannelId)
                            {
                                break;
                            }
        
                            SendHeader(KcpHeader.ConfirmConnection);
                            ClearConnectTimeout();
                            // 创建KCP和相关的初始化
                            _kcp = new Kcp(channelId, Output);
                            _kcp.SetNoDelay(1, 5, 2, true);
                            _kcp.SetWindowSize(_kcpSettings.SendWindowSize, _kcpSettings.ReceiveWindowSize);
                            _kcp.SetMtu(_kcpSettings.Mtu);
                            _kcp.SetMinrto(30);
                            // 调用ChannelId改变事件、就算没有改变也要发下、接收事件的地方会判定下
                            ThreadSynchronizationContext.Post(() =>
                            {
                                OnConnectComplete?.Invoke();
                            });
                            // 发送缓存中的消息
                            while (_messageCache.TryDequeue(out var messageQueue))
                            {
                                var stream = _packetParser.Pack(
                                    messageQueue.RpcId, messageQueue.RouteTypeOpCode, messageQueue.RouteId,
                                    messageQueue.MemoryStream, messageQueue.Message);
                                Send(stream);
                            }
                            _connectionSuccessful = true;
                            // 到这里正确创建上连接了、可以正常发送消息了
                            break;
                        }
                        case KcpHeader.ReceiveData:
                        {
                            var messageLength = receiveLength - 5;
                            
                            if (messageLength <= 0)
                            {
                                Log.Warning($"KCPClient KcpHeader.Data  messageLength <= 0");
                                break;
                            }

                            if (channelId != ChannelId)
                            {
                                break;
                            }

                            _kcp.Input(_rawReceiveBuffer, 5, messageLength);
                            AddToUpdate(0);
                            KcpReceive();
                            break;
                        }
                        case KcpHeader.Disconnect:
                        {
                            if (channelId != ChannelId)
                            {
                                break;
                            }
                            
                            _isDisconnect = true;
                            Dispose();
                            break;
                        }
                    }
                }
                // this is fine, the socket might have been closed in the other end
                catch (SocketException) { }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        private void KcpReceive()
        {
            if (IsDisposed)
            {
                return;
            }
            
            while (true)
            {
                try
                {
                    // 获得一个完整消息的长度
                    
                    var peekSize = _kcp.PeekSize();

                    // 如果没有接收的消息那就跳出当前循环。

                    if (peekSize < 0)
                    {
                        return;
                    }

                    // 如果为0，表示当前消息发生错误。提示下、一般情况不会发生的

                    if (peekSize == 0)
                    {
                        throw new Exception("SocketError.NetworkReset");
                    }

                    _receiveMemoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
                    _receiveMemoryStream.SetLength(peekSize);
                    _receiveMemoryStream.Seek(0, SeekOrigin.Begin);
                    var receiveCount = _kcp.Receive(_receiveMemoryStream.GetBuffer().AsMemory(), peekSize);

                    // 如果接收的长度跟peekSize不一样，不需要处理，因为消息肯定有问题的(虽然不可能出现)。

                    if (receiveCount != peekSize)
                    {
                        Log.Error($"receiveCount != peekSize receiveCount:{receiveCount} peekSize:{peekSize}");
                        break;
                    }

                    if (!_packetParser.UnPack(_receiveMemoryStream, out var packInfo))
                    {
                        break;
                    }
                    
                    ThreadSynchronizationContext.Post(() =>
                    {
                        if (IsDisposed)
                        {
                            return;
                        }
                        
                        Session.Receive(packInfo);
                    });
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        /// <summary>
        /// 添加更新操作到更新列表。
        /// </summary>
        /// <param name="tillTime">操作的时间。</param>
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToUpdate(uint tillTime)
        {
            if (tillTime == 0)
            {
                KcpUpdate();
                return;
            }
        
            if (tillTime < _updateMinTime || _updateMinTime == 0)
            {
                _updateMinTime = tillTime;
            }

            _updateTimer.Add(tillTime);
        }
        
        /// <summary>
        /// 更新 KCP 连接状态。
        /// </summary>
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void KcpUpdate()
        {
            var nowTime = TimeNow;
            
            try
            {
                _kcp.Update(nowTime);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
                
            AddToUpdate(_kcp.Check(nowTime));
        }
        
        /// <summary>
        /// 创建一个新的通道 ID。
        /// </summary>
        /// <returns>新的通道 ID。</returns>
        private uint CreateChannelId()
        {
            return 0xC0000000 | (uint) new Random().Next();
        }
        
        /// <summary>
        /// 发送指定的 KCP 消息头到远程端点。
        /// </summary>
        /// <param name="kcpHeader">要发送的 KCP 消息头。</param>
        private void SendHeader(KcpHeader kcpHeader)
        {
            if (_socket == null || !_socket.Connected)
            {
                return;
            }
            
            var buff = new byte[5];
            buff.WriteTo(0, (byte) kcpHeader);
            buff.WriteTo(1, ChannelId);
            _socket.Send(buff, 5, SocketFlags.None);
        }
        
        /// <summary>
        /// 清除连接超时计时器。
        /// </summary>
        private void ClearConnectTimeout()
        {
            if (_connectTimeoutId == 0)
            {
                return;
            }

            if (_networkThread == Thread.CurrentThread)
            {
                ThreadSynchronizationContext.Post(() =>
                {
                    Scene.TimerComponent.Core.Remove(ref _connectTimeoutId);
                });
                
                return;
            }

            Scene.TimerComponent.Core.Remove(ref _connectTimeoutId);
        }
        
        private void Output(byte[] bytes, int count)
        {
            if (IsDisposed)
            {
                return;
            }
            
            try
            {
                if (count == 0)
                {
                    throw new Exception("KcpOutput count 0");
                }
                
                bytes.WriteTo(0, (byte) KcpHeader.ReceiveData);
                bytes.WriteTo(1, ChannelId);
                _socket.Send(bytes, 0, count + 5, SocketFlags.None);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #endregion
    }
}