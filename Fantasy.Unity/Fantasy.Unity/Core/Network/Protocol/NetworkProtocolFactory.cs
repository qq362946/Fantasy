using System;
using System.Net;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    public static class NetworkProtocolFactory
    {
#if FANTASY_NET
        public static ANetwork CreateServer(Scene scene, NetworkProtocolType protocolType, NetworkTarget networkTarget, IPEndPoint address)
        {
            switch (protocolType)
            {
                case NetworkProtocolType.TCP:
                {
                    return new TCPServerNetwork(scene, networkTarget, address);
                }
                case NetworkProtocolType.KCP:
                {
                    return new KCPServerNetwork(scene, networkTarget, address);
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported NetworkProtocolType:{protocolType}");
                }
            }
        }
#endif
        public static AClientNetwork CreateClient(Scene scene, NetworkProtocolType protocolType, NetworkTarget networkTarget)
        {
            switch (protocolType)
            {
                case NetworkProtocolType.TCP:
                {
                    return new TCPClientNetwork(scene, networkTarget);
                }
                case NetworkProtocolType.KCP:
                {
                    return new KCPClientNetwork(scene, networkTarget);
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported NetworkProtocolType:{protocolType}");
                }
            }
        }
    }
}