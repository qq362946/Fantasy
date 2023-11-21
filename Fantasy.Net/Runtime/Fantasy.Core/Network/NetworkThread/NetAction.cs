using System;
using System.IO;
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 不同类型的网络操作。
    /// </summary>
    public enum NetActionType
    {
        /// <summary>
        /// 无操作。
        /// </summary>
        None = 0,
        /// <summary>
        /// 发送数据。
        /// </summary>
        Send = 1,
        /// <summary>
        /// 发送内存流数据。
        /// </summary>
        SendMemoryStream = 2,
        /// <summary>
        /// 移除通道。
        /// </summary>
        RemoveChannel = 3,
    }

    /// <summary>
    /// 表示一个网络操作，可以是发送消息、移除通道等操作。
    /// </summary>
    public struct NetAction : IDisposable
    {
        /// <summary>
        /// 用于发送的对象，可能是消息对象或其他数据。
        /// </summary>
        public object Obj;
        /// <summary>
        /// 要发送的 RPC ID。
        /// </summary>
        public uint RpcId;
        /// <summary>
        /// 关联的实体 ID。
        /// </summary>
        public long EntityId;
        /// <summary>
        /// 关联的网络 ID。
        /// </summary>
        public long NetworkId;
        /// <summary>
        /// 关联的通道 ID。
        /// </summary>
        public uint ChannelId;
        /// <summary>
        /// 关联的路由类型 Op Code。
        /// </summary>
        public long RouteTypeOpCode;
        /// <summary>
        /// 用于发送的内存流。
        /// </summary>
        public MemoryStream MemoryStream;
        /// <summary>
        /// 网络操作的类型。
        /// </summary>
        public NetActionType NetActionType;

        /// <summary>
        /// 初始化一个新的 NetAction 结构体实例，用于发送内存流。
        /// </summary>
        /// <param name="networkId">关联的网络 ID。</param>
        /// <param name="channelId">关联的通道 ID。</param>
        /// <param name="rpcId">要发送的 RPC ID。</param>
        /// <param name="routeTypeOpCode">关联的路由类型 Op Code。</param>
        /// <param name="entityId">关联的实体 ID。</param>
        /// <param name="netActionType">网络操作的类型。</param>
        /// <param name="memoryStream">要发送的内存流。</param>
        public NetAction(long networkId, uint channelId, uint rpcId, long routeTypeOpCode, long entityId, NetActionType netActionType, MemoryStream memoryStream)
        {
            Obj = null;
            RpcId = rpcId;
            EntityId = entityId;
            NetworkId = networkId;
            ChannelId = channelId;
            RouteTypeOpCode = routeTypeOpCode;
            MemoryStream = memoryStream;
            NetActionType = netActionType;
        }

        /// <summary>
        /// 初始化一个新的 NetAction 结构体实例，用于发送对象。
        /// </summary>
        /// <param name="networkId">关联的网络 ID。</param>
        /// <param name="channelId">关联的通道 ID。</param>
        /// <param name="rpcId">要发送的 RPC ID。</param>
        /// <param name="routeTypeOpCode">关联的路由类型 Op Code。</param>
        /// <param name="entityId">关联的实体 ID。</param>
        /// <param name="netActionType">网络操作的类型。</param>
        /// <param name="obj">要发送的对象。</param>
        public NetAction(long networkId, uint channelId, uint rpcId, long routeTypeOpCode, long entityId, NetActionType netActionType, object obj)
        {
            Obj = obj;
            RpcId = rpcId;
            EntityId = entityId;
            NetworkId = networkId;
            ChannelId = channelId;
            MemoryStream = null;
            RouteTypeOpCode = routeTypeOpCode;
            NetActionType = netActionType;
        }

        /// <summary>
        /// 释放资源并清理当前实例的状态。
        /// </summary>
        public void Dispose()
        {
            Obj = null;
            MemoryStream = null;
            RpcId = 0;
            EntityId = 0;
            NetworkId = 0;
            ChannelId = 0;
            RouteTypeOpCode = 0;
            NetActionType = NetActionType.None;
        }
    }
}