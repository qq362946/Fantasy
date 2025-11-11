using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

namespace Fantasy;

public sealed class C2G_MapUnsubscribeSphereEventRequestHandler : MessageRPC<C2G_MapUnsubscribeSphereEventRequest, G2C_MapUnsubscribeSphereEventResponse>
{
    protected override async FTask Run(Session session, C2G_MapUnsubscribeSphereEventRequest request, G2C_MapUnsubscribeSphereEventResponse response, Action reply)
    {
        var scene = session.Scene;
        var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
       
        _ = await scene.NetworkMessagingComponent.Call(
            sceneConfig.Address,
            new G2Map_UnsubscribeSphereEventRequest()
            {
                GateAddress = scene.Address
            });
    }
}