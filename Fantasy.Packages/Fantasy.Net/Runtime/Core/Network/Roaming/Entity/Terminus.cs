#if FANTASY_NET
using System.Runtime.CompilerServices;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
using Fantasy.Serialize;
using LightProto;
using MemoryPack;
// ReSharper disable UnassignedField.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable CheckNamespace
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
namespace Fantasy.Network.Roaming;

/// <summary>
/// 目标 Scene 上的漫游终端，负责关联业务实体、消息转发和跨 Scene 传送。
/// </summary>
[MemoryPackable]
public sealed partial class Terminus : Entity
{
    /// <summary>
    /// 当前对外可路由的实体地址；关联业务实体后指向该实体，否则指向 Terminus 自身。
    /// </summary>
    internal long TerminusId;
    /// <summary>
    /// 防止异步销毁流程重复进入。
    /// </summary>
    internal bool IsDisposeTerminus;
    /// <summary>
    /// 当前目标 Scene 对应的漫游类型。
    /// </summary>
    public int RoamingType { get; internal set; }
    /// <summary>
    /// 管理源端漫游上下文的 Scene 地址。
    /// </summary>
    public long ForwardSceneAddress{ get; internal set; }
    /// <summary>
    /// 当前拥有该 Terminus 的 SessionRoamingComponent.RuntimeId。
    /// 同一个 Gate 内重连时保持不变，跨 Gate 重建组件时会发生变化。
    /// </summary>
    public long OwnerRoamingRuntimeId { get; internal set; }
    /// <summary>
    /// 接收客户端转发消息的 Session 地址，由漫游系统在重连时更新。
    /// </summary>
    public long ForwardSessionAddress{ get; internal set; }
    /// <summary>
    /// Terminus 销毁时是否同时销毁关联实体。
    /// </summary>
    public bool IsAutoDisposeLinkTerminusEntity { get; internal set; }
    /// <summary>
    /// 接收发往当前 Terminus 消息的业务实体。
    /// </summary>
    public Entity? TerminusEntity { get; private set; }
    /// <summary>
    /// 按目标 roamingType 串行发送请求，避免传送期间并发刷新路由地址。
    /// </summary>
    [ProtoIgnore]
    [MemoryPackIgnore]
    internal CoroutineLock? RoamingMessageLock;
    /// <summary>
    /// 是否暂停向源端 Session 转发消息。
    /// </summary>
    [ProtoIgnore]
    [MemoryPackIgnore]
    internal bool StopForwarding;
    /// <summary>
    /// 缓存其他 roamingType 对应的 TerminusId；路由失效时会清除并重新查询。
    /// </summary>
    [ProtoIgnore]
    [MemoryPackIgnore]
    private readonly Dictionary<int, long> _roamingTerminusId = new Dictionary<int, long>();

    /// <summary>
    /// 以断开原因启动 Terminus 的异步销毁流程。
    /// </summary>
    public override void Dispose()
    {
        if (IsDisposed || IsDisposeTerminus)
        {
            return;
        }

        DisposeAsync(DisposeTerminusType.UnLink).Coroutine();
    }

    /// <summary>
    /// 从当前 Scene 移除 Terminus，发布离开事件并释放本地状态。
    /// </summary>
    /// <param name="disposeTerminusType">Terminus 离开当前 Scene 的原因。</param>
    internal async FTask DisposeAsync(DisposeTerminusType disposeTerminusType)
    {
        if (IsDisposed || IsDisposeTerminus)
        {
            return;
        }

        IsDisposeTerminus = true;

        try
        {
            await Scene.TerminusComponent.RemoveTerminusAsync(disposeTerminusType, Id, false);
        }
        finally
        {
            TerminusId = 0;
            RoamingType = 0;
            ForwardSceneAddress = 0;
            OwnerRoamingRuntimeId = 0;
            ForwardSessionAddress = 0;

            TerminusEntity = null;
            IsAutoDisposeLinkTerminusEntity = false;

            try
            {
                RoamingMessageLock?.Dispose();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                RoamingMessageLock = null;
                _roamingTerminusId.Clear();
                base.Dispose();
            }
        }
    }

