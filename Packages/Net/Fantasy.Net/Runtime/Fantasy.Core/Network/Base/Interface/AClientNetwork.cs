using System;
using System.IO;
using System.Net;

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 抽象客户端网络基类。
    /// </summary>
    public abstract class AClientNetwork : ANetwork
    {
        /// <summary>
        /// 获取或设置通道ID。
        /// </summary>
        public uint ChannelId { get; protected set; }

        /// <summary>
        /// 在网络连接释放时触发的事件。
        /// </summary>
        public abstract event Action OnDispose;

        /// <summary>
        /// 在连接失败时触发的事件。
        /// </summary>
        public abstract event Action OnConnectFail;

        /// <summary>
        /// 在连接完成时触发的事件。
        /// </summary>
        public abstract event Action OnConnectComplete;

        /// <summary>
        /// 在连接断开时触发的事件。
        /// </summary>
        public abstract event Action OnConnectDisconnect;

        /// <summary>
        /// 在通道ID更改时触发的事件。
        /// </summary>
        public abstract event Action<uint> OnChangeChannelId;

        /// <summary>
        /// 在接收到内存流时触发的事件。
        /// </summary>
        public abstract event Action<APackInfo> OnReceiveMemoryStream;

        /// <summary>
        /// 初始化抽象客户端网络基类的新实例。
        /// </summary>
        /// <param name="scene">场景对象。</param>
        /// <param name="networkType">网络类型。</param>
        /// <param name="networkProtocolType">网络协议类型。</param>
        /// <param name="networkTarget">网络目标类型。</param>
        protected AClientNetwork(Scene scene, NetworkType networkType, NetworkProtocolType networkProtocolType, NetworkTarget networkTarget) : base(scene, networkType, networkProtocolType, networkTarget) { }

        /// <summary>
        /// 连接到远程终端。
        /// </summary>
        /// <param name="remoteEndPoint">远程终端的 <see cref="IPEndPoint"/>。</param>
        /// <param name="onConnectComplete">连接成功时的回调。</param>
        /// <param name="onConnectFail">连接失败时的回调。</param>
        /// <param name="onConnectDisconnect">连接断开时的回调。</param>
        /// <param name="connectTimeout">连接超时时间（毫秒）。</param>
        /// <returns>通道ID。</returns>
        public abstract uint Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000);

        /// <summary>
        /// 释放网络资源。
        /// </summary>
        public override void Dispose()
        {
            ChannelId = 0;
            base.Dispose();
        }
    }
}