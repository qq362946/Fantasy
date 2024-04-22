using System;
using System.IO;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    public interface INetworkChannel : IDisposable
    {
        public Session Session { get;}
        public bool IsDisposed { get;}
        public void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message);
        public void Send(MemoryStream memoryStream);
    }
}