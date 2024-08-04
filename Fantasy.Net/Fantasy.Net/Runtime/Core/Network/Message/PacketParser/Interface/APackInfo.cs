using System;
using System.IO;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    public abstract class APackInfo : IDisposable
    {
        /// <summary>
        /// 数据包的 RPC ID。
        /// </summary>
        public uint RpcId;
        /// <summary>
        /// 数据包的路由 ID。
        /// </summary>
        public long RouteId;
        /// <summary>
        /// 数据包的协议编号。
        /// </summary>
        public uint ProtocolCode;
        /// <summary>
        /// 数据包的路由类型编码。
        /// </summary>
        public long RouteTypeCode;
        /// <summary>
        /// 数据包消息体的长度。
        /// </summary>
        public int MessagePacketLength;
        /// <summary>
        /// 内存块的所有者，用于存储数据包的内存数据。
        /// </summary>
        public MemoryStream MemoryStream { get; protected set; }
        /// <summary>
        /// 所属于的Network
        /// </summary>
        protected ANetwork Network;
        /// <summary>
        /// 获取一个值，表示是否已经被释放。
        /// </summary>
        public bool IsDisposed;
        public abstract object Deserialize(Type messageType);
        public abstract MemoryStream RentMemoryStream(int size = 0);
        /// <summary>
        /// 释放资源，包括内存块等。
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            RpcId = 0;
            RouteId = 0;
            ProtocolCode = 0;
            RouteTypeCode = 0;
            MessagePacketLength = 0;
            Network.ReturnMemoryStream(MemoryStream);
            MemoryStream = null;
            IsDisposed = true;
            Network = null;
        }
    }
}