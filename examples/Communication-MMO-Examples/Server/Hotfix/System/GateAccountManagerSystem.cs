using Fantasy;
using MongoDB.Driver.Core.Operations;

namespace BestGame;

public sealed class GateAccountManagerDestroySystem : DestroySystem<GateAccountManager>
{
    protected override void Destroy(GateAccountManager self)
    {
        self.GateAccounts.Clear();
    }
}

/// 网关帐号管理,角色退出网关方法
public static class GateAccountManagerSystem
{
    public static void Add(this GateAccountManager self, GateAccount gateAccount)
    {
        self.GateAccounts.Add(gateAccount.Id, gateAccount);
    }

    public static bool TryGetValue(this GateAccountManager self, long accountId, out GateAccount gateAccount)
    {
        return self.GateAccounts.TryGetValue(accountId, out gateAccount);
    }

    public static void Remove(this GateAccountManager self, GateAccount gateAccount)
    {
        self.GateAccounts.Remove(gateAccount.Id);
        gateAccount.Dispose();
    }

    // 收到地图菜单操作消息主动退出游戏，或者掉线退出游戏
    public static void QuitGame(this GateAccountManager self, GateAccount gateAccount)
    {
        var gateRole = gateAccount.GetCurRole();

        // 设置在线状态
        gateRole.State = RoleState.None;
        // 重置网关sessionId
        gateRole.sessionRuntimeId = 0;

        // 角色数据变化存库
        gateAccount.SaveRole(gateRole).Coroutine(); 

        // 同时退出网关
        self.QuitGate(gateAccount);
    }

    // 菜单操作主动退出网关，或者掉线退出网关
    // force: true掉线退出网关，false主动退出网关
    // 重置gateAccount数据，和移除sessionPlayerComponent,寻址路由组件
    public static void QuitGate(this GateAccountManager self, GateAccount gateAccount,bool force = false)
    {
        // 不用从GateAccountManager清除gateAccount的，重置gateAccount数据即可
        gateAccount.SessionRumtimeId = 0;
        gateAccount.LoginedGate = false;
        gateAccount.SelectRoleId = 0;

        // 如果是掉线强制退出网关，已经dispose不用移除sessionPlayerComponent,寻址路由组件
        if(force) return;

        // 断开session连接，移除sessionPlayerComponent,寻址路由组件
        if (gateAccount.TryGeySession(out Session session))
            session.Disconnect(0).Coroutine();
    }
}