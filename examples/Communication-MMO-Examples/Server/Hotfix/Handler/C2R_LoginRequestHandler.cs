using Fantasy;

namespace BestGame;

/// 验证账号，
/// 请求sessionKey，
/// 返回客户端网关地址
public class C2R_LoginRequestHandler : MessageRPC<C2R_LoginRequest,R2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2R_LoginRequest request, R2C_LoginResponse response, Action reply)
    {
        response.ErrorCode = await Check(session, request, response);
    }

    private async FTask<uint> Check(Session session, C2R_LoginRequest request, R2C_LoginResponse response)
    {
        string authName = request.AuthName?.Trim() ?? "";
        string pw = request.Pw?.Trim() ?? "";

        // 版本号不一致
        if (request.Version != ConstValue.Version)
            return ErrorCode.H_A2C_Login_Res_VersionERROR;

        // 账号校验
        var err = authName.IsAccountValid();
        if (err != ErrorCode.Success)
            return err;

        // 密码校验
        err = pw.IsPasswordValid();
        if (err != ErrorCode.Success)
            return err;
        
        var accountManager = session.Scene.GetComponent<AccountManager>();
        
        var _authAccountLock = new CoroutineLockQueueType("AuthAccountLock");
        using (await _authAccountLock.Lock(authName.GetHashCode()))
        {
            long now = TimeHelper.Now;
            // 从缓存获取账号或者查库、缓存账号
            var account = await accountManager.GetAccount(authName, true);

            // 账号没注册
            if (account == null)
                return ErrorCode.Error_LoginAccountNotRegister;

            // 密码错误
            if (account.Pw != pw)
                return ErrorCode.Error_LoginAccountPwError;

            var ip = session.RemoteEndPoint.ToString();

            // 请求网关key
            var keyRsp = (G2R_GetLoginKeyResponse) await MessageHelper.CallInnerRoute(session.Scene,
                SceneConfigData.Instance.Get(account.GateSceneId).EntityId,
                new R2G_GetLoginKeyRequest() { 
                    AuthName = account.AuthName,
                    AccountId = account.Id
                });

            // 返回网关地址,网关key
            var gateScene = SceneConfigData.Instance.Get(account.GateSceneId);
            response.GateAddress = SceneHelper.GetOutAddress(account.GateSceneId);
            response.GatePort = gateScene.OuterPort;
            response.Key = keyRsp.Key;

            // 更新登录时间数据
            account.LastLoginIp = ip;
            account.LastLoginTime = now;
            
            await accountManager.SaveAccount(account);

            return ErrorCode.Success;
        }
    }
}