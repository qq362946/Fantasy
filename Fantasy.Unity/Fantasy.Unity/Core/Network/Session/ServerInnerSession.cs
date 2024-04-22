#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 网络服务器内部会话。
/// </summary>
public sealed class ServerInnerSession : Session
{
    /// <summary>
    /// 发送消息到服务器内部。
    /// </summary>
    /// <param name="message">要发送的消息。</param>
    /// <param name="rpcId">RPC 标识符。</param>
    /// <param name="routeId">路由标识符。</param>
    public override void Send(object message, uint rpcId = 0, long routeId = 0)
    {
        if (IsDisposed)
        {
            return;
        }
        
        NetworkMessageScheduler.InnerScheduler(this, rpcId, routeId, ((IMessage)message).OpCode(), 0, message).Coroutine();
    }

    /// <summary>
    /// 发送路由消息到服务器内部。
    /// </summary>
    /// <param name="routeMessage">要发送的路由消息。</param>
    /// <param name="rpcId">RPC 标识符。</param>
    /// <param name="routeId">路由标识符。</param>
    public override void Send(IRouteMessage routeMessage, uint rpcId = 0, long routeId = 0)
    {
        if (IsDisposed)
        {
            return;
        }

        NetworkMessageScheduler.InnerScheduler(this, rpcId, routeId, routeMessage.OpCode(), routeMessage.RouteTypeOpCode(), routeMessage).Coroutine();
    }

    /// <summary>
    /// 发送内存流到服务器内部（不支持）。
    /// </summary>
    /// <param name="memoryStream">要发送的内存流。</param>
    /// <param name="rpcId">RPC 标识符。</param>
    /// <param name="routeTypeOpCode">路由类型和操作码。</param>
    /// <param name="routeId">路由标识符。</param>
    public override void Send(MemoryStream memoryStream, uint rpcId = 0, long routeTypeOpCode = 0, long routeId = 0)
    {
        throw new Exception("The use of this method is not supported");
    }

    /// <summary>
    /// 调用请求并等待响应（不支持）。
    /// </summary>
    /// <param name="request">要调用的请求。</param>
    /// <param name="routeId">路由标识符。</param>
    /// <returns>一个代表异步操作的任务，返回响应。</returns>
    public override FTask<IResponse> Call(IRequest request, long routeId = 0)
    {
        throw new Exception("The use of this method is not supported");
    }
}
#endif