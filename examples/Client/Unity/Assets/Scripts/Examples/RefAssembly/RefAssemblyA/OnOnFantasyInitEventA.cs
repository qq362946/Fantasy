using Fantasy.Async;
using Fantasy.Event;

namespace Fantasy
{
    public struct TestRefAssemblyEventA
    {
        
    }

    public class OnOnFantasyInitEvent : AsyncEventSystem<OnCreateScene>
    {
        protected override async FTask Handler(OnCreateScene self)
        {
            Log.Debug("OnOnFantasyInitEventA Handler");
            self.Scene.EventComponent.Publish(new TestRefAssemblyEventA());
            await FTask.CompletedTask;
        }
    }
}