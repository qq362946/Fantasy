#if FANTASY_NET
using Fantasy.Entitas;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.Network.Roaming;

/// <summary>
/// 挂载在 Terminus 上的反向生命周期标记。
/// </summary>
/// <remarks>仅在 autoDispose 为 <see langword="true"/> 时添加，使 Terminus 销毁时一并销毁关联实体。</remarks>
internal sealed class TerminusEntityFlagComponent : Entity
{
    /// <summary>
    /// 需要随 Terminus 一同销毁的关联实体。
    /// </summary>
    public EntityReference<Entity> LinkEntity;

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        Entity linkEntity = LinkEntity;

        if (linkEntity != null)
        {
            // 组件跟随 Terminus 释放，因此在这里完成 Terminus -> Entity 的级联销毁。
            linkEntity.Dispose();
        }

        base.Dispose();
    }
}

/// <summary>
/// 挂载在关联实体上的 Terminus 标记和生命周期入口。
/// </summary>
/// <remarks>用于阻止实体重复关联；关联实体销毁时始终会级联销毁 Terminus。</remarks>
internal sealed class TerminusFlagComponent : Entity
{
    /// <summary>
    /// 当前实体关联的 Terminus。
    /// </summary>
    public EntityReference<Terminus> Terminus;

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        Terminus terminus = Terminus;

        if (terminus != null)
        {
            // Entity -> Terminus 的级联与 autoDispose 无关，保证业务实体消失后不会留下孤立路由。
            terminus.Dispose();
        }

        base.Dispose();
    }
}
#endif
