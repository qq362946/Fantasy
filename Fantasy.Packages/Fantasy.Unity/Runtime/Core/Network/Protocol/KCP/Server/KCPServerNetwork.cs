#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Network.Interface;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

namespace Fantasy.Network.KCP
{
    internal sealed class KCPServerNetworkUpdateSystem : UpdateSystem<KCPServerNetwork>
    {
        protected override void Update(KCPServerNetwork self)
        {
            self.Update();
        }
    }

    internal struct PendingConnection(uint channelId, IPEndPoint remoteEndPoint, long time)
    {
        public readonly uint ChannelId = channelId;
        public readonly long TimeOutId = time + 10 * 1000; // 设置10秒超时，如果10秒内没有确认连接则删除。
        public readonly IPEndPoint RemoteEndPoint = remoteEndPoint;
    }

    public sealed class KCPServerNetwork : ANetwork
    {
        private Socket _socket;
        private long _startTime;
        private long _updateMinTime = long.MaxValue;
        private long _pendingMinTime = long.MaxValue;
       
        private EndPoint _receiveAnyEndPoint;
        private EndPoint _receiveRemoteEndPoint;
        private const int MinReceiveCountPerUpdate = 2048;
        private const int MaxReceiveCountPerUpdate = 32768;
        private const int ReceiveCountPerConnection = 12;
        
        private const int ReceiveBufferSize = ushort.MaxValue;
        private readonly byte[] _sendBuff = new byte[5];
        private readonly byte[] _receiveBuffer = new byte[ReceiveBufferSize];
        
        private readonly List<long> _pendingTimeOutTime = new List<long>();
        private readonly HashSet<uint> _updateChannels = new HashSet<uint>();
        private readonly Dictionary<uint, long> _channelUpdateTime = new Dictionary<uint, long>();
        private readonly List<long> _updateTimeOutTime = new List<long>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly SortedOneToManyList<long, uint> _updateTimer = new SortedOneToManyList<long, uint>();

        private readonly Dictionary<uint, PendingConnection> _pendingConnection = new Dictionary<uint, PendingConnection>();
        private readonly SortedOneToManyList<long, uint> _pendingConnectionTimeOut = new SortedOneToManyList<long, uint>();
        private readonly Dictionary<uint, KCPServerNetworkChannel> _connectionChannel = new Dictionary<uint, KCPServerNetworkChannel>();

        public KCPSettings Settings { get; private set; }

        private long TimeNow => Environment.TickCount64 - _startTime;

        public void Initialize(NetworkTarget networkTarget, IPEndPoint address)
        {
            _startTime = Environment.TickCount64;
            Settings = KCPSettings.Create(networkTarget);
            base.Initialize(NetworkType.Server, NetworkProtocolType.KCP, networkTarget, false);
            _socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Blocking = false;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            
            _socket.Bind(address);
            _socket.SetSocketBufferToOsLimit();
            _socket.SetSioUdpConnReset();
            
            _receiveAnyEndPoint = address.AddressFamily == AddressFamily.InterNetworkV6
                ? new IPEndPoint(IPAddress.IPv6Any, 0)
                : new IPEndPoint(IPAddress.Any, 0);
            
            _receiveRemoteEndPoint = _receiveAnyEndPoint;
            
            Log.Info($"SceneConfigId = {Scene.SceneConfigId} networkTarget = {networkTarget.ToString()} KCPServer Listen {address}");
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    _cancellationTokenSource.Cancel();
                }
                catch (OperationCanceledException)
                {
                    // 通常情况下，此处的异常可以忽略
                }
            }

            foreach (var (_, channel) in _connectionChannel.ToArray())
            {
                channel.Dispose();
            }

