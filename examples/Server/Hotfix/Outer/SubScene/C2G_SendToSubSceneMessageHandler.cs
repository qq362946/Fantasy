using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public class C2G_SendToSubSceneMessageHandler : Message<C2G_SendToSubSceneMessage>
{
    protected override async FTask Run(Session session, C2G_SendToSubSceneMessage message)
    {
        var subSceneRouteId = session.GetComponent<GateSubSceneFlagComponent>().SubSceneRouteId;
        session.Scene.NetworkMessagingComponent.SendInnerRoute(subSceneRouteId, new G2SubScene_SentMessage()
        {
            Tag = "Hi SubScene",
        });
        await FTask.CompletedTask;
    }
}