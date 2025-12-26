namespace Fantasy;

public static class PlayerUnitSystem
{
    public static UnitInfo ToProtocol(this PlayerUnit self)
    {
        var unitInfo = UnitInfo.Create();

        unitInfo.UnitId = self.Id;
        unitInfo.Name = self.Name;
        unitInfo.UnitType = (int)self.UnitType;
        unitInfo.Pos = self.Transform.ToProtocol();

        return unitInfo;
    }
}