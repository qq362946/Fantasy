#if FANTASY_NET
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fantasy.DataStructure.Collection;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Timer;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Network.Roaming;

/// <summary>
/// 管理当前 Scene 上的源端漫游上下文。
/// 通常运行在 Gate 等转发服务器上，并按 roamingId 统一处理首次创建、断线重连和延迟销毁。
/// </summary>
public sealed class RoamingComponent : Entity
{
    // Close 开始后禁止创建新漫游，也禁止延迟任务继续回收组件。
    private bool _isInnerDisposed;

    // 所有影响同一 roamingId 生命周期的操作（包括定时器回调）都必须经过这把锁。
    private CoroutineLock _roamingManageLock;
    private TimerSchedulerNet _timerSchedulerNet;

    // Session 会变化，roamingId 才是断线重连期间稳定的身份。
    private readonly Dictionary<long, SessionRoamingComponent> _sessionRoamingComponents = new();
    // 每个 roamingId 最多保留一个延迟销毁任务。
    private readonly Dictionary<long, long> _delayRemoveTaskId = new();

    #region Initialize&Dispose

    /// <summary>
    /// 初始化漫游管理所需的锁和网络定时器。
    /// </summary>
    internal RoamingComponent Initialize()
    {
        _isInnerDisposed = false;
        _timerSchedulerNet = Scene.TimerComponent.Net;
        _roamingManageLock = Scene.CoroutineLockComponent.Create(this.GetType().TypeHandle.Value.ToInt64());
        return this;
    }

    /// <summary>
    /// 异步关闭全部漫游连接后销毁组件。
    /// </summary>
    public override void Dispose()
    {
        DisposeAsync().Coroutine();
    }

