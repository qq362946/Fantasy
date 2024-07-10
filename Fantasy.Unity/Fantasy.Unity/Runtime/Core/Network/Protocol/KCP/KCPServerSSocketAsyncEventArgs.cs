#if FANTASY_NET
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using KCP;

// ReSharper disable InconsistentNaming

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    public class KCPServerSSocketAsyncEventArgs : IDisposable
    {
        private uint _channelId;
        private Socket _socket;
        private KCPServerSocket _kcpServerSocket;
        private readonly int _maxSndWnd;
        private readonly byte[] _channelIdBytes = new byte[sizeof(uint)];
        private readonly byte[] _receiveBuffer = new byte[2048];
        public Kcp Kcp { get; private set; }
        public uint ChannelId 
        {
            get => _channelId;
            private set
            {
                _channelId = value;
                _channelId.GetBytes(_channelIdBytes);
            }
        }
        public bool IsDisposed { get; private set; }
        public IPEndPoint IPEndPoint { get; private set; }
        public event EventHandler OnDisconnectCompleted;
        public OnReceiveCompletedDelegate OnReceiveCompleted;

        public KCPServerSSocketAsyncEventArgs(KCPServerSocket kcpServerSocket, uint channelId, IPEndPoint ipEndPoint)
        {
            _socket = kcpServerSocket.Socket;
            ChannelId = channelId;
            _kcpServerSocket = kcpServerSocket;
            IPEndPoint = ipEndPoint;
            Kcp = new Kcp(channelId, KcpSpanCallback);
            Kcp.SetNoDelay(1, 5, 2, 1);
            Kcp.SetWindowSize(kcpServerSocket.Settings.SendWindowSize, kcpServerSocket.Settings.ReceiveWindowSize);
            Kcp.SetMtu(kcpServerSocket.Settings.Mtu);
            Kcp.SetMinrto(30);
            _maxSndWnd = kcpServerSocket.Settings.MaxSendWindowSize;
        }

        public void Send(MemoryStream memoryStream)
        {
            _kcpServerSocket.ThreadSynchronizationContext.Post(() =>
            {
                if (Kcp.WaitSendCount > _maxSndWnd)
                {
                    // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
                    Log.Warning($"ERR_KcpWaitSendSizeTooLarge {Kcp.WaitSendCount} > {_maxSndWnd}");
                    Dispose();
                    return;
                }

                Kcp.Send(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                
                _kcpServerSocket.AddUpdateChannel(ChannelId, 0);
            });
        }
        
        public void Input(byte[] data, int offset, int size)
        {
            Kcp.Input(data, offset, size);
            _kcpServerSocket.AddUpdateChannel(ChannelId, 0);
            
            while (!IsDisposed)
            {
                try
                {
                    var peekSize = Kcp.PeekSize();

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

                    var receiveCount = Kcp.Receive(_receiveBuffer, peekSize);
                    if (receiveCount != peekSize)
                    {
                        Log.Error($"receiveCount != peekSize receiveCount:{receiveCount} peekSize:{peekSize}");
                        return;
                    }
                    
                    OnReceiveCompleted(_receiveBuffer, ref receiveCount);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private void KcpSpanCallback(byte[] buffer, ref int count)
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (count == 0)
                {
                    throw new Exception("KcpOutput count 0");
                }
                
                buffer[0] = (byte)KcpHeader.ReceiveData;
                buffer[1] = _channelIdBytes[0];
                buffer[2] = _channelIdBytes[1];
                buffer[3] = _channelIdBytes[2];
                buffer[4] = _channelIdBytes[3];
                _socket.SendTo(buffer, 0, count + 5, SocketFlags.None, IPEndPoint);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            OnDisconnectCompleted.Invoke(this, null);
            _kcpServerSocket.RemoveConnection(ChannelId);
            Kcp = null;
            _socket = null;
            _kcpServerSocket = null;
            IPEndPoint = null;
            ChannelId = 0;
        }
    }
}
#endif