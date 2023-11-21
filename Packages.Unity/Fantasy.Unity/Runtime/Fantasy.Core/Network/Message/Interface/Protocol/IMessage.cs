namespace Fantasy
{
    /// <summary>
    /// 表示通用消息接口。
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        uint OpCode();
    }

    /// <summary>
    /// 表示请求消息接口。
    /// </summary>
    public interface IRequest : IMessage
    {
        
    }

    /// <summary>
    /// 表示响应消息接口。
    /// </summary>
    public interface IResponse : IMessage
    {
        /// <summary>
        /// 获取或设置错误代码。
        /// </summary>
        uint ErrorCode { get; set; }
    }
}