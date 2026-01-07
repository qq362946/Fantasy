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
    public static Position ToProtocol(this TransformComponent self)
    {
        return self.TempPosition.Transform(self.Position);
    }
}