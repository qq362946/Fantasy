using System;
using System.Buffers;
using System.IO;
using Fantasy.DataStructure;
using Fantasy.Helper;

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 抽象的包解析器基类，用于解析网络通信数据包。
    /// </summary>
    public abstract class APacketParser : IDisposable
    {
        /// <summary>
        /// 内存池，用于分配内存块。
        /// </summary>
        protected MemoryPool<byte> MemoryPool;
        /// <summary>
        /// 获取一个值，表示是否已经被释放。
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// 根据网络目标创建相应的包解析器实例。
        /// </summary>
        /// <param name="networkTarget">网络目标，指示是内部网络通信还是外部网络通信。</param>
        /// <returns>创建的包解析器实例。</returns>
        public static APacketParser CreatePacketParser(NetworkTarget networkTarget)
        {
            switch (networkTarget)
            {
                case NetworkTarget.Inner:
                {
#if FANTASY_NET
                    return new InnerPacketParser();
#else
                    throw new NotSupportedException($"PacketParserHelper Create NotSupport {networkTarget}");
#endif
                }
                case NetworkTarget.Outer:
                {
                    return new OuterPacketParser();
                }
                default:
                {
                    throw new NotSupportedException($"PacketParserHelper Create NotSupport {networkTarget}");
                }
            }
        }

        /// <summary>
        /// 从循环缓冲区解析数据包。
        /// </summary>
        /// <param name="buffer">循环缓冲区。</param>
        /// <param name="packInfo">解析得到的数据包信息。</param>
        /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
        public abstract bool UnPack(CircularBuffer buffer, out APackInfo packInfo);
        /// <summary>
        /// 从内存块解析数据包。
        /// </summary>
        /// <param name="memoryOwner">内存块的所有者。</param>
        /// <param name="packInfo">解析得到的数据包信息。</param>
        /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
        public abstract bool UnPack(IMemoryOwner<byte> memoryOwner, out APackInfo packInfo);
        /// <summary>
        /// 释放资源，包括内存池等。
        /// </summary>
        public virtual void Dispose()
        {
            IsDisposed = true;
            MemoryPool.Dispose();
        }
    }
}