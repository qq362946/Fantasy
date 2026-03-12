using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Model.Roaming;
using Fantasy.Network.Roaming;

namespace Fantasy.Outer.Roaming;

public sealed class OnCreateTerminusEvent : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        switch (self.Scene.SceneType)
        {
            case SceneType.Map:
            {
                if (self.Args is MaoRoamingArgs maoRoamingArgs)
                {
                    Log.Debug($"MaoRoamingArgs {maoRoamingArgs.Tag} {self.Type.ToString()}");
                }
                
                Log.Debug($"OnCreateTerminusEvent Map {self.Type.ToString()}");

                break;
            }
            case SceneType.Chat:
            {
                Log.Debug($"OnCreateTerminusEvent Chat {self.Type.ToString()}");
                break;
            }
        }

        await FTask.CompletedTask;
    }
}