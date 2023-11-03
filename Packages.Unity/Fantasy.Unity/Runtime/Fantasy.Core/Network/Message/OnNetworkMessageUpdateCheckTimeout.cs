#if FANTASY_NET
using Fantasy.Helper;

namespace Fantasy.Core.Network;

/// <summary>
/// 定时处理网络消息超时的任务。
/// </summary>
public sealed class OnNetworkMessageUpdateCheckTimeout : TimerHandler<MessageHelper.NetworkMessageUpdate>
{
    /// <summary>
    /// 处理网络消息超时的逻辑。
    /// </summary>
    /// <param name="self">计时器回调参数，不会被使用。</param>
    public override void Handler(MessageHelper.NetworkMessageUpdate self)
    {
        var timeNow = TimeHelper.Now;

        // 遍历请求回调字典，检查是否有超时的请求，将超时请求添加到超时消息发送列表中。
        foreach (var (rpcId, value) in MessageHelper.RequestCallback)
        {
            if (timeNow < value.CreateTime + MessageHelper.Timeout)
            {
                break;
            }

            MessageHelper.TimeoutRouteMessageSenders.Add(rpcId, value);
        }

        // 如果没有超时的请求，直接返回。
        if (MessageHelper.TimeoutRouteMessageSenders.Count == 0)
        {
            return;
        }

        // 处理超时的请求，根据请求类型生成相应的响应消息，并进行处理。
        foreach (var (rpcId, routeMessageSender) in MessageHelper.TimeoutRouteMessageSenders)
        {
            uint responseRpcId = 0;

            try
            {
                switch (routeMessageSender.Request)
                {
                    case IRouteMessage iRouteMessage:
                    {
                        // TODO: 根据路由消息生成响应，并进行处理。
                        // var routeResponse = RouteMessageDispatcher.CreateResponse(iRouteMessage, ErrorCode.ErrRouteTimeout);
                        // responseRpcId = routeResponse.RpcId;
                        // routeResponse.RpcId = routeMessageSender.RpcId;
                        // MessageHelper.ResponseHandler(routeResponse);
                        break;
                    }
                    case IRequest iRequest:
                    {
                        // 根据普通请求生成响应，并进行处理。
                        var response = MessageDispatcherSystem.Instance.CreateResponse(iRequest, CoreErrorCode.ErrRpcFail);
                        responseRpcId = routeMessageSender.RpcId;
                        MessageHelper.ResponseHandler(responseRpcId, response);
                        Log.Warning($"timeout rpcId:{rpcId} responseRpcId:{responseRpcId} {iRequest.ToJson()}");
                        break;
                    }
                    default:
                    {
                        // 处理不支持的请求类型。
                        Log.Error(routeMessageSender.Request != null
                            ? $"Unsupported protocol type {routeMessageSender.Request.GetType()} rpcId:{rpcId}"
                            : $"Unsupported protocol type:{routeMessageSender.MessageType.FullName} rpcId:{rpcId}");

                        MessageHelper.RequestCallback.Remove(rpcId);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"responseRpcId:{responseRpcId} routeMessageSender.RpcId:{routeMessageSender.RpcId} {e}");
            }
        }

        // 清空超时消息发送列表。
        MessageHelper.TimeoutRouteMessageSenders.Clear();
    }
}
#endif