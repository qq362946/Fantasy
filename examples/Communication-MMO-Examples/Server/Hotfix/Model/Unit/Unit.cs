using Fantasy;

namespace BestGame;
public class Unit : Entity
{
    /// 客户端与网关连接SessionRuntimeId
    public long SessionRuntimeId;

    // 网关路由id
    public long GateSceneRouteId;

    /// 存入MoveInfo
    public MoveInfo moveInfo;

    /// 下线状态
    public UnitState unitState = UnitState.None;
}

public enum UnitState
{
    None = 0,
    NoOffline = 1, // 不下线
    CheckStay = 2, // 检测挂机
    ForceOffline = 3, // 强制下线
    DelayOffline = 4 // 延迟下线中
}