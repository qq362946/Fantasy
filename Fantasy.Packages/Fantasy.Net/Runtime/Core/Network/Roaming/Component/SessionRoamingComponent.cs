#if FANTASY_NET
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.IdFactory;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
using Fantasy.PacketParser.Interface;
using Fantasy.Platform.Net;
using Fantasy.Scheduler;
using Fantasy.Timer;
// ReSharper disable CheckNamespace
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Fantasy.Network.Roaming;

internal static class RoamingConstants
{
    // 路由切换窗口内允许短暂查询不到 Terminus；最多等待 20 次、每次间隔 100 毫秒。
    public const int MaxRetryCount = 20;
    public const int RetryIntervalMs = 100;
    // 给客户端断线重连预留的默认保活时间。
    public const int DefaultDelayRemoveMs = 180_000;
}

/// <summary>
/// 表示一个 roamingId 在源端的全部漫游关系。
/// </summary>
/// <remarks>
/// 该实体由 <see cref="RoamingComponent"/> 管理，不挂载在 Session 下。Session 断线重建后，组件会保留并重新绑定到新 Session。
/// </remarks>
public sealed class SessionRoamingComponent : Entity
{
    // Session 引用会在断线等待期清空，OwnerSessionRuntimeId 则保留用于识别旧 Session 的延迟回调。
    private EntityReference<Session> _session;
    /// <summary>
    /// 当前绑定的 Session；重新赋值时同时推进 owner 代数。
    /// </summary>
    internal Session Session
    {
        get => _session;
        set
        {
            _session = value;
            OwnerSessionRuntimeId = value.RuntimeId;
        }
    }

    internal long OwnerSessionRuntimeId;

    // 在 Terminus 传送期间保护各 roamingType 的 TerminusId。
    private CoroutineLock? _roamingLock;
    // 同一 roamingType 的请求必须串行，避免多个请求同时刷新正在迁移的路由地址。
    private CoroutineLock? _roamingMessageLock;
    private TimerComponent _timerComponent;
    private NetworkMessagingComponent _networkMessagingComponent;
    private MessageDispatcherComponent _messageDispatcherComponent;

    /// <summary>
    /// 按 roamingType 保存与各目标 Scene 建立的漫游连接。
    /// </summary>
    private readonly Dictionary<int, Roaming> _roaming = new Dictionary<int, Roaming>();

    /// <summary>
    /// 初始化消息、定时器和锁依赖，并绑定首次建立连接的 Session。
    /// </summary>
    /// <param name="session">首次绑定的 Session。</param>
    internal void Initialize(Session session)
    {
        var scene = session.Scene;
        _timerComponent = scene.TimerComponent;
        _networkMessagingComponent = scene.NetworkMessagingComponent;
        _messageDispatcherComponent = scene.MessageDispatcherComponent;
        _roamingLock = scene.CoroutineLockComponent.Create(this.GetType().TypeHandle.Value.ToInt64());
        _roamingMessageLock = scene.CoroutineLockComponent.Create(this.GetType().TypeHandle.Value.ToInt64());

        Session = session;
        session.SessionRoamingComponent = this;
    }

