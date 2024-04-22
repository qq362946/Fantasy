using Fantasy;

namespace Hotfix;


public class TestServerRequestHandler : MessageRPC<TestServerRequest, TestServerResponse>
{
    private static int count;

    protected override async FTask Run(Session session, TestServerRequest request, TestServerResponse response, Action reply)
    {
        // Log.Debug($"count:1000000");
        if (++count % 1000000 == 0)
        {
            Log.Debug($"count:1000000");
        }
        // Log.Debug($"KcpClientReceive count:{count}");
        // Log.Debug($"{count}");
        // Log.Debug($"TestServerRequestHandler:{request.Tag} Server:{session.Scene.Server.Id} Scene:{session.Scene.SceneConfigId}");
        await FTask.CompletedTask;
    }
}