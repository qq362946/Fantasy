using Fantasy;

namespace BestGame;

public class CreateCharacterRequestHandler : MessageRPC<C2G_CreateCharacterRequest,G2C_CreateCharacterResponse>
{
    protected override async FTask Run(Session session, C2G_CreateCharacterRequest request, G2C_CreateCharacterResponse response, Action reply)
    {
        Log.Debug($"<--收到创建角色的请求");
        response.Message = "-->创建角色成功";

        await FTask.CompletedTask;
    }
}