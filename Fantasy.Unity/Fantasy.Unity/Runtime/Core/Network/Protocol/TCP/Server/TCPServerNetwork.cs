#if FANTASY_NET
using System.Net;
using System.Net.Sockets;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    /// <summary>
    /// 表示 TCP 协议服务端网络类。
    /// </summary>
    public class TCPServerNetwork : ANetwork
    {
        private readonly Random _random;
        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _acceptAsync;
        private readonly Dictionary<uint, INetworkChannel> _connectionChannel = new Dictionary<uint, INetworkChannel>();
        
        #region 跟随Scene或者Server所在的线程执行

        public TCPServerNetwork(Scene scene, NetworkTarget networkTarget, IPEndPoint address) : base(scene, NetworkType.Server, NetworkProtocolType.TCP, networkTarget)
        {
            _random = new Random();
            _acceptAsync = new SocketAsyncEventArgs();
            _socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }

            _socket.Bind(address);
            _socket.Listen(int.MaxValue);
            _socket.SetSocketBufferToOsLimit();
            _acceptAsync.Completed += OnCompleted;
            AcceptAsync();
        }

        /// <summary>
        /// 异步接受客户端连接请求。
        /// </summary>
        private void AcceptAsync()
        {
            _acceptAsync.AcceptSocket = null;

            if (_socket.AcceptAsync(_acceptAsync))
            {
                return;
            }

            OnAcceptComplete(_acceptAsync);
        }
        
        private void OnAcceptComplete(SocketAsyncEventArgs asyncEventArgs)
        {
            if (asyncEventArgs.AcceptSocket == null)
            {
                return;
            }

            if (asyncEventArgs.SocketError != SocketError.Success)
            {
                Log.Error($"Socket Accept Error: {_acceptAsync.SocketError}");
                return;
            }

            try
            {
                var channelId = 0xC0000000 | (uint) _random.Next();

                while (_connectionChannel.ContainsKey(channelId))
                {
                    channelId = 0xC0000000 | (uint) _random.Next();
                }

                _connectionChannel.Add(channelId, new TCPServerNetworkChannel(this, asyncEventArgs.AcceptSocket, channelId));
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                AcceptAsync();
            }
        }
        
        public override void RemoveChannel(uint channelId)
        {
            if (IsDisposed || !_connectionChannel.Remove(channelId, out var channel))
            {
                return;
            }
#if FANTASY_DEVELOP
            Log.Debug($"TCPServerNetwork _connectionChannel:{_connectionChannel.Count}");
#endif
            if (channel.IsDisposed)
            {
                return;
            }

            channel.Dispose();
        }

        #endregion

        #region 网络线程（由Socket底层产生的线程）

        private void OnCompleted(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            switch (asyncEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                {
                    ThreadSynchronizationContext.Post(() =>
                    {
                        OnAcceptComplete(asyncEventArgs);
                    });
                    
                    break;
                }
                default:
                {
                    throw new Exception($"Socket Accept Error: {asyncEventArgs.LastOperation}");
                }
            }
        }

        #endregion
    }
}
#endif
