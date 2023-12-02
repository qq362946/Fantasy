
namespace Fantasy;

public class Cell : Entity
{
    public readonly Dictionary<long, AOIEntity> Units = new();
    // 订阅了这个Cell的进入事件
    public readonly Dictionary<long, AOIEntity> SubsEnterEntities = new();
    // 订阅了这个Cell的退出事件
    public readonly Dictionary<long, AOIEntity> SubsLeaveEntities = new();
}