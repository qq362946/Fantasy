using Fantasy;

namespace BestGame;

/// 注册account存库并缓存账号在accountManager
/// 确定并将gateScene信息缓存在account
public class C2R_RegisterRequestHandler : MessageRPC<C2R_RegisterRequest,R2C_RegisterResponse>
{
    protected override async FTask Run(Session session, C2R_RegisterRequest request, R2C_RegisterResponse response, Action reply)
    {
        response.ErrorCode = await Check(session, request, response);
    }

    private async FTask<uint> Check(Session session, C2R_RegisterRequest request, R2C_RegisterResponse response)
    {
        string authName = request.AuthName?.Trim() ?? "";
        string pw = request.Pw?.Trim() ?? "";
        string pw2 = request.Pw2?.Trim() ?? "";

        // 版本号不一致
        if (request.Version != ConstValue.Version)
            return ErrorCode.H_A2C_Login_Res_VersionERROR;
        
        // 账号校验
        var err = authName.IsAccountValid();
        if (err != ErrorCode.Success) 
            return err;
        
        // 两次密码不一致
        if (pw != pw2) 
            return ErrorCode.Error_ReSetPwNotSame;

        // 密码校验
        err = pw.IsPasswordValid();
        if (err != ErrorCode.Success) 
            return err;
        

        var accountManager = session.Scene.GetComponent<AccountManager>();
        
        // 为什么要Lock？这样此时别人不能用这个名注册
        var _authAccountLock = new CoroutineLockQueueType("AuthAccountLock");
        using (await _authAccountLock.Lock(authName.GetHashCode()))
        {
            long now = TimeHelper.Now;
            Account account = await accountManager.GetAccount(authName);

            // 账号已注册
            if (account != null)
                return ErrorCode.Error_RegisterAccountAlreayRegister;

            var zoneId = request.ZoneId;
            // 生成账号id
            var zoneAuthId = IdGenerate.GenerateId(zoneId);

            // 所在区服随机一个Gate
            // 存库记录这个网关SceneId，此账号以后都登录此网关
            // 从SceneConfig获取的Id，都是Scene的ConfigId，不要与scene实例Id混淆概念啦
            var gateSceneId = SceneHelper.GetSceneRandom(SceneType.Gate,zoneId).Id;
            var ip = session.RemoteEndPoint.ToString();

            // 创建帐号，赋值帐号数据
            account = Entity.Create<Account>(session.Scene,zoneAuthId);
            account.GateSceneId = gateSceneId; 
            account.Phone = "";
            account.AuthName = authName;
            account.RegisterIp = ip;
            account.RegisterTime = now;
            account.LastLoginIp = ip;
            account.LastLoginTime = now;
            account.Pw = pw;
            accountManager.AccountDic.Add(authName, account);

            // 存库
            await accountManager.SaveAccount(account);

            return ErrorCode.Success;
        }
    }
}