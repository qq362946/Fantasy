using Fantasy.Entitas.Interface;

namespace Fantasy;

public sealed class PlayerUnitDestroySystem : DestroySystem<PlayerUnit>
{
    protected override void Destroy(PlayerUnit self)
    {
        Log.Debug("111111111");
    }
}

public static class PlayerUnitSystem
{
    public static UnitInfo ToProtocol(this PlayerUnit self, bool autoReturn)
    {
        var unitInfo = UnitInfo.Create(autoReturn);

        unitInfo.UnitId = self.Id;
        unitInfo.Name = self.Name;
        unitInfo.UnitType = (int)self.UnitType;
        unitInfo.Pos = self.Transform.ToProtocol();

        return unitInfo;
    }
}