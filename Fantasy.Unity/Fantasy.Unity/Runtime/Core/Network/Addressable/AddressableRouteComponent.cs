#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if FANTASY_NET
namespace Fantasy;

public class AddressableRouteComponentAwakeSystem : AwakeSystem<AddressableRouteComponent>
{
    protected override void Awake(AddressableRouteComponent self)
    {
        var selfScene = self.Scene;
        self.TimerComponent = selfScene.TimerComponent;
        self.NetworkMessagingComponent = selfScene.NetworkMessagingComponent;
        self.MessageDispatcherComponent = selfScene.MessageDispatcherComponent;
        self.AddressableRouteLock = selfScene.CoroutineLockComponent.Create(self.GetType().TypeHandle.Value.ToInt64());
    }
}

public class AddressableRouteComponentDestroySystem : DestroySystem<AddressableRouteComponent>
{
    protected override void Destroy(AddressableRouteComponent self)
    {
        self.AddressableRouteLock.Dispose();

        self.RouteId = 0;
        self.AddressableId = 0;
        self.TimerComponent = null;
        self.AddressableRouteLock = null;
        self.NetworkMessagingComponent = null;
        self.MessageDispatcherComponent = null;
    }
}

/// <summary>
/// 可寻址路由消息组件，挂载了这个组件可以接收和发送 Addressable 消息。
/// </summary>
public sealed class AddressableRouteComponent : Entity
{
    /// <summary>
    /// 用于存储当前可寻址路由消息的路由 ID
    /// </summary>
    public long RouteId;
    /// <summary>
    /// 可寻址路由消息组件的地址映射 ID，只可在类内部设置，外部只可读取
    /// </summary>
    public long AddressableId;
    
    /// <summary>
    /// 用于管理 Addressable 路由消息的锁队列。
    /// </summary>
    public CoroutineLock AddressableRouteLock;
    /// <summary>
    /// 任务调度器组件。
    /// </summary>
    public TimerComponent TimerComponent;
    /// <summary>
    /// 网络消息组件。
    /// </summary>
    public NetworkMessagingComponent NetworkMessagingComponent;
    /// <summary>
    /// 网络消息分发组件。
    /// </summary>
    public MessageDispatcherComponent MessageDispatcherComponent;
    
    /// <summary>
    /// 发送可寻址路由消息。
    /// </summary>
    /// <param name="message">可寻址路由消息。</param>
    public void Send(IAddressableRouteMessage message)
    {
        Call(message).Coroutine();
    }
    
    /// <summary>
    /// 发送可寻址路由消息。
    /// </summary>
    /// <param name="routeTypeOpCode">路由类型操作码。</param>
    /// <param name="requestType">请求类型。</param>
    /// <param name="message">消息数据。</param>
    public void Send(long routeTypeOpCode, Type requestType, MemoryStream message)
    {
        Call(routeTypeOpCode, requestType, message).Coroutine();
    }
    
    /// <summary>
    /// 调用可寻址路由消息并等待响应。
    /// </summary>
    /// <param name="routeTypeOpCode">路由类型操作码。</param>
    /// <param name="requestType">请求类型。</param>
    /// <param name="request">请求数据。</param>
    public async FTask<IResponse> Call(long routeTypeOpCode, Type requestType, MemoryStream request)
    {
        // 如果组件已被释放，则创建一个带有错误代码的响应，表示路由未找到
        if (IsDisposed)
        {
            return MessageDispatcherComponent.CreateResponse(requestType, InnerErrorCode.ErrNotFoundRoute);
        }

        var failCount = 0; // 用于计算失败尝试次数
        var runtimeId = RunTimeId; // 保存当前运行时 ID，用于判断是否超时
        IResponse iRouteResponse = null;

        // 使用锁来确保同一时间只有一个线程可以访问 AddressableId 和 _routeId
        using (await AddressableRouteLock.Wait(AddressableId, "AddressableRouteComponent Call MemoryStream"))
        {
            while (!IsDisposed)
            {
                if (RouteId == 0)
                {
                    RouteId = await AddressableHelper.GetAddressableRouteId(Scene, AddressableId);
                }

                if (RouteId == 0)
                {
                    return MessageDispatcherComponent.CreateResponse(requestType, InnerErrorCode.ErrNotFoundRoute);
                }

                iRouteResponse = await NetworkMessagingComponent.CallInnerRoute(RouteId, routeTypeOpCode, requestType, request);

                // 如果当前运行时 ID 不等于保存的运行时 ID，说明超时
                if (runtimeId != RunTimeId)
                {
                    iRouteResponse.ErrorCode = InnerErrorCode.ErrRouteTimeout;
                }

                switch (iRouteResponse.ErrorCode)
                {
                    case InnerErrorCode.ErrRouteTimeout:
                    {
                        return iRouteResponse;
                    }
                    case InnerErrorCode.ErrNotFoundRoute:
                    {
                        if (++failCount > 20)
                        {
                            Log.Error($"AddressableComponent.Call failCount > 20 route send message fail, routeId: {RouteId} AddressableRouteComponent:{Id}");
                            return iRouteResponse;
                        }

                        await TimerComponent.Net.WaitAsync(500);

                        if (runtimeId != RunTimeId)
                        {
                            iRouteResponse.ErrorCode = InnerErrorCode.ErrRouteTimeout;
                        }

                        RouteId = 0; // 重置路由 ID，以便下次重新获取
                        continue; // 继续下一次循环，重试发送消息
                    }
                    default:
                    {
                        return iRouteResponse; // 对于其他情况，直接返回响应，无需额外处理
                    }
                }
            }
        }

        return iRouteResponse;
    }
    
    /// <summary>
    /// 调用可寻址路由消息并等待响应。
    /// </summary>
    /// <param name="request">可寻址路由请求。</param>
    public async FTask<IResponse> Call(IAddressableRouteMessage request)
    {
        if (IsDisposed)
        {
            return MessageDispatcherComponent.CreateResponse(request, InnerErrorCode.ErrNotFoundRoute);
        }

        var failCount = 0;
        var runtimeId = RunTimeId;

        using (await AddressableRouteLock.Wait(AddressableId,"AddressableRouteComponent Call"))
        {
            while (true)
            {
                if (RouteId == 0)
                {
                    RouteId = await AddressableHelper.GetAddressableRouteId(Scene, AddressableId);
                }

                if (RouteId == 0)
                {
                    return MessageDispatcherComponent.CreateResponse(request, InnerErrorCode.ErrNotFoundRoute);
                }

                var iRouteResponse = await NetworkMessagingComponent.CallInnerRoute(RouteId, request);

                if (runtimeId != RunTimeId)
                {
                    iRouteResponse.ErrorCode = InnerErrorCode.ErrRouteTimeout;
                }

                switch (iRouteResponse.ErrorCode)
                {
                    case InnerErrorCode.ErrNotFoundRoute:
                    {
                        if (++failCount > 20)
                        {
                            Log.Error($"AddressableRouteComponent.Call failCount > 20 route send message fail, routeId: {RouteId} AddressableRouteComponent:{Id}");
                            return iRouteResponse;
                        }

                        await TimerComponent.Net.WaitAsync(500);

                        if (runtimeId != RunTimeId)
                        {
                            iRouteResponse.ErrorCode = InnerErrorCode.ErrRouteTimeout;
                        }

                        RouteId = 0;
                        continue;
                    }
                    case InnerErrorCode.ErrRouteTimeout:
                    {
                        return iRouteResponse;
                    }
                    default:
                    {
                        return iRouteResponse;
                    }
                }
            }
        }
    }
}
#endif
