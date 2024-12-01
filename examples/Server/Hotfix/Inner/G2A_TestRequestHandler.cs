using System;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public class G2A_TestRequestHandler : RouteRPC<Scene, G2A_TestRequest, G2A_TestResponse>
{
    private static int Count;
    protected override async FTask Run(Scene entity, G2A_TestRequest request, G2A_TestResponse response, Action reply)
    {
        if (++Count % 1000000 == 0)
        {
            Log.Debug($"count:1000000");
        }
        // Log.Debug($"{Count}");
        await FTask.CompletedTask;
    }
}