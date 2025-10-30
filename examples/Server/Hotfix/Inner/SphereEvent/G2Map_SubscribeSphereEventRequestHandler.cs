using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Sphere;

namespace Fantasy;

public sealed class OnTestSphereEvent : SphereEventSystem<TestSphereEvent>
{
    protected override async FTask Handler(Scene scene, TestSphereEvent args)
    {
        Log.Debug($"OnTestSphereEvent {args.Tag} scene:{scene.SceneType}");
        await FTask.CompletedTask;
    }
}

public sealed class G2Map_SubscribeSphereEventRequestHandler : RouteRPC<Scene, G2Map_SubscribeSphereEventRequest, G2Map_SubscribeSphereEventResponse>
{
    protected override async FTask Run(Scene scene, G2Map_SubscribeSphereEventRequest request, G2Map_SubscribeSphereEventResponse response, Action reply)
    {
        await scene.SphereEventComponent.Subscribe<TestSphereEvent>(request.GateRouteId);
    }
}