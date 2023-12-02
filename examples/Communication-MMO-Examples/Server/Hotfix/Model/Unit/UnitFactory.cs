using Fantasy;
using BestGame;
public static class UnitFactory
{
    /// <summary>
    /// 创建UnitInfo
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public static RoleInfo CreateUnitInfo(this Unit unit)
    {
        var info = unit.RoleInfo;
        info.LastMoveInfo = new MoveInfo(){
            Position = unit.Position.ToPosition(),
            Rotation = unit.Rotation.ToRotation(),
        };

        return info;
    }
}