    #region Link

    /// <summary>
    /// 创建业务实体并关联到当前 Terminus。
    /// </summary>
    /// <remarks>
    /// 关联后发往 Terminus 的消息由业务实体处理。关联实体销毁时始终会销毁 Terminus；
    /// <paramref name="autoDispose"/> 仅控制 Terminus 销毁时是否反向销毁关联实体。
    /// </remarks>
    /// <param name="autoDispose">Terminus 销毁时是否同时销毁新建实体。</param>
    /// <param name="startForwarding">关联完成后是否允许向源端 Session 转发消息。</param>
    /// <typeparam name="T">要创建并关联的实体类型。</typeparam>
    /// <returns>创建的实体；当前 Terminus 已有关联实体时返回 <see langword="null"/>。</returns>
    public async FTask<T> LinkTerminusEntity<T>(bool autoDispose, bool startForwarding = true) where T : Entity, new()
    {
        if (!IsCanLink())
        {
            return null;
        }

        var linkEntity = Entity.Create<T>(Scene, true, true);
        await LinkEntity(linkEntity, autoDispose);
        StopForwarding = !startForwarding;
        return linkEntity;
    }

    /// <summary>
    /// 将已有业务实体关联到当前 Terminus。
    /// </summary>
    /// <remarks>
    /// 关联实体销毁时始终会销毁 Terminus；<paramref name="autoDispose"/> 仅控制 Terminus 销毁时是否反向销毁关联实体。
    /// </remarks>
    /// <param name="entity">要关联的现有实体。</param>
    /// <param name="autoDispose">Terminus 销毁时是否同时销毁关联实体。</param>
    /// <param name="startForwarding">关联完成后是否允许向源端 Session 转发消息。</param>
    public async FTask LinkTerminusEntity(Entity entity, bool autoDispose, bool startForwarding = true)
    {
        if (entity == null)
        {
            Log.Error("Entity cannot be empty");
            return;
        }

        if (!IsCanLink())
        {
            return;
        }

        // 同一个实体只能属于一个 Terminus；已失效的旧标记可以安全回收。
        var terminusFlagComponent = entity.GetComponent<TerminusFlagComponent>();

        if (terminusFlagComponent != null)
        {
            Terminus terminus = terminusFlagComponent.Terminus;

            if (terminus != null)
            {
                Log.Error($"Entity {entity.Id} is already linked to Terminus {terminus.Id}");
                return;
            }
            else
            {
                // 旧 Terminus 已销毁但标记尚未来得及清理，不应阻止本次重新关联。
                entity.RemoveComponent<TerminusFlagComponent>();
            }
        }

        await LinkEntity(entity, autoDispose);
        StopForwarding = !startForwarding;
    }

    private async FTask LinkEntity(Entity entity, bool autoDispose)
    {
        var isLocked = false;
        var syncRoaming = TerminusId != 0;
        var entityRuntimeId = entity.RuntimeId;

        IsAutoDisposeLinkTerminusEntity =  autoDispose;

        try
        {
            if (syncRoaming)
            {
                // Terminus 已对外可路由时，先暂停源端该 roamingType 的消息，避免切换实体地址时落到旧目标。
                var lockErrorCode = await Lock();

                if (lockErrorCode != 0)
                {
                    Log.Error($"Failed to lock Terminus {Id} before linking entity. ErrorCode: {lockErrorCode}. Link operation aborted.");
                    return;
                }
            }

            isLocked = true;
            TerminusEntity = entity;
            TerminusId = entityRuntimeId;

            SetTerminusFlag(entity);

            if (syncRoaming)
            {
                // 提交新的 TerminusId 后，源端队列才会继续发送。
                await UnLock();
            }

            isLocked = false;
        }
        catch (Exception e)
        {
            Log.Error(e);

            if (syncRoaming && isLocked)
            {
                await UnLock();
            }
        }
    }