            _connectionChannel.Clear();
            _pendingConnection.Clear();
            _updateChannels.Clear();
            _channelUpdateTime.Clear();
            _updateTimeOutTime.Clear();
            _pendingTimeOutTime.Clear();
            _updateTimer.Clear();
            _pendingConnectionTimeOut.Clear();

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            base.Dispose();
        }

        #region ReceiveSocket
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetReceiveCountPerUpdate()
        {
            var count = _connectionChannel.Count * ReceiveCountPerConnection;

            if (count < MinReceiveCountPerUpdate)
            {
                return MinReceiveCountPerUpdate;
            }

            if (count > MaxReceiveCountPerUpdate)
            {
                return MaxReceiveCountPerUpdate;
            }

            return count;
        }

        private void ReceiveSocket()
        {
            var maxReceiveCount = GetReceiveCountPerUpdate();

            for (var i = 0; i < maxReceiveCount; i++)
            {
                if (_cancellationTokenSource.IsCancellationRequested || _socket == null)
                {
                    return;
                }

                try
                {
                    if (!_socket.Poll(0, SelectMode.SelectRead))
                    {
                        return;
                    }
                    
                    _receiveRemoteEndPoint = _receiveAnyEndPoint;
                
                    var receivedBytes = _socket.ReceiveFrom(
                        _receiveBuffer,
                        SocketFlags.None,
                        ref _receiveRemoteEndPoint);
                
                    if (receivedBytes < 5)
                    {
                        continue;
                    }
                
                    var memory = _receiveBuffer.AsMemory(0, receivedBytes);
                    var span = memory.Span;
                    
                    var header = (KcpHeader)span[0];
                    var channelId = MemoryMarshal.Read<uint>(span.Slice(1, 4));
                    var payload = memory.Slice(5);
                
                    ReceiveData(header, channelId, payload, (IPEndPoint)_receiveRemoteEndPoint);
                }
                catch (SocketException ex) when (
                    ex.SocketErrorCode == SocketError.WouldBlock ||
                    ex.SocketErrorCode == SocketError.IOPending ||
                    ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException ex)
                {
                    Log.Error($"KCP ReceiveFrom SocketException:{ex.SocketErrorCode} {ex.Message}");
                    Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return;
                }
            }
        }

        #endregion

        #region ReceiveData
        
        private void ReceiveData(KcpHeader header, uint channelId, ReadOnlyMemory<byte> buffer, IPEndPoint ipEndPoint)
        {
            // 已建立连接的数据包和断开包必须来自握手时记录的终端。
            if (header is KcpHeader.ReceiveData or KcpHeader.Disconnect)
            {
                if (!_connectionChannel.TryGetValue(channelId, out var channel) ||
                    !channel.RemoteEndPoint.IPEndPointEquals(ipEndPoint))
                {
                    // 不记录日志，避免伪造数据包造成日志放大。
                    return;
                }
                
                if (header == KcpHeader.Disconnect)
                {
                    RemoveChannel(channelId);
                    return;
                }
                
                if (buffer.Length == 0)
                {
#if FANTASY_DEVELOP
                    Log.Warning("KCP Server ReceiveData buffer.Length == 0");
#endif
                    return;
                }
                
                // 限制进入 KCP 的 UDP 数据长度，防止超大恶意分片占用非托管内存
                if (buffer.Length > Settings.Mtu)
                {
                    RemoveChannel(channelId);
                    return;
                }
                
                channel.Input(buffer);
                return;
            }
            
            switch (header)
            {
                // 客户端请求建立KCP连接
                case KcpHeader.RequestConnection:
                {
                    if (_pendingConnection.TryGetValue(channelId, out var pendingConnection))
                    {
                        if (ipEndPoint.IPEndPointEquals(pendingConnection.RemoteEndPoint))
                        {
                            // 同一客户端重复请求，重新发送待确认握手。
                            SendWaitConfirmConnection(ref channelId, ipEndPoint);
                        }
                        else
                        {
                            // 不同客户端使用了相同的通道ID。
                            SendRepeatChannelId(ref channelId, ipEndPoint);
                        }

                        break;
                    }
                    
                    if (_connectionChannel.TryGetValue(channelId, out var requestChannel))
                    {
                        if (requestChannel.RemoteEndPoint.IPEndPointEquals(ipEndPoint))
                        {
                            // 连接已经建立，重新发送最终确认。
                            SendConfirmConnection(ref channelId, ipEndPoint);
                        }
                        else
                        {
                            SendRepeatChannelId(ref channelId, ipEndPoint);
                        }

                        break;
                    }

                    AddPendingConnection(ref channelId, ipEndPoint);
                    break;
                }
                // 客户端确认建立KCP连接
                case KcpHeader.ConfirmConnection:
                {
                    if (_connectionChannel.TryGetValue(channelId, out var confirmChannel))
                    {
                        if (confirmChannel.RemoteEndPoint.IPEndPointEquals(ipEndPoint))
                        {
                            // 最终确认可能丢失，收到重复确认时再次发送。
                            SendConfirmConnection(ref channelId, ipEndPoint);
                        }

                        break;
                    }
                    
                    if (!ConfirmPendingConnection(ref channelId, ipEndPoint))
                    {
                        break;
                    }

                    AddConnection(ref channelId, ipEndPoint.Clone());
                    SendConfirmConnection(ref channelId, ipEndPoint);
                    break;
                }
            }
        }

        #endregion

        #region Update

        public void Update()
        {
            ReceiveSocket();
            
            var timeNow = TimeNow;
            CheckUpdateTimerOut(timeNow);
            UpdateChannel(timeNow);
            PendingTimerOut(timeNow);
        }

        private void CheckUpdateTimerOut(long nowTime)
        {
            if (_updateTimer.Count == 0)
            {
                return;
            }

            if (_updateMinTime > nowTime)
            {
                return;
            }

            _updateTimeOutTime.Clear();
            _updateMinTime = long.MaxValue;

            foreach (var kv in _updateTimer)
            {
                var timeId = kv.Key;

                if (timeId > nowTime)
                {
                    _updateMinTime = timeId;
                    break;
                }

                _updateTimeOutTime.Add(timeId);
            }

            foreach (var timeId in _updateTimeOutTime)
            {
                foreach (var channelId in _updateTimer[timeId])
                {
                    _channelUpdateTime.Remove(channelId);
                    _updateChannels.Add(channelId);
                }

                _updateTimer.RemoveKey(timeId);
            }
        }

        private void UpdateChannel(long timeNow)
        {
            var kcpNow = unchecked((uint)timeNow);
            
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

                channel.Kcp.Update(kcpNow);
                var delay = unchecked((int)(channel.Kcp.Check(kcpNow) - kcpNow));
                AddUpdateChannel(channelId, timeNow + delay);
            }

            _updateChannels.Clear();
        }

        private void PendingTimerOut(long timeNow)
        {
            if (_pendingConnectionTimeOut.Count == 0)
            {
                return;
            }

            if (_pendingMinTime > timeNow)
            {
                return;
            }

            _pendingTimeOutTime.Clear();
            _pendingMinTime = long.MaxValue;

            foreach (var kv in _pendingConnectionTimeOut)
            {
                var timeId = kv.Key;

                if (timeId > timeNow)
                {
                    _pendingMinTime = timeId;
                    break;
                }

                _pendingTimeOutTime.Add(timeId);
            }

            foreach (var timeId in _pendingTimeOutTime)
            {
                foreach (var channelId in _pendingConnectionTimeOut[timeId])
                {
                    _pendingConnection.Remove(channelId);
                }

                _pendingConnectionTimeOut.RemoveKey(timeId);
            }
        }
        
        public void AddUpdateChannel(uint channelId)
        {
            RemoveUpdateTimer(channelId);
            _updateChannels.Add(channelId);
        }

        public void AddUpdateChannel(uint channelId, long tillTime)
        {
            RemoveUpdateTimer(channelId);
            
            if (tillTime < _updateMinTime)
            {
                _updateMinTime = tillTime;
            }

            _channelUpdateTime[channelId] = tillTime;
            _updateTimer.Add(tillTime, channelId);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveUpdateTimer(uint channelId)
        {
            if (_channelUpdateTime.Remove(channelId, out var updateTime))
            {
                _updateTimer.RemoveValue(updateTime, channelId);
            }
        }

        #endregion

        #region Pending

        private void AddPendingConnection(ref uint channelId, IPEndPoint ipEndPoint)
        {
            var now = TimeNow;
            var pendingConnection = new PendingConnection(channelId, ipEndPoint, now);

            if (pendingConnection.TimeOutId < _pendingMinTime)
            {
                _pendingMinTime = pendingConnection.TimeOutId;
            }

            _pendingConnection.Add(channelId, pendingConnection);
            _pendingConnectionTimeOut.Add(pendingConnection.TimeOutId, channelId);
            SendWaitConfirmConnection(ref channelId, ipEndPoint);
        }

        private bool ConfirmPendingConnection(ref uint channelId, EndPoint ipEndPoint)
        {
            if (!_pendingConnection.TryGetValue(channelId, out var pendingConnection))
            {
                return false;
            }

            if (!ipEndPoint.IPEndPointEquals(pendingConnection.RemoteEndPoint))
            {
                return false;
            }

            _pendingConnection.Remove(channelId);
            _pendingConnectionTimeOut.RemoveValue(pendingConnection.TimeOutId, pendingConnection.ChannelId);
#if FANTASY_DEVELOP
        Log.Debug($"KCPSocket _pendingConnection:{_pendingConnection.Count} _pendingConnectionTimer:{_pendingConnectionTimeOut.Count}");
#endif
            return true;
        }

        #endregion

        #region Connection

        private void AddConnection(ref uint channelId, IPEndPoint ipEndPoint)
        {
            var eventArgs = new KCPServerNetworkChannel(this, channelId, ipEndPoint);
            _connectionChannel.Add(channelId, eventArgs);
#if FANTASY_DEVELOP
        Log.Debug($"AddConnection _connectionChannel:{_connectionChannel.Count}");
#endif
        }

        public override void RemoveChannel(uint channelId)
        {
            _updateChannels.Remove(channelId);
            RemoveUpdateTimer(channelId);
            
            if (!_connectionChannel.Remove(channelId, out var channel))
            {
                return;
            }

            if (!channel.IsDisposed)
            {
                SendDisconnect(ref channelId, channel.RemoteEndPoint);
                channel.Dispose();
            }
#if FANTASY_DEVELOP
        Log.Debug($"RemoveChannel _connectionChannel:{_connectionChannel.Count}");
#endif
        }

        #endregion

        #region Send

        private const byte KcpHeaderDisconnect = (byte)KcpHeader.Disconnect;
        private const byte KcpHeaderRepeatChannelId = (byte)KcpHeader.RepeatChannelId;
        private const byte KcpHeaderWaitConfirmConnection = (byte)KcpHeader.WaitConfirmConnection;
        private const byte KcpHeaderConfirmConnection = (byte)KcpHeader.ConfirmConnection;

        private void SendDisconnect(ref uint channelId, EndPoint clientEndPoint)
        {
            _sendBuff[0] = KcpHeaderDisconnect;
            MemoryMarshal.Write(_sendBuff.AsSpan(1), in channelId);
            SendAsync(_sendBuff, 0, 5, clientEndPoint);
        }

        private void SendRepeatChannelId(ref uint channelId, EndPoint clientEndPoint)
        {
            _sendBuff[0] = KcpHeaderRepeatChannelId;
            MemoryMarshal.Write(_sendBuff.AsSpan(1), in channelId);
            SendAsync(_sendBuff, 0, 5, clientEndPoint);
        }

        private void SendWaitConfirmConnection(ref uint channelId, EndPoint clientEndPoint)
        {
            _sendBuff[0] = KcpHeaderWaitConfirmConnection;
            MemoryMarshal.Write(_sendBuff.AsSpan(1), in channelId);
            SendAsync(_sendBuff, 0, 5, clientEndPoint);
        }
        
        private void SendConfirmConnection(ref uint channelId, EndPoint clientEndPoint)
        {
            _sendBuff[0] = KcpHeaderConfirmConnection;
            MemoryMarshal.Write(_sendBuff.AsSpan(1), in channelId);
            SendAsync(_sendBuff, 0, 5, clientEndPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendAsync(byte[] buffer, int offset, int count, EndPoint endPoint)
        {
            try
            {
                var socket = _socket;

                if (socket == null)
                {
                    return;
                }
                
                socket.SendTo(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None, endPoint);
            }
            catch (SocketException ex) when (
                ex.SocketErrorCode == SocketError.WouldBlock ||
                ex.SocketErrorCode == SocketError.IOPending ||
                ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable ||
                ex.SocketErrorCode == SocketError.Interrupted ||
                ex.SocketErrorCode == SocketError.OperationAborted)
            {
            }
            catch (SocketException ex)
            {
                Log.Error($"KCP SendTo SocketException:{ex.SocketErrorCode} {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        #endregion
    }
}

#endif