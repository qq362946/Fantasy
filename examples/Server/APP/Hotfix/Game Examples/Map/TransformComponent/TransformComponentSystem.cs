using System.Numerics;
using Fantasy.Entitas.Interface;

namespace Fantasy;

public sealed class TransformComponentDestroySystem : DestroySystem<TransformComponent>
{
    protected override void Destroy(TransformComponent self)
    {
        self.Position = Vector3.Zero;
        self.Rotation = Quaternion.Zero;
    }
}

public static class TransformComponentSystem
{
    public static PositionInfo ToProtocol(this TransformComponent self)
    {
        var positionInfo = PositionInfo.Create();
        positionInfo.Pos = self.TempPosition.Transform(self.Position);
        return positionInfo;
    }
}