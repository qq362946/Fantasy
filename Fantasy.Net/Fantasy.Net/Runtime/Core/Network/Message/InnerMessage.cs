using System.ComponentModel.DataAnnotations;
using Fantasy.Network.Interface;
using Fantasy.Serialize;
using MemoryPack;
// ReSharper disable InconsistentNaming
// ReSharper disable PropertyCanBeMadeInitOnly.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.InnerMessage
{
    [MemoryPackable]
    public sealed partial class Response : AMessage, IResponse
    {
        public uint OpCode()
        {
            return Fantasy.Network.OpCode.DefaultResponse;
        }
        [MemoryPackOrder(0)]
        public long RpcId { get; set; }
        [MemoryPackOrder(1)]
        public uint ErrorCode { get; set; }
    }
    [MemoryPackable]
    public sealed partial class RouteResponse : AMessage, IRouteResponse
    {
        public uint OpCode()
        {
            return Fantasy.Network.OpCode.DefaultRouteResponse;
        }
        [MemoryPackOrder(0)]
        public long RpcId { get; set; }
        [MemoryPackOrder(1)]
        public uint ErrorCode { get; set; }
    }
    [MemoryPackable]
    public partial class PingRequest : AMessage, IRequest
    {
        public uint OpCode()
        {
            return Fantasy.Network.OpCode.PingRequest;
        }
        [MemoryPackIgnore] 
        public PingResponse ResponseType { get; set; }
        [MemoryPackOrder(0)]
        public long RpcId { get; set; }
    }
    
    [MemoryPackable]
    public partial class PingResponse : AMessage, IResponse
    {
        public uint OpCode()
        {
            return Fantasy.Network.OpCode.PingResponse;
        }
        [MemoryPackOrder(0)]
        public long RpcId { get; set; }
        [MemoryPackOrder(2)]
        public uint ErrorCode { get; set; }
        [MemoryPackOrder(1)]
        public long Now;
    }
    [MemoryPackable]
    public partial class I_AddressableAdd_Request : AMessage, IRouteRequest
    {
        [MemoryPackIgnore]
        public I_AddressableAdd_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableAddRequest; }
        public long RouteTypeOpCode() { return 1; }
        [MemoryPackOrder(0)]
        public long AddressableId { get; set; }
        [MemoryPackOrder(1)]
        public long RouteId { get; set; }
        [MemoryPackOrder(2)]
        public bool IsLock { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableAdd_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableAddResponse; }
        [MemoryPackOrder(0)]
        public uint ErrorCode { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableGet_Request : AMessage, IRouteRequest
    {
        [MemoryPackIgnore]
        public I_AddressableGet_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableGetRequest; }
        public long RouteTypeOpCode() { return 1; }
        [MemoryPackOrder(0)]
        public long AddressableId { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableGet_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableGetResponse; }
        [MemoryPackOrder(1)]
        public uint ErrorCode { get; set; }
        [MemoryPackOrder(0)]
        public long RouteId { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableRemove_Request : AMessage, IRouteRequest
    {
        [MemoryPackIgnore]
        public I_AddressableRemove_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableRemoveRequest; }
        public long RouteTypeOpCode() { return 1; }
        [MemoryPackOrder(0)]
        public long AddressableId { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableRemove_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableRemoveResponse; }
        [MemoryPackOrder(0)]
        public uint ErrorCode { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableLock_Request : AMessage, IRouteRequest
    {
        [MemoryPackIgnore]
        public I_AddressableLock_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableLockRequest; }
        public long RouteTypeOpCode() { return 1; }
        [MemoryPackOrder(0)]
        public long AddressableId { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableLock_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableLockResponse; }
        [MemoryPackOrder(0)]
        public uint ErrorCode { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableUnLock_Request : AMessage, IRouteRequest
    {
        [MemoryPackIgnore]
        public I_AddressableUnLock_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableUnLockRequest; }
        public long RouteTypeOpCode() { return 1; }
        [MemoryPackOrder(0)]
        public long AddressableId { get; set; }
        [MemoryPackOrder(1)]
        public long RouteId { get; set; }
        [MemoryPackOrder(2)]
        public string Source { get; set; }
    }
    [MemoryPackable]
    public partial class I_AddressableUnLock_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.Network.OpCode.AddressableUnLockResponse; }
        [MemoryPackOrder(0)]
        public uint ErrorCode { get; set; }
    }
    [MemoryPackable]
    public partial class LinkEntity_Request : AMessage, IRouteRequest
    {
        public uint OpCode() { return Fantasy.Network.OpCode.LinkEntityRequest; }
        public long RouteTypeOpCode() { return 1; }
        [MemoryPackOrder(0)]
        public int EntityType { get; set; }
        [MemoryPackOrder(1)]
        public long RuntimeId { get; set; }
        [MemoryPackOrder(2)]
        public long LinkGateSessionRuntimeId { get; set; }
    }
    [MemoryPackable]
    public partial class LinkEntity_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.Network.OpCode.LinkEntityResponse; }
        [MemoryPackOrder(0)]
        public uint ErrorCode { get; set; }
    }
}