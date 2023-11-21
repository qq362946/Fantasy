using Fantasy;

namespace BestGame;

public class C2R_LoginRequestHandler : MessageRPC<C2R_LoginRequest,R2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2R_LoginRequest request, R2C_LoginResponse response, Action reply)
    {
        Log.Debug($"<--收到登录账号的请求");
        response.Message = "-->登录账号成功";

        await FTask.CompletedTask;
    }
}