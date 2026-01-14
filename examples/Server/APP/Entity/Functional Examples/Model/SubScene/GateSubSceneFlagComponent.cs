using Fantasy.Entitas;

namespace Fantasy;

public sealed class GateSubSceneFlagComponent : Entity
{
    public long SubSceneAddressId;

    public override void Dispose()
    {
        SubSceneAddressId = 0;
        base.Dispose();
    }
}