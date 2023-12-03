using Fantasy;

namespace BestGame
{
    public sealed class GuiderComponent : Entity
    {
        public void RegisterGate()
        {

        }

        public void RegisterRealme()
        {

        }

        public void RegisterMap()
        {

        }

        /// 获取网关账号缓存，无则创建网关账号并缓存，存库
        public async FTask<(uint, GateAccount)> LoginCheck(Session session, long accountId,string authName)
        {
            Scene scene = session.Scene;

            var accountManager = scene.GetComponent<GateAccountManager>();

            // 缓存有网关账号
            if(accountManager.TryGetValue(accountId, out GateAccount gateAccount))
            {
                // 存在连接
                if (gateAccount.TryGeySession(out Session existedSession))
                {
                    // 同Session重复连接，异常直接断开
                    if (existedSession == session)
                    {
                        session.Disconnect(0).Coroutine();
                        return (ErrorCode.Error_LoginGateSameSession, null);
                    }
                    // 他处网关账号顶下线
                    existedSession.ForceDisconnect().Coroutine();
                }
            }
            else
            {
                var db = scene.World.DateBase;
                gateAccount = await db.Query<GateAccount>(accountId);
                // 没有网关账号创建一个
                if (gateAccount == null)
                {
                    gateAccount = Entity.Create<GateAccount>(session.Scene, accountId);
                    gateAccount.AuthName = authName;
                    gateAccount.RegisterTime = TimeHelper.Now;

                    // 账号存库
                    await db.Save(gateAccount);
                }  
                accountManager.GateAccounts.Add(accountId, gateAccount);
            }

            return (ErrorCode.Success, gateAccount);
        }

        /// 网关帐号完成登录状态
        public void LoggedIn(Session session,GateAccount gateAccount)
        {
            if (!session.IsDisposed)
            {
                // 在session上缓存玩家PlayerId,gateAccount
                var sessionPlayer = session.GetComponent<SessionPlayerComponent>() 
                    ?? session.AddComponent<SessionPlayerComponent>();
                
                // gateAccount.Id = accountId
                sessionPlayer.gateAccount = gateAccount;

                // gateAccount缓存登录状态，SessionRumtimeId
                gateAccount.SessionRumtimeId = session.RuntimeId;
                gateAccount.LoginedGate = true;
            }
        }

        /// 设置角色进入地图
        public void SetRoleEnterMap(Session session,int mapNum,long sessionRuntimeId)
        {
            
            var sessionPlayer = session.GetComponent<SessionPlayerComponent>();
            var gateAccount = sessionPlayer.gateAccount;
            var gateRole = gateAccount.GetRole(gateAccount.SelectRoleId);

            // 记录最后进入角色时间
            gateRole.LastEnterRoleTime = TimeHelper.Now;

            // 记录最后进入的地图
            gateRole.LastMap = mapNum;
            // 设置在线状态
            gateRole.State = RoleState.Online;
            // 记录网关sessionId
            gateRole.sessionRuntimeId = sessionRuntimeId;
            
            // Session有效才挂AddressableRouteComponent
            if (LoginHelper.CheckSessionValid(session, sessionRuntimeId))
            {
                // 挂寻址路由组件，session就可以收、转发路由消息了
                // AddressableRouteComponent组件是只给session用的，SetAddressableId设置转发目标
                session.AddComponent<AddressableRouteComponent>().SetAddressableId(gateAccount.AddressableId);
            }
        }
    }
}