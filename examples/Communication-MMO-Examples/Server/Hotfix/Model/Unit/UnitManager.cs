using Fantasy;

namespace BestGame;
public class UnitManager : Entity 
{
    /// unit容器
    public readonly Dictionary<long, Unit> UnitDic = new Dictionary<long, Unit>();

    public void Add(Unit unit)
    {
        UnitDic.Add(unit.Id, unit);
    }

    public Unit Get(long id)
    {
        UnitDic.TryGetValue(id, out Unit unit);
        return unit;
    }

    public Unit[] GetAll()
    {
        return UnitDic.Values.ToArray();
    }
}