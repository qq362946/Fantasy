#if FANTASY_NET
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

namespace Fantasy;
public sealed class TCPServerNetworkChannel : ANetworkServerChannel
{
    private bool _isInnerDispose;
    private readonly Socket _socket;
    private readonly ANetwork _network;
    private readonly Pipe _pipe = new Pipe();
    private readonly ReadOnlyMemoryPacketParser _packetParser;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    
    public TCPServerNetworkChannel(ANetwork network, Socket socket, uint id) : base(network, id, socket.RemoteEndPoint)
    {
        _socket = socket;
        _network = network;
        _socket.NoDelay = true;
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
        _network.RemoveChannel(Id);
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
                
        if (_socket.Connected)
        {
            _socket.Disconnect(true);
            _socket.Close();
        }
            
        _packetParser.Dispose();
    }

    #region ReceiveSocket

    private async FTask ReceiveSocketAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var memory = _pipe.Writer.GetMemory(8192);
                var count = await _socket.ReceiveAsync(memory, SocketFlags.None, _cancellationTokenSource.Token);
                _pipe.Writer.Advance(count);
                await _pipe.Writer.FlushAsync();
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

    public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStreamBuffer memoryStream, object message)
    {
        Send(_packetParser.Pack(ref rpcId, ref routeTypeOpCode, ref routeId, memoryStream, message)).Coroutine();
    }

    private async FTask Send(MemoryStreamBuffer memoryStream)
    {
        try
        {
            await _socket.SendAsync(new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length), SocketFlags.None);
        }
        catch (SocketException)
        {
            // 一般发生在地方Socket断开时出现，所以也额可以忽略。
            Dispose();
        }
        catch (OperationCanceledException)
        {
            // 取消操作，可以忽略这个异常。这个属于正常逻辑
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
        finally
        {
            _network.ReturnMemoryStream(memoryStream);
        }
    }

    private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Send:
            {
                var eUserToken = (MemoryStreamBuffer)e.UserToken;
                Scene.ThreadSynchronizationContext.Post(() =>
                {
                    _network.ReturnMemoryStream(eUserToken);
                });
                return;
            }
            case SocketAsyncOperation.Disconnect:
            {
                Scene.ThreadSynchronizationContext.Post(Dispose);
                return;
            }
        }
    }

    #endregion
}
#endif