using System;
using System.Buffers;
using System.IO;


namespace Fantasy
{
    /// <summary>
    /// 抽象的数据包信息基类，用于存储解析得到的数据包信息。
    /// </summary>
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
        public IMemoryOwner<byte> MemoryOwner;
        /// <summary>
        /// 获取一个值，表示是否已经被释放。
        /// </summary>
        public bool IsDisposed;

        /// <summary>
        /// 从对象池中获取一个类型为 T 的未释放实例。
        /// </summary>
        /// <typeparam name="T">要获取的实例类型，必须是 APackInfo 的子类。</typeparam>
        /// <returns>获取的未释放实例。</returns>
        public static T Rent<T>() where T : APackInfo
        {
            var aPackInfo = Pool<T>.Rent();
            aPackInfo.IsDisposed = false;
            return aPackInfo;
        }

        /// <summary>
        /// 根据指定类型反序列化消息体。
        /// </summary>
        /// <param name="messageType">要反序列化成的类型。</param>
        /// <returns>反序列化得到的消息体。</returns>
        public abstract object Deserialize(Type messageType);
        /// <summary>
        /// 创建用于写入数据包消息体的内存流。
        /// </summary>
        /// <returns>创建的内存流。</returns>
        public abstract MemoryStream CreateMemoryStream();
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
            MemoryOwner.Dispose();
            MemoryOwner = null;
            IsDisposed = true;
        }
    }
}