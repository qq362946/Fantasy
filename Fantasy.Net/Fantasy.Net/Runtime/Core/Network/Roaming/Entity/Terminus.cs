#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Scheduler;
using MongoDB.Bson.Serialization.Attributes;
// ReSharper disable UnassignedField.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
namespace Fantasy.Network.Roaming;

/// <summary>
/// 漫游终端实体
/// </summary>
public sealed class Terminus : Entity
{
    /// <summary>
    /// 当前漫游终端的TerminusId。
    /// 可以通过 TerminusId 发送消息给这个漫游终端。
    /// 也可以理解为实体的RuntimeId。
    /// </summary>
    internal long TerminusId;
    /// <summary>
    /// 当前漫游终端的类型。
    /// </summary>
    [BsonElement("r")]
    internal int RoamingType;
    /// <summary>
    /// 漫游转发Session所在的Scene的RouteId。
    /// </summary>
    [BsonElement("s")]
    internal long ForwardSceneRouteId;
    /// <summary>
    /// 漫游转发Session的RouteId。
    /// 不知道原理千万不要手动赋值这个。
    /// </summary>
    [BsonElement("f")]
    internal long ForwardSessionRouteId;
    /// <summary>
    /// 关联的玩家实体
    /// </summary>
    [BsonElement("e")]
    public Entity TerminusEntity;
    /// <summary>
    /// 漫游消息锁。
    /// </summary>
    [BsonIgnore]
    internal CoroutineLock RoamingMessageLock;
    /// <summary>
    /// 获得转发的SessionRouteId，可以通过这个Id来发送消息来自动转发到客户端。
    /// </summary>
    public long SessionRouteId => ForwardSessionRouteId;
    /// <summary>
    /// 存放其他漫游终端的Id。
    /// 通过这个Id可以发送消息给它。
    /// </summary>
    [BsonIgnore]
    private readonly Dictionary<int, long> _roamingTerminusId = new Dictionary<int, long>();
    /// <summary>
    /// 创建关联的终端实体。
    /// 创建完成后，接收消息都是由关联的终端实体来处理。
    /// 注意，当你销毁这个实体的时候，并不能直接销毁Terminus，会导致无法接收到漫游消息。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T LinkTerminusEntity<T>() where T : Entity, new()
    {
        if (TerminusEntity != null)
        {
            Log.Error($"TerminusEntity:{TerminusEntity.Type.FullName} Already exists!");
            return null;
        }

        var t = Entity.Create<T>(Scene, true, true);
        TerminusEntity = t;
        TerminusId = TerminusEntity.RuntimeId;
        return t;
    }

    /// <summary>
    /// 关联的终端实体。
    /// 注意，当你销毁这个实体的时候，并不能直接销毁Terminus，会导致无法接收到漫游消息
    /// </summary>
    /// <param name="entity"></param>
    public void LinkTerminusEntity(Entity entity)
    {
        if (entity == null)
        {
            Log.Error("Entity cannot be empty");
            return;
        }
        
        if (TerminusEntity != null)
        {
            Log.Error($"TerminusEntity:{TerminusEntity.Type.FullName} Already exists!");
            return;
        }
        
        TerminusEntity = entity;
        TerminusId = TerminusEntity.RuntimeId;
    }

    #region Transfer

    /// <summary>
    /// 传送漫游终端
    /// 传送完成后，漫游终端和关联的玩家实体都会被销毁。
    /// 所以如果有其他组件关联这个实体，要提前记录好Id，方便传送后清理。
    /// </summary>
    /// <returns></returns>
    public async FTask<uint> StartTransfer(long targetSceneRouteId)
    {
        var currentSceneRouteId = Scene.SceneConfig.RouteId;
        if (targetSceneRouteId == currentSceneRouteId)
        {
            Log.Warning($"Unable to teleport to your own scene targetSceneRouteId:{targetSceneRouteId} == currentSceneRouteId:{currentSceneRouteId}");
            return 0;
        }
        
        try
        {
            // 传送目标服务器之前要先锁定，防止再传送过程中还有其他消息发送过来。
            var lockErrorCode = await Lock();
            if (lockErrorCode != 0)
            {
                return lockErrorCode;
            }
            // 开始执行传送请求。
            var response = (I_TransferTerminusResponse)await Scene.NetworkMessagingComponent.CallInnerRoute(
                targetSceneRouteId,
                new I_TransferTerminusRequest()
                {
                    Terminus = this
                });
            if (response.ErrorCode != 0)
            {
                // 如果传送出现异常，需要先解锁，不然会出现一直卡死的问题。
                await UnLock();
                return response.ErrorCode;
            }
            // 在当前Scene下移除漫游终端。
            Scene.TerminusComponent.RemoveTerminus(Id);
        }
        catch (Exception e)
        {
            Log.Error(e);
            // 如果代码执行出现任何异常，要先去解锁，避免会出现卡死的问题。
            await UnLock();
            return InnerErrorCode.ErrTerminusStartTransfer;
        }

        return 0;
    }

