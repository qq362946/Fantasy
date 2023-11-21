
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8603

#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 消息辅助类，用于处理网络消息的发送和接收。
/// </summary>
public static class MessageHelper
{
    private static uint _rpcId;
    /// <summary>
    /// 超时时间（毫秒）。
    /// </summary>
    public const long Timeout = 40000;
    /// <summary>
    /// 存储请求回调的字典。
    /// </summary>
    public static readonly SortedDictionary<uint, MessageSender> RequestCallback = new();
    /// <summary>
    /// 存储超时路由消息发送者的字典。
    /// </summary>
    public static readonly Dictionary<uint, MessageSender> TimeoutRouteMessageSenders = new();
    
    /// <summary>
    /// 定时检查过期的Call消息事件。
    /// </summary>
    public struct NetworkMessageUpdate { }

    static MessageHelper()
    {
        // 每隔一段时间执行网络消息更新
        TimerScheduler.Instance.Core.RepeatedTimer(10000, new NetworkMessageUpdate());
    }

    /// <summary>
    /// 将消息发送给内部服务器。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="routeId">路由ID</param>
    /// <param name="message">消息</param>
    public static void SendInnerServer(Scene scene, uint routeId, IMessage message)
    {
        scene.Server.GetSession(routeId).Send(message);
    }

    /// <summary>
    /// 将消息发送给内部路由。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="message">消息</param>
    public static void SendInnerRoute(Scene scene, long entityId, IRouteMessage message)
    {
        if (entityId == 0)
        {
            Log.Error($"SendInnerRoute appId == 0");
            return;
        }

        EntityIdStruct entityIdStruct = entityId;
        var session = scene.Server.GetSession(entityIdStruct.LocationId);
        session.Send(message, 0, entityId);
    }

    /// <summary>
    /// 将消息发送给内部路由，并指定路由类型操作码和消息数据流。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="routeTypeOpCode">路由类型操作码</param>
    /// <param name="message">消息数据流</param>
    public static void SendInnerRoute(Scene scene, long entityId, long routeTypeOpCode, MemoryStream message)
    {
        if (entityId == 0)
        {
            Log.Error($"SendInnerRoute appId == 0");
            return;
        }

        EntityIdStruct entityIdStruct = entityId;
        var session = scene.Server.GetSession(entityIdStruct.LocationId);
        session.Send(message, 0, routeTypeOpCode, entityId);
    }

    /// <summary>
    /// 将消息发送给一组内部路由。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="routeIdCollection">路由ID集合</param>
    /// <param name="message">消息</param>
    public static void SendInnerRoute(Scene scene, ICollection<long> routeIdCollection, IRouteMessage message)
    {
        if (routeIdCollection.Count <= 0)
        {
            Log.Error($"SendInnerRoute routeId.Count <= 0");
            return;
        }

        foreach (var routeId in routeIdCollection)
        {
            SendInnerRoute(scene, routeId, message);
        }
    }

    /// <summary>
    /// 将消息发送给可寻址对象，并在协程中执行。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="addressableId">可寻址对象ID</param>
    /// <param name="message">消息</param>
    public static void SendAddressable(Scene scene, long addressableId, IRouteMessage message)
    {
        // 调用可寻址组件发送消息并在协程中执行
        CallAddressable(scene, addressableId, message).Coroutine();
    }

    /// <summary>
    /// 异步调用内部路由，并指定路由类型操作码、请求类型和请求数据流。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="routeTypeOpCode">路由类型操作码</param>
    /// <param name="requestType">请求类型</param>
    /// <param name="request">请求数据流</param>
    /// <returns>异步任务，返回响应</returns>
    public static async FTask<IResponse> CallInnerRoute(Scene scene, long entityId, long routeTypeOpCode, Type requestType, MemoryStream request)
    {
        if (entityId == 0)
        {
            Log.Error($"CallInnerRoute appId == 0");
            return null;
        }

        EntityIdStruct entityIdStruct = entityId;
        var rpcId = ++_rpcId;
        var session = scene.Server.GetSession(entityIdStruct.LocationId);
        var requestCallback = FTask<IResponse>.Create(false);
        RequestCallback.Add(rpcId, MessageSender.Create(rpcId, requestType, requestCallback));
        session.Send(request, rpcId, routeTypeOpCode, entityId);
        return await requestCallback;
    }
    
