using MessagePack;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    [MessagePackObject]
    public sealed class Response : AMessage, IResponse
    {
        public uint OpCode()
        {
            return Fantasy.OpCode.DefaultResponse;
        }
        [Key(0)] public long RpcId { get; set; }
        [Key(1)] public uint ErrorCode { get; set; }
    }
    [MessagePackObject]
    public sealed class RouteResponse : AMessage, IRouteResponse
    {
        public uint OpCode()
        {
            return Fantasy.OpCode.DefaultRouteResponse;
        }
        [Key(0)] public long RpcId { get; set; }
        [Key(1)] public uint ErrorCode { get; set; }
    }
    [MessagePackObject]
    public class PingRequest : AMessage, IRequest
    {
        public uint OpCode()
        {
            return Fantasy.OpCode.PingRequest;
        }
        [IgnoreMember] public PingResponse ResponseType { get; set; }
        [Key(0)] public long RpcId { get; set; }
    }
    
    [MessagePackObject]
    public class PingResponse : AMessage, IResponse
    {
        public uint OpCode()
        {
            return Fantasy.OpCode.PingResponse;
        }
        [Key(0)] public long RpcId { get; set; }
        [Key(2)] public uint ErrorCode { get; set; }
        [Key(1)] public long Now;
    }
    [MessagePackObject]
    public partial class I_AddressableAdd_Request : AMessage, IRouteRequest
    {
        [IgnoreMember]
        public I_AddressableAdd_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.OpCode.AddressableAddRequest; }
        public long RouteTypeOpCode() { return 1; }
        [Key(1)]
        public long AddressableId { get; set; }
        [Key(2)]
        public long RouteId { get; set; }
        [Key(3)] 
        public bool IsLock { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableAdd_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.OpCode.AddressableAddResponse; }
        [Key(0)]
        public uint ErrorCode { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableGet_Request : AMessage, IRouteRequest
    {
        [IgnoreMember]
        public I_AddressableGet_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.OpCode.AddressableGetRequest; }
        public long RouteTypeOpCode() { return 1; }
        [Key(0)]
        public long AddressableId { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableGet_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.OpCode.AddressableGetResponse; }
        [Key(1)]
        public uint ErrorCode { get; set; }
        [Key(0)]
        public long RouteId { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableRemove_Request : AMessage, IRouteRequest
    {
        [IgnoreMember]
        public I_AddressableRemove_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.OpCode.AddressableRemoveRequest; }
        public long RouteTypeOpCode() { return 1; }
        [Key(0)]
        public long AddressableId { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableRemove_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.OpCode.AddressableRemoveResponse; }
        [Key(0)]
        public uint ErrorCode { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableLock_Request : AMessage, IRouteRequest
    {
        [IgnoreMember]
        public I_AddressableLock_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.OpCode.AddressableLockRequest; }
        public long RouteTypeOpCode() { return 1; }
        [Key(0)]
        public long AddressableId { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableLock_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.OpCode.AddressableLockResponse; }
        [Key(0)]
        public uint ErrorCode { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableUnLock_Request : AMessage, IRouteRequest
    {
        [IgnoreMember]
        public I_AddressableUnLock_Response ResponseType { get; set; }
        public uint OpCode() { return Fantasy.OpCode.AddressableUnLockRequest; }
        public long RouteTypeOpCode() { return 1; }
        [Key(0)]
        public long AddressableId { get; set; }
        [Key(1)]
        public long RouteId { get; set; }
        [Key(2)]
        public string Source { get; set; }
    }
    [MessagePackObject]
    public partial class I_AddressableUnLock_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.OpCode.AddressableUnLockResponse; }
        [Key(0)]
        public uint ErrorCode { get; set; }
    }
    [MessagePackObject]
    public class LinkEntity_Request : AMessage, IRouteRequest
    {
        public uint OpCode() { return Fantasy.OpCode.LinkEntityRequest; }
        public long RouteTypeOpCode() { return 1; }
        [Key(0)]
        public int EntityType { get; set; }
        [Key(1)]
        public long RuntimeId { get; set; }
        [Key(2)]
        public long LinkGateSessionRuntimeId { get; set; }
    }
    [MessagePackObject]
    public partial class LinkEntity_Response : AMessage, IRouteResponse
    {
        public uint OpCode() { return Fantasy.OpCode.LinkEntityResponse; }
        [Key(0)]
        public uint ErrorCode { get; set; }
    }
}