    private async FTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        try
        {
            await Close();
        }
        finally
        {
            base.Dispose();
        }
    }

    /// <summary>
    /// 关闭并清理当前 Scene 管理的全部漫游连接。
    /// </summary>
    /// <remarks>Scene 关闭时应在网络组件销毁前等待此方法完成。</remarks>
    internal async FTask Close()
    {
        if (_isInnerDisposed)
        {
            return;
        }

        _isInnerDisposed = true;

        foreach (var (_, taskId) in _delayRemoveTaskId)
        {
            _timerSchedulerNet.Remove(taskId);
        }

        _delayRemoveTaskId.Clear();
        _timerSchedulerNet = null;

        using var sessionRoamingComponents = ListPool<SessionRoamingComponent>.Create();

        sessionRoamingComponents.AddRange(_sessionRoamingComponents.Values);

        // 先从索引中摘除，避免 await 期间的重入再次取得正在关闭的组件。
        _sessionRoamingComponents.Clear();

        foreach (var sessionRoamingComponent in sessionRoamingComponents)
        {
            try
            {
                await sessionRoamingComponent.UnLinkAll();
            }
            catch (Exception e)
            {
                // 关闭阶段尽量清理全部连接，单个 roamingId 失败不能中断后续清理。
                Log.Error(
                    $"SessionRoamingComponent UnLinkAll failed, " +
                    $"roamingId:{sessionRoamingComponent.Id} {e}");
            }
            finally
            {
                sessionRoamingComponent.Dispose();
            }
        }
    }

    #endregion

    #region Create

    /// <summary>
    /// 获取或创建 <paramref name="roamingId"/> 对应的漫游上下文，并绑定到当前 Session。
    /// </summary>
    /// <remarks>
    /// 已存在的 roamingId 会取消待执行的销毁任务并绑定到新 Session；调用方随后应对需要恢复的每个 roamingType 执行 Link。
    /// </remarks>
    /// <param name="session">本次建立或恢复漫游关系的 Session。</param>
    /// <param name="roamingId">业务定义的稳定身份；断线重连前后必须保持一致。</param>
    /// <param name="delayRemove">Session 释放后保留漫游上下文的毫秒数；小于等于 0 时立即销毁。</param>
    /// <returns>与 <paramref name="roamingId"/> 关联的漫游上下文。</returns>
    /// <exception cref="ObjectDisposedException">漫游管理组件已经关闭。</exception>
    internal async FTask<SessionRoamingComponent> GetOrCreateRoaming(Session session, long roamingId, int delayRemove)
    {
        if (_isInnerDisposed)
        {
            throw new ObjectDisposedException(nameof(RoamingComponent));
        }

        SessionRoamingComponent sessionRoamingComponent;

        if (session.SessionRoamingComponent != null)
        {
            sessionRoamingComponent = session.SessionRoamingComponent;
            var sessionRoamingFlgComponent = session.GetComponent<SessionRoamingFlgComponent>();

            if (sessionRoamingComponent.Id == roamingId)
            {
                // 重复调用只更新当前 Session 的销毁策略，不重复建立漫游关系。
                sessionRoamingFlgComponent.DelayRemove = delayRemove;
                return sessionRoamingComponent;
            }
            else
            {
                // 一个 Session 同时只绑定一个 roamingId；切换身份前先按原策略解除旧绑定。
                await Remove(sessionRoamingComponent.Id,
                    sessionRoamingFlgComponent.DelayRemove, sessionRoamingFlgComponent.OwnerSessionRuntimeId);
            }
        }

        using (await _roamingManageLock.Wait(roamingId))
        {
            if (_sessionRoamingComponents.TryGetValue(roamingId, out sessionRoamingComponent))
            {
                // 这里只恢复本地上下文绑定；目标 Terminus 由调用方随后执行 Link 恢复。
                CancelRemoveTask(roamingId);

                Session parentSession = sessionRoamingComponent.Session;

                if (parentSession != null)
                {
                    // 这是主动换绑；移除旧 Flag 时不能再触发一次延迟销毁。
                    parentSession.GetComponent<SessionRoamingFlgComponent>().DoNotRemove = true;
                    parentSession.RemoveComponent<SessionRoamingFlgComponent>();
                    parentSession.SessionRoamingComponent = null;
                }

                sessionRoamingComponent.Session = session;
                session.SessionRoamingComponent = sessionRoamingComponent;
                AddSessionRoamingFlgComponent(session, sessionRoamingComponent, roamingId, delayRemove);
                return sessionRoamingComponent;
            }

            sessionRoamingComponent = Entity.Create<SessionRoamingComponent>(Scene, roamingId, true, true);
            sessionRoamingComponent.Initialize(session);
            _sessionRoamingComponents.Add(roamingId, sessionRoamingComponent);
            AddSessionRoamingFlgComponent(session, sessionRoamingComponent, roamingId, delayRemove);
            return sessionRoamingComponent;
        }
    }

    private void AddSessionRoamingFlgComponent(Session session, SessionRoamingComponent sessionRoamingComponent, long roamingId, int delayRemove)
    {
        var sessionRoamingFlgComponent = session.AddComponent<SessionRoamingFlgComponent>(roamingId);
        sessionRoamingFlgComponent.OwnerSessionRuntimeId = session.RuntimeId;
        sessionRoamingFlgComponent.DelayRemove = delayRemove;
        sessionRoamingFlgComponent.SessionRoamingComponent = sessionRoamingComponent;
    }

    #endregion

    #region Get

    /// <summary>
    /// 获取指定 roamingId 的漫游上下文。
    /// </summary>
    /// <param name="roamingId">业务定义的漫游身份。</param>
    /// <returns>已注册的漫游上下文；不存在时返回 <see langword="null"/>。</returns>
    internal SessionRoamingComponent Get(long roamingId)
    {
        return _sessionRoamingComponents.GetValueOrDefault(roamingId);
    }

    /// <summary>
    /// 尝试获取指定 roamingId 的漫游上下文。
    /// </summary>
    /// <param name="roamingId">业务定义的漫游身份。</param>
    /// <param name="sessionRoamingComponent">找到的漫游上下文。</param>
    /// <returns>找到时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    internal bool TryGet(long roamingId, out SessionRoamingComponent sessionRoamingComponent)
    {
        return _sessionRoamingComponents.TryGetValue(roamingId, out sessionRoamingComponent);
    }

    #endregion

    #region Remove

    /// <summary>
    /// 取消指定 roamingId 尚未执行的延迟销毁任务。
    /// </summary>
    /// <param name="roamingId">业务定义的漫游身份。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CancelRemoveTask(long roamingId)
    {
        if (_delayRemoveTaskId.Remove(roamingId, out var taskId))
        {
            _timerSchedulerNet.Remove(taskId);
        }
    }

    private void DetachSessionBinding(SessionRoamingComponent sessionRoamingComponent)
    {
        var session = sessionRoamingComponent.Session;

        if (session != null && ReferenceEquals( session.SessionRoamingComponent, sessionRoamingComponent))
        {
            var flag = session.GetComponent<SessionRoamingFlgComponent>();

            if (flag != null)
            {
                flag.DoNotRemove = true;
                session.RemoveComponent<SessionRoamingFlgComponent>();
            }

            session.SessionRoamingComponent = null;
        }

        // 只清除易失的 Session 引用。owner 代数必须保留给延迟任务做重连校验。
        sessionRoamingComponent.ClearSessionReference();
    }


    /// <summary>
    /// 立即或延迟移除指定 roamingId 的漫游上下文。
    /// </summary>
    /// <remarks>owner 不匹配表示请求来自已被替换的旧 Session，此时不会影响当前连接。</remarks>
    /// <param name="roamingId">要移除的业务漫游身份。</param>
    /// <param name="delayRemove">延迟毫秒数；小于等于 0 时立即移除。</param>
    /// <param name="ownerSessionRuntimeId">发起移除的 Session RuntimeId；为 0 时不校验 owner。</param>
    internal async FTask Remove(long roamingId, int delayRemove = 0, long ownerSessionRuntimeId = 0)
    {
        if (_isInnerDisposed)
        {
            return;
        }

        using (await _roamingManageLock.Wait(roamingId))
        {
            if (!_sessionRoamingComponents.TryGetValue(roamingId, out var sessionRoamingComponent))
            {
                return;
            }

            if (ownerSessionRuntimeId != 0 && sessionRoamingComponent.OwnerSessionRuntimeId != ownerSessionRuntimeId)
            {
                return;
            }

            DetachSessionBinding(sessionRoamingComponent);
            CancelRemoveTask(roamingId);

            if (delayRemove <= 0)
            {
                await InnerRemove(sessionRoamingComponent, ownerSessionRuntimeId);
                return;
            }

            // 即使调用者未指定 owner，定时器也必须绑定当前代数，避免重连后误删新 Session。
            var expectedOwnerSessionRuntimeId = sessionRoamingComponent.OwnerSessionRuntimeId;

            // 保留 Terminus 等待重连期间，先停止向已经失效的 Session 转发消息。
            await sessionRoamingComponent.StopForwarding();

            var taskId = _timerSchedulerNet.OnceTimer(delayRemove, () =>
            {
                // 定时器不能直接销毁：重新进入 Remove 才能复用同一把锁和 owner 校验。
                Remove(roamingId, 0, expectedOwnerSessionRuntimeId).Coroutine();
            });
            _delayRemoveTaskId.Add(roamingId, taskId);
        }
    }

    private async FTask InnerRemove(SessionRoamingComponent sessionRoamingComponent, long ownerSessionRuntimeId)
    {
        if (_isInnerDisposed)
        {
            return;
        }

        if (sessionRoamingComponent == null)
        {
            throw new NullReferenceException("SessionRoamingComponent is null");
        }

        if (ownerSessionRuntimeId != 0 && sessionRoamingComponent.OwnerSessionRuntimeId != ownerSessionRuntimeId)
        {
            return;
        }

        var roamingId = sessionRoamingComponent.Id;

        // 调用方持有 roamingId 管理锁；先通知全部目标端，再从本地索引移除。
        await sessionRoamingComponent.UnLinkAll();

        sessionRoamingComponent.Dispose();
        _sessionRoamingComponents.Remove(roamingId);
    }

    #endregion
}