    private bool IsCanLink()
    {
        // 直接引用存在时，当前 Terminus 已完成关联。
        if (TerminusEntity != null)
        {
            Log.Error($"TerminusEntity:{TerminusEntity.Type.FullName} Already exists!");
            return false;
        }

        // autoDispose 模式下还需检查反向标记，避免重复关联。
        var terminusEntityFlagComponent = GetComponent<TerminusEntityFlagComponent>();

        if (terminusEntityFlagComponent == null)
        {
            return true;
        }

        Entity linkEntity = terminusEntityFlagComponent.LinkEntity;

        if (linkEntity != null)
        {
            Log.Error($"TerminusEntity:{linkEntity.Type.FullName} Already exists!");
            return false;
        }

        // 反向标记存在但实体已失效属于异常残留；清理后允许重新关联。
        Log.Warning($"Terminus {Id} has TerminusEntityFlagComponent but LinkEntity is null. This should not happen normally. The linked entity may have been disposed without properly cleaning up the Terminus relationship. Cleaning up orphaned component.");
        RemoveComponent<TerminusEntityFlagComponent>();
        return true;
    }

    /// <summary>
    /// 开启或暂停向源端 Session 转发消息。
    /// </summary>
    /// <param name="isStartForwarding"><see langword="true"/> 为开启；<see langword="false"/> 为暂停。</param>
    public void SetForwarding(bool isStartForwarding)
    {
        StopForwarding = !isStartForwarding;
    }

    #endregion

    #region Transfer

    /// <summary>
    /// 将当前 Terminus 及其关联实体传送到另一个 Scene。
    /// </summary>
    /// <remarks>
    /// 成功后源 Scene 中的 Terminus 和关联实体会被销毁；需要在源端清理外部关系时，应在传送前保存所需 Id。
    /// </remarks>
    /// <param name="targetSceneAddress">目标 Scene 地址。</param>
    /// <returns>0 表示成功；其他值为传送或路由错误码。</returns>
    public async FTask<uint> StartTransfer(long targetSceneAddress)
    {
        var currentSceneAddress = Scene.Address;

        if (targetSceneAddress == currentSceneAddress)
        {
            Log.Warning($"Unable to teleport to your own scene targetSceneAddress:{targetSceneAddress} == currentSceneAddress:{currentSceneAddress}");
            return 0;
        }

        var isLocked = false;

        try
        {
            // 锁定源端路由，使传送窗口内的新消息排队等待，而不是发往即将失效的地址。
            var lockErrorCode = await Lock();
            if (lockErrorCode != 0)
            {
                Log.Error($"Failed to lock Terminus {Id} before transfer. ErrorCode: {lockErrorCode}");
                return lockErrorCode;
            }
            isLocked = true;

            // 业务实体存在时由实体承载传送事件，否则由 Terminus 自身承载。
            if (this.TerminusEntity == null)
            {
                await Scene.EntityComponent.TransferOut(this);
            }
            else
            {
                await Scene.EntityComponent.TransferOut(TerminusEntity);
            }

            // 序列化后的 Terminus 在目标 Scene 注册并提交新的路由地址。
            using var response = (I_TransferTerminusResponse)await Scene.NetworkMessagingComponent.Call(
                targetSceneAddress,
                new I_TransferTerminusRequest()
                {
                    Terminus = this
                });
            if (response.ErrorCode != 0)
            {
                // 目标端失败时保留源端实体，并恢复旧路由继续服务。
                await UnLock();
                isLocked = false;
                return response.ErrorCode;
            }
            // 目标端已接管路由，最后清理源 Scene 中的副本。
            await Scene.TerminusComponent.RemoveTerminusAsync(DisposeTerminusType.Transfer, Id, true);
        }
        catch (Exception e)
        {
            Log.Error(e);

            // 异常路径也必须释放远端迁移锁，否则该 roamingType 的后续请求会永久阻塞。
            if (isLocked)
            {
                await UnLock();
            }

            return InnerErrorCode.ErrTerminusStartTransfer;
        }

        return 0;
    }

