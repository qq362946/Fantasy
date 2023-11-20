using Fantasy.Core.Network;
using Fantasy.Helper;
using Fantasy;
using Fantasy.Core;

namespace BestGame;

/// 验证sessionKey，
/// 创建、缓存、获取网关账号
/// session上缓存玩家PlayerId,gateAccount
/// gateAccount缓存LoginedGate状态，session.RuntimeId
public class C2G_LoginGateRequestHandler : MessageRPC<C2G_LoginGateRequest,G2C_LoginGateResponse>
{
    protected override async FTask Run(Session session, C2G_LoginGateRequest request, G2C_LoginGateResponse response, Action reply)
    {
        response.ErrorCode = await Check(session, request, response);
    }

    private async FTask<uint> Check(Session session, C2G_LoginGateRequest request, G2C_LoginGateResponse response)
    {
        var SessionKeyComponent = session.Scene.GetComponent<SessionKeyComponent>();
        // 用key查出他的Account id
        var gateKey = SessionKeyComponent.Get(request.Key);

        var accountId = gateKey.AccountId;
        var authName = gateKey.AuthName;

        // 验证登录Key是否正确
        if (accountId == 0)
        {
            return ErrorCode.H_C2G_LoginGate_AccountIdIsNull;
        }

        var _LockGateAccountLock = new CoroutineLockQueueType("LockGateAccountLock");
        using (await _LockGateAccountLock.Lock(accountId))
        {
            // 缓存有就获取网关账号，无则创建网关账号
            var (err, gateAccount) = await LoginCheck(session, accountId,authName);

            if (err != ErrorCode.Success) return err;

            if (!session.IsDisposed)
            {
                // 在session上缓存玩家PlayerId,gateAccount
                var sessionPlayer = session.GetComponent<SessionPlayerComponent>() 
                    ?? session.AddComponent<SessionPlayerComponent>();
                
                sessionPlayer.playerId = accountId;
                sessionPlayer.gateAccount = gateAccount;

                // gateAccount缓存登录状态，SessionRumtimeId
                gateAccount.SessionRumtimeId = session.RuntimeId;
                gateAccount.LoginedGate = true;
            }

            // 使Key过期
            SessionKeyComponent.Remove(request.Key);
            return ErrorCode.Success;
        }
    }

    private async FTask<(uint, GateAccount)> LoginCheck(Session session, long accountId,string authName)
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
}