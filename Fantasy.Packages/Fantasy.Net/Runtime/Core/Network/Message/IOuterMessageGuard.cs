using System;
using Fantasy.Network;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Network.Interface
{
    public interface IOuterMessageGuard
    {
        bool TryReject(Session session, uint protocolCode, uint rpcId, uint opCodeType, Type messageType, out uint errorCode);
    }
}
