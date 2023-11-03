using System;
using System.IO;
using System.Net;
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 抽象的网络通道基类。
    /// </summary>
    public abstract class ANetworkChannel
    {
        /// <summary>
        /// 获取通道的唯一标识 ID。
        /// </summary>
        public uint Id { get; private set; }
        /// <summary>
        /// 获取或设置通道所属的场景。
        /// </summary>
        public Scene Scene { get; protected set; }
        /// <summary>
        /// 获取通道所属的网络 ID。
        /// </summary>
        public long NetworkId { get; private set; }
        /// <summary>
        /// 获取通道是否已经被释放。
        /// </summary>
        public bool IsDisposed { get; protected set; }
        /// <summary>
        /// 获取通道的远程终端点。
        /// </summary>
        public EndPoint RemoteEndPoint { get; protected set; }
        /// <summary>
        /// 获取通道的数据包解析器。
        /// </summary>
        public APacketParser PacketParser { get; protected set; }

        /// <summary>
        /// 当通道被释放时触发的事件。
        /// </summary>
        public abstract event Action OnDispose;
        /// <summary>
        /// 当通道接收到内存流数据包时触发的事件。
        /// </summary>
        public abstract event Action<APackInfo> OnReceiveMemoryStream;

        /// <summary>
        /// 初始化抽象网络通道基类的新实例。
        /// </summary>
        /// <param name="scene">通道所属的场景。</param>
        /// <param name="id">通道的唯一标识 ID。</param>
        /// <param name="networkId">通道所属的网络 ID。</param>
        protected ANetworkChannel(Scene scene, uint id, long networkId)
        {
            Id = id;
            Scene = scene;
            NetworkId = networkId;
        }

        /// <summary>
        /// 释放通道资源。
        /// </summary>
        public virtual void Dispose()
        {
            NetworkThread.Instance.RemoveChannel(NetworkId, Id);
            
            Id = 0;
            Scene = null;
            NetworkId = 0;
            IsDisposed = true;
            RemoteEndPoint = null;

            if (PacketParser != null)
            {
                PacketParser.Dispose();
                PacketParser = null;
            }
        }
    }
}