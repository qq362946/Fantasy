#if !FANTASY_WEBGL
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
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using Fantasy.Serialize;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

namespace Fantasy.Network.TCP
{
    public sealed class TCPClientNetwork : AClientNetwork
    {
        private bool _isSending;
        private bool _isInnerDispose;
        private long _connectTimeoutId;
        private bool _connectDisconnectEvent = true;
        private Socket _socket;
        private IPEndPoint _remoteEndPoint;
        private SocketAsyncEventArgs _sendArgs;
        private ReadOnlyMemoryPacketParser _packetParser;
        private readonly Pipe _pipe = new Pipe();
        private readonly Queue<MemoryStreamBuffer> _sendBuffers = new Queue<MemoryStreamBuffer>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        private Action _onConnectFail;
        private Action _onConnectComplete;
        private Action _onConnectDisconnect;
        
        public uint ChannelId { get; private set; }

        public void Initialize(NetworkTarget networkTarget)
        {
            base.Initialize(NetworkType.Client, NetworkProtocolType.TCP, networkTarget);
        }
        
        public override void Dispose()
        {
            if (IsDisposed || _isInnerDispose)
            {
                return;
            }

            try
            {
                _isSending = false;
                _isInnerDispose = true;
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
                    _onConnectDisconnect?.Invoke();
                }

                if (_socket.Connected)
                {
                    _socket.Close();
                    _socket = null;
                }

                _sendBuffers.Clear();
                _packetParser?.Dispose();
                ChannelId = 0;
                _sendArgs = null;
                _connectDisconnectEvent = true;
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

        /// <summary>
        /// 连接到远程服务器。
        /// </summary>
        /// <param name="remoteAddress">远程服务器的终端点。</param>
        /// <param name="onConnectComplete">连接成功时的回调。</param>
        /// <param name="onConnectFail">连接失败时的回调。</param>
        /// <param name="onConnectDisconnect">连接断开时的回调。</param>
        /// <param name="isHttps"></param>
        /// <param name="connectTimeout">连接超时时间，单位：毫秒。</param>
        /// <returns>连接的会话。</returns>
        public override Session Connect(string remoteAddress, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, bool isHttps, int connectTimeout = 5000)
        {
            // 如果已经初始化过一次，抛出异常，要求重新实例化
            
            if (IsInit)
            {
                throw new NotSupportedException("TCPClientNetwork Has already been initialized. If you want to call Connect again, please re instantiate it.");
            }
            
            IsInit = true;
            _isSending = false;
            _onConnectFail = onConnectFail;
            _onConnectComplete = onConnectComplete;
            _onConnectDisconnect = onConnectDisconnect;
            // 设置连接超时定时器
            _connectTimeoutId = Scene.TimerComponent.Net.OnceTimer(connectTimeout, () =>
            {
                _connectDisconnectEvent = false;
                _onConnectFail?.Invoke();
                Dispose();
            });
            _packetParser = PacketParserFactory.CreateReadOnlyMemoryPacketParser(this);
            _remoteEndPoint = NetworkHelper.GetIPEndPoint(remoteAddress);
            _socket = new Socket(_remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true;
            _socket.SetSocketBufferToOsLimit();
            _sendArgs = new SocketAsyncEventArgs();
            _sendArgs.Completed += OnSendCompleted;
            var outArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = _remoteEndPoint
            };
            outArgs.Completed += OnConnectSocketCompleted;
           
            if (!_socket.ConnectAsync(outArgs))
            {
                OnReceiveSocketComplete();
            }
            
            Session = Session.Create(this, _remoteEndPoint);
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
                        _onConnectFail?.Invoke();
                        Dispose();
                    });
                }
            }
        }
        
        private void OnReceiveSocketComplete()
        {
            ClearConnectTimeout();
            _onConnectComplete?.Invoke();
            ReadPipeDataAsync().Coroutine();
            ReceiveSocketAsync().Coroutine();
        }

        #region ReceiveSocket

        private async FTask ReceiveSocketAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var count = 0;
                    var memory = _pipe.Writer.GetMemory(8192);
