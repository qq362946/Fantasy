using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public sealed class G2M_PalyerJoinHandler : Roaming<PlayerUnit,G2M_PalyerJoin>
{
    protected override async FTask Run(PlayerUnit playerUnit, G2M_PalyerJoin message)
    {
        Log.Debug($"{playerUnit.Name} message:{message.Sex}");
        await FTask.CompletedTask;
    }
}