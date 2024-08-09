#if !FANTASY_WEBGL && FANTASY_KCPUNSAFE
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using KCP;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable PossibleNullReferenceException
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace Fantasy
{
    public sealed class KCPClientNetworkUpdateSystem : UpdateSystem<KCPClientNetwork>
    {
        protected override void Update(KCPClientNetwork self)
        {
            self.CheckUpdate();
        }
    }
    public sealed class KCPClientNetwork : AClientNetwork
    {
        private Kcp _kcp;
        private Socket _socket;
        private int _maxSndWnd;
        private long _startTime;
        private uint _channelId;
        private bool _isConnected;
        private bool _isDisconnect;
        private long _updateMinTime;
        private bool _isInnerDispose;
        private long _connectTimeoutId;
        private IPEndPoint _remoteAddress;
        private BufferPacketParser _packetParser;
        private readonly Pipe _pipe = new Pipe();
        private readonly byte[] _sendBuff = new byte[5];
        private readonly byte[] _receiveBuffer = new byte[Packet.PacketBodyMaxLength + 20];
        private readonly byte[] _channelIdBytes = new byte[sizeof(uint)];
        private readonly Queue<uint> _updateTimeOutTime = new Queue<uint>();
        private readonly SortedSet<uint> _updateTimer = new SortedSet<uint>();
        private readonly Queue<MemoryStream> _messageCache = new Queue<MemoryStream>();
        private readonly SocketAsyncEventArgs _connectEventArgs = new SocketAsyncEventArgs();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
#if FANTASY_UNITY
        private readonly EndPoint _ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
#endif
        private event Action OnConnectFail;
        private event Action OnConnectComplete;
        private event Action OnConnectDisconnect;
        
        public uint ChannelId 
        {
            get => _channelId;
            private set
            {
                _channelId = value;
                _channelId.GetBytes(_channelIdBytes);
            }
        }
        
        private uint TimeNow => (uint) (TimeHelper.Now - _startTime);
        
        public void Initialize(NetworkTarget networkTarget)
        {
            base.Initialize(NetworkType.Client, NetworkProtocolType.KCP, networkTarget);
            _packetParser = PacketParserFactory.CreateClientBufferPacket(this);
        }
        
        public override void Dispose()
        {
            if (IsDisposed || _isInnerDispose)
            {
                return;
            }
            
            _isInnerDispose = true;
            
            if (!_isDisconnect)
            {
                SendDisconnect();
            }
            
            base.Dispose();
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
            
            OnConnectDisconnect?.Invoke();
            _kcp.Dispose();
            
            if (_socket.Connected)
            {
                _socket.Close();
            }
            
            _packetParser.Dispose();
            ChannelId = 0;
            _isConnected = false;
            _messageCache.Clear();
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
            _connectTimeoutId = Scene.TimerComponent.Net.OnceTimer(connectTimeout, () => { OnConnectFail?.Invoke(); });
            _connectEventArgs.RemoteEndPoint = _remoteAddress;
            _socket = new Socket(_remoteAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Blocking = false;
            _socket.SetSocketBufferToOsLimit();
            _socket.SetSioUdpConnReset();
            _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            _kcp = KCPFactory.Create(NetworkTarget, ChannelId, KcpSpanCallback, out var kcpSettings);
            _maxSndWnd = kcpSettings.MaxSendWindowSize;
            
            if (!_socket.ConnectAsync(_connectEventArgs))
            {
                try
                {
                    OnReceiveSocketComplete();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    OnConnectFail?.Invoke();
                }
            }
            
            Session = Session.Create(this, _remoteAddress);
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
                        OnConnectFail?.Invoke();
                        Dispose();
                    });
                }
            }
        }

        private void OnReceiveSocketComplete()
        {
            SendRequestConnection();
            ReadPipeDataAsync().Coroutine();
            ReceiveSocketAsync().Coroutine();
        }

        #endregion

        #region ReceiveSocket

        private async FTask ReceiveSocketAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var memory = _pipe.Writer.GetMemory(8192);
#if FANTASY_UNITY
                    MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> arraySegment);
                    var result = await _socket.ReceiveFromAsync(arraySegment, SocketFlags.None, _ipEndPoint);
                    _pipe.Writer.Advance(result.ReceivedBytes);
                    await _pipe.Writer.FlushAsync();
#else
                    var result = await _socket.ReceiveAsync(memory, SocketFlags.None, _cancellationTokenSource.Token);
                    _pipe.Writer.Advance(result);
                    await _pipe.Writer.FlushAsync();
