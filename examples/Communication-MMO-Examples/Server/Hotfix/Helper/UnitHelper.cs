using Fantasy;

namespace BestGame;

public static class UnitHelper
{
    public static void NoticeUnitAdd(Unit unit, Unit enter)
    {
        var m2cUnitCreate = new M2C_UnitCreate();
        var unitInfo = enter.CreateUnitInfo();
        m2cUnitCreate.UnitInfos.Add(unitInfo);

        // 通知客户端有新的Unit进入到视野中
        MessageHelper.SendInnerRoute(unit.Scene, unit.SessionRuntimeId, m2cUnitCreate);
    }

    public static void NoticeUnitRemove(Unit unit, Unit leave)
    {
        var removeUnits = new M2C_UnitRemove();
        removeUnits.UnitIds.Add(leave.Id);
        
        // 通知客户端有Unit离开视野
        MessageHelper.SendInnerRoute(unit.Scene, unit.SessionRuntimeId, removeUnits);
    }

    public static uint GetConfigIdByClassName( string className)
    {
        var unitConfigs = UnitConfigData.Instance.List;
        foreach (var config in unitConfigs)
        {
            if (config.ClassName == className)
                return config.Id;
        }

        // 如果未找到匹配的对象，则返回默认值（例如 0）
        return 0;
    }

    public static int GetMapNumByUnitConfigId(uint unitConfigId)
    {
        var unitConfig = UnitConfigData.Instance.Get(unitConfigId);
        return unitConfig.MapNum;
    }

    public static MoveInfo GetMoveInfoByUnitConfigId(uint unitConfigId)
    {
        var unitConfig = UnitConfigData.Instance.Get(unitConfigId);
        var position = StringHelper.ParseCoordinates(unitConfig.Position);
        var rotation = StringHelper.ParseRotation(unitConfig.Angle);

        return MoveMessageHelper.MoveInfo(position, rotation);
    }
}