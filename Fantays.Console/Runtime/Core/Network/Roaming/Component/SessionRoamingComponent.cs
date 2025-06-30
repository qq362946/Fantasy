#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
using Fantasy.PacketParser.Interface;
using Fantasy.Platform.Net;
using Fantasy.Scheduler;
using Fantasy.Timer;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Fantasy.Network.Roaming;

internal sealed class SessionRoamingComponentDestroySystem : DestroySystem<SessionRoamingComponent>
{
    protected override void Destroy(SessionRoamingComponent self)
    {
        self.RoamingLock.Dispose();
        self.RoamingMessageLock.Dispose();
        
        self.RoamingLock = null;
        self.RoamingMessageLock = null;
        self.TimerComponent = null;
        self.NetworkMessagingComponent = null;
        self.MessageDispatcherComponent = null;
    }
}

/// <summary>
/// Session的漫游组件。
/// 用于关联对应的Session的功能。
/// 但这个组件并不会挂载到这个Session下。
/// </summary>
public sealed class SessionRoamingComponent : Entity
{
    internal CoroutineLock RoamingLock;
    internal CoroutineLock RoamingMessageLock;
    internal TimerComponent TimerComponent;
    internal NetworkMessagingComponent NetworkMessagingComponent;
    internal MessageDispatcherComponent MessageDispatcherComponent;
    /// <summary>
    /// 漫游的列表。
    /// </summary>
    private readonly Dictionary<int, Roaming> _roaming = new Dictionary<int, Roaming>();

    internal void Initialize(Session session)
    {
        session.SessionRoamingComponent = this;
        
        var scene = session.Scene;
        TimerComponent = scene.TimerComponent;
        NetworkMessagingComponent = scene.NetworkMessagingComponent;
        MessageDispatcherComponent = scene.MessageDispatcherComponent;
        RoamingLock = scene.CoroutineLockComponent.Create(this.GetType().TypeHandle.Value.ToInt64());
        RoamingMessageLock = scene.CoroutineLockComponent.Create(this.GetType().TypeHandle.Value.ToInt64());
    }
    
    #region Get

    /// <summary>
    /// 尝试获取一个漫游。
    /// </summary>
    /// <param name="roamingType"></param>
    /// <param name="roaming"></param>
    /// <returns></returns>
    public bool TryGetRoaming(int roamingType, out Roaming roaming)
    {
        return _roaming.TryGetValue(roamingType, out roaming);
    }

    #endregion
    
    #region Link

    /// <summary>
    /// 建立漫游关系。
    /// </summary>
    /// <param name="session">要建立漫游协议的目标Scene的SceneConfig。</param>
    /// <param name="targetSceneConfig">需要转发的Session</param>
    /// <param name="roamingTyp">要创建的漫游协议类型。</param>
    /// <returns>如果建立完成会返回为0，其余不为0的都是发生错误了。可以通过InnerErrorCode.cs来查看错误。</returns>
    public async FTask<uint> Link(Session session, SceneConfig targetSceneConfig, int roamingTyp)
    {
        return await Link(targetSceneConfig.RouteId, session.RuntimeId, roamingTyp);
    }

    /// <summary>
    /// 建立漫游关系。
    /// </summary>
    /// <param name="targetSceneRouteId">要建立漫游协议的目标Scene的RouteId。</param>
    /// <param name="forwardSessionRouteId">需要转发的Session的RouteId。</param>
    /// <param name="roamingType">要创建的漫游协议类型。</param>
    /// <returns>如果建立完成会返回为0，其余不为0的都是发生错误了。可以通过InnerErrorCode.cs来查看错误。</returns>
    public async FTask<uint> Link(long targetSceneRouteId, long forwardSessionRouteId, int roamingType)
    {
        if (_roaming.ContainsKey(roamingType))
        {
            return InnerErrorCode.ErrLinkRoamingAlreadyExists;
        }

        var response = (I_LinkRoamingResponse)await Scene.NetworkMessagingComponent.CallInnerRoute(targetSceneRouteId,
            new I_LinkRoamingRequest()
            {
                RoamingId = Id,
                RoamingType = roamingType,
                ForwardSessionRouteId = forwardSessionRouteId,
                SceneRouteId = Scene.RuntimeId
            });
        
        if (response.ErrorCode != 0)
        {
            return response.ErrorCode;
        }

        var roaming = Entity.Create<Roaming>(Scene, true, true);
        roaming.TerminusId = response.TerminusId;
        roaming.TargetSceneRouteId = targetSceneRouteId;
        roaming.ForwardSessionRouteId = forwardSessionRouteId;
        roaming.SessionRoamingComponent = this;
        roaming.RoamingType = roamingType;
        roaming.RoamingLock = RoamingLock;
        _roaming.Add(roamingType, roaming);
        return 0;
    }

    #endregion

    #region UnLink

    /// <summary>
    /// 断开当前的所有漫游关系。
    /// <param name="removeRoamingType">要移除的RoamingType，默认不设置是移除所有漫游。</param>
    /// </summary>
    public async FTask UnLink(int removeRoamingType = 0)
    {
        switch (removeRoamingType)
        {
            case 0:
            {
                foreach (var (roamingType,roaming) in _roaming)
                {
                    try
                    {
                        var errorCode = await roaming.Disconnect();
                
                        if (errorCode != 0)
                        {
                            Log.Warning($"roaming roamingId:{Id} roamingType:{roamingType} disconnect  errorCode:{errorCode}");
                        }
                    }
                    finally
                    {
                        roaming.Dispose();
                    }
                }
        
                _roaming.Clear();
                return;
            }
            default:
            {
                if (!_roaming.Remove(removeRoamingType, out var roaming))
                {
                    return;
                }

                var errorCode = await roaming.Disconnect();
        
                if (errorCode != 0)
                {
                    Log.Warning($"roaming roamingId:{Id} roamingType:{removeRoamingType} disconnect  errorCode:{errorCode}");
                }

                roaming.Dispose();
                return;
            }
        }
    }

