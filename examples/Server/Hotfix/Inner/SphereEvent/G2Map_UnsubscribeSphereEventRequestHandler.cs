using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class G2Map_UnsubscribeSphereEventRequestHandler : AddressRPC<Scene, G2Map_UnsubscribeSphereEventRequest, Map2G_UnsubscribeSphereEventResponse>
{
    protected override async FTask Run(Scene scene, G2Map_UnsubscribeSphereEventRequest request, Map2G_UnsubscribeSphereEventResponse response, Action reply)
    {
        await scene.SphereEventComponent.Unsubscribe<TestSphereEvent>(request.GateAddress);
    }
}