    /// <summary>
    /// 在 Terminus 与关联实体之间建立查询和生命周期标记。
    /// </summary>
    /// <param name="entity">要关联的业务实体。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetTerminusFlag(Entity entity)
    {
        // Entity -> Terminus 标记始终存在，用于查询并在实体销毁时清理 Terminus。
        entity.AddComponent<TerminusFlagComponent>().Terminus = this;

        // 只有 autoDispose 模式需要 Terminus -> Entity 的反向级联。
        if (IsAutoDisposeLinkTerminusEntity)
        {
            AddComponent<TerminusEntityFlagComponent>().LinkEntity = entity;
        }
    }

    /// <summary>
    /// 在目标 Scene 恢复序列化状态、注册实体并提交新的路由地址。
    /// </summary>
    /// <param name="scene">接收传送的目标 Scene。</param>
    /// <returns>源端解除迁移锁的错误码，0 表示成功。</returns>
    internal async FTask<uint> TransferComplete(Scene scene)
    {
        // 先恢复到目标 Scene，再重新生成只在进程内有效的 RuntimeId 和关联标记。
        Deserialize(scene);
        TerminusId = RuntimeId;

        if (TerminusEntity != null)
        {
            TerminusEntity.Deserialize(scene);
            TerminusId = TerminusEntity.RuntimeId;
            SetTerminusFlag(TerminusEntity);
            await Scene.EntityComponent.TransferIn(TerminusEntity);
        }
        else
        {
            await Scene.EntityComponent.TransferIn(this);
        }

        // 最后把新地址提交给源端；提交成功后排队消息才会继续发送。
        return await UnLock();
    }

    /// <summary>
    /// 请求源端锁定当前 roamingType 的路由；锁定期间消息会排队等待。
    /// </summary>
    /// <returns>源端返回的错误码，0 表示锁定成功。</returns>
    private async FTask<uint> Lock()
    {
        using var response = await Scene.NetworkMessagingComponent.Call(ForwardSceneAddress,
            new I_LockTerminusIdRequest()
            {
                RoamingId = Id,
                RoamingType = RoamingType
            });
        return response.ErrorCode;
    }

    /// <summary>
    /// 向源端提交当前 TerminusId 和 Scene 地址，并解除路由锁。
    /// </summary>
    /// <returns>源端返回的错误码，0 表示提交成功。</returns>
    private async FTask<uint> UnLock()
    {
        using var response = await Scene.NetworkMessagingComponent.Call(ForwardSceneAddress,
            new I_UnLockTerminusIdRequest()
            {
                RoamingId = Id,
                RoamingType = RoamingType,
                TerminusId = TerminusId,
                TargetSceneAddress = Scene.Address
            });
        return response.ErrorCode;
    }

    #endregion

    #region Message

    /// <summary>
    /// 从源端查询另一 roamingType 当前可用的 TerminusId。
    /// </summary>
    /// <param name="roamingType">要查询的目标漫游类型。</param>
    /// <returns>目标 TerminusId；未就绪或当前 Terminus 已销毁时返回 0。</returns>
    private async FTask<long> GetTerminusId(int roamingType)
    {
        if (IsDisposed)
        {
            return 0;
        }

        using var response = (I_GetTerminusIdResponse)await Scene.NetworkMessagingComponent.Call(
            ForwardSceneAddress,
            new I_GetTerminusIdRequest()
            {
                RoamingId = Id,
                RoamingType = roamingType
            });
        return response.TerminusId;
    }

    /// <summary>
    /// 向当前源端 Session 转发消息。
    /// </summary>
    /// <remarks>转发已暂停时会直接释放消息，避免向失效 Session 发送。</remarks>
    /// <param name="message">要转发的漫游消息。</param>
    public void Send<T>(T message) where T : IRoamingMessage
    {
        if (StopForwarding)
        {
            message.Dispose();
            return;
        }

        Scene.NetworkMessagingComponent.Send(ForwardSessionAddress, message);
    }

    /// <summary>
    /// 向另一 roamingType 的 Terminus 发送单向消息。
    /// </summary>
    /// <param name="roamingType">目标漫游类型。</param>
    /// <param name="message">要发送的漫游消息。</param>
    public void Send<T>(int roamingType, T message) where T : IRoamingMessage
    {
        SendAsync(roamingType, message).Coroutine();
    }

    private async FTask SendAsync<T>(int roamingType, T message) where T : IRoamingMessage
    {
        using var response = await Call(roamingType, message);
    }

