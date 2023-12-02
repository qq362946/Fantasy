namespace Fantasy;

public class AOIEntity : Entity
{
    // 所在的格子
    public Cell Cell;
    // 可视距离
    public int ViewDistance;
    // 所属于的Unit
    public Unit Unit => (Unit)this.Parent;
    // 进入视野的Cell
    public HashSet<long> SubEnterCells = new HashSet<long>();
    // 离开视野的Cell
    public HashSet<long> SubLeaveCells = new HashSet<long>();
    // 我看的见的Unit
    public readonly Dictionary<long, AOIEntity> SeeUnits = new Dictionary<long, AOIEntity>();
    // 看见我的Unit
    public readonly Dictionary<long, AOIEntity> BeSeeUnits = new Dictionary<long, AOIEntity>();
    // 我看的见的Player
    public readonly Dictionary<long, AOIEntity> SeePlayers = new Dictionary<long, AOIEntity>();
    // 看见我的Player
    public readonly Dictionary<long, AOIEntity> BeSeePlayers = new Dictionary<long, AOIEntity>();
}