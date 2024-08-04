namespace Fantasy;

public class I_G2A_PingRequestHandler : RouteRPC<Scene, I_G2A_PingRequest, I_A2G_PingResponse>
{
    private static int count;
    
    protected override async FTask Run(Scene scene, I_G2A_PingRequest request, I_A2G_PingResponse response, Action reply)
    {
        if (++count % 1000000 == 0)
        {
            Log.Debug($"count:1000000");
        }
        // Log.Debug(count.ToString());
        await FTask.CompletedTask;
    }
}