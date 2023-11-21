
#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 可寻址路由消息组件，挂载了这个组件可以接收和发送 Addressable 消息。
/// </summary>
public sealed class AddressableRouteComponent : Entity
{
    private long _routeId; // 用于存储当前可寻址路由消息的路由 ID
    /// <summary>
    /// 可寻址路由消息组件的地址映射 ID，只可在类内部设置，外部只可读取
    /// </summary>
    public long AddressableId { get; private set; }

    /// <summary>
    /// 用于管理 Addressable 路由消息的锁队列。
    /// </summary>
    public static readonly CoroutineLockQueueType AddressableRouteMessageLock = new CoroutineLockQueueType("AddressableRouteMessageLock");

    /// <summary>
    /// 释放资源并重置路由和地址映射。
    /// </summary>
    public override void Dispose()
    {
        _routeId = 0;
        AddressableId = 0;
        base.Dispose();
    }

    /// <summary>
    /// 设置可寻址路由消息组件的地址映射 ID。
    /// </summary>
    /// <param name="addressableId">地址映射 ID。</param>
    public void SetAddressableId(long addressableId)
    {
        AddressableId = addressableId;
    }

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
            return MessageDispatcherSystem.Instance.CreateResponse(requestType, CoreErrorCode.ErrNotFoundRoute);
        }

        var failCount = 0; // 用于计算失败尝试次数
        var runtimeId = RuntimeId; // 保存当前运行时 ID，用于判断是否超时
        IResponse iRouteResponse = null;

        // 使用锁来确保同一时间只有一个线程可以访问 AddressableId 和 _routeId
        using (await AddressableRouteMessageLock.Lock(AddressableId, "AddressableRouteComponent Call MemoryStream"))
        {
            while (!IsDisposed)
            {
                if (_routeId == 0)
                {
                    _routeId = await AddressableHelper.GetAddressableRouteId(Scene, AddressableId);
                }

                if (_routeId == 0)
                {
                    return MessageDispatcherSystem.Instance.CreateResponse(requestType, CoreErrorCode.ErrNotFoundRoute);
                }

                iRouteResponse = await MessageHelper.CallInnerRoute(Scene, _routeId, routeTypeOpCode, requestType, request);

                // 如果当前运行时 ID 不等于保存的运行时 ID，说明超时
                if (runtimeId != RuntimeId)
                {
                    iRouteResponse.ErrorCode = CoreErrorCode.ErrRouteTimeout;
                }

                switch (iRouteResponse.ErrorCode)
                {
                    case CoreErrorCode.ErrRouteTimeout:
                    {
                        return iRouteResponse;
                    }
                    case CoreErrorCode.ErrNotFoundRoute:
                    {
                        if (++failCount > 20)
                        {
                            Log.Error($"AddressableComponent.Call failCount > 20 route send message fail, routeId: {_routeId} AddressableRouteComponent:{Id}");
                            return iRouteResponse;
                        }

                        await TimerScheduler.Instance.Core.WaitAsync(500);

                        if (runtimeId != RuntimeId)
                        {
                            iRouteResponse.ErrorCode = CoreErrorCode.ErrRouteTimeout;
                        }

                        _routeId = 0; // 重置路由 ID，以便下次重新获取
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
            return MessageDispatcherSystem.Instance.CreateResponse(request, CoreErrorCode.ErrNotFoundRoute);
        }

        var failCount = 0;
        var runtimeId = RuntimeId;

        using (await AddressableRouteMessageLock.Lock(AddressableId,"AddressableRouteComponent Call"))
        {
            while (true)
            {
                if (_routeId == 0)
                {
                    _routeId = await AddressableHelper.GetAddressableRouteId(Scene, AddressableId);
                }

                if (_routeId == 0)
                {
                    return MessageDispatcherSystem.Instance.CreateResponse(request, CoreErrorCode.ErrNotFoundRoute);
                }

                var iRouteResponse = await MessageHelper.CallInnerRoute(Scene, _routeId, request);

                if (runtimeId != RuntimeId)
                {
                    iRouteResponse.ErrorCode = CoreErrorCode.ErrRouteTimeout;
                }

                switch (iRouteResponse.ErrorCode)
                {
                    case CoreErrorCode.ErrNotFoundRoute:
                    {
                        if (++failCount > 20)
                        {
                            Log.Error($"AddressableRouteComponent.Call failCount > 20 route send message fail, routeId: {_routeId} AddressableRouteComponent:{Id}");
                            return iRouteResponse;
                        }

                        await TimerScheduler.Instance.Core.WaitAsync(500);

                        if (runtimeId != RuntimeId)
                        {
                            iRouteResponse.ErrorCode = CoreErrorCode.ErrRouteTimeout;
                        }

                        _routeId = 0;
                        continue;
                    }
                    case CoreErrorCode.ErrRouteTimeout:
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