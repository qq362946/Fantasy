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
                var maoRoamingArgs = self.Args as MaoRoamingArgs;

                if (maoRoamingArgs == null)
                {
                    // Log.Error($"maoRoamingArgs is null!");
                    break;
                }
                
                Log.Debug($"MaoRoamingArgs {maoRoamingArgs.Tag}");

                break;
            }
        }

        await FTask.CompletedTask;
    }
}