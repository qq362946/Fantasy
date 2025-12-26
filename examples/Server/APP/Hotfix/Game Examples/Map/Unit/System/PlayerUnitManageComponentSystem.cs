using Fantasy.Entitas.Interface;

namespace Fantasy;

public sealed class PlayerUnitManageComponentDestroySystem : DestroySystem<PlayerUnitManageComponent>
{
    protected override void Destroy(PlayerUnitManageComponent self)
    {
        foreach (var (_, unit) in self.Units)
        {
            unit.Dispose();
        }

        self.Units.Clear();
    }
}

public static class PlayerUnitManageComponentSystem
{
    public static bool Add(this PlayerUnitManageComponent self, PlayerUnit unit)
    {
        if (!self.Units.TryAdd(unit.Id, unit))
        {
            return false;
        }

        return true;
    }

    public static bool Remove(this PlayerUnitManageComponent self, long unitId, bool isDispose = true)
    {
        if (!self.Units.Remove(unitId, out var unit))
        {
            return false;
        }

        if (isDispose)
        {
            unit.Dispose();
        }
        
        return true;
    }

    public static bool TryGetUnit(this PlayerUnitManageComponent self, long unitId, out PlayerUnit unit)
    {
        return self.Units.TryGetValue(unitId, out unit!);
    }

    public static void PlayerJoin(this PlayerUnitManageComponent self, PlayerUnit playerUnit)
    {
        
    }
}