using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Platform.Unity;

namespace Fantasy
{
    public class TestRefAssemblyEventB
    {
        
    }
    
    public class OnOnFantasyInitEventB : AsyncEventSystem<OnCreateScene>
    {
        protected override async FTask Handler(OnCreateScene self)
        {
            Log.Debug("OnOnFantasyInitEventB Handler");
            self.Scene.EventComponent.Publish(new TestRefAssemblyEventA());
            await FTask.CompletedTask;
        }
    }

    public class OnTestRefAssemblyEventAEvent : EventSystem<TestRefAssemblyEventA>
    {
        protected override void Handler(TestRefAssemblyEventA self)
        {
            Log.Debug("OnTestRefAssemblyEventAEvent Handler");
        }
    }
}