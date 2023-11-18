using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Fantasy.DataStructure;
using Fantasy.Helper;
using kcp2k;

// ReSharper disable InconsistentNaming
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.Core.Network
{
    /// <summary>
    /// KCP 服务端网络实现。
    /// </summary>
    public class KCPServerNetwork : ANetwork, INetworkUpdate
    {
        #region 逻辑线程
        /// <summary>
        /// 构造函数，初始化 KCP 服务端网络实例。
        /// </summary>
        /// <param name="scene">场景实例。</param>
        /// <param name="networkTarget">网络目标。</param>
        /// <param name="address">绑定的地址和端口。</param>
        public KCPServerNetwork(Scene scene, NetworkTarget networkTarget, IPEndPoint address) : base(scene, NetworkType.Server, NetworkProtocolType.KCP, networkTarget)
        {
            _startTime = TimeHelper.Now;
            NetworkThread.Instance.AddNetwork(this);

            NetworkThread.Instance.SynchronizationContext.Post(() =>
            {
                KcpSettings = KCPSettings.Create(NetworkTarget);
                _rawReceiveBuffer = new byte[KcpSettings.Mtu + 5];
                
                _socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
                
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                }
                
                _socket.Bind(address);
                _socket.SetSocketBufferToOsLimit();
                NetworkHelper.SetSioUdpConnReset(_socket);
            });
        }

        /// <summary>
        /// 释放<see cref="KCPServerNetwork"/>实例使用的所有资源。
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

                _socket = null;
                KcpSettings = null;
                _updateMinTime = 0;
                _pendingMinTime = 0;
                base.Dispose();
            });
        }

        #endregion

        #region 网络主线程

        private Socket _socket;
        private uint _updateMinTime;
        private uint _pendingMinTime;
        private byte[] _rawReceiveBuffer;
        private readonly long _startTime;
        private readonly byte[] _sendBuff = new byte[5];
        private EndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private readonly Queue<uint> _updateTimeOutTime = new Queue<uint>();
        private readonly HashSet<uint> _updateChannels = new HashSet<uint>();
        private readonly Queue<uint> _pendingTimeOutTime = new Queue<uint>();
        private readonly SortedOneToManyHashSet<uint, uint> _updateTimer = new SortedOneToManyHashSet<uint, uint>();
        private readonly SortedOneToManyList<uint, uint> _pendingConnectionTimer = new SortedOneToManyList<uint, uint>();
        private readonly Dictionary<uint, KCPServerNetworkChannel> _pendingConnection = new Dictionary<uint, KCPServerNetworkChannel>();
        private readonly Dictionary<uint, KCPServerNetworkChannel> _connectionChannel = new Dictionary<uint, KCPServerNetworkChannel>();
        private KCPSettings KcpSettings { get; set; }
        private uint TimeNow => (uint) (TimeHelper.Now - _startTime);

        /// <summary>
        /// 向指定通道发送数据，使用KCP协议。
        /// </summary>
        /// <param name="channelId">通道ID。</param>
        /// <param name="rpcId">远程过程调用的ID。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="routeId">路由ID。</param>
        /// <param name="memoryStream">包含要发送数据的<see cref="MemoryStream"/>。</param>
        public override void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (!_connectionChannel.TryGetValue(channelId, out var channel))
            {
                return;
            }

            var sendMemoryStream = Pack(rpcId, routeTypeOpCode, routeId, memoryStream, null);
            channel.Send(sendMemoryStream);
        }

        /// <summary>
        /// 向指定通道发送数据，使用KCP协议。
        /// </summary>
        /// <param name="channelId">通道ID。</param>
        /// <param name="rpcId">远程过程调用的ID。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="routeId">路由ID。</param>
        /// <param name="message">包含要发送数据的对象。</param>
        public override void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, object message)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (!_connectionChannel.TryGetValue(channelId, out var channel))
            {
                return;
            }

            var memoryStream = Pack(rpcId, routeTypeOpCode, routeId, null, message);
            channel.Send(memoryStream);
        }

        /// <summary>
        /// 发送指定通道的数据，以重复通道ID的方式。
        /// </summary>
        /// <param name="channelId">通道ID。</param>
        /// <param name="clientEndPoint">客户端终结点。</param>
        private void SendToRepeatChannelId(uint channelId, EndPoint clientEndPoint)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            _sendBuff.WriteTo(0, (byte) KcpHeader.RepeatChannelId);
            _sendBuff.WriteTo(1, channelId);
            _socket.SendTo(_sendBuff, 0, 5, SocketFlags.None, clientEndPoint);
        }

        /// <summary>
        /// 接收来自客户端的数据。
        /// </summary>
        private void Receive()
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            // 循环接收数据，直到没有数据可用
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
                            var pendingChannel = new KCPServerNetworkChannel(Scene, channelId, Id, _clientEndPoint, _socket, timeNow);
                            
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
                            _connectionChannel.Add(channel.Id, channel);
                            channel.Connect(kcp, AddToUpdate, KcpSettings.MaxSendWindowSize, NetworkTarget, NetworkMessageScheduler);
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
        /// 移除待处理连接。
        /// </summary>
        /// <param name="channelId">通道ID。</param>
        /// <param name="remoteEndPoint">远程终结点。</param>
        /// <param name="channel">待处理的网络通道。</param>
        /// <returns>是否成功移除待处理连接。</returns>
        private bool RemovePendingConnection(uint channelId, EndPoint remoteEndPoint, out KCPServerNetworkChannel channel)
        {
#if FANTASY_DEVELOP
            channel = null;
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return false;
            }
#endif
            if (!_pendingConnection.TryGetValue(channelId, out channel) || channel.IsDisposed)
            {
                return false;
            }

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
        /// 从网络中移除指定通道。
        /// </summary>
        /// <param name="channelId">要移除的通道ID。</param>
        public override void RemoveChannel(uint channelId)
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

        /// <summary>
        /// 更新网络处理逻辑。
        /// </summary>
        public void Update()
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            // 接收来自客户端的数据
            Receive();
            
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

                    if (channelKcp != null)
                    {
                        AddToUpdate(channelKcp.Check(nowTime), channelId);
                    }
                }
            
                _updateChannels.Clear();
            }

            if (_pendingConnection.Count <= 0 || nowTime < _pendingMinTime)
            {
                return;
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
        }

        /// <summary>
        /// 将通道加入到更新列表中。
        /// </summary>
        /// <param name="tillTime">更新时间。</param>
        /// <param name="channelId">通道ID。</param>
        private void AddToUpdate(uint tillTime, uint channelId)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
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