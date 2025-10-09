#if FANTASY_NET
using System.Buffers;
using System.IO.Pipelines;
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

    public sealed class KCPServerNetwork : ANetwork
    {
        private Socket _socket;
        private long _startTime;
        private uint _updateMinTime;
        private uint _pendingMinTime;
        private bool _allowWraparound = true;
        private readonly Pipe _pipe = new Pipe();
        private readonly byte[] _sendBuff = new byte[5];
        private readonly List<uint> _pendingTimeOutTime = new List<uint>();
        private readonly HashSet<uint> _updateChannels = new HashSet<uint>();
        private readonly List<uint> _updateTimeOutTime = new List<uint>();
        private readonly Queue<IPEndPoint> _endPoint = new Queue<IPEndPoint>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly SortedOneToManyList<uint, uint> _updateTimer = new SortedOneToManyList<uint, uint>();

        private readonly Dictionary<uint, PendingConnection> _pendingConnection = new Dictionary<uint, PendingConnection>();
        private readonly SortedOneToManyList<uint, uint> _pendingConnectionTimeOut = new SortedOneToManyList<uint, uint>();
        private readonly Dictionary<uint, KCPServerNetworkChannel> _connectionChannel = new Dictionary<uint, KCPServerNetworkChannel>();

        public KCPSettings Settings { get; private set; }

        private uint TimeNow => (uint)(TimeHelper.Now - _startTime);

        public void Initialize(NetworkTarget networkTarget, IPEndPoint address)
        {
            _startTime = TimeHelper.Now;
            Settings = KCPSettings.Create(networkTarget);
            base.Initialize(NetworkType.Server, NetworkProtocolType.KCP, networkTarget);
            _socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Blocking = false;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }

            _socket.Blocking = false;
            _socket.Bind(address);
            _socket.SetSocketBufferToOsLimit();
            _socket.SetSioUdpConnReset();
            ReadPipeDataAsync().Coroutine();
            ReceiveSocketAsync().Coroutine();
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

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            base.Dispose();
        }

        #region ReceiveSocket

        private async FTask ReceiveSocketAsync()
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var memory = _pipe.Writer.GetMemory(8192);
                    var socketReceiveFromResult = await _socket.ReceiveFromAsync(memory, SocketFlags.None, remoteEndPoint, _cancellationTokenSource.Token);
                    var receivedBytes = socketReceiveFromResult.ReceivedBytes;
                    
                    if (receivedBytes == 5)
                    {
                        switch ((KcpHeader)memory.Span[0])
                        {
                            case KcpHeader.RequestConnection:
                            case KcpHeader.ConfirmConnection:
                            {
                                _endPoint.Enqueue(socketReceiveFromResult.RemoteEndPoint.Clone());
                                break;
                            }
                        }
                    }

                    _pipe.Writer.Advance(receivedBytes);
                    await _pipe.Writer.FlushAsync();
                }
                catch (SocketException ex)
                {
                    Log.Error($"Socket exception: {ex.Message}");
                    Dispose();
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error($"Unexpected exception: {ex.Message}");
                }
            }

            await _pipe.Writer.CompleteAsync();
        }

        #endregion

        #region ReceivePipeData

        private async FTask ReadPipeDataAsync()
        {
            var pipeReader = _pipe.Reader;
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                ReadResult result = default;

                try
                {
                    result = await pipeReader.ReadAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // 出现这个异常表示取消了_cancellationTokenSource。一般Channel断开会取消。
                    break;
                }

                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End;

                while (TryReadMessage(ref buffer, out var header, out var channelId, out var message))
                {
                    ReceiveData(ref header, ref channelId, ref message);
                    consumed = buffer.Start;
                }

                if (result.IsCompleted)
                {
                    break;
                }

                pipeReader.AdvanceTo(consumed, examined);
            }

            await pipeReader.CompleteAsync();
        }

        private unsafe bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out KcpHeader header, out uint channelId, out ReadOnlyMemory<byte> message)
        {
            if (buffer.Length < 5)
            {
                channelId = 0;
                message = default;
                header = KcpHeader.None;
                if (buffer.Length > 0)
                {
                    buffer = buffer.Slice(buffer.Length);
                }
                return false;
            }

            var readOnlyMemory = buffer.First;

            if (MemoryMarshal.TryGetArray(readOnlyMemory, out var arraySegment))
            {
                fixed (byte* bytePointer = &arraySegment.Array[arraySegment.Offset])
                {
                    header = (KcpHeader)bytePointer[0];
                    channelId = Unsafe.ReadUnaligned<uint>(ref bytePointer[1]);
                }
            }
            else
            {
                // 如果无法获取数组段，回退到安全代码来执行。这种情况几乎不会发生、为了保险还是写一下了。
                var firstSpan = readOnlyMemory.Span;
                header = (KcpHeader)firstSpan[0];
                channelId = MemoryMarshal.Read<uint>(firstSpan.Slice(1, 4));
            }
            
            message = readOnlyMemory.Slice(5);
            buffer = buffer.Slice(readOnlyMemory.Length);
            return true;
        }

        private void ReceiveData(ref KcpHeader header, ref uint channelId, ref ReadOnlyMemory<byte> buffer)
        {
            switch (header)
            {
                // 客户端请求建立KCP连接
                case KcpHeader.RequestConnection:
                {
                    _endPoint.TryDequeue(out var ipEndPoint);

                    if (_pendingConnection.TryGetValue(channelId, out var pendingConnection))
                    {
                        if (!ipEndPoint.IPEndPointEquals(pendingConnection.RemoteEndPoint))
                        {
                            // 重复通道ID，向客户端发送重复通道ID消息
                            SendRepeatChannelId(ref channelId, ipEndPoint);
                        }

                        break;
                    }

                    if (_connectionChannel.ContainsKey(channelId))
                    {
                        // 已存在的通道ID，向客户端发送重复通道ID消息
                        SendRepeatChannelId(ref channelId, ipEndPoint);
                        break;
                    }

                    AddPendingConnection(ref channelId, ipEndPoint);
                    break;
                }
                // 客户端确认建立KCP连接
                case KcpHeader.ConfirmConnection:
                {
                    _endPoint.TryDequeue(out var ipEndPoint);
                    if (!ConfirmPendingConnection(ref channelId, ipEndPoint))
                    {
                        break;
                    }

                    AddConnection(ref channelId, ipEndPoint.Clone());
                    break;
                }
                // 接收KCP的数据
                case KcpHeader.ReceiveData:
                {
                    if (buffer.Length == 5)
                    {
                        Log.Warning($"KCP Server KcpHeader.Data  buffer.Length == 5");
                        break;
                    }

                    if (_connectionChannel.TryGetValue(channelId, out var channel))
                    {
                        channel.Input(buffer);
                    }

                    break;
                }
                // 断开KCP连接
                case KcpHeader.Disconnect:
                {
                    // 断开不需要清楚PendingConnection让ClearPendingConnection自动清楚就可以了，并且不一定有Pending。
                    RemoveChannel(channelId);
                    break;
                }
            }
        }

        #endregion

        #region Update

        public void Update()
        {
            var timeNow = TimeNow;
            _allowWraparound = timeNow < _updateMinTime;
            CheckUpdateTimerOut(ref timeNow);
            UpdateChannel(ref timeNow);
            PendingTimerOut(ref timeNow);
            _allowWraparound = true;
        }

        private void CheckUpdateTimerOut(ref uint nowTime)
        {
            if (_updateTimer.Count == 0)
            {
                return;
            }

            if (IsTimeGreaterThan(_updateMinTime, nowTime))
            {
                return;
            }

            _updateTimeOutTime.Clear();

            foreach (var kv in _updateTimer)
            {
                var timeId = kv.Key;

                if (IsTimeGreaterThan(timeId, nowTime))
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
                    _updateChannels.Add(channelId);
                }

                _updateTimer.RemoveKey(timeId);
            }
        }

        private void UpdateChannel(ref uint timeNow)
        {
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

                channel.Kcp.Update(timeNow);
                AddUpdateChannel(channelId, channel.Kcp.Check(timeNow));
            }

            _updateChannels.Clear();
        }

        private void PendingTimerOut(ref uint timeNow)
        {
            if (_pendingConnectionTimeOut.Count == 0)
            {
                return;
            }

            if (IsTimeGreaterThan(_pendingMinTime, timeNow))
            {
                return;
            }

            _pendingTimeOutTime.Clear();

            foreach (var kv in _pendingConnectionTimeOut)
            {
                var timeId = kv.Key;

                if (IsTimeGreaterThan(timeId, timeNow))
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

        public void AddUpdateChannel(uint channelId, uint tillTime)
        {
            if (tillTime == 0)
            {
                _updateChannels.Add(channelId);
                return;
            }

            if (IsTimeGreaterThan(_updateMinTime, tillTime))
            {
                _updateMinTime = tillTime;
            }

            _updateTimer.Add(tillTime, channelId);
        }

        private const uint HalfMaxUint = uint.MaxValue / 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsTimeGreaterThan(uint timeId, uint nowTime)
        {
            if (!_allowWraparound)
            {
                return timeId > nowTime;
            }

            var diff = timeId - nowTime;
            // 如果 diff 的值在 [0, HalfMaxUint] 范围内，说明 timeId 是在 nowTime 之后或相等。
            // 如果 diff 的值在 (HalfMaxUint, uint.MaxValue] 范围内，说明 timeId 是在 nowTime 之前（时间回绕的情况）。
            return diff < HalfMaxUint || diff == HalfMaxUint;
        }

        #endregion

        #region Pending

        private void AddPendingConnection(ref uint channelId, IPEndPoint ipEndPoint)
        {
            var now = TimeNow;
            var pendingConnection = new PendingConnection(channelId, ipEndPoint, now);

            if (IsTimeGreaterThan(_pendingMinTime, pendingConnection.TimeOutId) || _pendingMinTime == 0)
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
                Log.Error($"KCPSocket syn address diff: {channelId} {pendingConnection.RemoteEndPoint} {ipEndPoint}");
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
        Log.Debug($"AddConnection _connectionChannel:{_connectionChannel.Count()}");
#endif
        }

        public override void RemoveChannel(uint channelId)
        {
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
        Log.Debug($"RemoveChannel _connectionChannel:{_connectionChannel.Count()}");
#endif
        }

        #endregion

        #region Send

        private const byte KcpHeaderDisconnect = (byte)KcpHeader.Disconnect;
        private const byte KcpHeaderRepeatChannelId = (byte)KcpHeader.RepeatChannelId;
        private const byte KcpHeaderWaitConfirmConnection = (byte)KcpHeader.WaitConfirmConnection;

        private unsafe void SendDisconnect(ref uint channelId, EndPoint clientEndPoint)
        {
            fixed (byte* p = _sendBuff)
            {
                p[0] = KcpHeaderDisconnect;
                *(uint*)(p + 1) = channelId;
            }
            
            SendAsync(_sendBuff, 0, 5, clientEndPoint);
        }

        private unsafe void SendRepeatChannelId(ref uint channelId, EndPoint clientEndPoint)
        {
            fixed (byte* p = _sendBuff)
            {
                p[0] = KcpHeaderRepeatChannelId;
                *(uint*)(p + 1) = channelId;
            }
            
            SendAsync(_sendBuff, 0, 5, clientEndPoint);
        }

        private unsafe void SendWaitConfirmConnection(ref uint channelId, EndPoint clientEndPoint)
        {
            fixed (byte* p = _sendBuff)
            {
                p[0] = KcpHeaderWaitConfirmConnection;
                *(uint*)(p + 1) = channelId;
            }
            
            SendAsync(_sendBuff, 0, 5, clientEndPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendAsync(byte[] buffer, int offset, int count, EndPoint endPoint)
        {
            try
            {
                _socket.SendTo(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None, endPoint);
            }
            catch (ArgumentException ex)
            {
                Log.Error($"ArgumentException: {ex.Message}"); // 处理参数错误
            }
            catch (SocketException)
            {
                //Log.Error($"SocketException: {ex.Message}"); // 处理网络错误
            }
            catch (ObjectDisposedException)
            {
                // 处理套接字已关闭的情况
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"InvalidOperationException: {ex.Message}"); // 处理无效操作
            }
            catch (Exception ex)
            {
                Log.Error($"Exception: {ex.Message}"); // 捕获其他异常
            }
        }

        #endregion
    }
}

#endif