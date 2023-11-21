using Fantasy;

namespace BestGame;

public class C2R_RegisterRequestHandler : MessageRPC<C2R_RegisterRequest,R2C_RegisterResponse>
{
    protected override async FTask Run(Session session, C2R_RegisterRequest request, R2C_RegisterResponse response, Action reply)
    {
        Log.Debug($"<--收到注册账号的请求");
        response.Message = "-->注册账号成功";

        await FTask.CompletedTask;
    }
}