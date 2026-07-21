#if FANTASY_NET || FANTASY_CONSOLE
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using Fantasy.Serialize;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy.Network.WebSocket
{
    public sealed class WebSocketClientNetwork : AClientNetwork
    {
        private bool _isSending;
        private bool _isInnerDispose;
        private bool _connectDisconnectEvent = true;
        private ClientWebSocket _clientWebSocket;
        private ReadOnlyMemoryPacketParser _packetParser;
        private readonly Pipe _pipe = new Pipe();
        private readonly Queue<MemoryStreamBuffer> _sendBuffers = new Queue<MemoryStreamBuffer>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private Action _onConnectFail;
        private Action _onConnectComplete;
        private Action _onConnectDisconnect;
        
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

        public void Initialize(NetworkTarget networkTarget, bool enableReceiveMessageJsonLog)
        {
            base.Initialize(NetworkType.Client, NetworkProtocolType.WebSocket, networkTarget, enableReceiveMessageJsonLog);
            _packetParser = (ReadOnlyMemoryPacketParser)PacketParserFactory.CreateWebglBufferPacketParser(this);
        }

        public override void Dispose()
        {
            if (IsDisposed || _isInnerDispose)
            {
                return;
            }
            
            _isInnerDispose = true;
            _isSending = false;
            ClearSendBuffers();
            
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
                InvokeSafely(_onConnectDisconnect);
            }
            
            CloseAndDisposeAsync().Coroutine();
            base.Dispose();
        }

        private async FTask CloseAndDisposeAsync()
        {
            try
            {
                if (_clientWebSocket.State == WebSocketState.Open ||
                    _clientWebSocket.State == WebSocketState.CloseReceived)
                {
                    using var closeTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                    await _clientWebSocket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client Closing", closeTimeout.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                try
                {
                    _clientWebSocket.Abort();
                }
                catch
                {
                    // 关闭过程中的异常可以忽略
                }
            }
            finally
            {
                try
                {
                    _packetParser.Dispose();
                    _packetParser = null;
                    _isSending = false;

                    _clientWebSocket.Dispose();
                    _clientWebSocket = null;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public override Session Connect(
            string remoteAddress,
            Action onConnectComplete,
            Action onConnectFail,
            Action onConnectDisconnect,
            bool isHttps,
            int connectTimeout = 5000)
        {
            if (IsInit)
            {
                throw new NotSupportedException(
                    $"WebSocketClientNetwork Id:{Id} Has already been initialized. " +
                    "If you want to call Connect again, please re instantiate it.");
            }

            try
            {
                IsInit = true;
                _onConnectFail = onConnectFail;
                _onConnectComplete = onConnectComplete;
                _onConnectDisconnect = onConnectDisconnect;
                _clientWebSocket = new ClientWebSocket();
                
#if FANTASY_UNITY || FANTASY_CONSOLE
                Session = EnableMessageJsonLog
                    ? Session.CreateDebugClientSession(this, null)
                    : Session.Create(this, null);
#else
                Session = Session.Create(this, null);
#endif
                var session = Session;
                
                ConnectAsync(
                    remoteAddress,
                    isHttps,
                    connectTimeout).Coroutine();

                return session;
            }
            catch
            {
                _connectDisconnectEvent = false;
                Dispose();
                throw;
            }
        }

        private async FTask ConnectAsync(
            string remoteAddress,
            bool isHttps,
            int connectTimeout)
        {
            try
            {
                var webSocketAddress =
                    WebSocketHelper.GetWebSocketAddress(
                        remoteAddress,
                        isHttps);

                await _clientWebSocket
                    .ConnectAsync(
                        new Uri(webSocketAddress),
                        _cancellationTokenSource.Token)
                    .WaitAsync(
                        TimeSpan.FromMilliseconds(connectTimeout),
                        _cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                // 主动销毁引发的取消不能再触发连接失败回调。
                if (_isInnerDispose || _cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (e is WebSocketException)
                {
                    Log.Error($"WebSocket error: {e.Message}");
                }
                else if (e is not TimeoutException)
                {
                    Log.Error($"An error occurred: {e.Message}");
                }

                // 连接失败只触发 Fail，Dispose 时不再触发 Disconnect。
                _connectDisconnectEvent = false;
                InvokeSafely(_onConnectFail);
                Dispose();
                return;
            }

            if (_isInnerDispose || _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            ReadPipeDataAsync().Coroutine();
            ReceiveSocketAsync().Coroutine();

            // 接收协程可能同步发现异常并触发 Dispose。
            if (_isInnerDispose ||
                _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            // 发送连接建立前缓存的消息。
            if (_sendBuffers.Count > 0)
            {
                Send().Coroutine();
            }

            // 发送协程也可能同步触发 Dispose。
            if (_isInnerDispose ||
                _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            InvokeSafely(_onConnectComplete);
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
                   
                    // 服务器发送了关闭帧，客户端需要响应关闭帧
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        Dispose();
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
                catch (WebSocketException)
                {
                    // 对端未完成关闭握手也表示连接已经不可继续使用。
                    Dispose();
                    break;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Dispose();
                    break;
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
                while (!_cancellationTokenSource.IsCancellationRequested &&
                       _packetParser.UnPack(ref buffer, out var packInfo))
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        packInfo.Dispose();
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

        public override void Send(uint rpcId, long address, MemoryStreamBuffer memoryStream, IMessage message, Type messageType)
        {
            if (IsDisposed || _isInnerDispose)
            {
                message?.Dispose();
                return;
            }
            
            _sendBuffers.Enqueue(_packetParser.Pack(ref rpcId, ref address, memoryStream, message, messageType));

            if (!_isSending &&
                _clientWebSocket?.State == WebSocketState.Open)
            {
                Send().Coroutine();
            }
        }
        
        private void ReturnMemoryStream(MemoryStreamBuffer memoryStream)
        {
            if ((memoryStream.MemoryStreamBufferSource & MemoryStreamBufferSource.Return) == 0)
            {
                return;
            }

            if (_isInnerDispose)
            {
                memoryStream.Dispose();
                return;
            }

            MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
        }

        private void ClearSendBuffers()
        {
            while (_sendBuffers.TryDequeue(out var memoryStream))
            {
                ReturnMemoryStream(memoryStream);
            }
        }
        
        private async FTask Send()
        {
            if (_isSending ||
                IsDisposed ||
                _clientWebSocket?.State != WebSocketState.Open)
            {
                return;
            }
            
            _isSending = true;

            try
            {
                while (_isSending)
                {
                    if (!_sendBuffers.TryDequeue(out var memoryStream))
                    {
                        return;
                    }

                    try
                    {
                        await _clientWebSocket.SendAsync(
                            new ArraySegment<byte>(memoryStream.GetBuffer(),
                                0,
                                (int)memoryStream.Position),
                            WebSocketMessageType.Binary,
                            true,
                            _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                        Dispose();
                        return;
                    }
                    finally
                    {
                        ReturnMemoryStream(memoryStream);
                    }
                }
            }
            finally
            {
                _isSending = false;
            }
        }

        #endregion

        public override void RemoveChannel(uint channelId)
        {
            Dispose();
        }
    }
}
#endif