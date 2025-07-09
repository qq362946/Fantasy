using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public class
    G2Map_TestRouteMessageRequestHandler : RouteRPC<Terminus, G2Map_TestRouteMessageRequest,
    Map2G_TestRouteMessageResponse>
{
    protected override async FTask Run(Terminus terminus, G2Map_TestRouteMessageRequest request,
        Map2G_TestRouteMessageResponse response,
        Action reply)
    {
        Log.Debug($"G2Map_TestRouteMessageRequestHandler Tag:{request.Tag}");
        await FTask.CompletedTask;
    }
}