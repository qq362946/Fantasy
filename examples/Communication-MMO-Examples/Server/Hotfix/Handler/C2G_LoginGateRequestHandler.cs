using Fantasy;

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
            return ErrorCode.H_C2G_LoginGate_AccountIdIsNull;
        
        var _LockGateAccountLock = new CoroutineLockQueueType("LockGateAccountLock");
        using (await _LockGateAccountLock.Lock(accountId))
        {
            // 缓存有就获取网关账号，无则创建网关账号
            var guider = session.Scene.GetComponent<GuiderComponent>();
            var (err, gateAccount) = await guider.LoginCheck(session, accountId,authName);
            if (err != ErrorCode.Success) return err;

            // 完成网关登录
            guider.LoggedIn(session,gateAccount);

            // 使Key过期
            SessionKeyComponent.Remove(request.Key);
            return ErrorCode.Success;
        }
    }
}