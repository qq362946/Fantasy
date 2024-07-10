#if FANTASY_NET
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    internal struct PendingConnection
    {
        public readonly uint ChannelId;
        public readonly uint TimeOutId;
        public readonly IPEndPoint RemoteEndPoint;

        public PendingConnection(uint channelId, IPEndPoint remoteEndPoint, uint time)
        {
            ChannelId = channelId;
            RemoteEndPoint = remoteEndPoint;
            TimeOutId = time + 10 * 1000; // 设置10秒超时，如果10秒内没有确认连接则删除。
        }
    }
    
    public sealed class KCPServerSocket : IDisposable
    {
        public Socket Socket;
        private Thread _thread;
        private uint _updateMinTime;
        private uint _pendingMinTime;
        private readonly long _startTime;
        public readonly IPEndPoint BindAddress;
        private readonly byte[] _rawReceiveBuffer = new byte[2048];
        private readonly byte[] _sendBuff = new byte[5];
        private EndPoint _ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private readonly Queue<uint> _pendingTimeOutTime = new Queue<uint>();
        private readonly HashSet<uint> _updateChannels = new HashSet<uint>();
        private readonly Queue<uint> _updateTimeOutTime = new Queue<uint>();
        private readonly Dictionary<uint, KCPServerSSocketAsyncEventArgs> _connectionChannel = new Dictionary<uint, KCPServerSSocketAsyncEventArgs>();
        private readonly SortedOneToManyHashSet<uint, uint> _updateTimer = new SortedOneToManyHashSet<uint, uint>();
        private readonly Dictionary<uint, PendingConnection> _pendingConnection = new Dictionary<uint, PendingConnection>();
        private readonly SortedOneToManyList<uint, uint> _pendingConnectionTimeOut = new SortedOneToManyList<uint, uint>();
        public bool IsDisposed { get; private set; }
        public KCPSettings Settings { get; private set; }
        public ThreadSynchronizationContext ThreadSynchronizationContext { get; private set; }
        private uint TimeNow => (uint) (TimeHelper.Now - _startTime);
        public event EventHandler<KCPServerSSocketAsyncEventArgs> OnConnectionCompleted;

        public KCPServerSocket(NetworkTarget networkTarget, IPEndPoint address)
        {
            BindAddress = address;
            _startTime = TimeHelper.Now;
            Settings = KCPSettings.Create(networkTarget);
        }

        public void Start()
        {
            Socket = new Socket(BindAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            if (BindAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            Socket.Bind(BindAddress);
            Socket.SetSocketBufferToOsLimit();
            Socket.SetSioUdpConnReset();
            _thread = new Thread(Loop);
            _thread.Start();
        }

        private void Loop()
        {
            ThreadSynchronizationContext = new ThreadSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
            SynchronizationContext.SetSynchronizationContext(ThreadSynchronizationContext);
            
            while (!IsDisposed)
            {
                while (Socket != null && Socket.Available > 0)
                {
                    try
                    {
                        var receiveLength = Socket.ReceiveFrom(_rawReceiveBuffer, ref _ipEndPoint);

                        if (receiveLength < 5)
                        {
                            continue;
                        }

                        var channelId = 0U;
                        var header = (KcpHeader)_rawReceiveBuffer[0];

                        switch (header)
                        {
                            case KcpHeader.RequestConnection: // 客户端请求建立KCP连接
                            {
                                if (receiveLength != 5)
                                {
                                    break; // 数据长度异常，忽略处理
                                }

                                channelId = BitConverter.ToUInt32(_rawReceiveBuffer, 1);

                                if (_pendingConnection.TryGetValue(channelId, out var pendingConnection))
                                {
                                    if (!_ipEndPoint.IPEndPointEquals(pendingConnection.RemoteEndPoint))
                                    {
                                        // 重复通道ID，向客户端发送重复通道ID消息
                                        SendToRepeatChannelId(channelId, _ipEndPoint);
                                    }

                                    break;
                                }

                                if (_connectionChannel.ContainsKey(channelId))
                                {
                                    // 已存在的通道ID，向客户端发送重复通道ID消息
                                    SendToRepeatChannelId(channelId, _ipEndPoint);
                                    break;
                                }

                                AddPendingConnection(channelId, _ipEndPoint.Clone());
                                break;
                            }
                            case KcpHeader.ConfirmConnection: // 客户端确认建立KCP连接
                            {
                                if (receiveLength != 5)
                                {
                                    break; // 数据长度异常，忽略处理
                                }

                                channelId = BitConverter.ToUInt32(_rawReceiveBuffer, 1);

                                if (!ConfirmPendingConnection(channelId, _ipEndPoint))
                                {
                                    break;
                                }

                                AddConnection(channelId, _ipEndPoint.Clone());
                                break;
                            }
                            case KcpHeader.ReceiveData: // 接收KCP的数据
                            {
                                var messageLength = receiveLength - 5;
                                
                                if (messageLength <= 0)
                                {
                                    Log.Warning($"KCP Server KcpHeader.Data  messageLength <= 0");
                                    break;
                                }

                                channelId = BitConverter.ToUInt32(_rawReceiveBuffer, 1);

                                if (!_connectionChannel.TryGetValue(channelId, out var channel))
                                {
                                    break;
                                }
                                
                                channel.Input(_rawReceiveBuffer, 5, messageLength);
                                break;
                            }
                            case KcpHeader.Disconnect:
                            {
                                // 断开不需要清楚PendingConnection让ClearPendingConnection自动清楚就可以了，并且不一定有Pending。
                                RemoveConnection(BitConverter.ToUInt32(_rawReceiveBuffer, 1));
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

                try
                {
                    CheckUpdateChannel();
                    ClearPendingConnection();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                
                ThreadSynchronizationContext.Update();
                Thread.Yield();
            }
        }
        
        /// <summary>
        /// 发送指定通道的数据，以重复通道ID的方式。
        /// </summary>
        /// <param name="channelId">通道ID。</param>
        /// <param name="clientEndPoint">客户端终结点。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendToRepeatChannelId(uint channelId, EndPoint clientEndPoint)
        {
            _sendBuff.WriteTo(0, (byte) KcpHeader.RepeatChannelId);
            _sendBuff.WriteTo(1, channelId);
            Socket.SendTo(_sendBuff, 0, 5, SocketFlags.None, clientEndPoint);
        }

        #region Pending

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPendingConnection(uint channelId, IPEndPoint ipEndPoint)
        {
            var now = TimeNow;
            var pendingConnection = new PendingConnection(channelId, ipEndPoint, now);

            if (pendingConnection.TimeOutId < _pendingMinTime || _pendingMinTime == 0)
            {
                _pendingMinTime = pendingConnection.TimeOutId;
            }
            
            _pendingConnection.Add(channelId, pendingConnection);
            _pendingConnectionTimeOut.Add(pendingConnection.TimeOutId, channelId);
            _sendBuff.WriteTo(0, (byte) KcpHeader.WaitConfirmConnection);
            _sendBuff.WriteTo(1, channelId);
            Socket.SendTo(_sendBuff, 0, 5, SocketFlags.None, ipEndPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ConfirmPendingConnection(uint channelId, EndPoint ipEndPoint)
        {
            if (!_pendingConnection.TryGetValue(channelId, out var pendingConnection))
            {
                return false;
            }
            
            if (!ipEndPoint.IPEndPointEquals(pendingConnection.RemoteEndPoint))
            {
                Log.Error($"KCPSocket syn address diff: {channelId} {pendingConnection.RemoteEndPoint} {ipEndPoint}");
                return false;
            }

            _pendingConnection.Remove(channelId);
            _pendingConnectionTimeOut.RemoveValue(pendingConnection.TimeOutId, pendingConnection.ChannelId);
            Log.Debug($"KCPSocket _pendingConnection:{_pendingConnection.Count} _pendingConnectionTimer:{_pendingConnectionTimeOut.Count}");
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearPendingConnection()
        {
            var now = TimeNow;
            
            if (now < _pendingMinTime || _pendingConnection.Count == 0 || _pendingConnectionTimeOut.Count == 0)
            {
                return;
            }

            foreach (var (time, pendingList) in _pendingConnectionTimeOut)
            {
                if (time > now)
                {
                    _pendingMinTime = time;
                    break;
                }
                
                foreach (var channelId in pendingList)
                {
                    _pendingConnection.Remove(channelId);
                }
                
                _pendingTimeOutTime.Enqueue(time);
            }

            while (_pendingTimeOutTime.TryDequeue(out var timeOutId))
            {
                _pendingConnectionTimeOut.RemoveKey(timeOutId);
            }
        }

        #endregion

        #region Connection

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddConnection(uint channelId, IPEndPoint ipEndPoint)
        {
            var eventArgs = new KCPServerSSocketAsyncEventArgs(this, channelId, ipEndPoint);
            _connectionChannel.Add(channelId, eventArgs);
            OnConnectionCompleted?.Invoke(this, eventArgs);
            Log.Debug($"AddConnection _connectionChannel:{_connectionChannel.Count()}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveConnection(uint channelId)
        {
            if (!_connectionChannel.Remove(channelId, out var eventArgs))
            {
                return;
            }
            
            eventArgs.Dispose();
        }
        
        public KCPServerSSocketAsyncEventArgs GetConnection(uint channelId)
        {
            return _connectionChannel.TryGetValue(channelId, out var channel) ? channel : null;
        }

        #endregion

        #region Update

        public void AddUpdateChannel(uint channelId, uint tillTime)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckUpdateChannel()
        {
            try
            {
                var nowTime = TimeNow;
            
                if(nowTime >= _updateMinTime && _updateTimer.Count > 0)
                {
                    foreach (var (timeId,channelList) in _updateTimer)
                    {
                        if (timeId > nowTime)
                        {
                            _updateMinTime = timeId;
                            break;
                        }
                
                        foreach (var channelId in channelList)
                        {
                            _updateChannels.Add(channelId);
                        }
            
                        _updateTimeOutTime.Enqueue(timeId);
                    }
            
                    while (_updateTimeOutTime.TryDequeue(out var time))
                    {
                        _updateTimer.RemoveKey(time);
                    }
                }

                if (_updateChannels.Count == 0)
                {
                    return;
                }
            
                foreach (var channelId in _updateChannels)
                {
                    if (!_connectionChannel.TryGetValue(channelId, out var channel))
                    {
                        continue;
                    }

                    if (channel.IsDisposed)
                    {
                        _connectionChannel.Remove(channelId);
                        continue;
                    }
                
                    channel.Kcp.Update(nowTime);
                    AddUpdateChannel(channelId, channel.Kcp.Check(nowTime));
                }
            
                _updateChannels.Clear();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #endregion
        
        public void Dispose()
        {
            IsDisposed = true;
            _thread.Join();
            Socket.Dispose();
        }
    }
}
#endif