#endif
                }
                catch (SocketException)
                {
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
                    channelId = (uint)(bytePointer[1] | (bytePointer[2] << 8) | (bytePointer[3] << 16) | (bytePointer[4] << 24));
                    message = readOnlyMemory.Slice(5);
                }
            }
            else
            {
                // 如果无法获取数组段，回退到安全代码来执行。这种情况几乎不会发生、为了保险还是写一下了。
                var firstSpan = readOnlyMemory.Span;
                header = (KcpHeader)firstSpan[0];
                channelId = BitConverter.ToUInt32(firstSpan.Slice(1, 4));
                message = readOnlyMemory.Slice(5);
            }
        
            buffer = buffer.Slice(readOnlyMemory.Length);
            return true;
        }

        private void ReceiveData(ref KcpHeader header, ref uint channelId, ref ReadOnlyMemory<byte> buffer)
        {
            switch (header)
            {
                // 发送握手给服务器
                case KcpHeader.RepeatChannelId:
                {
                    // 到这里是客户端的channelId再服务器上已经存在、需要重新生成一个再次尝试连接
                    ChannelId = CreateChannelId();
                    SendRequestConnection();
                    break;
                }
                // 收到服务器发送会来的确认握手
                case KcpHeader.WaitConfirmConnection:
                {
                    if (channelId != ChannelId)
                    {
                        break;
                    }
                    
                    ClearConnectTimeout();
                    SendConfirmConnection();
                    OnConnectComplete?.Invoke();
                    _isConnected = true;
                    while (_messageCache.TryDequeue(out var memoryStream))
                    {
                        SendMemoryStream(memoryStream);
                    }
                    break;
                }
                // 收到服务器发送的消息
                case KcpHeader.ReceiveData:
                {
                    if (buffer.Length == 5)
                    {
                        Log.Warning($"KCP Server KcpHeader.Data  buffer.Length == 5");
                        break;
                    }
                    
                    if (channelId != ChannelId)
                    {
                        break;
                    }
                    
                    Input(buffer);
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
            _kcp.Input(buffer);
            AddToUpdate(0);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var peekSize = _kcp.PeekSize();

                    switch (peekSize)
                    {
                        case < 0:
                        {
                            return;
                        }
                        case 0:
                        {
                            Log.Error($"SocketError.NetworkReset peekSize:{peekSize}");
                            return;
                        }
                    }

                    var receiveCount = _kcp.Receive(_receiveBuffer, peekSize);

                    if (receiveCount != peekSize)
                    {
                        Log.Error($"receiveCount != peekSize receiveCount:{receiveCount} peekSize:{peekSize}");
                        return;
                    }
                    
                    if (!_packetParser.UnPack(_receiveBuffer, ref receiveCount, out var packInfo))
                    {
                        continue;
                    }
                    
                    Session.Receive(packInfo);
                }
                catch (ScanException e)
                {
                    Log.Debug($"RemoteAddress:{_remoteAddress} \n{e}");
                    Dispose();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        #endregion
        
        #region Update
        
        public void CheckUpdate()
        {
            var nowTime = TimeNow;

            try
            {
                if (nowTime >= _updateMinTime && _updateTimer.Count > 0)
                {
                    foreach (var timeId in _updateTimer)
                    {
                        if (timeId > nowTime)
                        {
                            _updateMinTime = timeId;
                            break;
                        }

                        _updateTimeOutTime.Enqueue(timeId);
                    }

                    while (_updateTimeOutTime.TryDequeue(out var timeId))
                    {
                        KcpUpdate();
                        _updateTimer.Remove(timeId);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
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

        #endregion

        #region Send
        
        private const byte KcpHeaderDisconnect = (byte)KcpHeader.Disconnect;
        private const byte KcpHeaderReceiveData = (byte)KcpHeader.ReceiveData;
        private const byte KcpHeaderRequestConnection = (byte)KcpHeader.RequestConnection;
        private const byte KcpHeaderConfirmConnection = (byte)KcpHeader.ConfirmConnection;

        public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            
            var buffer = _packetParser.Pack(ref rpcId, ref routeTypeOpCode, ref routeId, memoryStream, message);

            if (!_isConnected)
            {
                _messageCache.Enqueue(buffer);
                return;
            }

            SendMemoryStream(buffer);
        }

        private void SendMemoryStream(MemoryStream memoryStream)
        {
            if (_kcp.WaitSendCount > _maxSndWnd)
            {
                // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
                Log.Warning($"ERR_KcpWaitSendSizeTooLarge {_kcp.WaitSendCount} > {_maxSndWnd}");
                Dispose();
                return;
            }
            
            _kcp.Send(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            ReturnMemoryStream(memoryStream);
            AddToUpdate(0);
        }

        private unsafe void SendRequestConnection()
        {
            try
            {
                fixed (byte* p = _sendBuff)
                {
                    p[0] = KcpHeaderRequestConnection;
                    p[1] = _channelIdBytes[0];
                    p[2] = _channelIdBytes[1];
                    p[3] = _channelIdBytes[2];
                    p[4] = _channelIdBytes[3];
                }
                
                SendAsync(_sendBuff, 0, 5);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private unsafe void SendConfirmConnection()
        {
            try
            {
                fixed (byte* p = _sendBuff)
                {
                    p[0] = KcpHeaderConfirmConnection;
                    p[1] = _channelIdBytes[0];
                    p[2] = _channelIdBytes[1];
                    p[3] = _channelIdBytes[2];
                    p[4] = _channelIdBytes[3];
                }

                SendAsync(_sendBuff, 0, 5);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        private unsafe void SendDisconnect()
        {
            try
            {
                fixed (byte* p = _sendBuff)
                {
                    p[0] = KcpHeaderDisconnect;
                    p[1] = _channelIdBytes[0];
                    p[2] = _channelIdBytes[1];
                    p[3] = _channelIdBytes[2];
                    p[4] = _channelIdBytes[3];
                }
                
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
                _socket.Send(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
            }
            catch (ArgumentException ex)
            {
                Log.Error($"ArgumentException: {ex.Message}"); // 处理参数错误
            }
            catch (SocketException)
            {
                //Log.Error($"SocketException: {ex.Message}"); // 处理网络错误
                Dispose();
            }
            catch (ObjectDisposedException ex)
            {
                Log.Error($"ObjectDisposedException: {ex.Message}"); // 处理套接字已关闭的情况
                Dispose();
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
        
        private unsafe void KcpSpanCallback(byte[] buffer, ref int count)
        {
            if (IsDisposed)
            {
                return;
            }

            if (count == 0)
            {
                throw new Exception("KcpOutput count 0");
            }
            
            fixed (byte* p = buffer)
            {
                p[0] = KcpHeaderReceiveData;
                p[1] = _channelIdBytes[0];
                p[2] = _channelIdBytes[1];
                p[3] = _channelIdBytes[2];
                p[4] = _channelIdBytes[3];
            }

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
            return 0xC0000000 | (uint)new Random().Next();
        }
    }
}
#endif