#if FANTASY_NET
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
namespace Fantasy;

public sealed class WebSocketServerNetworkChannel : ANetworkServerChannel
{
    private bool _isInnerDispose;
    private readonly Pipe _pipe = new Pipe();
    private readonly WebSocket _webSocket;
    private readonly WebSocketServerNetwork _network;
    private readonly ReadOnlyMemoryPacketParser _packetParser;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    
    public WebSocketServerNetworkChannel(ANetwork network, uint id, HttpListenerWebSocketContext httpListenerWebSocketContext, IPEndPoint remoteEndPoint) : base(network, id, remoteEndPoint)
    {
        _network = (WebSocketServerNetwork)network;
        _webSocket = httpListenerWebSocketContext.WebSocket;
        _packetParser = PacketParserFactory.CreateServerReadOnlyMemoryPacket(network);
        ReadPipeDataAsync().Coroutine();
        ReceiveSocketAsync().Coroutine();
    }
    
    public override void Dispose()
    {
        if (IsDisposed || _isInnerDispose)
        {
            return;
        }
        
        _isInnerDispose = true;
        if (_cancellationTokenSource.IsCancellationRequested)
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
        _network.RemoveChannel(Id);
        base.Dispose();
        _webSocket.Dispose();
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
                var receiveResult = await _webSocket.ReceiveAsync(memory, _cancellationTokenSource.Token);

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
            Log.Warning($"RemoteAddress:{RemoteEndPoint} \n{e}");
            Dispose();
        }
        catch (Exception e)
        {
            Log.Error($"RemoteAddress:{RemoteEndPoint} \n{e}");
            Dispose();
        }
    }

    #endregion

    #region Send

    public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
    {
        SendAsync(_packetParser.Pack(ref rpcId, ref routeTypeOpCode, ref routeId, memoryStream, message)).Coroutine();
    }

    private async FTask SendAsync(MemoryStream memoryStream)
    {
        await _webSocket.SendAsync(new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length), WebSocketMessageType.Binary, true, _cancellationTokenSource.Token);
        _network.ReturnMemoryStream(memoryStream);
    }

    #endregion
}
#endif