using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.Roaming;

namespace Fantasy;

public sealed class OnDisposeTerminusEvent : AsyncEventSystem<OnDisposeTerminus>
{
    protected override async FTask Handler(OnDisposeTerminus self)
    {
        switch (self.Scene.SceneType)
        {
            case SceneType.Map:
            {
                Log.Debug($"Map断开了漫游");

                break;
            }
            case SceneType.Chat:
            {
                Log.Debug($"Chat断开了漫游");
                break;
            }
        }
        
        await FTask.CompletedTask;

    }
}