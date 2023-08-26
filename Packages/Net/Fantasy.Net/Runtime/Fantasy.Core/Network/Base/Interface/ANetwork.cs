using System;
using System.Buffers;
using System.IO;
using Fantasy.Helper;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 消息缓存信息结构。
    /// </summary>
    public struct MessageCacheInfo
    {
        /// <summary>
        /// 获取或设置 RPC ID。
        /// </summary>
        public uint RpcId;

        /// <summary>
        /// 获取或设置路由 ID。
        /// </summary>
        public long RouteId;

        /// <summary>
        /// 获取或设置路由类型与操作码。
        /// </summary>
        public long RouteTypeOpCode;

        /// <summary>
        /// 获取或设置消息对象。
        /// </summary>
        public object Message;

        /// <summary>
        /// 获取或设置内存流。
        /// </summary>
        public MemoryStream MemoryStream;
    }

    /// <summary>
    /// 抽象网络基类。
    /// </summary>
    public abstract class ANetwork : IDisposable
    {
        /// <summary>
        /// 获取网络的唯一ID。
        /// </summary>
        public long Id { get; protected set; }

        /// <summary>
        /// 获取场景对象。
        /// </summary>
        public Scene Scene { get; protected set; }

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
        /// 打包消息的委托。
        /// </summary>
        protected readonly Func<uint, long, long, MemoryStream, object, MemoryStream> Pack;

        /// <summary>
        /// 初始化抽象网络基类的新实例。
        /// </summary>
        /// <param name="scene">场景对象。</param>
        /// <param name="networkType">网络类型。</param>
        /// <param name="networkProtocolType">网络协议类型。</param>
        /// <param name="networkTarget">网络目标类型。</param>
        protected ANetwork(Scene scene, NetworkType networkType, NetworkProtocolType networkProtocolType, NetworkTarget networkTarget)
        {
            Scene = scene;
            NetworkType = networkType;
            NetworkTarget = networkTarget;
            NetworkProtocolType = networkProtocolType;
            Id = IdFactory.NextRunTimeId();
#if FANTASY_NET
            if (networkTarget == NetworkTarget.Inner)
            {
                Pack = InnerPack;
                NetworkMessageScheduler = new InnerMessageScheduler();
                return;
            }
#endif
            Pack = OuterPack;
            
            switch (networkType)
            {
                case NetworkType.Client:
                {
                    NetworkMessageScheduler = new ClientMessageScheduler();
                    return;
                }
                case NetworkType.Server:
                {
                    NetworkMessageScheduler = new OuterMessageScheduler();
                    return;
                }
            }
        }
#if FANTASY_NET
        /// <summary>
        /// 内部消息打包方法。
        /// </summary>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型与操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="memoryStream">内存流，用于消息数据。</param>
        /// <param name="message">消息对象。</param>
        /// <returns>打包后的内存流。</returns>
        private MemoryStream InnerPack(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
        {
#if FANTASY_DEVELOP
            // 检查是否在正确的网络线程中
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return null;
            }
#endif
            // 根据是否提供内存流打包消息
            return memoryStream == null
                ? InnerPacketParser.Pack(rpcId, routeId, message)
                : InnerPacketParser.Pack(rpcId, routeId, memoryStream);
        }
#endif

        /// <summary>
        /// 外部消息打包方法。
        /// </summary>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型与操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="memoryStream">内存流，用于消息数据。</param>
        /// <param name="message">消息对象。</param>
        /// <returns>打包后的内存流。</returns>
        private MemoryStream OuterPack(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
        {
#if FANTASY_DEVELOP
            // 检查是否在正确的网络线程中
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return null;
            }
#endif
            // 根据是否提供内存流打包消息
            return memoryStream == null
                ? OuterPacketParser.Pack(rpcId, routeTypeOpCode, message)
                : OuterPacketParser.Pack(rpcId, routeTypeOpCode, memoryStream);
        }

        /// <summary>
        /// 发送消息。
        /// </summary>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型与操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="message">消息对象。</param>
        public abstract void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, object message);
        /// <summary>
        /// 发送消息。
        /// </summary>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型与操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="memoryStream">内存流，用于消息数据。</param>
        public abstract void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream);
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
            // 从网络线程中移除网络对象
            NetworkThread.Instance?.RemoveNetwork(Id);

            Id = 0;
            Scene = null;
            IsDisposed = true;
            NetworkType = NetworkType.None;
            NetworkTarget = NetworkTarget.None;
            NetworkProtocolType = NetworkProtocolType.None;
        }
    }
}