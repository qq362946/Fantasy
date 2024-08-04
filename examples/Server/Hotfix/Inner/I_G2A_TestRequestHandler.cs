namespace Fantasy;

public class I_G2A_TestRequestHandler : RouteRPC<Scene, I_G2A_TestRequest, I_A2G_TestResponse>
{
    protected override async FTask Run(Scene scene, I_G2A_TestRequest request, I_A2G_TestResponse response, Action reply)
    {
        await FTask.CompletedTask;
        Log.Debug($"Authentication RunTimeId:{scene.RunTimeId} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId} message:{request.Name}");
        response.Name = "88888888";
    }
}