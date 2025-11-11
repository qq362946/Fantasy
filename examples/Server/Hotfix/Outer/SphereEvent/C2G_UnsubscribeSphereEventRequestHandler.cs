using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

namespace Fantasy;

public sealed class C2G_UnsubscribeSphereEventRequestHandler : MessageRPC<C2G_UnsubscribeSphereEventRequest, G2C_UnsubscribeSphereEventResponse>
{
    protected override async FTask Run(Session session, C2G_UnsubscribeSphereEventRequest request, G2C_UnsubscribeSphereEventResponse response, Action reply)
    {
        var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
        // 注销远程订阅者
        await session.Scene.SphereEventComponent.RevokeRemoteSubscriber<TestSphereEvent>(sceneConfig.Address);
    }
}