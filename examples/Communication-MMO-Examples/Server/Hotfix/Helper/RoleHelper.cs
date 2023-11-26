using Fantasy;

namespace BestGame;

public static class RoleHelper
{
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

        return MessageInfoHelper.MoveInfo(position, rotation);
    }
}