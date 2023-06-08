using System;
using System.Net;

namespace Fantasy.Core.Network
{
    public sealed class ClientNetworkComponent : Entity
    {
        private AClientNetwork Network { get; set; }
        public Session Session { get; private set; }

        public void Initialize(NetworkProtocolType networkProtocolType, NetworkTarget networkTarget)
        {
            switch (networkProtocolType)
            {
                case NetworkProtocolType.KCP:
                {
                    Network = new KCPClientNetwork(Scene, networkTarget);
                    return;
                }
                case NetworkProtocolType.TCP:
                {
                    Network = new TCPClientNetwork(Scene, networkTarget);
                    return;
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported NetworkProtocolType:{networkProtocolType}");
                }
            }
        }

        public void Connect(IPEndPoint remoteEndPoint, Action onConnectFail, int connectTimeout = 5000)
        {
            if (Network == null || Network.IsDisposed)
            {
                throw new NotSupportedException("Network is null or isDisposed");
            }

            Network.Connect(remoteEndPoint, onConnectFail, connectTimeout);
            Session = Session.Create(Network);
        }

        public override void Dispose()
        {
            if (Network != null)
            {
                Network.Dispose();
                Network = null;
            }

            Session = null;
            base.Dispose();
        }
    }
}