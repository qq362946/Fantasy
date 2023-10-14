namespace Fantasy.Core
{
    /// <summary>
    /// 定义 Fantasy 框架中的核心错误代码。
    /// </summary>
    public class CoreErrorCode
    {
        /// <summary>
        /// 表示 Rpc 消息发送失败的错误代码。
        /// </summary>
        public const uint ErrRpcFail = 100000002; 

        /// <summary>
        /// 表示未找到 Route 消息的错误代码。
        /// </summary>
        public const uint ErrNotFoundRoute = 100000003; 

        /// <summary>
        /// 表示发送 Route 消息超时的错误代码。
        /// </summary>
        public const uint ErrRouteTimeout = 100000004; 

        /// <summary>
        /// 表示未找到实体的错误代码。
        /// </summary>
        public const uint Error_NotFindEntity = 100000008; 

        /// <summary>
        /// 表示传送过程中发生错误的错误代码。
        /// </summary>
        public const uint Error_Transfer = 100000009;
    }
}
