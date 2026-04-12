#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy.Network.Roaming;

internal sealed class SessionRoamingFlgComponent : Entity
{
    private bool _isInnerDisposed;
    public bool DoNotRemove;
    public int DelayRemove;
    
    public override void Dispose()
    {
        if (_isInnerDisposed)
        {
            return;
        }
        
        _isInnerDisposed = true;
        DisposeAsync().Coroutine();
    }

    private async FTask DisposeAsync()
    {
        var roamingId = Id;

        if (!DoNotRemove && DelayRemove > 0)
        {
            var sceneRoamingComponent = Scene.RoamingComponent;

            if (sceneRoamingComponent.TryGet(roamingId, out var roamingComponent))
            {
                await roamingComponent.StopForwarding();
                await sceneRoamingComponent.Remove(roamingId, 0, DelayRemove);
            }
        }

        DelayRemove = 0;
        DoNotRemove = false;
        _isInnerDisposed = false;
        
        base.Dispose();
    }
}
#endif
