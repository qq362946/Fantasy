using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Roaming;

namespace Fantasy;

public class C2Chat_TestRPCRoamingRequestHandler : RoamingRPC<Terminus, C2Chat_TestRPCRoamingRequest, Chat2C_TestRPCRoamingResponse>
{
    protected override async FTask Run(Terminus terminus, C2Chat_TestRPCRoamingRequest request, Chat2C_TestRPCRoamingResponse response, Action reply)
    {
        Log.Debug($"C2Chat_TestRPCRoamingRequestHandler message:{request.Tag} SceneType:{terminus.Scene.SceneType} SceneId:{terminus.Scene.RuntimeId}");
        await FTask.CompletedTask;
    }
}