/// <summary>
/// 提供 Session 和 Scene 上常用的漫游扩展方法。
/// </summary>
public static class RoamingHelper
{
    /// <summary>
    /// 为 Session 获取或创建漫游上下文；同一 roamingId 已存在时复用本地上下文并绑定新 Session。
    /// </summary>
    /// <param name="session">要启用漫游的 Session。</param>
    /// <param name="roamingId">断线重连前后保持稳定的业务身份。</param>
    /// <param name="delayRemove">Session 释放后保留漫游上下文的毫秒数；小于等于 0 时立即销毁。</param>
    /// <returns>与该 roamingId 关联的漫游上下文。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FTask<SessionRoamingComponent> GetOrCreateRoaming(this Session session, long roamingId, int delayRemove = RoamingConstants.DefaultDelayRemoveMs)
    {
        return session.Scene.RoamingComponent.GetOrCreateRoaming(session, roamingId, delayRemove);
    }

    /// <summary>
    /// 获取 Scene 中指定 roamingId 的漫游上下文。
    /// </summary>
    /// <param name="scene">漫游上下文所在的 Scene。</param>
    /// <param name="roamingId">业务定义的漫游身份。</param>
    /// <returns>已注册的漫游上下文；不存在时返回 <see langword="null"/>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SessionRoamingComponent GetRoaming(this Scene scene, long roamingId)
    {
        return scene.RoamingComponent.Get(roamingId);
    }

