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

            // 向map发送断线消息
            var mapScene = gateAccount.GetMapScene(gateRole.LastMap,self.Scene.World.Id);
            // MessageHelper.SendInnerRoute(self.Scene,mapScene.EntityId,new G2M_SessionDisconnectMsg{
            //     UnitId = gateRole.Id
            // });
            Log.Debug($"SessionPlayerComponentDestroySystem Destroy {self.Id}");

            // 强制退出网关
            gateAccountManager.QuitGate(gateAccount,true);
        }
    }
}