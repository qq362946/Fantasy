namespace Fantasy.Hotfix.System;

public static class AccountManageComponentSystem
{
    public static Unit Add(this AccountManageComponent self, long unitId)
    {
        var unit = Entity.Create<Unit>(self.Scene, unitId);
        unit.Name = "Fantasy";
        self.Units.Add(unitId, unit);
        return unit;
    }
}