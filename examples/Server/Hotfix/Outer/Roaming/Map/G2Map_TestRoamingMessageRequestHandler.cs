using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public class G2Map_TestRoamingMessageRequestHandler : RoamingRPC<Terminus,G2Map_TestRoamingMessageRequest,Map2G_TestRoamingMessageResponse>
{
    protected override async FTask Run(Terminus terminus, G2Map_TestRoamingMessageRequest request, Map2G_TestRoamingMessageResponse response,
        Action reply)
    {
        Log.Debug($"G2Map_TestRoamingMessageRequestHandler Tag:{request.Tag}");
        await FTask.CompletedTask;
    }
}