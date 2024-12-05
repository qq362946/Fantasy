using Fantasy.Entitas;

namespace Fantasy;

public sealed class GateSubSceneFlagComponent : Entity
{
    public long SubSceneRouteId;

    public override void Dispose()
    {
        SubSceneRouteId = 0;
        base.Dispose();
    }
}