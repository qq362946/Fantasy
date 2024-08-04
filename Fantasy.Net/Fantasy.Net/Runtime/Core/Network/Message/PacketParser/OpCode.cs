#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    /// <summary>
    /// 定义了各种消息操作码，用于标识不同类型的消息和请求。
    /// </summary>
    public static class OpCode
    {
        /// <summary>
        /// 外网消息操作码的基准值。
        /// </summary>
        public const uint OuterMessage = 100000000; 
        /// <summary>
        /// 外网请求操作码的基准值。
        /// </summary>
        public const uint OuterRequest = 110000000;
        /// <summary>
        /// 内网消息操作码的基准值。
        /// </summary>
        public const uint InnerMessage = 120000000;
        /// <summary>
        /// 内网请求操作码的基准值。
        /// </summary>
        public const uint InnerRequest = 130000000;
        /// <summary>
        /// 内网Bson消息操作码的基准值。
        /// </summary>
        public const uint InnerBsonMessage = 140000000;
        /// <summary>
        /// 内网Bson请求操作码的基准值。
        /// </summary>
        public const uint InnerBsonRequest = 150000000;
        /// <summary>
        /// 外网回复操作码的基准值。
        /// </summary>
        public const uint OuterResponse = 160000000;
        /// <summary>
        /// 内网回复操作码的基准值。
        /// </summary>
        public const uint InnerResponse = 170000000;
        /// <summary>
        /// 内网Bson回复操作码的基准值。
        /// </summary>
        public const uint InnerBsonResponse = 180000000;
        /// <summary>
        /// 外网路由消息操作码的基准值。
        /// </summary>
        public const uint OuterRouteMessage = 190000000;
        /// <summary>
        /// 外网路由请求操作码的基准值。
        /// </summary>
        public const uint OuterRouteRequest = 200000000;
        /// <summary>
        /// 内网路由消息操作码的基准值。
        /// </summary>
        public const uint InnerRouteMessage = 210000000;
        /// <summary>
        /// 内网路由请求操作码的基准值。
        /// </summary>
        public const uint InnerRouteRequest = 220000000;
        /// <summary>
        /// 内网Bson路由消息操作码的基准值。
        /// </summary>
        public const uint InnerBsonRouteMessage = 230000000;
        /// <summary>
        /// 内网Bson路由请求操作码的基准值。
        /// </summary>
        public const uint InnerBsonRouteRequest = 240000000;
        /// <summary>
        /// 外网路由回复操作码的基准值。
        /// </summary>
        public const uint OuterRouteResponse = 250000000;
        /// <summary>
        /// 内网路由回复操作码的基准值。
        /// </summary>
        public const uint InnerRouteResponse = 260000000;
        /// <summary>
        /// 内网Bson路由回复操作码的基准值。
        /// </summary>
        public const uint InnerBsonRouteResponse = 270000000;
        /// <summary>
        /// 心跳消息请求操作码。
        /// </summary>
        public const uint PingRequest = 1;
        /// <summary>
        /// 心跳消息回复操作码。
        /// </summary>
        public const uint PingResponse = 2;
        /// <summary>
        /// 默认回复操作码。
        /// </summary>
        public const uint DefaultResponse = 3;
        /// <summary>
        /// 可寻址消息：添加请求操作码。
        /// </summary>
        public const uint AddressableAddRequest = InnerRouteRequest + 1;
        /// <summary>
        /// 可寻址消息：添加回复操作码。
        /// </summary>
        public const uint AddressableAddResponse = InnerRouteResponse + 1;
        /// <summary>
        /// 可寻址消息：获取请求操作码。
        /// </summary>
        public const uint AddressableGetRequest = InnerRouteRequest + 2;
        /// <summary>
        /// 可寻址消息：获取回复操作码。
        /// </summary>
        public const uint AddressableGetResponse = InnerRouteResponse + 2;
        /// <summary>
        /// 可寻址消息：移除请求操作码。
        /// </summary>
        public const uint AddressableRemoveRequest = InnerRouteRequest + 3;
        /// <summary>
        /// 可寻址消息：移除回复操作码。
        /// </summary>
        public const uint AddressableRemoveResponse = InnerRouteResponse + 3;
        /// <summary>
        /// 可寻址消息：锁定请求操作码。
        /// </summary>
        public const uint AddressableLockRequest = InnerRouteRequest + 4;
        /// <summary>
        /// 可寻址消息：锁定回复操作码。
        /// </summary>
        public const uint AddressableLockResponse = InnerRouteResponse + 4;
        /// <summary>
        /// 可寻址消息：解锁请求操作码。
        /// </summary>
        public const uint AddressableUnLockRequest = InnerRouteRequest + 5;
        /// <summary>
        /// 可寻址消息：解锁回复操作码。
        /// </summary>
        public const uint AddressableUnLockResponse = InnerRouteResponse + 5;
        /// <summary>
        /// 连接Entity到目标进程、目标进程可以通过EntityType、发送消息给这个Entity
        /// </summary>
        public const uint LinkEntityRequest = InnerRouteRequest + 6;
        public const uint LinkEntityResponse = InnerRouteResponse + 6;
        /// <summary>
        /// 默认的Route返回操作码。
        /// </summary>
        public const uint DefaultRouteResponse = InnerRouteResponse + 7;
        
    }
}