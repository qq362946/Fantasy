using System;
using System.Net;
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 客户端Network网络组件。
    /// </summary>
    public sealed class ClientNetworkComponent : Entity
    {
        /// <summary>
        /// 获取关联的客户端Network网络实例。
        /// </summary>
        private AClientNetwork Network { get; set; }
        /// <summary>
        /// 获取与客户端网络关联的会话。
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// 初始化客户端网络组件。
        /// </summary>
        /// <param name="networkProtocolType">网络协议类型。</param>
        /// <param name="networkTarget">网络目标。</param>
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

        /// <summary>
        /// 连接到指定的远程终端。
        /// </summary>
        /// <param name="remoteEndPoint">远程终端的IP地址和端口。</param>
        /// <param name="onConnectComplete">连接成功时的回调。</param>
        /// <param name="onConnectFail">连接失败时的回调。</param>
        /// <param name="onConnectDisconnect">连接断开时的回调。</param>
        /// <param name="connectTimeout">连接超时时间（毫秒）。</param>
        public void Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000)
        {
            if (Network == null || Network.IsDisposed)
            {
                throw new NotSupportedException("Network is null or isDisposed");
            }
            
            Network.Connect(remoteEndPoint, onConnectComplete, onConnectFail, onConnectDisconnect, connectTimeout);
            Session = Session.Create(Network, remoteEndPoint);
        }

        /// <summary>
        /// 释放客户端网络组件及关联的资源。
        /// </summary>
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