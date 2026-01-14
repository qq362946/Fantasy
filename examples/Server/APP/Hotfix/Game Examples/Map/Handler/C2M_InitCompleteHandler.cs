using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public sealed class C2M_InitCompleteHandler : Roaming<PlayerUnit, C2M_InitComplete>
{
    protected override async FTask Run(PlayerUnit unit, C2M_InitComplete message)
    {
        if (!unit.TryGetLinkTerminus(out var linkTerminus))
        {
            Log.Error($"PlayerUnit:{unit.Id} not link terminus");
            return;
        }

        var scene = unit.Scene;
        var unitId = unit.Id;
        using var unitInfo = unit.ToProtocol(false);
        var playerUnitManageComponent = unit.Scene.GetComponent<PlayerUnitManageComponent>();
        
        // 1. 同步场景中其他单位给新玩家
        PlayerUnitManageHelper.SyncOtherUnits(linkTerminus, scene, unitId, playerUnitManageComponent);
        // 2. 发送自己的单位给客户端
        PlayerUnitManageHelper.SendUnitCreate(linkTerminus, unitInfo, isSelf: true);
        // 3. 将新玩家广播给场景中的其他人
        PlayerUnitManageHelper.BroadcastUnitCreate(scene, unitInfo, unitId, playerUnitManageComponent);
        
        await FTask.CompletedTask;
    }
}