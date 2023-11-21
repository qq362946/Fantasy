using Fantasy;

namespace BestGame;

public sealed class UnitComponentDestroySystem : DestroySystem<UnitComponent>
{
    protected override void Destroy(UnitComponent self)
    {
        self.units.Clear();
    }
}

public static class UnitComponentSystem
{
    public static async FTask<bool> QuitMap(this UnitComponent self, Unit unit,long delay=0)
    {
        if(delay>0)
        {
            // delay>0，unit准备掉线延迟下线
            unit.unitState = UnitState.DelayOffline;

            await TimerScheduler.Instance.Core.WaitAsync(delay);

            // 重连，unitState状态改变，unit终止下线
            if(unit.unitState != UnitState.DelayOffline)
                return false;
        }
        
        // 发消息到网关，其它服，gateAccount更新角色信息
        MessageHelper.SendInnerRoute(self.Scene,unit.GateSceneRouteId,new M2G_QuitMapMsg{
            AccountId = unit.Id,
        });

        // 聊天服务器下线,邮件服下线,帮会服下线！
        // ...

        // UnitCache缓存数据
        // await UnitCacheHelper.DisconnectSaveUnit(unit.Scene, unit.Id, unit.accountId);

        // 移除unit组件
        unit.RemoveComponent<MoveComponent>();
        unit.RemoveComponent<MoveSyncComponent>();
        unit.RemoveComponent<NoticeUnitSyncComponent>();

        // 移除AddressableMessageComponent
        await AddressableHelper.RemoveAddressable(unit.Scene, unit.Id);
        unit.RemoveComponent<AddressableMessageComponent>();
        self.Remove(unit.Id);

        return true;
    }
}