    #endregion

    #region OuterMessage

    /// <summary>
    /// 发送一个消息给漫游终
    /// </summary>
    /// <param name="message"></param>
    public void Send(IRoamingMessage message)
    {
        Call(message.RouteType, message).Coroutine();
    }

    /// <summary>
    /// 发送一个消息给漫游终端
    /// </summary>
    /// <param name="roamingType"></param>
    /// <param name="message"></param>
    public void Send(int roamingType, IRouteMessage message)
    {
        Call(roamingType, message).Coroutine();
    }

    /// <summary>
    /// 发送一个RPC消息给漫游终端
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async FTask<IResponse> Call(IRoamingMessage message)
    {
        return await Call(message.RouteType, message);
    }
    
    /// <summary>
    /// 发送一个RPC消息给漫游终端
    /// </summary>
    /// <param name="roamingType"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async FTask<IResponse> Call(int roamingType, IRouteMessage message)
    {
        if (!_roaming.TryGetValue(roamingType, out var roaming))
        {
            return MessageDispatcherComponent.CreateResponse(message.GetType(), InnerErrorCode.ErrNotFoundRoaming);
        }

        var failCount = 0;
        var runtimeId = RuntimeId;
        var routeId = roaming.TerminusId;
        var requestType = message.GetType();
        
        IResponse iRouteResponse = null;

        using (await RoamingMessageLock.Wait(roaming.RoamingType, "RoamingComponent Call MemoryStream"))
        {
            while (!IsDisposed)
            {
                if (routeId == 0)
                {
                    routeId = await roaming.GetTerminusId();
                }

                if (routeId == 0)
                {
                    return MessageDispatcherComponent.CreateResponse(requestType, InnerErrorCode.ErrNotFoundRoaming);
                }

                iRouteResponse = await NetworkMessagingComponent.CallInnerRoute(routeId, message);

                if (runtimeId != RuntimeId)
                {
                    iRouteResponse.ErrorCode = InnerErrorCode.ErrRoamingTimeout;
                }

                switch (iRouteResponse.ErrorCode)
                {
                    case InnerErrorCode.ErrRouteTimeout:
                    case InnerErrorCode.ErrRoamingTimeout:
                    {
                        return iRouteResponse;
                    }
                    case InnerErrorCode.ErrNotFoundRoute:
                    case InnerErrorCode.ErrNotFoundRoaming:
                    {
                        if (++failCount > 20)
                        {
                            Log.Error($"RoamingComponent.Call failCount > 20 route send message fail, LinkRoamingId: {routeId}");
                            return iRouteResponse;
                        }

                        await TimerComponent.Net.WaitAsync(100);

                        if (runtimeId != RuntimeId)
                        {
                            iRouteResponse.ErrorCode = InnerErrorCode.ErrNotFoundRoaming;
                        }

                        routeId = 0;
                        continue;
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

    #endregion

    #region InnerMessage

    internal async FTask Send(int roamingType, Type requestType, APackInfo packInfo)
    {
        await Call(roamingType, requestType, packInfo);
    }

    internal async FTask<IResponse> Call(int roamingType, Type requestType, APackInfo packInfo)
    {
        if (IsDisposed)
        {
            return MessageDispatcherComponent.CreateResponse(requestType, InnerErrorCode.ErrNotFoundRoaming);
        }

        packInfo.IsDisposed = true;
        
        if (!_roaming.TryGetValue(roamingType, out var roaming))
        {
            return MessageDispatcherComponent.CreateResponse(requestType, InnerErrorCode.ErrNotFoundRoaming);
        }
        
        var failCount = 0;
        var runtimeId = RuntimeId;
        var routeId = roaming.TerminusId;
        IResponse iRouteResponse = null;
        
        try
        {
            using (await RoamingMessageLock.Wait(roamingType, "RoamingComponent Call MemoryStream"))
            {
                while (!IsDisposed)
                {
                    if (routeId == 0)
                    {
                        routeId = await roaming.GetTerminusId();
                    }

                    if (routeId == 0)
                    {
                        return MessageDispatcherComponent.CreateResponse(requestType, InnerErrorCode.ErrNotFoundRoaming);
                    }

                    iRouteResponse = await NetworkMessagingComponent.CallInnerRoute(routeId, requestType, packInfo);

                    if (runtimeId != RuntimeId)
                    {
                        iRouteResponse.ErrorCode = InnerErrorCode.ErrRoamingTimeout;
                    }

                    switch (iRouteResponse.ErrorCode)
                    {
                        case InnerErrorCode.ErrRouteTimeout:
                        case InnerErrorCode.ErrRoamingTimeout:
                        {
                            return iRouteResponse;
                        }
                        case InnerErrorCode.ErrNotFoundRoute:
                        case InnerErrorCode.ErrNotFoundRoaming:
                        {
                            if (++failCount > 20)
                            {
                                Log.Error($"RoamingComponent.Call failCount > 20 route send message fail, LinkRoamingId: {routeId}");
                                return iRouteResponse;
                            }

                            await TimerComponent.Net.WaitAsync(100);

                            if (runtimeId != RuntimeId)
                            {
                                iRouteResponse.ErrorCode = InnerErrorCode.ErrNotFoundRoaming;
                            }
                            routeId = 0;
                            continue;
                        }
                        default:
                        {
                            return iRouteResponse; // 对于其他情况，直接返回响应，无需额外处理
                        }
                    }
                }
            }
        }
        finally
        {
            packInfo.Dispose();
        }

        return iRouteResponse;
    }

    #endregion
}
#endif