    /// <summary>
    /// 调用另一 roamingType 对应的 Terminus。
    /// </summary>
    /// <param name="roamingType">目标漫游类型，不能与当前 <see cref="RoamingType"/> 相同。</param>
    /// <param name="request">要发送的漫游请求。</param>
    /// <returns>目标端响应；路由不存在、未就绪或当前 Terminus 已销毁时返回对应错误响应。</returns>
    public async FTask<IResponse> Call<T>(int roamingType, T request) where T : IRoamingMessage
    {
        var protocolCode = request.OpCode();

        if (IsDisposed)
        {
            request.Dispose();
            return null;
        }

        var scene = Scene;
        var messageDispatcherComponent = scene.MessageDispatcherComponent;

        if (roamingType == RoamingType)
        {
            request.Dispose();

            Log.Warning(
                $"Does not support sending messages to the same scene as roamingType " +
                $"currentRoamingType:{RoamingType} roamingType:{roamingType}");

            return messageDispatcherComponent.CreateResponse(
                protocolCode,
                InnerErrorCode.ErrNotFoundRoaming);
        }

        var failCount = 0;
        var runtimeId = RuntimeId;
        var requestType = typeof(T);
        var timerComponent = scene.TimerComponent;
        var roamingMessageLock = RoamingMessageLock!;
        var networkMessagingComponent = scene.NetworkMessagingComponent;

        _roamingTerminusId.TryGetValue(roamingType, out var address);

        // 请求只序列化一次，路由切换重试时复用同一载荷，并在 finally 中统一归还。
        var buffer = networkMessagingComponent.Pack(request);

        try
        {
            // 每个目标 roamingType 串行刷新地址，避免传送期间多个请求同时缓存不同代路由。
            using (await roamingMessageLock.Wait(roamingType, "Terminus Call request"))
            {
                while (runtimeId == RuntimeId)
                {
                    if (address == 0)
                    {
                        address = await GetTerminusId(roamingType);

                        if (runtimeId != RuntimeId)
                        {
                            return messageDispatcherComponent.CreateResponse(
                                protocolCode,
                                InnerErrorCode.ErrRoamingDisposed);
                        }

                        if (address != 0)
                        {
                            _roamingTerminusId[roamingType] = address;
                        }
                        else
                        {
                            return messageDispatcherComponent.CreateResponse(
                                protocolCode,
                                InnerErrorCode.ErrRoamingNotReady);
                        }
                    }

                    var iRouteResponse = await networkMessagingComponent.Call(
                        address,
                        requestType,
                        protocolCode,
                        buffer);

                    if (runtimeId != RuntimeId)
                    {
                        iRouteResponse.ErrorCode = InnerErrorCode.ErrRoamingTimeout;
                        return iRouteResponse;
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
                            // 目标 Terminus 可能正在传送；短暂等待后清除缓存并向源端重新查询。
                            if (++failCount > RoamingConstants.MaxRetryCount)
                            {
                                Log.Error(
                                    $"Terminus.Call failCount > " +
                                    $"{RoamingConstants.MaxRetryCount} route send message fail, " +
                                    $"TerminusId: {address}");

                                return iRouteResponse;
                            }

                            try
                            {
                                await timerComponent.Net.WaitAsync(
                                    RoamingConstants.RetryIntervalMs);
                            }
                            catch
                            {
                                iRouteResponse.Dispose();
                                throw;
                            }

                            if (runtimeId != RuntimeId)
                            {
                                iRouteResponse.ErrorCode =
                                    InnerErrorCode.ErrRoamingDisposed;

                                return iRouteResponse;
                            }

                            iRouteResponse.Dispose();
                            address = 0;
                            continue;
                        }
                        default:
                        {
                            return iRouteResponse;
                        }
                    }
                }
            }

            return messageDispatcherComponent.CreateResponse(
                protocolCode,
                InnerErrorCode.ErrRoamingDisposed);
        }
        finally
        {
            networkMessagingComponent.MemoryStreamBufferPool.ReturnMemoryStream(buffer);
        }
    }

    #endregion
}
#endif
