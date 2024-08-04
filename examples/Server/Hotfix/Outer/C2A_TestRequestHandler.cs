namespace Fantasy;

public class C2A_TestRequestHandler : MessageRPC<C2A_TestRequest,A2C_TestResponse>
{
    private static int count;
    protected override async FTask Run(Session session, C2A_TestRequest request, A2C_TestResponse response, Action reply)
    {
        if (++count % 10000 == 0)
        {
            Log.Debug($"count:10000");
        }
        // Log.Debug($"count:{count}");
        await FTask.CompletedTask;
        // Log.Debug($"C2A_TestMessageHandler Authentication RunTimeId:{session.Scene.RunTimeId} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
    }
}