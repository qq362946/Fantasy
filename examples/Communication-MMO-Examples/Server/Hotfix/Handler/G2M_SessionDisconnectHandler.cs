using Fantasy;

namespace BestGame;

/// <summary>
/// 玩家掉线，从网关发来消息通知Map,作玩家退出地图处理
/// </summary>
public class G2M_SessionDisconnectHandler : Addressable<Unit,G2M_SessionDisconnectMsg>
{
    protected override async FTask Run(Unit unit, G2M_SessionDisconnectMsg message)
    {
        var unitId = unit.Id;
        var unitManager = unit.Scene.GetComponent<UnitManager>();

        // 玩家延迟2分钟退出地图处理，防止玩家掉线后，马上重连，频繁的进出地图操作
        // 这里为了测试方便，用3秒代替
        var hasQuit = await unitManager.QuitMap(unit,3000);

        if(!hasQuit){
            // 在地图延迟2分钟下线期间，玩家重新登录回到地图了
            // ...
        }else{
            Log.Info($"玩家{unitId}已经退出地图");
        }

        await FTask.CompletedTask;
    }
}