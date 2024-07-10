using System.Net;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if FANTASY_NET
namespace Fantasy
{
    public sealed class KCPServerNetwork : ANetwork
    {
        private KCPServerSocket _kcpServerSocket;
        private readonly Dictionary<uint, KCPServerNetworkChannel> _connectionChannel = new Dictionary<uint, KCPServerNetworkChannel>();
        public KCPServerNetwork(Scene scene, NetworkTarget networkTarget, IPEndPoint address) : base(scene, NetworkType.Server, NetworkProtocolType.KCP, networkTarget)
        {
            _kcpServerSocket = new KCPServerSocket(networkTarget, address);
            _kcpServerSocket.OnConnectionCompleted += OnConnectionCompleted;
            _kcpServerSocket.Start();
        }

        private void OnConnectionCompleted(object? sender, KCPServerSSocketAsyncEventArgs e)
        {
            _connectionChannel.Add(e.ChannelId, new KCPServerNetworkChannel(this, e));
        }

        public override void RemoveChannel(uint channelId)
        {
            _kcpServerSocket.RemoveConnection(channelId);
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            foreach (var (_, kcpSocketAsyncEventArgs) in _connectionChannel.ToArray())
            {
                _kcpServerSocket.ThreadSynchronizationContext.Post(() =>
                {
                    kcpSocketAsyncEventArgs.Dispose();
                });
            }
            _connectionChannel.Clear();
            _kcpServerSocket.Dispose();
            _kcpServerSocket = null;
            base.Dispose();
        }
    }
}
#endif