using System;
using System.Threading;

namespace Fantasy
{
    /// <summary>
    /// 抽象网络基类。
    /// </summary>
    public abstract class ANetwork : IDisposable
    {
        private static long _idGenerator = long.MinValue;
        
        /// <summary>
        /// 获取网络的唯一ID。
        /// </summary>
        public long Id { get; private set; }
        /// <summary>
        /// 获取或设置网络所在的场景。
        /// </summary>
        public Scene Scene { get; private set; }
        /// <summary>
        /// 获取或设置网络是否已被释放。
        /// </summary>
        public bool IsDisposed { get; protected set; }
        /// <summary>
        /// 获取网络类型。
        /// </summary>
        public NetworkType NetworkType { get; private set; }
        /// <summary>
        /// 获取网络目标类型。
        /// </summary>
        public NetworkTarget NetworkTarget { get; private set; }
        /// <summary>
        /// 获取网络协议类型。
        /// </summary>
        public NetworkProtocolType NetworkProtocolType { get; private set; }
        /// <summary>
        /// 获取或设置网络消息调度器。
        /// </summary>
        public ANetworkMessageScheduler NetworkMessageScheduler { get; protected set; }
        /// <summary>
        /// 上下同步文本。
        /// </summary>
        public ThreadSynchronizationContext ThreadSynchronizationContext { get; private set; }
        
        /// <summary>
        /// 初始化网络基类的新实例。
        /// </summary>
        /// <param name="scene">当前网络所在的Scene。</param>
        /// <param name="networkType">网络类型。</param>
        /// <param name="networkProtocolType">网络协议类型。</param>
        /// <param name="networkTarget">网络目标类型。</param>
        protected ANetwork(Scene scene, NetworkType networkType, NetworkProtocolType networkProtocolType, NetworkTarget networkTarget)
        {
            Id = Interlocked.Add(ref _idGenerator, 1L);
            
            Scene = scene;
            NetworkType = networkType;
            NetworkTarget = networkTarget;
            NetworkProtocolType = networkProtocolType;
            ThreadSynchronizationContext = scene.ThreadSynchronizationContext;
#if FANTASY_NET
            if (networkTarget == NetworkTarget.Inner)
            {
                NetworkMessageScheduler = new InnerMessageScheduler(scene);
                return;
            }
#endif
            switch (networkType)
            {
                case NetworkType.Client:
                {
                    NetworkMessageScheduler = new ClientMessageScheduler(scene);
                    break;
                }
                case NetworkType.Server:
                {
                    NetworkMessageScheduler = new OuterMessageScheduler(scene);
                    break;
                }
            }
        }
        
        /// <summary>
        /// 移除通道。
        /// </summary>
        /// <param name="channelId">通道 ID。</param>
        public abstract void RemoveChannel(uint channelId);
        /// <summary>
        /// 释放资源。
        /// </summary>
        public virtual void Dispose()
        {
            Id = 0;
            Scene = null;
            IsDisposed = true;
            NetworkType = NetworkType.None;
            NetworkTarget = NetworkTarget.None;
            NetworkProtocolType = NetworkProtocolType.None;
            ThreadSynchronizationContext = null;
        }
    }
}