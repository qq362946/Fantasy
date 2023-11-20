using Fantasy;

namespace BestGame;
public class UnitComponent : Entity 
{
    /// unit容器
    public readonly Dictionary<long, Unit> units = new Dictionary<long, Unit>();

    public Unit Get(long id)
    {
        units.TryGetValue(id, out Unit unit);
        return unit;
    }

    public Unit[] GetAll()
    {
        return units.Values.ToArray();
    }

    public void Add(Unit unit)
    {
        units.Add(unit.Id, unit);
    }

    public void Remove(long id)
    {
        Unit unit;
        units.TryGetValue(id, out unit);
        units.Remove(id);
        unit?.Dispose();
    }
}