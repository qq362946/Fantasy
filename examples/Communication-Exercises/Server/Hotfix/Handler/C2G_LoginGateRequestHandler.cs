using Fantasy;

namespace BestGame;

public class C2G_LoginGateRequestHandler : MessageRPC<C2G_LoginGateRequest,G2C_LoginGateResponse>
{
    protected override async FTask Run(Session session, C2G_LoginGateRequest request, G2C_LoginGateResponse response, Action reply)
    {
        // 通信测试练习还没有用户账号系统，写定一个playerId
        // sessionPlayer存入PlayerId，用以在需要的地方，获取SessionPlayerComponent，取得PlayerId
        var sessionPlayer =  session.AddComponent<SessionPlayerComponent>();
        
        // 缓存玩家PlayerId
        sessionPlayer.playerId = 123433;

        Log.Debug($"<--收到请求登录网关的消息");
        response.Message = "-->登录网关成功";
        await FTask.CompletedTask;
    }
}