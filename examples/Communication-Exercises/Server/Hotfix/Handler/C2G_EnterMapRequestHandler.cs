using Fantasy;

namespace BestGame;

public class C2G_EnterMapRequestHandler : MessageRPC<C2G_EnterMapRequest,G2C_EnterMapResponse>
{
    protected override async FTask Run(Session session, C2G_EnterMapRequest request, G2C_EnterMapResponse response, Action reply)
    {
        var sessionPlayer = session.GetComponent<SessionPlayerComponent>();

        // 向map请求创建unit
        var entityId = SceneHelper.GetSceneEntityId(3072);
        var result = (M2G_CreateUnitResponse)await MessageHelper.CallInnerRoute(session.Scene,entityId,
        new G2M_CreateUnitRequest()
        {
            PlayerId = sessionPlayer.playerId,
            SessionRuntimeId = session.RuntimeId,
        });

        Log.Info(result.Message);

        // 挂寻址路由组件，session就可以收、转发路由消息了
        // AddressableRouteComponent组件是只给session用的，SetAddressableId设置转发目标
        session.AddComponent<AddressableRouteComponent>().SetAddressableId(result.AddressableId);

        // 缓存玩家AddressableId
        sessionPlayer.AddressableId = result.AddressableId;
        
        Log.Debug($"<--收到进入地图的请求");
        response.Message = "-->进入地图成功";
    }
}