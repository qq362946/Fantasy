using Fantasy;

namespace BestGame;

public sealed class UnitManagerDestroySystem : DestroySystem<UnitManager>
{
    protected override void Destroy(UnitManager self)
    {
        self.UnitDic.Clear();
    }
}

public static class UnitManagerSystem
{
    public static async FTask SaveUnit(this UnitManager self, Unit unit)
    {
        await self.GetDB().Save(unit);
    }

    public static void CacheUnit(this UnitManager self, Unit unit,bool save = false)
    {
        if (!self.UnitDic.ContainsKey(unit.Id))
        {
            self.UnitDic.Add(unit.Id, unit);
            if(save) self.SaveUnit(unit).Coroutine();
        }
    }

    public static async FTask<Unit> GetUnit(this UnitManager self, long unitId)
    {
        self.UnitDic.TryGetValue(unitId, out Unit unit);

        if (unit == null){
            unit = await self.GetDB().Query<Unit>(unitId);
            if (unit != null) self.UnitDic.Add(unit.Id, unit);
        }
            
        return unit;
    }

    public static void Remove(this UnitManager self,long id)
    {
        Unit unit;
        self.UnitDic.TryGetValue(id, out unit);
        self.UnitDic.Remove(id);
        unit?.Dispose();
    }

    public static IDateBase GetDB(this UnitManager self)
    {
        return self.Scene.World.DateBase;
    }

    // 从地图下的菜单主动退出游戏
    public static async FTask QuitGame(this UnitManager self, Unit unit)
    {
        await DoQuit(self,unit);

        // 发消息到网关，其它服，gateAccount更新角色信息
        MessageHelper.SendInnerRoute(self.Scene,unit.GateRouteId,new M2G_QuitMapMsg{
            AccountId = unit.RoleInfo.AccountId
        });
    }

    // 收到网关消息，掉线退出地图
    public static async FTask<bool> QuitMap(this UnitManager self, Unit unit,long delay=0)
    {
        if(delay>0)
        {
            // delay>0，unit准备掉线延迟下线
            unit.unitState = OnlineState.DelayOffline;

            await TimerScheduler.Instance.Core.WaitAsync(delay);

            // 重连，unitState状态改变，unit终止下线
            if(unit.unitState != OnlineState.DelayOffline)
                return false;
        }

        DoQuit(self,unit).Coroutine();

        return true;
    }

    /// <summary>
    /// unit从地图移除,移除unit组件,AOI管理器移除unit,移除AddressableMessageComponent组件
    /// </summary>
    public static async FTask DoQuit(this UnitManager self, Unit unit)
    {
        // 聊天服务器下线,邮件服下线,帮会服下线！
        // ...

        // 保存unit及其组件下线前的数据
        // ...

        // 移除unit组件
        unit.RemoveComponent<MoveComponent>();
        unit.RemoveComponent<MoveSyncComponent>();

        // AOI管理器移除unit
        AOIHelper.RemoveAOI(unit);

        
        // 通常不用手动调用RemoveAddressable，只有在传送时可以用
        // 这里顺便举个例子，打开下面注释，会有两次RemoveAddressable
        // await AddressableHelper.RemoveAddressable(unit.Scene, unit.Id);

        // 移除AddressableMessageComponent组件
        unit.RemoveComponent<AddressableMessageComponent>();
        self.Remove(unit.Id);
        await FTask.CompletedTask;
    }
}