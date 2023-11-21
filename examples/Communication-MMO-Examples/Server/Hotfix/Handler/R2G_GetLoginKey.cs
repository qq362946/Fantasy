using Fantasy;

namespace BestGame;

public class R2G_GetLoginKeyHandler : RouteRPC<Scene,R2G_GetLoginKeyRequest,G2R_GetLoginKeyResponse>
{
    protected override async FTask Run(Scene scene,R2G_GetLoginKeyRequest request, G2R_GetLoginKeyResponse response, Action reply)
    {
        long key = RandomHelper.RandInt64();
        var gateKey = new GateKey{
            AuthName = request.AuthName,
            AccountId = request.AccountId
        };
        scene.GetComponent<SessionKeyComponent>().Add(key, gateKey);
        response.Key = key;

        await FTask.CompletedTask;
    }
}
