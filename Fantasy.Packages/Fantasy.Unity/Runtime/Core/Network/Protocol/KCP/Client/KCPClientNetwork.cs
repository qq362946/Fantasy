#if !FANTASY_WEBGL
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using Fantasy.Serialize;
using KCP;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable PossibleNullReferenceException
// ReSharper disable InconsistentNaming
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace Fantasy.Network.KCP
{
    internal sealed class KCPClientNetworkUpdateSystem : UpdateSystem<KCPClientNetwork>
    {
        protected override void Update(KCPClientNetwork self)
        {
            self.CheckUpdate();
        }
    }
    internal sealed class KCPClientNetwork : AClientNetwork
    {
        private Kcp _kcp;
        private Socket _socket;
        private int _maxSndWnd;
        private int _packetHeadLength;
        private long _startTime;
        private bool _isConnected;
        private bool _isDisconnect;
        private bool _receiveStarted;
        private uint _updateMinTime;
        private bool _isInnerDispose;
        private long _connectTimeoutId;
        private bool _allowWraparound = true;
        private bool _connectDisconnectEvent = true;
        private IPEndPoint _remoteAddress;
        private BufferPacketParser _packetParser;
        
        private const int MaxReceiveCountPerUpdate = 2048;
        private readonly byte[] _sendBuff = new byte[5];
        private readonly byte[] _receiveBuffer = new byte[ProgramDefine.MaxMessageSize + 20];
        private readonly List<uint> _updateTimeOutTime = new List<uint>();
        private readonly SortedSet<uint> _updateTimer = new SortedSet<uint>();
        private readonly SocketAsyncEventArgs _connectEventArgs = new SocketAsyncEventArgs();
        private readonly Queue<MemoryStreamBuffer> _messageCache = new Queue<MemoryStreamBuffer>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private event Action OnConnectFail;
        private event Action OnConnectComplete;
        private event Action OnConnectDisconnect;
        
        private static void InvokeSafely(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        public uint ChannelId { get; private set; }
        private uint TimeNow => (uint) (TimeHelper.Now - _startTime);

        public void Initialize(NetworkTarget networkTarget, bool enableReceiveMessageJsonLog)
        {
            base.Initialize(NetworkType.Client, NetworkProtocolType.KCP, networkTarget, enableReceiveMessageJsonLog);
            _packetParser = PacketParserFactory.CreateBufferPacketParser(this);
#if FANTASY_NET
            _packetHeadLength = this.NetworkTarget == NetworkTarget.Inner
                ? Packet.InnerPacketHeadLength
                : Packet.OuterPacketHeadLength;
#else
            _packetHeadLength = Packet.OuterPacketHeadLength;
#endif
        }

        public override void Dispose()
        {
            if (IsDisposed || _isInnerDispose)
            {
                return;
            }

            try
            {
                _isInnerDispose = true;

                if (!_isDisconnect)
                {
                    SendDisconnect();
                }

                ClearConnectTimeout();

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

                if (_connectDisconnectEvent)
                {
                    InvokeSafely(OnConnectDisconnect);
                }

                if (_kcp != null)
                {
                    _kcp.Dispose();
                    _kcp = null;
                }
                
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }

                _packetParser?.Dispose();
                ChannelId = 0;
                _isConnected = false;
                _receiveStarted = false;
                _connectDisconnectEvent = true;
                
                while (_messageCache.TryDequeue(out var memoryStream))
                {
                    if (MemoryStreamBufferSource.Return.HasFlag(memoryStream.MemoryStreamBufferSource))
                    {
                        MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                base.Dispose();
            }
        }

        #region Connect

        public override Session Connect(string remoteAddress, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, bool isHttps, int connectTimeout = 5000)
        {
            if (IsInit)
            {
                throw new NotSupportedException($"KCPClientNetwork Has already been initialized. If you want to call Connect again, please re instantiate it.");
            }
            
            IsInit = true;
            _startTime = TimeHelper.Now;
            ChannelId = CreateChannelId();
            _remoteAddress = NetworkHelper.GetIPEndPoint(remoteAddress);
            OnConnectFail = onConnectFail;
            OnConnectComplete = onConnectComplete;
            OnConnectDisconnect = onConnectDisconnect;
            _connectEventArgs.Completed += OnConnectSocketCompleted;
            _connectTimeoutId = Scene.TimerComponent.Net.OnceTimer(connectTimeout, () =>
            {
                _connectDisconnectEvent = false;
                InvokeSafely(OnConnectFail);
                Dispose();
            });
            _connectEventArgs.RemoteEndPoint = _remoteAddress;
            _socket = new Socket(_remoteAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Blocking = false;
            _socket.SetSocketBufferToOsLimit();
            _socket.SetSioUdpConnReset();
            _socket.Bind(new IPEndPoint(_remoteAddress.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0));
            _kcp = KCPFactory.Create(NetworkTarget, ChannelId, KcpSpanCallback, out var kcpSettings);
            _maxSndWnd = kcpSettings.MaxSendWindowSize;
            
            if (!_socket.ConnectAsync(_connectEventArgs))
            {
                if (_connectEventArgs.SocketError != SocketError.Success)
                {
                    _connectDisconnectEvent = false;
                    InvokeSafely(OnConnectFail);
                    Dispose();
                    return null;
                }
                
                OnReceiveSocketComplete();
            }
            
#if FANTASY_UNITY || FANTASY_CONSOLE
            Session = EnableMessageJsonLog
                ? Session.CreateDebugClientSession(this, _remoteAddress)
                : Session.Create(this, _remoteAddress);
#else
            Session = Session.Create(this, _remoteAddress);
#endif
            return Session;
        }

        private void OnConnectSocketCompleted(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (asyncEventArgs.LastOperation == SocketAsyncOperation.Connect)
            {
                if (asyncEventArgs.SocketError == SocketError.Success)
                {
                    Scene.ThreadSynchronizationContext.Post(OnReceiveSocketComplete);
                }
                else
                {
                    Scene.ThreadSynchronizationContext.Post(() =>
                    {
                        _connectDisconnectEvent = false;
                        InvokeSafely(OnConnectFail);
                        Dispose();
                    });
                }
            }
        }

        private void OnReceiveSocketComplete()
        {
            _receiveStarted = true;
            SendRequestConnection();
        }

        #endregion

        #region ReceiveSocket

        private void ReceiveSocket()
        {
            if (!_receiveStarted || _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            
            var socket = _socket;

            if (socket == null)
            {
                return;
            }

            for (var i = 0; i < MaxReceiveCountPerUpdate; i++)
            {
                try
                {
                    if (socket.Available <= 0)
                    {
                        return;
                    }
                    
                    var receivedBytes = socket.Receive(_receiveBuffer, SocketFlags.None);

                    if (receivedBytes < 5)
                    {
                        continue;
                    }
                    
                    var memory = _receiveBuffer.AsMemory(0, receivedBytes);
                    var span = memory.Span;
                    var header = (KcpHeader)span[0];
                    var channelId = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(1, 4));
                    var payload = memory.Slice(5);
                    
                    ReceiveData(header, channelId, payload);
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
                catch (SocketException)
                {
                    if (!_isConnected)
                    {
                        _connectDisconnectEvent = false;
                        InvokeSafely(OnConnectFail);
                    }
                    
                    Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }
        
        #endregion

        #region ReceiveData

        private void ReceiveData(KcpHeader header, uint channelId, ReadOnlyMemory<byte> buffer)
        {
            if (header == KcpHeader.ReceiveData)
            {
                if (channelId != ChannelId)
                {
                    return;
                }
                
                if (buffer.Length == 0 || (uint)buffer.Length > _kcp.MaximumTransmissionUnit)
                {
                    Dispose();
                    return;
                }
                    
                Input(buffer);
                return;
            }
            
            switch (header)
            {
                // 发送握手给服务器
                case KcpHeader.RepeatChannelId:
                {
                    // 到这里是客户端的channelId再服务器上已经存在、需要重新生成一个再次尝试连接
                    if (IsDisposed || _cancellationTokenSource.IsCancellationRequested || _isConnected || channelId != ChannelId)
                    {
                        break;
                    }
                    
                    var newChannelId = CreateChannelId();
                    var newKcp = KCPFactory.Create(
                        NetworkTarget,
                        newChannelId,
                        KcpSpanCallback,
                        out var kcpSettings);
                    
                    _kcp.Dispose();
                    _kcp = newKcp;
                    ChannelId = newChannelId;
                    _maxSndWnd = kcpSettings.MaxSendWindowSize;
                    
                    SendRequestConnection();
                    break;
                }
                // 收到服务器发送会来的确认握手
                case KcpHeader.WaitConfirmConnection:
                {
                    if (channelId != ChannelId || _isConnected)
                    {
                        break;
                    }
                    
                    ClearConnectTimeout();
                    SendConfirmConnection();
                    InvokeSafely(OnConnectComplete);
                    
                    if (IsDisposed || _cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    _isConnected = true;
                    while (_messageCache.TryDequeue(out var memoryStream))
                    {
                        SendMemoryStream(memoryStream);
                    }
                    break;
                }
                // 接收到服务器的断开连接消息
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

        private void Input(ReadOnlyMemory<byte> buffer)
        {
            if (_kcp.Input(buffer.Span) < 0)
            {
                Dispose();
                return;
            }
            
            AddToUpdate(0);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var peekSize = _kcp.PeekSize();

                    if (peekSize < 0)
                    {
                        return;
                    }
                    
                    if (peekSize < _packetHeadLength ||
                        peekSize > _receiveBuffer.Length ||
                        peekSize - _packetHeadLength > ProgramDefine.MaxMessageSize)
                    {
                        Dispose();
                        return;
                    }

                    var receiveCount = _kcp.Receive(_receiveBuffer.AsSpan(0, peekSize));

                    if (receiveCount != peekSize || 
                        !_packetParser.UnPack(_receiveBuffer, ref receiveCount, out var packInfo))
                    {
                        Dispose();
                        return;
                    }
                    
                    Session.Receive(packInfo);
                }
                catch (ScanException e)
                {
                    Log.Debug($"RemoteAddress:{_remoteAddress} \n{e}");
                    Dispose();
                    return;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Dispose();
                    return;
                }
            }
        }

        #endregion
        
        #region Update
        
        public void CheckUpdate()
        {
            ReceiveSocket();
            
            var nowTime = TimeNow;
            _allowWraparound = nowTime < _updateMinTime;

            if (IsTimeGreaterThan(nowTime, _updateMinTime) && _updateTimer.Count > 0)
            {
                foreach (var timeId in _updateTimer)
                {
                    if (IsTimeGreaterThan(timeId, nowTime))
                    {
                        _updateMinTime = timeId;
                        break;
                    }

                    _updateTimeOutTime.Add(timeId);
                }

                foreach (var timeId in _updateTimeOutTime)
                {
                    _updateTimer.Remove(timeId);
                    KcpUpdate();
                }
                
                _updateTimeOutTime.Clear();
            }

            _allowWraparound = true;
        }
        
        private void AddToUpdate(uint tillTime)
        {
            if (tillTime == 0)
            {
                KcpUpdate();
                return;
            }

            if (IsTimeGreaterThan(_updateMinTime, tillTime) || _updateMinTime == 0)
            {
                _updateMinTime = tillTime;
            }

            _updateTimer.Add(tillTime);
        }
        
        private void KcpUpdate()
        {
            if (_kcp == null)
            {
                return;
            }
            
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

        #region Send
        
        private const byte KcpHeaderDisconnect = (byte)KcpHeader.Disconnect;
        private const byte KcpHeaderReceiveData = (byte)KcpHeader.ReceiveData;
        private const byte KcpHeaderRequestConnection = (byte)KcpHeader.RequestConnection;
        private const byte KcpHeaderConfirmConnection = (byte)KcpHeader.ConfirmConnection;

        public override void Send(uint rpcId, long address, MemoryStreamBuffer memoryStream, IMessage message, Type messageType)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            var buffer = _packetParser.Pack(ref rpcId, ref address, memoryStream, message, messageType);

            if (!_isConnected)
            {
                _messageCache.Enqueue(buffer);
                return;
            }

            SendMemoryStream(buffer);
        }

        private void SendMemoryStream(MemoryStreamBuffer memoryStream)
        {
            if (_kcp.WaitSendCount > _maxSndWnd)
            {
                // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
                Log.Warning($"ERR_KcpWaitSendSizeTooLarge {_kcp.WaitSendCount} > {_maxSndWnd}");
                
                if (MemoryStreamBufferSource.Return.HasFlag(memoryStream.MemoryStreamBufferSource))
                {
                    MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
                }
                
                Dispose();
                return;
            }
            
            var packetLength = (int)memoryStream.Position;
            var sendCount = -1;

            try
            {
                sendCount = _kcp.Send(memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Position));
                
                if (sendCount == packetLength)
                {
                    AddToUpdate(0);
                }
            }
            finally
            {
                if (MemoryStreamBufferSource.Return.HasFlag(memoryStream.MemoryStreamBufferSource))
                {
                    MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
                }
            }
            
            if (sendCount != packetLength)
            {
                Log.Error($"ERR_KcpSendFailed {sendCount} != {packetLength} RemoteAddress:{_remoteAddress}");
                Dispose();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSendHeader(byte header)
        {
            _sendBuff[0] = header;
            BinaryPrimitives.WriteUInt32LittleEndian(_sendBuff.AsSpan(1, 4), ChannelId);
        }

        private void SendRequestConnection()
        {
            try
            {
                WriteSendHeader(KcpHeaderRequestConnection);
                SendAsync(_sendBuff, 0, 5);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void SendConfirmConnection()
        {
            try
            {
                WriteSendHeader(KcpHeaderConfirmConnection);
                SendAsync(_sendBuff, 0, 5);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        private void SendDisconnect()
        {
            try
            {
                WriteSendHeader(KcpHeaderDisconnect);
                SendAsync(_sendBuff, 0, 5);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                var socket = _socket;

                if (socket == null)
                {
                    return;
                }
                
                socket.Send(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
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
        
        private void KcpSpanCallback(byte[] buffer, int count)
        {
            if (IsDisposed)
            {
                return;
            }

            if (count == 0)
            {
                throw new Exception("KcpOutput count 0");
            }
            
            buffer[0] = KcpHeaderReceiveData;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(1, 4), ChannelId);
            
            SendAsync(buffer, 0, count + 5);
        }

        #endregion
        
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

            Scene?.TimerComponent?.Net.Remove(ref _connectTimeoutId);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint CreateChannelId()
        {
            Span<byte> bytes = stackalloc byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            return 0xC0000000u | (value & 0x7FFFFFFFu);
        }
    }
}
#endif