    /// <summary>
    /// 异步调用内部路由，并传递路由消息。
    /// </summary>
    /// <param name="server">内部网络Server，可通过Scene.Server获得</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="request">路由消息</param>
    /// <returns></returns>
    public static async FTask<IResponse> CallInnerRoute(Server server, long entityId, IRouteMessage request)
    {
        if (entityId == 0)
        {
            Log.Error($"CallInnerRoute appId == 0");
            return null;
        }
        
        EntityIdStruct entityIdStruct = entityId;
        var rpcId = ++_rpcId;
        var session = server.GetSession(entityIdStruct.LocationId);
        var requestCallback = FTask<IResponse>.Create(false);
        RequestCallback.Add(rpcId, MessageSender.Create(rpcId, request, requestCallback));
        session.Send(request, rpcId, entityId);
        return await requestCallback;
    }

    /// <summary>
    /// 异步调用内部路由，并传递路由消息。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="request">路由消息</param>
    /// <returns>异步任务，返回响应</returns>
    public static async FTask<IResponse> CallInnerRoute(Scene scene, long entityId, IRouteMessage request)
    {
        if (entityId == 0)
        {
            Log.Error($"CallInnerRoute appId == 0");
            return null;
        }
        
        EntityIdStruct entityIdStruct = entityId;
        var rpcId = ++_rpcId;
        var session = scene.Server.GetSession(entityIdStruct.LocationId);
        var requestCallback = FTask<IResponse>.Create(false);
        RequestCallback.Add(rpcId, MessageSender.Create(rpcId, request, requestCallback));
        session.Send(request, rpcId, entityId);
        return await requestCallback;
    }

    /// <summary>
    /// 异步调用内部服务器路由，并传递请求消息。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="targetServerId">目标服务器ID</param>
    /// <param name="request">请求消息</param>
    /// <returns>异步任务，返回响应</returns>
    public static async FTask<IResponse> CallInnerServer(Scene scene, uint targetServerId, IRequest request)
    {
        var rpcId = ++_rpcId;
        var session = scene.Server.GetSession(targetServerId);
        var requestCallback = FTask<IResponse>.Create(false);
        RequestCallback.Add(rpcId, MessageSender.Create(rpcId, request, requestCallback));
        session.Send(request, rpcId);
        return await requestCallback;
    }

