#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
namespace Fantasy.Network.Roaming;

internal sealed class SessionRoamingFlgComponent : Entity
{
    public int DelayRemove;
    
    public override void Dispose()
    {
        DisposeAsync().Coroutine();
    }

    private async FTask DisposeAsync()
    {
        var roamingId = Id;
        await Scene.RoamingComponent.Remove(roamingId, 0, DelayRemove);
        DelayRemove = 0;
        base.Dispose();
    }
}
#endif
