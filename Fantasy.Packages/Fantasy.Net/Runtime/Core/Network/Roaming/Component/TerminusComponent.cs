#if FANTASY_NET
using System.Collections.Generic;
using System.Linq;
using Fantasy.Async;
using Fantasy.Entitas;
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.Network.Roaming;

/// <summary>
/// Terminus 建立事件的类型。
/// </summary>
public enum CreateTerminusType
{
    /// <summary>
    /// 未指定。
    /// </summary>
    None = 0,
    /// <summary>
    /// 目标 Scene 首次创建 Terminus。
    /// </summary>
    Link = 1,
    /// <summary>
    /// 目标 Scene 复用已有 Terminus 并恢复转发关系。
    /// </summary>
    ReLink = 2,
}

/// <summary>
/// Terminus 离开当前 Scene 的原因。
/// </summary>
public enum DisposeTerminusType
{
    /// <summary>
    /// 未指定。
    /// </summary>
    None = 0,
    /// <summary>
    /// 漫游关系断开，业务层通常在当前 Scene 执行下线清理。
    /// </summary>
    UnLink = 1,
    /// <summary>
    /// Terminus 已传送到其他 Scene，当前 Scene 只清理源端数据。
    /// </summary>
    Transfer = 2,
}

/// <summary>
/// Terminus 在目标 Scene 创建或恢复时发布的事件数据。
/// </summary>
public struct OnCreateTerminus
{
    /// <summary>
    /// 处理 Terminus 的目标 Scene。
    /// </summary>
    public readonly Scene Scene;
    /// <summary>
    /// 源端随 Link 请求传入的可选业务参数。
    /// </summary>
    public readonly Entity? Args;
    /// <summary>
    /// 本次创建或恢复的 Terminus。
    /// </summary>
    public readonly Terminus Terminus;
    /// <summary>
    /// 本次是首次创建还是恢复已有 Terminus。
    /// </summary>
    public readonly CreateTerminusType Type;

    /// <summary>
    /// 创建 Terminus 建立事件数据。
    /// </summary>
    /// <param name="scene">处理 Terminus 的目标 Scene。</param>
    /// <param name="createTerminusType">建立类型。</param>
    /// <param name="terminus">创建或恢复的 Terminus。</param>
    /// <param name="args">源端传入的可选业务参数。</param>
    public OnCreateTerminus(Scene scene, CreateTerminusType createTerminusType, Terminus terminus, Entity? args)
    {
        Args = args;
        Scene = scene;
        Terminus = terminus;
        Type = createTerminusType;
    }
}

/// <summary>
/// Terminus 从当前 Scene 断开或传送离开时发布的事件数据。
/// </summary>
public struct OnDisposeTerminus
{
    /// <summary>
    /// Terminus 当前所在的 Scene。
    /// </summary>
    public readonly Scene Scene;
    /// <summary>
    /// 即将离开当前 Scene 的 Terminus。
    /// </summary>
    public readonly Terminus Terminus;
    /// <summary>
    /// Terminus 离开当前 Scene 的原因。
    /// </summary>
    public readonly DisposeTerminusType Type;
    /// <summary>
    /// 创建 Terminus 离开事件数据。
    /// </summary>
    /// <param name="scene">Terminus 当前所在的 Scene。</param>
    /// <param name="disposeTerminusType">离开原因。</param>
    /// <param name="terminus">即将离开的 Terminus。</param>
    public OnDisposeTerminus(Scene scene, DisposeTerminusType disposeTerminusType, Terminus terminus)
    {
        Scene = scene;
        Terminus = terminus;
        Type = disposeTerminusType;
    }
}

/// <summary>
/// 管理当前目标 Scene 中全部 Terminus 的创建、重连、传送和销毁。
/// </summary>
/// <remarks>组件随 Scene 初始化，业务层通过 Terminus 生命周期事件创建、恢复或清理关联实体。</remarks>
public sealed class TerminusComponent : Entity
{
    // Close 后拒绝新的 Link 或 Transfer 注册。
    private bool _isClosed;
    // Dispose 是异步入口，该标记防止关闭流程被重复启动。
    private bool _isDisposing;

