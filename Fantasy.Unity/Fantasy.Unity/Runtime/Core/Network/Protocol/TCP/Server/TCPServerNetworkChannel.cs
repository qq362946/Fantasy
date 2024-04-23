using System.Net.Sockets;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#if FANTASY_NET
namespace Fantasy
{
    /// <summary>
    /// TCP 服务器网络通道，用于处理服务器与客户端之间的数据通信。
    /// </summary>
    public sealed class TCPServerNetworkChannel : ANetworkServerChannel
    {
        private bool _isSending;
        private readonly Socket _socket;
        private readonly TCPServerNetwork _network;
        private readonly CircularBuffer _sendBuffer = new CircularBuffer();
        private readonly CircularBuffer _receiveBuffer = new CircularBuffer();
        private readonly SocketAsyncEventArgs _outArgs = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs _innArgs = new SocketAsyncEventArgs();

        public TCPServerNetworkChannel(ANetwork network, Socket socket, uint id) : base(network, id, socket.RemoteEndPoint)
        {
            _socket = socket;
            _socket.NoDelay = true;
            _network = (TCPServerNetwork)network;
            _innArgs.Completed += OnComplete;
            _outArgs.Completed += OnComplete;
            Receive();
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            _isSending = false;
            
            if (_socket.Connected)
            {
                _socket.Disconnect(true);
                _socket.Close();
            }
            
            _outArgs.Dispose();
            _innArgs.Dispose();
            _sendBuffer.Dispose();
            _receiveBuffer.Dispose();
            _network.RemoveChannel(Id);
            base.Dispose();
        }

        #region Send

        public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
        {
            Send(PacketParser.Pack(rpcId, routeTypeOpCode, routeId, memoryStream, message));
        }

        /// <summary>
        /// 向通道发送内存流数据。
        /// </summary>
        /// <param name="memoryStream">待发送的内存流。</param>
        public override void Send(MemoryStream memoryStream)
        {
            _sendBuffer.Write(memoryStream);
            
            // 因为memoryStream对象池出来的、所以需要手动回收下
            
            memoryStream.Dispose();
            
            if (_isSending)
            {
                return;
            }
            
            Send();
        }

        private void Send()
        {
            if (_isSending || IsDisposed)
            {
                return;
            }

            while(true)
            {
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    
                    if (_sendBuffer.Length == 0)
                    {
                        _isSending = false;
                        return;
                    }
                    
                    _isSending = true;
                    
                    var sendSize = CircularBuffer.ChunkSize - _sendBuffer.FirstIndex;
                    
                    if (sendSize > _sendBuffer.Length)
                    {
                        sendSize = (int) _sendBuffer.Length;
                    }

                    _outArgs.SetBuffer(_sendBuffer.First, _sendBuffer.FirstIndex, sendSize);
                    
                    if (_socket.SendAsync(_outArgs))
                    {
                        return;
                    }
                    
                    SendCompletedHandler(_outArgs);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private void SendCompletedHandler(SocketAsyncEventArgs asyncEventArgs)
        {
            if (asyncEventArgs.SocketError != SocketError.Success || asyncEventArgs.BytesTransferred == 0)
            {
                return;
            }
            
            _sendBuffer.FirstIndex += asyncEventArgs.BytesTransferred;
        
            if (_sendBuffer.FirstIndex == CircularBuffer.ChunkSize)
            {
                _sendBuffer.FirstIndex = 0;
                _sendBuffer.RemoveFirst();
            }
        }

        private void OnSendComplete(SocketAsyncEventArgs asyncEventArgs)
        {
            if (IsDisposed)
            {
                return;
            }
            
            _isSending = false;
            SendCompletedHandler(asyncEventArgs);

            if (_sendBuffer.Length > 0)
            {
                Send();
            }
        }

        #endregion

        #region Receive

        /// <summary>
        /// 开始接收数据。
        /// </summary>
        private void Receive()
        {
            while (true)
            {
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    
                    var size = CircularBuffer.ChunkSize - _receiveBuffer.LastIndex;
                    _innArgs.SetBuffer(_receiveBuffer.Last, _receiveBuffer.LastIndex, size);
                
                    if (_socket.ReceiveAsync(_innArgs))
                    {
                        return;
                    }

                    ReceiveCompletedHandler(_innArgs);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        private void ReceiveCompletedHandler(SocketAsyncEventArgs asyncEventArgs)
        {
            if (asyncEventArgs.SocketError != SocketError.Success)
            {
                return;
            }
            
            if (asyncEventArgs.BytesTransferred == 0)
            {
                Dispose();
                return;
            }
           
            _receiveBuffer.LastIndex += asyncEventArgs.BytesTransferred;
            
            if (_receiveBuffer.LastIndex >= CircularBuffer.ChunkSize)
            {
                _receiveBuffer.AddLast();
                _receiveBuffer.LastIndex = 0;
            }

            while (true)
            {
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    if (!PacketParser.UnPack(_receiveBuffer, out var packInfo))
                    {
                        break;
                    }
                    
                    Session.Receive(packInfo);
                }
                catch (ScanException e)
                {
                    Log.Debug($"RemoteAddress:{RemoteEndPoint} \n{e}");
                    Dispose();
                }
                catch (Exception e)
                {
                    Log.Error($"RemoteAddress:{RemoteEndPoint} \n{e}");
                    Dispose();
                }
            }
        }
        
        private void OnReceiveComplete(SocketAsyncEventArgs asyncEventArgs)
        {
            ReceiveCompletedHandler(asyncEventArgs);
            Receive();
        }

        #endregion

        #region 网络线程（由Socket底层产生的线程）

        private void OnComplete(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            if (IsDisposed)
            {
                return;
            }
 
            switch (asyncEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                {
                    ChannelSynchronizationContext.Post(() => OnReceiveComplete(asyncEventArgs));
                    return;
                }
                case SocketAsyncOperation.Send:
                {
                    ChannelSynchronizationContext.Post(() => OnSendComplete(asyncEventArgs));
                    return;
                }
                case SocketAsyncOperation.Disconnect:
                {
                    ChannelSynchronizationContext.Post(Dispose);
                    return;
                }
                default:
                {
                    throw new Exception($"Socket Error: {asyncEventArgs.LastOperation}");
                }
            }
        }

        #endregion
    }
}
#endif