    /// <summary>
    /// 传送完成。
    /// 当传送完成后，需要清理漫游终端。
    /// </summary>
    /// <returns></returns>
    public async FTask<uint> TransferComplete(Scene scene)
    {
        // 首先恢复漫游终端的序列化数据。并且注册到框架中。
        Deserialize(scene);
        TerminusId = RuntimeId;
        if (TerminusEntity != null)
        {
            TerminusEntity.Deserialize(scene);
            TerminusId = TerminusEntity.RuntimeId;
        }
        // 然后要解锁下漫游
        return await UnLock();
    }

    /// <summary>
    /// 锁定漫游当执行锁定了后，所有消息都会被暂时放入队列中不会发送。
    /// 必须要解锁后才能继续发送消息。
    /// </summary>
    /// <returns></returns>
    public async FTask<uint> Lock()
    {
        var response = await Scene.NetworkMessagingComponent.CallInnerRoute(ForwardSceneRouteId,
            new I_LockTerminusIdRequest()
            {
                SessionRuntimeId = ForwardSessionRouteId,
                RoamingType = RoamingType
            });
        return response.ErrorCode;
    }
    
    /// <summary>
    /// 锁定漫游
    /// </summary>
    /// <returns></returns>
    public async FTask<uint> UnLock()
    {
        var response = await Scene.NetworkMessagingComponent.CallInnerRoute(ForwardSceneRouteId,
            new I_UnLockTerminusIdRequest()
            {
                SessionRuntimeId = ForwardSessionRouteId,
                RoamingType = RoamingType,
                TerminusId = TerminusId,
                TargetSceneRouteId = Scene.RouteId
            });
        return response.ErrorCode;
    }

    #endregion

    #region Message

    private async FTask<long> GetTerminusId(int roamingType)
    {
        if (IsDisposed)
        {
            return 0;
        }

        var response = (I_GetTerminusIdResponse)await Scene.NetworkMessagingComponent.CallInnerRoute(
            ForwardSceneRouteId,
            new I_GetTerminusIdRequest()
            {
                SessionRuntimeId = ForwardSessionRouteId,
                RoamingType = roamingType
            });
        return response.TerminusId;
    }

    /// <summary>
    /// 发送一个消息给客户端
    /// </summary>
    /// <param name="message"></param>
    public void Send(IRouteMessage message)
    {
        Scene.NetworkMessagingComponent.SendInnerRoute(ForwardSessionRouteId, message);
    }
    /// <summary>
    /// 发送一个漫游消息
    /// </summary>
    /// <param name="roamingType"></param>
    /// <param name="message"></param>
    public void Send(int roamingType, IRoamingMessage message)
    {
        Call(roamingType,  message).Coroutine();
    }

    /// <summary>
    /// 发送一个漫游RPC消息。
    /// </summary>
    /// <param name="roamingType"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async FTask<IResponse> Call(int roamingType, IRoamingMessage request)
    {
        if (IsDisposed)
        {
            return Scene.MessageDispatcherComponent.CreateResponse(request.GetType(), InnerErrorCode.ErrNotFoundRoaming);
        }

        if (roamingType == RoamingType)
        {
            Log.Warning($"Does not support sending messages to the same scene as roamingType currentRoamingType:{RoamingType} roamingType:{roamingType}");
            return Scene.MessageDispatcherComponent.CreateResponse(request.GetType(), InnerErrorCode.ErrNotFoundRoaming);
        }

        var failCount = 0;
        var runtimeId = RuntimeId;
        IResponse iRouteResponse = null;
        _roamingTerminusId.TryGetValue(roamingType, out var routeId);

        using (await RoamingMessageLock.Wait(roamingType, "Terminus Call request"))
        {
            while (!IsDisposed)
            {
                if (routeId == 0)
                {
                    routeId = await GetTerminusId(roamingType);
                    
                    if (routeId != 0)
                    {
                        _roamingTerminusId[roamingType] = routeId;
                    }
                    else
                    {
                        return Scene.MessageDispatcherComponent.CreateResponse(request.GetType(), InnerErrorCode.ErrNotFoundRoaming);
                    }
                }

                iRouteResponse = await Scene.NetworkMessagingComponent.CallInnerRoute(routeId, request);

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
                            Log.Error($"Terminus.Call failCount > 20 route send message fail, TerminusId: {routeId}");
                            return iRouteResponse;
                        }

                        await Scene.TimerComponent.Net.WaitAsync(100);

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
}
#endif