using System;
using System.Buffers;
using System.IO;

namespace Fantasy
{
    /// <summary>
    /// 抽象的包解析器基类，用于解析网络通信数据包。
    /// </summary>
    public abstract class APacketParser : IDisposable
    {
        /// <summary>
        /// 当前Scene。
        /// </summary>
        protected Scene Scene;
        /// <summary>
        /// 获取一个值，表示是否已经被释放。
        /// </summary>
        protected bool IsDisposed { get; private set; }
        /// <summary>
        /// 消息分发组件。
        /// </summary>
        protected MessageDispatcherComponent MessageDispatcherComponent;

        /// <summary>
        /// 根据网络目标创建相应的包解析器实例。
        /// </summary>
        /// <param name="scene">当前Scene</param>
        /// <param name="networkTarget">网络目标，指示是内部网络通信还是外部网络通信。</param>
        /// <returns>创建的包解析器实例。</returns>
        public static APacketParser CreatePacketParser(Scene scene, NetworkTarget networkTarget)
        {
            var messageDispatcherComponent = scene.MessageDispatcherComponent;
            
            switch (networkTarget)
            {
                case NetworkTarget.Inner:
                {
#if FANTASY_NET
                    var innerPacketParser = new InnerPacketParser();
                    innerPacketParser.Scene = scene;
                    innerPacketParser.MessageDispatcherComponent = messageDispatcherComponent;
                    return innerPacketParser;
#else
                    throw new NotSupportedException($"PacketParserHelper Create NotSupport {networkTarget}");
#endif
                }
                case NetworkTarget.Outer:
                {
                    var outerPacketParser = new OuterPacketParser();
                    outerPacketParser.Scene = scene;
                    outerPacketParser.MessageDispatcherComponent = messageDispatcherComponent;
                    return outerPacketParser;
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
        /// <param name="memoryStream">内存块的所有者。</param>
        /// <param name="packInfo">解析得到的数据包信息。</param>
        /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
        public abstract bool UnPack(MemoryStream memoryStream, out APackInfo packInfo);

        /// <summary>
        /// 消息打包。
        /// </summary>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型与操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="memoryStream">内存流，用于消息数据。</param>
        /// <param name="message">消息对象。</param>
        /// <returns>打包后的内存流。</returns>
        public abstract MemoryStream Pack(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message);
        /// <summary>
        /// 释放资源，包括内存池等。
        /// </summary>
        public virtual void Dispose()
        {
            IsDisposed = true;
            Scene = null;
            MessageDispatcherComponent = null;
        }
    }
}