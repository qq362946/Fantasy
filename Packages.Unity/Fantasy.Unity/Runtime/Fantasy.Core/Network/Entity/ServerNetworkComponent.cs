using System;
using System.Net;
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 服务端Network网络组件。
    /// </summary>
    public sealed class ServerNetworkComponent : Entity, INotSupportedPool
    {
        /// <summary>
        /// 获取关联的服务端Network网络实例。
        /// </summary>
        public ANetwork Network { get; private set; }

        /// <summary>
        /// 初始化服务端网络组件。
        /// </summary>
        /// <param name="networkProtocolType">网络协议类型。</param>
        /// <param name="networkTarget">网络目标。</param>
        /// <param name="address">绑定的IP地址和端口。</param>
        public void Initialize(NetworkProtocolType networkProtocolType, NetworkTarget networkTarget, IPEndPoint address)
        {
            switch (networkProtocolType)
            {
                case NetworkProtocolType.KCP:
                {
                    Network = new KCPServerNetwork(Scene, networkTarget, address);
                    Log.Info($"NetworkProtocol:KCP IPEndPoint:{address}");
                    return;
                }
                case NetworkProtocolType.TCP:
                {
                    Network = new TCPServerNetwork(Scene, networkTarget, address);
                    Log.Info($"NetworkProtocol:TCP IPEndPoint:{address}");
                    return;
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported NetworkProtocolType:{networkProtocolType}");
                }
            }
        }

        /// <summary>
        /// 释放服务端网络组件及关联的资源。
        /// </summary>
        public override void Dispose()
        {
            if (Network != null)
            {
                Network.Dispose();
                Network = null;
            }
            
            base.Dispose();
        }
    }
}