    /// <summary>
    /// 按 roamingId 保存当前 Scene 中的 Terminus。
    /// </summary>
    private readonly Dictionary<long, Terminus> _terminals = new();

    /// <summary>
    /// 异步关闭全部 Terminus 后销毁组件。
    /// </summary>
    public override void Dispose()
    {
        if (IsDisposed || _isDisposing)
        {
            return;
        }

        _isDisposing = true;
        DisposeAsync().Coroutine();
    }

    private async FTask DisposeAsync()
    {
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
    /// 以断开原因关闭并清理当前 Scene 中的全部 Terminus。
    /// </summary>
    internal async FTask Close()
    {
        if (IsDisposed || _isClosed)
        {
            return;
        }

        _isClosed = true;
        var roamingIds = _terminals.Keys.ToArray();

        try
        {
            foreach (var roamingId in roamingIds)
            {
                try
                {
                    await RemoveTerminusAsync(
                        DisposeTerminusType.UnLink,
                        roamingId,
                        true);
                }
                catch (Exception e)
                {
                    Log.Error(
                        $"TerminusComponent Close failed, " +
                        $"roamingId:{roamingId} {e}");
                }
            }
        }
        finally
        {
            _terminals.Clear();
        }
    }

    /// <summary>
    /// 获取或创建 roamingId 对应的 Terminus，并刷新源端转发地址。
    /// </summary>
    /// <remarks>
    /// 首次建立时发布 <see cref="CreateTerminusType.Link"/>；已存在时视为重连并发布
    /// <see cref="CreateTerminusType.ReLink"/>。业务层无需在源端区分两条调用路径。
    /// </remarks>
    /// <param name="scene">Terminus 所在的目标 Scene。</param>
    /// <param name="roamingId">断线重连前后保持稳定的业务身份，不能为 0。</param>
    /// <param name="roamingType">当前目标 Scene 对应的漫游类型。</param>
    /// <param name="forwardSessionAddress">接收转发消息的 Session 地址。</param>
    /// <param name="ownerRoamingRuntimeId">拥有当前 Terminus 的 SessionRoamingComponent.RuntimeId。</param>
    /// <param name="forwardSceneAddress">管理源端漫游上下文的 Scene 地址。</param>
    /// <param name="args">传给 <see cref="OnCreateTerminus"/> 事件的可选业务参数。</param>
    /// <returns>错误码和 Terminus；错误码为 0 时 Terminus 有效。</returns>
    internal async FTask<(uint, Terminus)> Create(Scene scene, long roamingId, int roamingType, long forwardSessionAddress, long forwardSceneAddress, long ownerRoamingRuntimeId, Entity? args)
    {
        if (_isClosed)
        {
            return (InnerErrorCode.ErrRoamingDisposed, null);
        }

        if (roamingId == 0 || ownerRoamingRuntimeId == 0)
        {
            return (InnerErrorCode.ErrCreateTerminusInvalidRoamingId, null);
        }

        if (!_terminals.TryGetValue(roamingId, out var terminus))
        {
            // 首次 Link：先登记 Terminus，再让业务事件建立关联实体。
            terminus = Entity.Create<Terminus>(scene, roamingId, false, true);
            terminus.IsDisposeTerminus = false;
            terminus.RoamingType = roamingType;
            terminus.ForwardSceneAddress = forwardSceneAddress;
            terminus.OwnerRoamingRuntimeId = ownerRoamingRuntimeId;
            terminus.ForwardSessionAddress = forwardSessionAddress;
            terminus.RoamingMessageLock = scene.CoroutineLockComponent.Create(terminus.Type.TypeHandle.Value.ToInt64());

            _terminals.Add(terminus.Id, terminus);
            await scene.EventComponent.PublishAsync(new OnCreateTerminus(scene, CreateTerminusType.Link, terminus, args));
        }
        else
        {
            // ReLink：业务实体和 Terminus 保持不变，只恢复最新的源端转发地址。
            terminus.StopForwarding = false;
            terminus.ForwardSceneAddress = forwardSceneAddress;
            terminus.OwnerRoamingRuntimeId = ownerRoamingRuntimeId;
            terminus.ForwardSessionAddress = forwardSessionAddress;

            await scene.EventComponent.PublishAsync(new OnCreateTerminus(scene, CreateTerminusType.ReLink, terminus, args));
        }

        if (_isClosed || terminus.IsDisposeTerminus || terminus.IsDisposed)
        {
            return (InnerErrorCode.ErrRoamingDisposed, null);
        }

        if (terminus.TerminusId == 0)
        {
            // 业务事件没有关联实体时，Terminus 自身就是可寻址目标。
            terminus.TerminusId = terminus.RuntimeId;
        }

        return (0U, terminus);
    }

    /// <summary>
    /// 将传送到当前 Scene 的 Terminus 加入管理索引。
    /// </summary>
    /// <param name="terminus">传送过来的 Terminus。</param>
    /// <returns>组件仍可接收 Terminus 时返回 <see langword="true"/>。</returns>
    internal bool AddTerminus(Terminus terminus)
    {
        if (_isClosed)
        {
            return false;
        }

        _terminals.Add(terminus.Id, terminus);
        return true;
    }

    /// <summary>
    /// 尝试获取指定 roamingId 的 Terminus。
    /// </summary>
    /// <param name="roamingId">漫游唯一标识。</param>
    /// <param name="terminus">找到的 Terminus。</param>
    /// <returns>找到时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    internal bool TryGetTerminus(long roamingId, out Terminus terminus)
    {
        return _terminals.TryGetValue(roamingId, out terminus);
    }

    /// <summary>
    /// 获取指定 roamingId 的 Terminus。
    /// </summary>
    /// <param name="roamingId">漫游唯一标识。</param>
    /// <returns>对应的 Terminus 实例。</returns>
    /// <exception cref="KeyNotFoundException">指定 roamingId 不存在。</exception>
    internal Terminus GetTerminus(long roamingId)
    {
        return _terminals[roamingId];
    }

    /// <summary>
    /// 从索引中同步移除 Terminus，并按需触发销毁。
    /// </summary>
    /// <remarks>该入口用于传送注册失败时的本地回滚，不发布异步离开事件。</remarks>
    /// <param name="roamingId">要移除的业务漫游身份。</param>
    /// <param name="isDispose">是否同时销毁 Terminus。</param>
    internal void Remove(long roamingId, bool isDispose)
    {
        if (!_terminals.Remove(roamingId, out var terminus))
        {
            return;
        }

        if (!isDispose || terminus.IsDisposeTerminus)
        {
            return;
        }

        if (terminus.IsDisposed)
        {
            return;
        }

        terminus.Dispose();
    }

    /// <summary>
    /// 从索引中移除 Terminus，发布离开事件，并按需完成异步销毁。
    /// </summary>
    /// <param name="disposeTerminusType">Terminus 离开当前 Scene 的原因。</param>
    /// <param name="roamingId">要移除的业务漫游身份。</param>
    /// <param name="isDispose">是否在事件发布后销毁 Terminus。</param>
    internal async FTask RemoveTerminusAsync(DisposeTerminusType disposeTerminusType, long roamingId, bool isDispose)
    {
        if (!_terminals.Remove(roamingId, out var terminus))
        {
            return;
        }

        var scene = terminus.Scene;

        try
        {
            await scene.EventComponent.PublishAsync(new OnDisposeTerminus(scene, disposeTerminusType, terminus));
        }
        finally
        {
            if (isDispose &&
                !terminus.IsDisposeTerminus &&
                !terminus.IsDisposed)
            {
                await terminus.DisposeAsync(disposeTerminusType);
            }
        }
    }
}
#endif
