namespace Fantasy
{
    /// <summary>
    /// 表示可以序列化为 BSON 格式的消息接口。
    /// </summary>
    public interface IBsonMessage : IMessage
    {
    
    }

    /// <summary>
    /// 表示可以序列化为 BSON 格式的请求消息接口。
    /// </summary>
    public interface IBsonRequest : IBsonMessage, IRequest
    {
        
    }

    /// <summary>
    /// 表示可以序列化为 BSON 格式的响应消息接口。
    /// </summary>
    public interface IBsonResponse : IBsonMessage, IResponse
    {
        
    }
}