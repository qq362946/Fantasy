using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;
using Fantasy.Sphere;

namespace Fantasy;

public sealed class TestSphereEvent : SphereEventArgs
{
    public string Tag { get; set; }
}

public sealed class C2G_SubscribeSphereEventRequestHandler : MessageRPC<C2G_SubscribeSphereEventRequest, G2C_SubscribeSphereEventResponse>
{
    protected override async FTask Run(Session session, C2G_SubscribeSphereEventRequest request, G2C_SubscribeSphereEventResponse response, Action reply)
    {
        var scene = session.Scene;
        var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
        var subscribeSphereEventResponse = (G2Map_SubscribeSphereEventResponse)await scene.NetworkMessagingComponent.CallInnerRoute(
            sceneConfig.RouteId, 
            new G2Map_SubscribeSphereEventRequest()
            {
                GateRouteId = scene.RouteId
            });
    }
}