    /// <summary>
    /// 释放本地锁和缓存引用。
    /// </summary>
    /// <remarks>调用方应先执行 <see cref="UnLinkAll"/> 通知目标端断开连接。</remarks>
    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        try
        {
            if (_roamingLock != null)
            {
                _roamingLock.Dispose();
                _roamingLock = null;
            }

            if (_roamingMessageLock != null)
            {
                _roamingMessageLock.Dispose();
                _roamingMessageLock = null;
            }
        }
        finally
        {
            _session.Clear();
            _timerComponent = null;
            OwnerSessionRuntimeId = 0;
            _networkMessagingComponent = null;
            _messageDispatcherComponent = null;
            base.Dispose();
        }
    }

    /// <summary>
    /// 清除已失效的 Session 引用，但保留 owner 代数供延迟销毁校验。
    /// </summary>
    internal void ClearSessionReference()
    {
        _session.Clear();
    }

    #region Get

    /// <summary>
    /// 尝试获取指定 roamingType 的漫游连接。
    /// </summary>
    /// <param name="roamingType">目标漫游类型。</param>
    /// <param name="roaming">找到的漫游连接。</param>
    /// <returns>找到时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool TryGetRoaming(int roamingType, out Roaming roaming)
    {
        return _roaming.TryGetValue(roamingType, out roaming);
    }

    #endregion

    #region Remove

    /// <summary>
    /// 从本地索引移除一个漫游连接。
    /// </summary>
    /// <param name="roamingType">要移除的漫游类型。</param>
    /// <param name="isDispose">是否同时销毁连接实体。</param>
    internal void Remove(int roamingType, bool isDispose)
    {
        if (!_roaming.Remove(roamingType, out var roaming))
        {
            return;
        }

        if (isDispose)
        {
            roaming.Dispose();
        }
    }

    #endregion

    #region Link

    /// <summary>
    /// 判断指定 roamingType 是否已经建立漫游关系。
    /// </summary>
    /// <param name="roamingType">要检查的漫游类型。</param>
    /// <returns>如果已建立漫游关系返回 true，否则返回 false。</returns>
    public bool IsLinked(int roamingType) => _roaming.ContainsKey(roamingType);

    /// <summary>
    /// 将所有目标 Terminus 的转发地址切换到新的 Session。
    /// </summary>
    /// <param name="session">重连后需要接收转发消息的 Session。</param>
    internal async FTask SetForwardSessionAddress(Session session)
    {
        using var tasks = ListPool<FTask>.Create();
        var forwardSessionAddress = session.RuntimeId;

        foreach (var (_, roaming) in _roaming)
        {
            tasks.Add(roaming.SetForwardSessionAddress(forwardSessionAddress));
        }

        await FTask.WaitAll(tasks);
    }

    /// <summary>
    /// 通知全部目标 Terminus 暂停向 Session 转发消息。
    /// </summary>
    /// <remarks>漫游上下文仍会保留，用于等待客户端在延迟销毁期限内重连。</remarks>
    internal async FTask StopForwarding()
    {
        using var tasks = ListPool<FTask>.Create();

        foreach (var (_, roaming) in _roaming)
        {
            tasks.Add(roaming.StopForwarding());
        }

        await FTask.WaitAll(tasks);
    }

    /// <summary>
    /// 建立或恢复指定 roamingType 的漫游关系。
    /// </summary>
    /// <remarks>同一 roamingType 已存在时复用本地 <see cref="Roaming"/>，由目标端决定创建还是恢复 Terminus。</remarks>
    /// <param name="targetSceneAddress">目标 Scene 地址。</param>
    /// <param name="forwardSessionAddress">接收目标端转发消息的 Session 地址。</param>
    /// <param name="roamingType">目标漫游类型，不能为 0。</param>
    /// <param name="args">仅在目标端创建或恢复事件中使用的可选业务参数。</param>
    /// <returns>0 表示成功；其他值为 <see cref="InnerErrorCode"/> 中定义的错误码。</returns>
    /// <exception cref="ArgumentException"><paramref name="roamingType"/> 为 0。</exception>
    public async FTask<uint> Link(long targetSceneAddress, long forwardSessionAddress, int roamingType, Entity? args = null)
    {
        if (roamingType == 0)
        {
            throw new ArgumentException("roamingType cannot be 0.", nameof(roamingType));
        }

        var request = I_LinkRoamingRequest.Create();

        request.RoamingId = Id;
        request.RoamingType = roamingType;
        request.ForwardSessionAddress = forwardSessionAddress;
        request.SceneAddress = Scene.RuntimeId;
        
        // 同一个 Gate 内复用 SessionRoamingComponent 时 RuntimeId 不变；
        // 跨 Gate 创建新组件时 RuntimeId 会变化。
        request.OwnerRoamingRuntimeId = RuntimeId;
        
        request.Args = args;

        if (!_roaming.TryGetValue(roamingType, out var roaming))
        {
            // 首次连接只有在目标端创建成功后才登记本地 Roaming，避免留下半初始化状态。
            using var response = (I_LinkRoamingResponse)await Scene.NetworkMessagingComponent.Call(targetSceneAddress, request);

            if (response.ErrorCode != 0)
            {
                return response.ErrorCode;
            }

            roaming = Entity.Create<Roaming>(Scene, true, true);
            roaming.TerminusId = response.TerminusId;
            roaming.TargetSceneAddress = targetSceneAddress;
            roaming.ForwardSessionAddress = forwardSessionAddress;
            roaming.SessionRoamingComponent = this;
            roaming.RoamingType = roamingType;
            roaming.RoamingLock = _roamingLock;
            _roaming.Add(roamingType, roaming);
        }
        else
        {
            // 先将地址置为未知；恢复完成前发起的消息会等待并重新查询 TerminusId。
            roaming.TerminusId = 0;
            roaming.TargetSceneAddress = targetSceneAddress;
            roaming.ForwardSessionAddress = forwardSessionAddress;

            using var response = (I_LinkRoamingResponse)await Scene.NetworkMessagingComponent.Call(targetSceneAddress, request);

            if (response.ErrorCode != 0)
            {
                return response.ErrorCode;
            }

            roaming.TerminusId = response.TerminusId;
        }

        return 0;
    }

    #endregion

    #region UnLink

    /// <summary>
    /// 断开并销毁当前 roamingId 的全部漫游连接。
    /// </summary>
    public async FTask UnLinkAll()
    {
        using var roamings = ListPool<Roaming>.Create();
        roamings.AddRange(_roaming.Values);

        // 先摘除集合，避免 await 期间重入的 Dispose 再次修改同一批连接。
        _roaming.Clear();

        foreach (var roaming in roamings)
        {
            var roamingType = roaming.RoamingType;

            try
            {
                var errorCode = await roaming.Disconnect();

                if (errorCode != 0)
                {
                    Log.Warning($"roaming roamingId:{Id} roamingType:{roamingType} disconnect errorCode:{errorCode}");
                }
            }
            catch (Exception e)
            {
                // 一条目标连接失败不能阻断其余连接的清理。
                Log.Error(
                    $"roaming disconnect failed, roamingId:{Id} " +
                    $"roamingType:{roamingType} {e}");
            }
            finally
            {
                roaming.Dispose();
            }
        }
    }

    /// <summary>
    /// 断开并销毁指定 roamingType 的漫游连接。
    /// </summary>
    /// <param name="removeRoamingType">要移除的漫游类型，不能为 0。</param>
    /// <param name="disposeIfEmpty">移除后没有连接时，是否销毁整个漫游上下文。</param>
    /// <returns>移除后没有剩余漫游连接时返回 <see langword="true"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="removeRoamingType"/> 为 0。</exception>
    public async FTask<bool> UnLink(int removeRoamingType, bool disposeIfEmpty)
    {
        if (removeRoamingType == 0)
        {
            throw new ArgumentException("removeRoamingType cannot be 0. Use UnLinkAll() to remove all roaming connections.", nameof(removeRoamingType));
        }

        if (!_roaming.Remove(removeRoamingType, out var roaming))
        {
            return _roaming.Count == 0;
        }

        try
        {
            var errorCode = await roaming.Disconnect();

            if (errorCode != 0)
            {
                Log.Warning(
                    $"roaming roamingId:{Id} roamingType:{removeRoamingType} disconnect  errorCode:{errorCode}");
            }
        }
        catch (Exception e)
        {
            Log.Error(
                $"roaming disconnect failed, roamingId:{Id} " +
                $"roamingType:{removeRoamingType} {e}");
        }
        finally
        {
            roaming.Dispose();
        }

        var isEmpty = _roaming.Count == 0;

        if(disposeIfEmpty && isEmpty)
        {
            Dispose();
        }

        return isEmpty;
    }

    #endregion

    #region OuterMessage

    /// <summary>
    /// 根据消息的 RouteType 向对应 Terminus 发送单向消息。
    /// </summary>
    /// <param name="message">要发送的漫游消息。</param>
    public void Send<T>(T message) where T : IRoamingMessage
    {
        var roamingType = message.RouteType;
        SendAsync(roamingType, message).Coroutine();
    }

    private async FTask SendAsync<T>(int roamingType, T message) where T : IRoamingMessage
    {
        using var response = await Call(roamingType, message);
    }

    /// <summary>
    /// 根据消息的 RouteType 调用对应 Terminus。
    /// </summary>
    /// <param name="message">要发送的漫游请求。</param>
    /// <returns>目标端响应；路由不存在或漫游已销毁时返回对应错误响应。</returns>
    public async FTask<IResponse> Call<T>(T message) where T : IRoamingMessage
    {
        return await Call(message.RouteType, message);
    }

    /// <summary>
    /// 调用指定 roamingType 对应的 Terminus。
    /// </summary>
    /// <param name="roamingType">目标漫游类型。</param>
    /// <param name="message">要发送的地址请求。</param>
    /// <returns>目标端响应；路由不存在、尚未就绪或漫游已销毁时返回对应错误响应。</returns>
    public async FTask<IResponse> Call<T>(int roamingType, T message) where T : IAddressRequest
    {
        var protocolCode = message.OpCode();
        var messageDispatcherComponent = _messageDispatcherComponent;

        if (!_roaming.TryGetValue(roamingType, out var roaming))
        {
            message.Dispose();

            return messageDispatcherComponent.CreateResponse(
                protocolCode,
                InnerErrorCode.ErrNotFoundRoaming);
        }

        var failCount = 0;
        var runtimeId = RuntimeId;
        var address = roaming.TerminusId;
        var requestType = typeof(T);
        var timerComponent = _timerComponent;
        var roamingMessageLock = _roamingMessageLock!;
        var networkMessagingComponent = _networkMessagingComponent;
        // 请求只序列化一次，重试时复用同一只读载荷，并在 finally 中统一归还缓冲区。
        var buffer = networkMessagingComponent.Pack(message);

        try
        {
            // 传送期间 TerminusId 会变化；同类型请求串行后只需由当前请求刷新路由。
            using (await roamingMessageLock.Wait(roamingType, "RoamingComponent Call MemoryStream"))
            {
                while (!IsDisposed)
                {
                    // RuntimeId 是实体代数；await 返回后代数变化说明原组件已经被销毁或复用。
                    if (runtimeId != RuntimeId)
                    {
                        return messageDispatcherComponent.CreateResponse(
                            protocolCode,
                            InnerErrorCode.ErrRoamingDisposed);
                    }

                    if (address == 0)
                    {
                        address = await roaming.GetTerminusId();

                        if (runtimeId != RuntimeId)
                        {
                            return messageDispatcherComponent.CreateResponse(
                                protocolCode,
                                InnerErrorCode.ErrRoamingDisposed);
                        }
                    }

                    if (address == 0)
                    {
                        return messageDispatcherComponent.CreateResponse(
                            protocolCode,
                            InnerErrorCode.ErrRoamingNotReady);
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
                            // 这两类错误可能只是 Terminus 正在传送，短暂等待后清空地址并重新查询。
                            if (++failCount > RoamingConstants.MaxRetryCount)
                            {
                                Log.Error(
                                    $"RoamingComponent.Call failCount > " +
                                    $"{RoamingConstants.MaxRetryCount} route send message fail, " +
                                    $"LinkRoamingId: {address}");

                                return iRouteResponse;
                            }

                            try
                            {
                                await timerComponent.Net.WaitAsync(RoamingConstants.RetryIntervalMs);
                            }
                            catch
                            {
                                iRouteResponse.Dispose();
                                throw;
                            }

                            if (runtimeId != RuntimeId)
                            {
                                iRouteResponse.ErrorCode = InnerErrorCode.ErrRoamingDisposed;
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

    #region InnerMessage

    internal async FTask Send(int roamingType, Type requestType, APackInfo packInfo)
    {
        using var response = await Call(roamingType, requestType, packInfo);
    }

    internal async FTask<IResponse> Call(int roamingType, Type requestType, APackInfo packInfo)
    {
        if (IsDisposed)
        {
            return _messageDispatcherComponent.CreateResponse(packInfo.ProtocolCode, InnerErrorCode.ErrRoamingDisposed);
        }

        if (!_roaming.TryGetValue(roamingType, out var roaming))
        {
            return _messageDispatcherComponent.CreateResponse(packInfo.ProtocolCode, InnerErrorCode.ErrNotFoundRoaming);
        }

        // 暂时接管包的生命周期，避免底层 Call 在重试完成前提前释放载荷。
        packInfo.IsDisposed = true;

        var failCount = 0;
        var runtimeId = RuntimeId;
        var address = roaming.TerminusId;
        IResponse iRouteResponse = null;

        try
        {
            // 与外部消息使用同一把分类型锁，保证传送窗口内的地址刷新顺序一致。
            using (await _roamingMessageLock!.Wait(roamingType, "RoamingComponent Call MemoryStream"))
            {
                while (!IsDisposed)
                {
                    if (address == 0)
                    {
                        address = await roaming.GetTerminusId();
                    }

                    if (address == 0)
                    {
                        return _messageDispatcherComponent.CreateResponse(packInfo.ProtocolCode, InnerErrorCode.ErrRoamingNotReady);
                    }

                    iRouteResponse = await _networkMessagingComponent.Call(address, requestType, packInfo);

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
                            // Terminus 传送完成后地址会改变，重试前必须丢弃旧地址并重新查询。
                            if (++failCount > RoamingConstants.MaxRetryCount)
                            {
                                Log.Error($"RoamingComponent.Call failCount > {RoamingConstants.MaxRetryCount} route send message fail, LinkRoamingId: {address}");
                                return iRouteResponse;
                            }

                            try
                            {
                                await _timerComponent.Net.WaitAsync(RoamingConstants.RetryIntervalMs);
                            }
                            catch
                            {
                                iRouteResponse.Dispose();
                                throw;
                            }

                            if (runtimeId != RuntimeId)
                            {
                                iRouteResponse.ErrorCode = InnerErrorCode.ErrRoamingDisposed;
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
        }
        finally
        {
            // 恢复常规释放语义，并确保包只在本层结束时释放一次。
            packInfo.IsDisposed = false;
            packInfo.Dispose();
        }

        return iRouteResponse;
    }

    #endregion
}
#endif
