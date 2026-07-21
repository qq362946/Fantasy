using System;
using System.Buffers;
using System.IO;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.PacketParser.Interface
{
    /// <summary>
    /// 抽象的包解析器基类，用于解析网络通信数据包。
    /// </summary>
    public abstract class APacketParser : IDisposable
    {
        internal Scene Scene;
        internal ANetwork Network;
        internal MessageDispatcherComponent MessageDispatcherComponent;
        protected bool IsDisposed { get; private set; }
        public abstract MemoryStreamBuffer Pack(ref uint rpcId, ref long address, MemoryStreamBuffer memoryStream, IMessage message, Type messageType);
        public abstract void PackMemoryStream(ref uint rpcId, ref long address, IMessage message, Type messageType, MemoryStreamBuffer memoryStream);
        
        protected MemoryStreamBuffer RentAndPack(ref uint rpcId, ref long address, IMessage message, Type messageType)
        {
            var memoryStream = Network.MemoryStreamBufferPool.RentMemoryStream(MemoryStreamBufferSource.Pack);

            try
            {
                PackMemoryStream(ref rpcId, ref address, message, messageType, memoryStream);
                return memoryStream;
            }
            catch
            {
                Network.MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
                throw;
            }
        }
        
        public virtual void Dispose()
        {
            IsDisposed = true;
            Scene = null;
            MessageDispatcherComponent = null;
        }
    }
}