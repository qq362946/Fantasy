#if FANTASY_NET
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using kcp2k;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 表示KCP协议服务器网络类。
    /// </summary>
    public class KCPServerNetwork : ANetwork
    {
        private Socket _socket;
        private readonly long _startTime;
        public readonly Thread NetworkThread;
        private readonly byte[] _rawReceiveBuffer;
        public ThreadSynchronizationContext NetworkThreadSynchronizationContext;
        private KCPSettings KcpSettings { get; set; }

        #region 跟随Scene或者Server所在的线程执行

        /// <summary>
        /// 构造函数，初始化 KCP 服务端网络实例。
        /// </summary>
        /// <param name="scene">场景实例。</param>
        /// <param name="networkTarget">网络目标。</param>
        /// <param name="address">绑定的地址和端口。</param>
        public KCPServerNetwork(Scene scene, NetworkTarget networkTarget, IPEndPoint address) : base(scene, NetworkType.Server, NetworkProtocolType.KCP, networkTarget)
        {
            _startTime = TimeHelper.Now;
            KcpSettings = KCPSettings.Create(NetworkTarget);
            _rawReceiveBuffer = new byte[KcpSettings.Mtu + 5];
            
            NetworkThread = new Thread(() =>
            {
                _socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                }
            
                _socket.Bind(address);
                _socket.SetSocketBufferToOsLimit();
                _socket.SetSioUdpConnReset();
                Loop();
            });
            NetworkThread.Start();
        }
        
        /// <summary>
        /// 从网络中移除指定通道。
        /// </summary>
        /// <param name="channelId">要移除的通道ID。</param>
        public override void RemoveChannel(uint channelId)
        {
            if (IsDisposed)
            {
                return;
            }
            
            if(NetworkThread != Thread.CurrentThread)
            {
                // 如果不是网络线程、要投递到网络线程上执行。
                NetworkThreadSynchronizationContext.Post(() =>
                {
                    RemoveChannel(channelId);
                });
                return;
            }

            if (_connectionChannel.Remove(channelId, out var channel))
            {
#if NETDEBUG
                Log.Debug($"KCPServerNetwork _connectionChannel:{_connectionChannel.Count}");
#endif
                channel.Dispose();
                return;
            }

            if (RemovePendingConnection(channelId, null, out channel) && !channel.IsDisposed)
            {
                channel.Dispose();
            }
        }
        
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            IsDisposed = true;
            NetworkThread.Join();
            
            if (_socket.Connected)
            {
                _socket.Disconnect(false);
                _socket.Close();
            }
            
            var channels = new List<KCPServerNetworkChannel>();

            channels.AddRange(_connectionChannel.Values);
            channels.AddRange(_pendingConnection.Values);

            foreach (var channel in channels.Where(channel => !channel.IsDisposed))
            {
                channel.Dispose();
            }
            
            _updateTimeOutTime.Clear();
            _updateChannels.Clear();
            _pendingTimeOutTime.Clear();
            _updateTimer.Clear();
            _pendingConnectionTimer.Clear();
            _pendingConnection.Clear();
            _connectionChannel.Clear();
            
            KcpSettings = null;
            _updateMinTime = 0;
            _pendingMinTime = 0;
            base.Dispose();
        }

        #endregion

        #region 网络线程
        
        private uint _updateMinTime;
        private uint _pendingMinTime;
        private readonly byte[] _sendBuff = new byte[5];
        private EndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private readonly Queue<uint> _updateTimeOutTime = new Queue<uint>();
        private readonly HashSet<uint> _updateChannels = new HashSet<uint>();
        private readonly Queue<uint> _pendingTimeOutTime = new Queue<uint>();
        private readonly SortedOneToManyHashSet<uint, uint> _updateTimer = new SortedOneToManyHashSet<uint, uint>();
        private readonly SortedOneToManyList<uint, uint> _pendingConnectionTimer = new SortedOneToManyList<uint, uint>();
        private readonly Dictionary<uint, KCPServerNetworkChannel> _pendingConnection = new Dictionary<uint, KCPServerNetworkChannel>();
        private readonly Dictionary<uint, KCPServerNetworkChannel> _connectionChannel = new Dictionary<uint, KCPServerNetworkChannel>();
        private uint TimeNow => (uint) (TimeHelper.Now - _startTime);
        
        private void Receive()
        {
            // 循环接收数据，直到没有数据可用
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            while (_socket != null && _socket.Available > 0)
            {
                try
                {
                    // 接收数据并更新客户端终结点
                    var receiveLength = _socket.ReceiveFrom(_rawReceiveBuffer, ref _clientEndPoint);
                    if (receiveLength < 1)
                    {
                        continue;
                    }
                    // 解析数据包头部
                    var header = (KcpHeader) _rawReceiveBuffer[0];
                    var channelId = BitConverter.ToUInt32(_rawReceiveBuffer, 1);
                    switch (header)
                    {
                        case KcpHeader.RequestConnection:
                        {
                            if (receiveLength != 5)
                            {
                                break; // 数据长度异常，忽略处理
                            }
                            
                            if (_pendingConnection.TryGetValue(channelId, out var channel))
                            {
                                if (!_clientEndPoint.Equals(channel.RemoteEndPoint))
                                {
                                    // 重复通道ID，向客户端发送重复通道ID消息
                                    SendToRepeatChannelId(channelId, _clientEndPoint);
                                }
                                
                                break;
                            }
                            
                            if (_connectionChannel.ContainsKey(channelId))
                            {
                                // 已存在的通道ID，向客户端发送重复通道ID消息
                                SendToRepeatChannelId(channelId, _clientEndPoint);
                                break;
                            }
                            
                            var timeNow = TimeNow;
                            var tillTime = timeNow + 10 * 1000;
                            var pendingChannel = new KCPServerNetworkChannel(this, channelId, _clientEndPoint, _socket, timeNow);
                            
                            if (tillTime < _pendingMinTime || _pendingMinTime == 0)
                            {
                                _pendingMinTime = tillTime;
                            }
                            
                            _pendingConnection.Add(channelId, pendingChannel);
                            _pendingConnectionTimer.Add(tillTime, channelId);
                            _sendBuff.WriteTo(0, (byte) KcpHeader.WaitConfirmConnection);
                            _sendBuff.WriteTo(1, channelId);
                            _socket.SendTo(_sendBuff, 0, 5, SocketFlags.None, _clientEndPoint);
                            break;
                        }
                        case KcpHeader.ConfirmConnection:
                        {
                            if (receiveLength != 5)
                            {
                                break;
                            }
                            
                            if (!RemovePendingConnection(channelId, _clientEndPoint, out var channel))
                            {
                                break;
                            }
                            
                            var kcp = new Kcp(channelId, channel.Output);
                            kcp.SetNoDelay(1, 5, 2, true);
                            kcp.SetWindowSize(KcpSettings.SendWindowSize, KcpSettings.ReceiveWindowSize);
                            kcp.SetMtu(KcpSettings.Mtu);
                            kcp.SetMinrto(30);
                            _connectionChannel.Add(channel.Id, channel);
                            channel.Connect(kcp, KcpSettings.MaxSendWindowSize);
                            break;
                        }
                        case KcpHeader.ReceiveData:
                        {
                            var messageLength = receiveLength - 5;
                            
                            if (messageLength <= 0)
                            {
                                Log.Warning($"KCP Server KcpHeader.Data  messageLength <= 0");
                                break;
                            }
                            
                            if (!_connectionChannel.TryGetValue(channelId, out var channel))
                            {
                                break;
                            }

                            channel.Kcp.Input(_rawReceiveBuffer, 5, messageLength);
                            AddToUpdate(0, channel.Id);
                            channel.Receive();
                            break;
                        }
                        case KcpHeader.Disconnect:
                        {
                            // Log.Debug("KcpHeader.Disconnect");
                            RemoveChannel(channelId);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        /// <summary>
        /// 发送指定通道的数据，以重复通道ID的方式。
        /// </summary>
        /// <param name="channelId">通道ID。</param>
        /// <param name="clientEndPoint">客户端终结点。</param>
        
        private void SendToRepeatChannelId(uint channelId, EndPoint clientEndPoint)
        {
            _sendBuff.WriteTo(0, (byte) KcpHeader.RepeatChannelId);
            _sendBuff.WriteTo(1, channelId);
            _socket.SendTo(_sendBuff, 0, 5, SocketFlags.None, clientEndPoint);
        }
        
        /// <summary>
        /// 移除待处理连接。
        /// </summary>
        /// <param name="channelId">通道ID。</param>
        /// <param name="remoteEndPoint">远程终结点。</param>
        /// <param name="channel">待处理的网络通道。</param>
        /// <returns>是否成功移除待处理连接。</returns>
        
        private bool RemovePendingConnection(uint channelId, EndPoint remoteEndPoint, out KCPServerNetworkChannel channel)
        {
            if (!_pendingConnection.TryGetValue(channelId, out channel) || channel.IsDisposed)
            {
                return false;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (remoteEndPoint != null && !remoteEndPoint.Equals(channel.RemoteEndPoint))
            {
                Log.Error($"KCPNetworkChannel syn address diff: {channelId} {channel.RemoteEndPoint} {remoteEndPoint}");
                return false;
            }

            _pendingConnection.Remove(channelId);
            _pendingConnectionTimer.RemoveValue(channel.CreateTime + 10 * 1000, channelId);
#if NETDEBUG
            Log.Debug($"KCPServerNetwork _pendingConnection:{_pendingConnection.Count} _pendingConnectionTimer:{_pendingConnectionTimer.Count}");
#endif
            return true;
        }
        
        /// <summary>
        /// 更新网络处理逻辑。
        /// </summary>
        private void Loop()
        {
            NetworkThreadSynchronizationContext = new ThreadSynchronizationContext(NetworkThread.ManagedThreadId);
            SynchronizationContext.SetSynchronizationContext(NetworkThreadSynchronizationContext);
            
            while (true)
            {
                if (IsDisposed)
                {
                    return;
                }
                
                // 接收来自客户端的数据
                Receive();
                // 执行上下文的更新
                NetworkThreadSynchronizationContext.Update();

                var nowTime = TimeNow;

                // 检查是否有定时更新任务需要执行
                if (nowTime >= _updateMinTime && _updateTimer.Count > 0)
                {
                    foreach (var timeId in _updateTimer)
                    {
                        var key = timeId.Key;

                        if (key > nowTime)
                        {
                            _updateMinTime = key;
                            break;
                        }

                        _updateTimeOutTime.Enqueue(key);
                    }

                    // 处理超时的更新任务
                    while (_updateTimeOutTime.TryDequeue(out var time))
                    {
                        foreach (var channelId in _updateTimer[time])
                        {
                            _updateChannels.Add(channelId);
                        }

                        _updateTimer.RemoveKey(time);
                    }
                }

                // 执行待更新的通道逻辑
                if (_updateChannels.Count > 0)
                {
                    foreach (var channelId in _updateChannels)
                    {
                        if (!_connectionChannel.TryGetValue(channelId, out var channel))
                        {
                            continue;
                        }

                        if (channel.IsDisposed)
                        {
                            continue;
                        }

                        var channelKcp = channel.Kcp;
                        
                        try
                        {
                            channelKcp.Update(nowTime);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }

                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        if (channelKcp != null)
                        {
                            AddToUpdate(channelKcp.Check(nowTime), channelId);
                        }
                    }

                    _updateChannels.Clear();
                }

                if (_pendingConnection.Count <= 0 || nowTime < _pendingMinTime)
                {
                    continue;
                }

                foreach (var timeId in _pendingConnectionTimer)
                {
                    var key = timeId.Key;

                    if (key > nowTime)
                    {
                        _pendingMinTime = key;
                        break;
                    }

                    foreach (var channelId in timeId.Value)
                    {
                        _pendingTimeOutTime.Enqueue(channelId);
                    }
                }

                while (_pendingTimeOutTime.TryDequeue(out var channelId))
                {
                    if (RemovePendingConnection(channelId, null, out var channel))
                    {
                        channel.Dispose();
                    }
                }
                
                Thread.Yield();
            }
        }

        /// <summary>
        /// 将通道加入到更新列表中。
        /// </summary>
        /// <param name="tillTime">更新时间。</param>
        /// <param name="channelId">通道ID。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddToUpdate(uint tillTime, uint channelId)
        {
            if (tillTime == 0)
            {
                _updateChannels.Add(channelId);
                return;
            }
        
            if (tillTime < _updateMinTime || _updateMinTime == 0)
            {
                _updateMinTime = tillTime;
            }
        
            _updateTimer.Add(tillTime, channelId);
        }
        
        #endregion
    }
}
#endif