    /// <summary>
    /// 尝试获取 Scene 中指定 roamingId 的漫游上下文。
    /// </summary>
    /// <param name="scene">漫游上下文所在的 Scene。</param>
    /// <param name="roamingId">业务定义的漫游身份。</param>
    /// <param name="sessionRoamingComponent">找到的漫游上下文。</param>
    /// <returns>找到时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetRoaming(this Scene scene, long roamingId, out SessionRoamingComponent sessionRoamingComponent)
    {
        return scene.RoamingComponent.TryGet(roamingId, out sessionRoamingComponent);
    }

    /// <summary>
    /// 尝试获取当前 Session 绑定的漫游上下文。
    /// </summary>
    /// <param name="session">要查询的 Session。</param>
    /// <param name="sessionRoamingComponent">当前绑定的漫游上下文。</param>
    /// <returns>已绑定时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetRoaming(this Session session, out SessionRoamingComponent sessionRoamingComponent)
    {
        sessionRoamingComponent = session.SessionRoamingComponent;
        return sessionRoamingComponent !=  null;
    }

    /// <summary>
    /// 移除当前 Session 绑定的漫游上下文。
    /// </summary>
    /// <param name="session">已绑定漫游上下文的 Session。</param>
    /// <param name="delayRemove">延迟毫秒数；小于等于 0 时立即移除。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async FTask RemoveRoaming(this Session session, int delayRemove = 0)
    {
        if (session.SessionRoamingComponent == null || session.SessionRoamingComponent.Id == 0)
        {
            return;
        }

        await session.Scene.RoamingComponent.Remove(session.SessionRoamingComponent.Id, delayRemove);
    }

    /// <summary>
    /// 从 Scene 中移除指定 roamingId 的漫游上下文。
    /// </summary>
    /// <param name="scene">漫游上下文所在的 Scene。</param>
    /// <param name="roamingId">要移除的业务漫游身份。</param>
    /// <param name="delayRemove">延迟毫秒数；小于等于 0 时立即移除。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async FTask RemoveRoaming(this Scene scene, long roamingId, int delayRemove = 0)
    {
        if (roamingId == 0)
        {
            return;
        }

        await scene.RoamingComponent.Remove(roamingId, delayRemove);
    }
}
#endif
