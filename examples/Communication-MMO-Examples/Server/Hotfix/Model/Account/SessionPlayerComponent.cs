using Fantasy;
namespace BestGame;

/// 这是一个在网关缓存玩家基本信息的组件，添加给客户端与网关的连接session
public class SessionPlayerComponent : Entity
{
    /// 缓存角色网关账号
    public GateAccount gateAccount;
    /// 缓存session状态
    public SessionState EnterState;
}

public enum SessionState
{
    None = 0,
    Entering = 1, // 登陆中
    Enter = 2, // 进入游戏
}

public static class SessionPlayerComponentSystem
{
    public class SessionPlayerComponentDestroySystem: DestroySystem<SessionPlayerComponent>
    {
        protected override void Destroy(SessionPlayerComponent self)
        {
            var gateAccountManager = self.Scene.GetComponent<GateAccountManager>();
            var gateAccount = self.gateAccount;
            var gateRole = gateAccount.GetCurRole();

            // gateRole==null,说明还没选定角色进入地图，直接退出网关
            if(gateRole != null)
            {
                // 向map发送断线消息
                MessageHelper.SendAddressable(self.Scene,gateAccount.SelectRoleId,new G2M_SessionDisconnectMsg{});

                // 退出网关，保存角色数据
                gateAccountManager.QuitGame(gateAccount);
            }
            else
            {
                // 退出网关，只重置gateAccount数据
                gateAccountManager.QuitGate(gateAccount,true);
            }
            
            Log.Debug($"玩家断开网关,SessionPlayerComponent Destroy {self.Id}");
        }
    }
}