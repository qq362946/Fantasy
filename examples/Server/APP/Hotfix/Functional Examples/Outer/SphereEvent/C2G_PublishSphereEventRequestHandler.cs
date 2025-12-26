using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Sphere;

namespace Fantasy;

public class C2G_PublishSphereEventRequestHandler : MessageRPC<C2G_PublishSphereEventRequest, G2C_PublishSphereEventResponse>
{
    protected override async FTask Run(Session session, C2G_PublishSphereEventRequest request, G2C_PublishSphereEventResponse response, Action reply)
    {
        var testSphereEvent = SphereEventArgs.Create<TestSphereEvent>(true);
        testSphereEvent.Tag = "Hi Sphere Event!";
        await session.Scene.SphereEventComponent.PublishToRemoteSubscribers(testSphereEvent,true);
    }
}