    /// <summary>
    /// 异步调用可寻址对象，并传递路由消息。
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="addressableId">可寻址对象ID</param>
    /// <param name="request">路由消息</param>
    /// <returns>异步任务，返回响应</returns>
    public static async FTask<IResponse> CallAddressable(Scene scene, long addressableId, IRouteMessage request)
    {
        var failCount = 0;

        // 使用可寻址消息锁，确保并发请求的同步
        using (await AddressableRouteComponent.AddressableRouteMessageLock.Lock(addressableId,"CallAddressable"))
        {
            var addressableRouteId = await AddressableHelper.GetAddressableRouteId(scene, addressableId);

            while (true)
            {
                // 如果找不到可寻址的路由 ID，尝试获取一次
                if (addressableRouteId == 0)
                {
                    addressableRouteId = await AddressableHelper.GetAddressableRouteId(scene, addressableId);
                }
                // 如果找不到可寻址的路由 ID，则返回错误响应
                if (addressableRouteId == 0)
                {
                    return MessageDispatcherSystem.Instance.CreateResponse(request, CoreErrorCode.ErrNotFoundRoute);
                }

                // 调用内部路由方法，发送消息并等待响应
                var iRouteResponse = await MessageHelper.CallInnerRoute(scene, addressableRouteId, request);

                // 根据响应中的错误码进行处理
                switch (iRouteResponse.ErrorCode)
                {
                    case CoreErrorCode.ErrNotFoundRoute:
                    {
                        // 如果连续失败次数超过阈值，记录错误并返回响应
                        if (++failCount > 20)
                        {
                            Log.Error($"AddressableComponent.Call failCount > 20 route send message fail, routeId: {addressableRouteId} AddressableMessageComponent:{addressableId}");
                            return iRouteResponse;
                        }

                        // 等待一段时间后重试
                        await TimerScheduler.Instance.Core.WaitAsync(500);
                        addressableRouteId = 0;
                        continue;
                    }
                    case CoreErrorCode.ErrRouteTimeout:
                    {
                        // 如果响应为路由超时错误，记录错误并返回响应
                        Log.Error($"CallAddressableRoute ErrorCode.ErrRouteTimeout Error:{iRouteResponse.ErrorCode} Message:{request}");
                        return iRouteResponse;
                    }
                    default:
                    {
                        // 其他错误码或正常响应，直接返回响应
                        return iRouteResponse;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 连接Entity到目标进程、目标进程可以通过EntityType、发送消息给这个Entity
    /// </summary>
    /// <param name="entity">要进行连接的Entity</param>
    /// <param name="addressableId">addressableId</param>
    /// <param name="entityType">设置连接的Entity的EntityType</param>
    /// <returns></returns>
    public static async FTask<bool> Link(Entity entity, int entityType, long addressableId)
    {
        var response = (LinkEntity_Response)await CallAddressable(entity.Scene, addressableId,
            new LinkEntity_Request()
            {
                EntityType = entityType, RuntimeId = entity.RuntimeId
            });

        if (response.ErrorCode == 0)
        {
            return true;
        }

        Log.Error($"Link error code:{response.ErrorCode}");
        return false;
    }

    /// <summary>
    /// 连接GateSession到目标进程中、连接成功后可以通过SendToClient给客户端发送消息
    /// </summary>
    /// <param name="session"></param>
    /// <param name="addressableId"></param>
    /// <returns></returns>
    public static async FTask<bool> LinkClient(Session session, long addressableId)
    {
        var response = (LinkEntity_Response)await CallAddressable(session.Scene, addressableId,
            new LinkEntity_Request()
            {
                LinkGateSessionRuntimeId = session.RuntimeId
            });

        if (response.ErrorCode == 0)
        {
            return true;
        }

        Log.Error($"Link error code:{response.ErrorCode}");
        return false;
    }

    /// <summary>
    /// 发送消息给客户端、如果是Gate进程请不要使用这个发送、请使用Session.Send发送。
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="message"></param>
    public static void SendToClient(Entity entity, IRouteMessage message)
    {
        var linkEntityComponent = entity.GetComponent<LinkEntityComponent>();

        if (linkEntityComponent == null)
        {
            Log.Error($"SendToClient not found  LinkEntityComponent or LinkEntityComponent.LinkGateSessionRuntimeId");
        }

        SendInnerRoute(entity.Scene, linkEntityComponent.LinkGateSessionRuntimeId, message);
    }

    /// <summary>
    /// 处理响应消息的方法。
    /// </summary>
    /// <param name="rpcId">RPC ID</param>
    /// <param name="response">响应消息</param>
    public static void ResponseHandler(uint rpcId, IResponse response)
    {
        if (!RequestCallback.Remove(rpcId, out var routeMessageSender))
        {
            throw new Exception($"not found rpc, response.RpcId:{rpcId} response message: {response.GetType().Name}");
        }

        ResponseHandler(routeMessageSender, response);
    }

    /// <summary>
    /// 处理响应消息的私有方法。
    /// </summary>
    /// <param name="messageSender">消息发送者</param>
    /// <param name="response">响应消息</param>
    private static void ResponseHandler(MessageSender messageSender, IResponse response)
    {
        if (response.ErrorCode == CoreErrorCode.ErrRouteTimeout)
        {
#if FANTASY_DEVELOP
            messageSender.Tcs.SetException(new Exception($"Rpc error: request, 注意RouteId消息超时，请注意查看是否死锁或者没有reply: RouteId: {messageSender.RouteId} {messageSender.Request.ToJson()}, response: {response}"));
#else
            messageSender.Tcs.SetException(new Exception($"Rpc error: request, 注意RouteId消息超时，请注意查看是否死锁或者没有reply: RouteId: {messageSender.RouteId} {messageSender.Request}, response: {response}"));
#endif
            messageSender.Dispose();
            return;
        }

        messageSender.Tcs.SetResult(response);
        messageSender.Dispose();
    }
}

#endif