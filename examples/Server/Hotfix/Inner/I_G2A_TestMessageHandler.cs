namespace Fantasy;

public class I_G2A_TestMessageHandler : Route<Scene,I_G2A_TestMessage>
{
    protected override async FTask Run(Scene scene, I_G2A_TestMessage message)
    {
        await FTask.CompletedTask;
        Log.Debug($"Authentication RunTimeId:{scene.RunTimeId} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId} message:{message.Name}");
    }
}