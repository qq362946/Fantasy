using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

namespace Fantasy;

public sealed class C2G_CreateSubSceneRequestHandler : MessageRPC<C2G_CreateSubSceneRequest, G2C_CreateSubSceneResponse>
{
    protected override async FTask Run(Session session, C2G_CreateSubSceneRequest request, G2C_CreateSubSceneResponse response, Action reply)
    {
        var scene = session.Scene;
        var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
        var createSubSceneResponse = (M2G_CreateSubSceneResponse)await scene.NetworkMessagingComponent.CallInnerRoute(sceneConfig.RouteId, new G2M_CreateSubSceneRequest());

        if (createSubSceneResponse.ErrorCode != 0)
        {
            // 创建SubScene失败。
            response.ErrorCode = createSubSceneResponse.ErrorCode;
            return;
        }
        
        // 记录下这个RouteId，以便后续的消息转发。
        session.AddComponent<GateSubSceneFlagComponent>().SubSceneRouteId = createSubSceneResponse.SubSceneRouteId;
    }
}