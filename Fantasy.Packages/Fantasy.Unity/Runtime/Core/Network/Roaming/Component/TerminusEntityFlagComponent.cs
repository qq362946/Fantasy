#if FANTASY_NET
using Fantasy.Entitas;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.Network.Roaming;

/// <summary>
/// 挂载到Terminus用，用来Terminus断开的时候断开关联的实体
/// </summary>
internal sealed class TerminusEntityFlagComponent : Entity
{
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
            linkEntity.Dispose();
        }
        
        base.Dispose();
    }
}

internal sealed class TerminusFlagComponent : Entity
{
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
            terminus.Dispose();
        }
        
        base.Dispose();
    }
}
#endif