using System;
using System.IO;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    public abstract class APackInfo : IDisposable
    {
        public uint RpcId;
        public long RouteId;
        public uint ProtocolCode;
        public long RouteTypeCode;
        public int MessagePacketLength;
        public MemoryStreamBuffer MemoryStream { get; protected set; }
        protected ANetwork Network;
        public bool IsDisposed;
        public abstract object Deserialize(Type messageType);
        public abstract MemoryStreamBuffer RentMemoryStream(int size = 0);
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