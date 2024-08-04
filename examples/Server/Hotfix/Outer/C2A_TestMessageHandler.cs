namespace Fantasy;

public class C2A_TestMessageHandler : Message<C2A_TestMessage>
{
    protected override async FTask Run(Session session, C2A_TestMessage message)
    {
        Log.Debug($"C2A_TestMessageHandler Authentication RunTimeId:{session.Scene.RunTimeId} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
        await FTask.CompletedTask;
    }
}