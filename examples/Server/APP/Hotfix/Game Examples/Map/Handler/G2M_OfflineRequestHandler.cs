using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class G2M_OfflineRequestHandler : RoamingRPC<PlayerUnit, G2M_OfflineRequest, M2G_OfflineResponse>
{
    protected override async FTask Run(PlayerUnit playerUnit, G2M_OfflineRequest request, M2G_OfflineResponse response, Action reply)
    {
        Log.Debug("1111111111");
        await FTask.CompletedTask;
    }
}