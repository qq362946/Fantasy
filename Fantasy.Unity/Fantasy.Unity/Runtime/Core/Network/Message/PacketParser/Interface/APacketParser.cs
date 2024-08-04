using System;
using System.Buffers;
using System.IO;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 抽象的包解析器基类，用于解析网络通信数据包。
    /// </summary>
    public abstract class APacketParser : IDisposable
    {
        internal Scene Scene;
        internal ANetwork Network;
        internal MessageDispatcherComponent MessageDispatcherComponent;
        protected readonly byte[] BodyBuffer = new byte[sizeof(int)];
        protected readonly byte[] RpcIdBuffer = new byte[sizeof(uint)];
        protected readonly byte[] OpCodeBuffer = new byte[sizeof(uint)];
        protected readonly byte[] RouteIdBuffer = new byte[sizeof(long)];
        protected readonly byte[] PackRouteTypeOpCode = new byte[sizeof(long)];
        protected bool IsDisposed { get; private set; }
        public abstract MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message);
        public virtual void Dispose()
        {
            IsDisposed = true;
            Scene = null;
            MessageDispatcherComponent = null;
            Array.Clear(BodyBuffer, 0, BodyBuffer.Length);
            Array.Clear(RpcIdBuffer, 0, RpcIdBuffer.Length);
            Array.Clear(OpCodeBuffer, 0, OpCodeBuffer.Length);
            Array.Clear(RouteIdBuffer, 0, OpCodeBuffer.Length);
            Array.Clear(PackRouteTypeOpCode, 0, PackRouteTypeOpCode.Length);
        }
    }
}