#if UNITY_2021
                    // Unity2021.3.14f有个恶心的问题，使用ReceiveAsync会导致memory不能正确写入
                    // 所有只能使用ReceiveFromAsync来接收消息，但ReceiveFromAsync只有一个接受ArraySegment的接口。
                    MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> arraySegment);
                    var result = await _socket.ReceiveFromAsync(arraySegment, SocketFlags.None, _remoteEndPoint);
                    count = result.ReceivedBytes;
#else
                    count = await _socket.ReceiveAsync(memory, SocketFlags.None, _cancellationTokenSource.Token);
#endif
                    if (count == 0)
                    {
                        Dispose();
                        return;
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
            _sendBuffers.Enqueue(_packetParser.Pack(ref rpcId, ref address, memoryStream, message, messageType));
            
            if (!_isSending)
            {
                Send();
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
                            return;
                        }
                        
                        var sent = _sendArgs.BytesTransferred;
                        if (sent == 0)
                        {
                            ReturnMemoryStream(memoryStreamBuffer);
                            _isSending = false;
                            return;
                        }
                        
                        offset += sent;
                    }
                    catch
                    {
                        ReturnMemoryStream(memoryStreamBuffer);
                        _isSending = false;
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
            if (MemoryStreamBufferSource.Return.HasFlag(memoryStream.MemoryStreamBufferSource))
            {
                MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
            }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            var memoryStreamBuffer = (MemoryStreamBuffer)asyncEventArgs.UserToken;
            // 限制最大重试次数，防止死循环
            // 理论上一个包不应该需要超过 10000 次 partial send
            const int maxRetries = 10000;

            for (var i = 0; i < maxRetries; i++)
            {
                if (asyncEventArgs.SocketError != SocketError.Success || asyncEventArgs.BytesTransferred == 0)
                {
                    Scene.ThreadSynchronizationContext.Post(() =>
                    {
                        _isSending = false;
                        ReturnMemoryStream(memoryStreamBuffer);
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
                    asyncEventArgs.SetBuffer(newOffset, remaining);
                
                    try
                    {
                        if (_socket.SendAsync(asyncEventArgs))
                        {
                            return;  // 继续异步发送，等待下次回调
                        }
            
                        continue;
                    }
                    catch
                    {
                        Scene.ThreadSynchronizationContext.Post(() =>
                        {
                            _isSending = false;
                            ReturnMemoryStream(memoryStreamBuffer);
                        });
                        return;
                    }
                }
                
                // 当前 buffer 发送完整，归还并继续下一个
                Scene.ThreadSynchronizationContext.Post(() =>
                {
                    ReturnMemoryStream(memoryStreamBuffer);
                
                    if (_sendBuffers.Count > 0)
                    {
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
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                ReturnMemoryStream(memoryStreamBuffer);
                _isSending = false;
                Dispose();
            });
        }
        
        /// <summary>
        /// 判断是否为致命错误（备用暂时没有任何地方使用）
        /// </summary>
        /// <param name="error">Socket 错误类型</param>
        /// <returns>true=致命错误需断开连接，false=暂时性错误可继续</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsFatalError(SocketError error)
        {
            switch (error)
            {
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                case SocketError.NotConnected:
                {
                    return true;
                }
                default:
                {
                    // 其他错误都容忍，丢弃消息但保持连接
                    return false;
                }

                #region 错误类型，可以根据项目的容忍度自行增加

                // WouldBlock: 非阻塞 Socket 操作会阻塞
                // 发生场景：非阻塞模式下，发送缓冲区满，无法立即完成操作
                // 处理：丢弃当前消息，继续处理队列
                // TryAgain: 资源暂时不可用，建议重试
                // 发生场景：系统资源暂时耗尽（如临时端口不足）
                // 处理：丢弃当前消息，继续处理队列
                // --- 连接相关错误 ---
                // ConnectionAborted: 连接被软件中止（本地系统中止）
                // 发生场景：本地应用程序或系统强制关闭连接
                // ConnectionReset: 连接被远程主机重置
                // 发生场景：远程主机强制关闭连接（如进程崩溃、网络设备重启）
                // ConnectionRefused: 连接被拒绝
                // 发生场景：目标端口没有监听，或防火墙拒绝连接
                // NotConnected: Socket 未连接
                // 发生场景：在未连接的 Socket 上执行需要连接的操作
                // Shutdown: Socket 已关闭
                // 发生场景：在已调用 Shutdown 的 Socket 上执行操作
                // Disconnecting: Socket 正在断开连接
                // 发生场景：在正在断开的 Socket 上执行操作
                // --- 网络相关错误 ---
                // NetworkDown: 网络已关闭
                // 发生场景：本地网络接口被禁用或网络驱动故障
                // NetworkReset: 网络连接被重置
                // 发生场景：网络设备重启或网络配置变更
                // NetworkUnreachable: 网络不可达
                // 发生场景：路由表中没有到达目标网络的路由
                // HostDown: 主机已关闭
                // 发生场景：目标主机关机或网络接口禁用
                // HostUnreachable: 主机不可达
                // 发生场景：路由表中没有到达目标主机的路由
                // HostNotFound: 主机未找到
                // 发生场景：DNS 解析失败或主机名不存在
                // --- 资源相关错误 ---
                // NoBufferSpaceAvailable: 没有可用的缓冲区空间
                // 发生场景：系统缓冲区耗尽（内存不足或连接过多）
                // TooManyOpenSockets: 打开的 Socket 过多
                // 发生场景：进程或系统达到 Socket 数量限制
                // SystemNotReady: 网络子系统未就绪
                // 发生场景：网络驱动未加载或网络栈初始化失败
                // --- 超时错误 ---
                // TimedOut: 操作超时
                // 发生场景：发送操作超过设定的超时时间（网络延迟过高）
                // --- 协议相关错误 ---
                // ProtocolNotSupported: 协议不支持
                // 发生场景：尝试使用系统不支持的协议
                // ProtocolType: 协议类型错误
                // 发生场景：协议类型与 Socket 类型不匹配
                // ProtocolOption: 协议选项错误
                // 发生场景：设置了无效的协议选项
                // ProtocolFamilyNotSupported: 协议族不支持
                // 发生场景：尝试使用系统不支持的协议族（如 IPv6 未启用）
                // --- 参数错误（代码 bug）---
                // InvalidArgument: 参数无效
                // 发生场景：传递了无效的参数（代码错误）
                // MessageSize: 消息大小错误
                // 发生场景：消息超过协议允许的最大大小
                // Fault: 指针参数无效
                // 发生场景：传递了无效的内存地址（代码错误）
                // --- 权限错误 ---
                // AccessDenied: 访问被拒绝
                // 发生场景：没有权限执行操作（如绑定特权端口）
                // --- 其他错误 ---
                // Success: 操作成功（不应该出现在错误处理中）
                // SocketError: Socket 错误（通用错误）
                // OperationAborted: 操作被中止
                // IOPending: I/O 操作挂起
                // Interrupted: 阻塞调用被中断
                // InProgress: 操作正在进行
                // AlreadyInProgress: 操作已在进行
                // NotSocket: 描述符不是 Socket
                // DestinationAddressRequired: 需要目标地址
                // MessageTooLong: 消息过长
                // SocketNotSupported: Socket 类型不支持
                // OperationNotSupported: 操作不支持
                // AddressFamilyNotSupported: 地址族不支持
                // AddressAlreadyInUse: 地址已被使用
                // AddressNotAvailable: 地址不可用
                // IsConnected: Socket 已连接
                // NotInitialized: Socket 未初始化
                // ProcessLimit: 进程限制
                // VersionNotSupported: 版本不支持

                #endregion
            }
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

            Scene?.TimerComponent?.Net?.Remove(ref _connectTimeoutId);
        }
    }
}
#endif