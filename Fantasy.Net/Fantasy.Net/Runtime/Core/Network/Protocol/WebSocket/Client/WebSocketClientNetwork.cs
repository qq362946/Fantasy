#if FANTASY_NET
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using Fantasy;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
public sealed class WebSocketClientNetwork : AClientNetwork
{
    private bool _isInnerDispose;
    private long _connectTimeoutId;
    private ClientWebSocket _clientWebSocket;
    private ReadOnlyMemoryPacketParser _packetParser;
    private readonly Pipe _pipe = new Pipe();
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private Action _onConnectFail;
    private Action _onConnectComplete;
    private Action _onConnectDisconnect;

    public void Initialize(NetworkTarget networkTarget)
    {
        base.Initialize(NetworkType.Client, NetworkProtocolType.WebSocket, networkTarget);
        _packetParser = PacketParserFactory.CreateClientReadOnlyMemoryPacket(this);
    }
    
    public override void Dispose()
    {
        if (IsDisposed || _isInnerDispose)
        {
            return;
        }

        _isInnerDispose = true;
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
        base.Dispose();
        ClearConnectTimeout();
        DisposeAsync().Coroutine();
        _onConnectDisconnect?.Invoke();
        _packetParser.Dispose();
        _packetParser = null;
    }
    
    private async FTask DisposeAsync()
    {
        if (_clientWebSocket == null)
        {
            return;
        }
        
        await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        _clientWebSocket.Dispose();
        _clientWebSocket = null;
    }

    public override Session Connect(string remoteAddress, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, bool isHttps, int connectTimeout = 5000)
    {
        if (IsInit)
        {
            throw new NotSupportedException($"WebSocketClientNetwork Id:{Id} Has already been initialized. If you want to call Connect again, please re instantiate it.");
        }

        IsInit = true;
        _onConnectFail = onConnectFail;
        _onConnectComplete = onConnectComplete;
        _onConnectDisconnect = onConnectDisconnect;
        // 设置连接超时定时器
        _connectTimeoutId = Scene.TimerComponent.Net.OnceTimer(connectTimeout, () =>
        {
            _onConnectFail?.Invoke();
            Dispose();
        });
        
        _clientWebSocket = new ClientWebSocket();
        var webSocketAddress = WebSocketHelper.GetWebSocketAddress(remoteAddress, isHttps);
        
        try
        {
            _clientWebSocket.ConnectAsync(new Uri(webSocketAddress), _cancellationTokenSource.Token).Wait();

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return null;
            }
        }
        catch (WebSocketException wse)
        {
            Log.Error($"WebSocket error: {wse.Message}");
            Dispose();
            return null;
        }
        catch (Exception e)
        {
            Log.Error($"An error occurred: {e.Message}");
            Dispose();
            return null;
        }
        
        ClearConnectTimeout();
        ReadPipeDataAsync().Coroutine();
        ReceiveSocketAsync().Coroutine();
        _onConnectComplete?.Invoke();
        Session = Session.Create(this, null);
        return Session;
    }

    #region ReceiveSocket

    private async FTask ReceiveSocketAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var memory = _pipe.Writer.GetMemory(8192);
                // 这里接收的数据不一定是一个完整的包。如果大于8192就会分成多个包。
                var receiveResult = await _clientWebSocket.ReceiveAsync(memory, _cancellationTokenSource.Token);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var count = receiveResult.Count;

                if (count > 0)
                {
                    await PipeWriterFlushAsync(count);
                }
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
            catch (WebSocketException wse)
            {
                Log.Error($"WebSocket error: {wse.Message}");
                Dispose();
                break;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        await _pipe.Writer.CompleteAsync();
    }

    private async FTask PipeWriterFlushAsync(int count)
    {
        _pipe.Writer.Advance(count);
        await _pipe.Writer.FlushAsync();
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

            while (TryReadMessage(ref buffer, out var message))
            {
                ReceiveData(ref message);
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

    private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlyMemory<byte> message)
    {
        if (buffer.Length == 0)
        {
            message = default;
            return false;
        }

        message = buffer.First;

        if (message.Length == 0)
        {
            message = default;
            return false;
        }

        buffer = buffer.Slice(message.Length);
        return true;
    }

    private void ReceiveData(ref ReadOnlyMemory<byte> buffer)
    {
        try
        {
            while (_packetParser.UnPack(ref buffer, out var packInfo))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                Session.Receive(packInfo);
            }
        }
        catch (ScanException e)
        {
            Log.Warning(e.Message);
            Dispose();
        }
        catch (Exception e)
        {
            Log.Error(e);
            Dispose();
        }
    }

    #endregion

    #region Send

    public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStreamBuffer memoryStream, object message)
    {
        SendAsync(_packetParser.Pack(ref rpcId, ref routeTypeOpCode, ref routeId, memoryStream, message)).Coroutine();
    }

    private async FTask SendAsync(MemoryStreamBuffer memoryStream)
    {
        await _clientWebSocket.SendAsync(new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length), WebSocketMessageType.Binary, true, _cancellationTokenSource.Token);
        ReturnMemoryStream(memoryStream);
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

        Scene.TimerComponent.Net.Remove(ref _connectTimeoutId);
    }
}
#endif