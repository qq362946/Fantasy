#if FANTASY_NET
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Security;
using Fantasy.PacketParser;
using Fantasy.Serialize;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

namespace Fantasy.Network.TCP
{
    public sealed class TCPServerNetworkChannel : ANetworkServerChannel
    {
        private bool _isSending;
        private bool _isInnerDispose;
        private bool _keyExchangeDone;
        private readonly Socket _socket;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly byte[] _encryptSendBuffer;
        private readonly byte[] _decryptReceiveBuffer;
        private readonly ANetwork _network;
        private readonly Pipe _pipe = new Pipe();
        private readonly SocketAsyncEventArgs _sendArgs;
        private readonly ReadOnlyMemoryPacketParser _packetParser;
        private readonly Queue<MemoryStreamBuffer> _sendBuffers = new Queue<MemoryStreamBuffer>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TCPServerNetworkChannel(ANetwork network, Socket socket, uint id, EncryptionHelper encryptionHelper = null) : base(network, id, socket.RemoteEndPoint)
        {
            _socket = socket;
            _network = network;

            try
            {
                _encryptionHelper = encryptionHelper;

                _socket.NoDelay = true;
                _sendArgs = new SocketAsyncEventArgs();
                _sendArgs.Completed += OnSendCompletedHandler;
                _packetParser = PacketParserFactory.CreateReadOnlyMemoryPacketParser(network);

                if (_encryptionHelper != null)
                {
                    (_encryptSendBuffer, _decryptReceiveBuffer) = EncryptionHelper.CreateBuffers();
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }
        
        internal void Start()
        {
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

            try
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException)
                    {
                        // Socket 可能已被远端关闭或处于不可 shutdown 状态，释放流程继续
                    }
                    catch (ObjectDisposedException)
                    {
                        // Socket 已被并发释放，释放流程继续
                    }
                    
                    try
                    {
                        _socket.Close();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
                
                _sendArgs?.Dispose();
                ClearSendBuffers();
                _packetParser?.Dispose();
                _isSending = false;
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

        #region ReceiveSocket

        private async FTask ReceiveSocketAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var memory = _pipe.Writer.GetMemory(8192);
                    var count = await _socket.ReceiveAsync(memory, SocketFlags.None, _cancellationTokenSource.Token);
                    
                    if (count == 0)
                    {
                        Dispose();
                        break;
                    }
                    
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
                    Dispose();
                    break;
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

                if (_encryptionHelper != null)
                {
                    try
                    {
                        // 加密路径：一次取整个可读序列（跨段时拷贝拼接），逐帧处理；
                        // 半包留在 pipe 里，按实际消费字节数推进 consumed，等下次补齐。
                        var message = buffer.IsSingleSegment ? buffer.First : buffer.ToArray();
                        var originalLength = message.Length;
                        while (true)
                        {
                            if (!TryHandleEncryption(ref message, out var disposed))
                            {
                                if (disposed) return;
                                break; // 数据不足，等下次
                            }
                        }
                        consumed = buffer.GetPosition(originalLength - message.Length);
                    }
                    catch (Exception e)
                    {
                        // 解密/密钥交换异常时断开连接，避免读协程泄漏
                        Log.Error(e);
                        Dispose();
                        return;
                    }
                }
                else
                {
                    while (TryReadMessage(ref buffer, out var message))
                    {
                        ReceiveData(ref message);
                        consumed = buffer.Start;
                    }
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

        private bool TryHandleEncryption(ref ReadOnlyMemory<byte> buffer, out bool disposed)
        {
            disposed = false;
            if (!_keyExchangeDone)
            {
                const int keyExchangePacketSize = sizeof(int) + 1 + EncryptionHelper.PublicKeySize;
                var sp = buffer.Span;

                if (sp.Length < keyExchangePacketSize) return false;

                if (sp[sizeof(int)] != EncryptionHelper.KeyExchangeMarker)
                {
                    Log.Warning($"TCP encryption required but client handshake has no marker, is encryption enabled in the client?, channelId={Id}");
                    Dispose();
                    disposed = true;
                    return false;
                }

                _encryptionHelper.DeriveSharedKey(sp.Slice(sizeof(int) + 1, EncryptionHelper.PublicKeySize).ToArray());
                if (ProgramDefine.ServerPrivateKey != null)
                {
                    // 固定密钥模式：握手包不返回公钥（0xED），客户端必须用配置的 serverPublicKey，否则无法完成握手
                    var fixedKf = new byte[sizeof(int) + 1];
                    BinaryPrimitives.WriteInt32LittleEndian(fixedKf, 1);
                    fixedKf[sizeof(int)] = EncryptionHelper.KeyExchangeMarkerFixed;
                    _socket.Send(fixedKf);
                }
                else
                {
                    // 临时密钥模式：握手包返回公钥（0xEC）
                    var kf = new byte[keyExchangePacketSize];
                    BinaryPrimitives.WriteInt32LittleEndian(kf, 33);
                    kf[sizeof(int)] = EncryptionHelper.KeyExchangeMarker;
                    Array.Copy(_encryptionHelper.PublicKey, 0, kf, sizeof(int) + 1, EncryptionHelper.PublicKeySize);
                    _socket.Send(kf);
                }
                _keyExchangeDone = true;
                // 加密就绪后，发送握手期间积压的数据（Send 内部会加密）
                if (!_isSending && _sendBuffers.Count > 0) Send();
                buffer = buffer.Slice(keyExchangePacketSize);
                return true;
            }

            if (_encryptionHelper is { IsReady: true })
            {
                var sp = buffer.Span;
                if (sp.Length < sizeof(int)) return false;
                var encLen = BinaryPrimitives.ReadInt32LittleEndian(sp);
                if (encLen < 0 || encLen > ProgramDefine.MaxMessageSize + EncryptionHelper.Overhead)
                {
                    // 非法长度头，断开连接
                    Dispose();
                    disposed = true;
                    return false;
                }
                if (sp.Length < sizeof(int) + encLen) return false;
                var decryptedLength = _encryptionHelper.Decrypt(sp, sizeof(int), encLen, _decryptReceiveBuffer, 0);
                var decryptedMemory = new ReadOnlyMemory<byte>(_decryptReceiveBuffer, 0, decryptedLength);
                ReceiveData(ref decryptedMemory);
                buffer = buffer.Slice(sizeof(int) + encLen);
                return true;
            }

            Log.Error("Encryption state inconsistent: key exchange done but helper not ready.");
            Dispose();
            disposed = true;
            return false;
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

        public override void Send(uint rpcId, long address, MemoryStreamBuffer memoryStream, IMessage message, Type messageType)
        {
            if (IsDisposed || _isInnerDispose)
            {
                message?.Dispose();
                return;
            }
            
            _sendBuffers.Enqueue(_packetParser.Pack(ref rpcId, ref address, memoryStream, message, messageType));

            // 加密握手完成前不触发实际发送，否则明文数据会被客户端当作加密数据解密而失败
            if (!_isSending)
            {
                if (_encryptionHelper == null || _encryptionHelper.IsReady)
                {
                    Send();
                }
            }
        }
        
        private void Send()
        {
            if (_isSending || IsDisposed)
            {
                return;
            }
            
            _isSending = true;

            while (_sendBuffers.TryDequeue(out var memoryStreamBuffer))
            {
                // 出队时加密
                if (_encryptionHelper is { IsReady: true })
                {
                    var buf = memoryStreamBuffer.GetBuffer();
                    var len = (int)memoryStreamBuffer.Position;
                    var encLen = _encryptionHelper.Encrypt(buf, 0, len, _encryptSendBuffer, 4);
                    BinaryPrimitives.WriteInt32LittleEndian(_encryptSendBuffer, encLen);
                    var encStream = _network.MemoryStreamBufferPool.RentMemoryStream(MemoryStreamBufferSource.Pack, 4 + encLen);
                    encStream.Write(_encryptSendBuffer, 0, 4 + encLen);
                    ReturnMemoryStream(memoryStreamBuffer);
                    memoryStreamBuffer = encStream;
                }

                var offset = 0;
                var totalLength = (int)memoryStreamBuffer.Position;
                var buffer = memoryStreamBuffer.GetBuffer();

                while (offset < totalLength)
                {
                    _sendArgs.UserToken = memoryStreamBuffer;
                    _sendArgs.SetBuffer(buffer, offset, totalLength - offset);

                    try
                    {
                        if (_socket.SendAsync(_sendArgs))
                        {
                            return;
                        }
                        
                        if (_sendArgs.SocketError != SocketError.Success)
                        {
                            ReturnMemoryStream(memoryStreamBuffer);
                            _isSending = false;
                            Dispose();
                            return;
                        }
                        
                        var sent = _sendArgs.BytesTransferred;
                        if (sent == 0)
                        {
                            ReturnMemoryStream(memoryStreamBuffer);
                            _isSending = false;
                            Dispose();
                            return;
                        }
                        
                        offset += sent;
                    }
                    catch
                    {
                        ReturnMemoryStream(memoryStreamBuffer);
                        _isSending = false;
                        Dispose();
                        return;
                    }
                }
                
                // 同步发送完整后归还 buffer
                ReturnMemoryStream(memoryStreamBuffer);
            }
            
            _isSending = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            _network.MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
        }
        
        private void ClearSendBuffers()
        {
            while (_sendBuffers.TryDequeue(out var memoryStreamBuffer))
            {
                ReturnMemoryStream(memoryStreamBuffer);
            }
        }
        
        private void OnSendCompletedHandler(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            var memoryStreamBuffer = (MemoryStreamBuffer)asyncEventArgs.UserToken;
            var synchronizationContext = Scene?.ThreadSynchronizationContext;
            
            if (_isInnerDispose || synchronizationContext == null)
            {
                ReturnMemoryStream(memoryStreamBuffer);
                return;
            }
            
            // 限制最大重试次数，防止死循环
            // 理论上一个包不应该需要超过 10000 次 partial send
            const int maxRetries = 10000;

            for (var i = 0; i < maxRetries; i++)
            {
                if (asyncEventArgs.SocketError != SocketError.Success || asyncEventArgs.BytesTransferred == 0)
                {
                    synchronizationContext.Post(() =>
                    {
                        _isSending = false;
                        ReturnMemoryStream(memoryStreamBuffer);
                        Dispose();
                    });
                
                    return;
                }
                
                var sent = asyncEventArgs.BytesTransferred;
                var total = asyncEventArgs.Count;
                
                if (sent < total)
                {
                    // 部分发送，更新 offset 继续发送剩余部分
                    var newOffset = asyncEventArgs.Offset + sent;
                    var remaining = total - sent;
                
                    try
                    {
                        asyncEventArgs.SetBuffer(newOffset, remaining);
                        
                        if (_socket.SendAsync(asyncEventArgs))
                        {
                            return;  // 继续异步发送，等待下次回调
                        }
            
                        continue;
                    }
                    catch
                    {
                        synchronizationContext.Post(() =>
                        {
                            _isSending = false;
                            ReturnMemoryStream(memoryStreamBuffer);
                            Dispose();
                        });
                        return;
                    }
                }
                
                // 当前 buffer 发送完整，归还并继续下一个
                synchronizationContext.Post(() =>
                {
                    ReturnMemoryStream(memoryStreamBuffer);
                
                    if (_sendBuffers.Count > 0)
                    {
                        _isSending = false;
                        Send();
                    }
                    else
                    {
                        _isSending = false;
                    }
                });
                
                return;
            }
            
            // 如果达到最大重试次数，记录错误并断开连接
            Log.Error($"OnSendCompleted exceeded max retries ({maxRetries}), possible infinite loop. Disconnecting.");
            
            synchronizationContext.Post(() =>
            {
                ReturnMemoryStream(memoryStreamBuffer);
                _isSending = false;
                Dispose();
            });
        }

        #endregion
    }
}
#endif
