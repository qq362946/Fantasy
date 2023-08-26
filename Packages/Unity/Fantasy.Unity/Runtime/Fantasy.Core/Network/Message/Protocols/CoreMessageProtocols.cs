using Fantasy.Core.Network;
using ProtoBuf;

namespace Fantasy
{
    /// <summary>
    /// 表示响应消息的基类，实现了 <see cref="IResponse"/> 接口。
    /// </summary>
    [ProtoContract]
    public sealed class Response : AProto, IResponse
    {
        /// <summary>
        /// 获取当前消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode()
        {
            return Opcode.DefaultResponse;
        }

        /// <summary>
        /// 获取或设置RPC标识。
        /// </summary>
        [ProtoMember(90)] public long RpcId { get; set; }
        /// <summary>
        /// 获取或设置错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)] public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 表示路由响应消息的类，实现了 <see cref="IRouteResponse"/> 接口。
    /// </summary>
    [ProtoContract]
    public sealed class RouteResponse : AProto, IRouteResponse
    {
        /// <summary>
        /// 获取当前消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode()
        {
            return Opcode.DefaultRouteResponse;
        }
        /// <summary>
        /// 获取或设置RPC标识。
        /// </summary>
        [ProtoMember(90)] public long RpcId { get; set; }
        /// <summary>
        /// 获取或设置错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)] public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 表示Ping请求消息的类，实现了 <see cref="IRequest"/> 接口。
    /// </summary>
    [ProtoContract]
    public class PingRequest : AProto, IRequest
    {
        /// <summary>
        /// 获取当前消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode()
        {
            return Opcode.PingRequest;
        }
        /// <summary>
        /// 获取或设置Ping响应类型。
        /// </summary>
        [ProtoIgnore] public PingResponse ResponseType { get; set; }
        /// <summary>
        /// 获取或设置RPC标识。
        /// </summary>
        [ProtoMember(90)] public long RpcId { get; set; }
    }
    /// <summary>
    /// 表示Ping响应消息的类，实现了 <see cref="IResponse"/> 接口。
    /// </summary>
    [ProtoContract]
    public class PingResponse : AProto, IResponse
    {
        /// <summary>
        /// 获取当前消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode()
        {
            return Opcode.PingResponse;
        }
        /// <summary>
        /// 获取或设置RPC标识。
        /// </summary>
        [ProtoMember(90)] public long RpcId { get; set; }
        /// <summary>
        /// 获取或设置错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)] public uint ErrorCode { get; set; }
        /// <summary>
        /// 获取或设置时间戳。
        /// </summary>
        [ProtoMember(1)] public long Now;
    }
    /// <summary>
    /// 添加一个可寻址地址请求
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableAdd_Request : AProto, IRouteRequest
    {
        /// <summary>
        /// 获取或设置响应类型。
        /// </summary>
        [ProtoIgnore]
        public I_AddressableAdd_Response ResponseType { get; set; }
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableAddRequest; }
        /// <summary>
        /// 获取消息的路由类型操作代码。
        /// </summary>
        /// <returns>路由类型操作代码。</returns>
        public long RouteTypeOpCode() { return 1; }
        /// <summary>
        /// 可寻址地址的标识。
        /// </summary>
        [ProtoMember(1)]
        public long AddressableId { get; set; }
        /// <summary>
        /// 路由的标识。
        /// </summary>
        [ProtoMember(2)]
        public long RouteId { get; set; }
        /// <summary>
        /// 是否锁定可寻址。
        /// </summary>
        [ProtoMember(3)] 
        public bool IsLock { get; set; }
    }
    /// <summary>
    /// 添加一个可寻址地址响应
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableAdd_Response : AProto, IRouteResponse
    {
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableAddResponse; }
        /// <summary>
        /// 错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 查询一个可寻址请求
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableGet_Request : AProto, IRouteRequest
    {
        /// <summary>
        /// 获取或设置响应类型。
        /// </summary>
        [ProtoIgnore]
        public I_AddressableGet_Response ResponseType { get; set; }
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableGetRequest; }
        /// <summary>
        /// 获取消息的路由类型操作代码。
        /// </summary>
        /// <returns>路由类型操作代码。</returns>
        public long RouteTypeOpCode() { return 1; }
        /// <summary>
        /// 可寻址地址的标识。
        /// </summary>
        [ProtoMember(1)]
        public long AddressableId { get; set; }
    }
    /// <summary>
    /// 查询一个可寻址响应
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableGet_Response : AProto, IRouteResponse
    {
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableGetResponse; }
        /// <summary>
        /// 错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 路由的标识。
        /// </summary>
        [ProtoMember(1)]
        public long RouteId { get; set; }
    }
    /// <summary>
    /// 删除一个可寻址请求
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableRemove_Request : AProto, IRouteRequest
    {
        /// <summary>
        /// 获取或设置响应类型。
        /// </summary>
        [ProtoIgnore]
        public I_AddressableRemove_Response ResponseType { get; set; }
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableRemoveRequest; }
        /// <summary>
        /// 获取消息的路由类型操作代码。
        /// </summary>
        /// <returns>路由类型操作代码。</returns>
        public long RouteTypeOpCode() { return 1; }
        /// <summary>
        /// 可寻址地址的标识。
        /// </summary>
        [ProtoMember(1)]
        public long AddressableId { get; set; }
    }
    /// <summary>
    /// 删除一个可寻址响应
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableRemove_Response : AProto, IRouteResponse
    {
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableRemoveResponse; }
        /// <summary>
        /// 错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 锁定一个可寻址请求。
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableLock_Request : AProto, IRouteRequest
    {
        /// <summary>
        /// 获取或设置响应类型。
        /// </summary>
        [ProtoIgnore]
        public I_AddressableLock_Response ResponseType { get; set; }
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableLockRequest; }
        /// <summary>
    /// 获取消息的路由类型操作代码。
    /// </summary>
    /// <returns>路由类型操作代码。</returns>
        public long RouteTypeOpCode() { return 1; }
        /// <summary>
        /// 可寻址地址的标识。
        /// </summary>
        [ProtoMember(1)]
        public long AddressableId { get; set; }
    }
    /// <summary>
    /// 锁定一个可寻址响应。
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableLock_Response : AProto, IRouteResponse
    {
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableLockResponse; }
        /// <summary>
        /// 获取或设置错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 解锁一个可寻址地址请求。
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableUnLock_Request : AProto, IRouteRequest
    {
        /// <summary>
        /// 获取或设置响应类型。
        /// </summary>
        [ProtoIgnore]
        public I_AddressableUnLock_Response ResponseType { get; set; }
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableUnLockRequest; }
        /// <summary>
        /// 获取消息的路由类型操作代码。
        /// </summary>
        /// <returns>路由类型操作代码。</returns>
        public long RouteTypeOpCode() { return 1; }
        /// <summary>
        /// 可寻址地址的标识。
        /// </summary>
        [ProtoMember(1)]
        public long AddressableId { get; set; }
        /// <summary>
        /// 路由的标识。
        /// </summary>
        [ProtoMember(2)]
        public long RouteId { get; set; }
        /// <summary>
        /// 请求解锁的源。
        /// </summary>
        [ProtoMember(3)]
        public string Source { get; set; }
    }
    /// <summary>
    /// 解锁一个可寻址地址响应。
    /// </summary>
    [ProtoContract]
    public partial class I_AddressableUnLock_Response : AProto, IRouteResponse
    {
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        public uint OpCode() { return Opcode.AddressableUnLockResponse; }
        /// <summary>
        /// 获取或设置错误代码。
        /// </summary>
        [ProtoMember(91, IsRequired = true)]
        public uint ErrorCode { get; set; }
    }
}