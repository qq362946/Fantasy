using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Roaming;

namespace Fantasy;

public class C2Map_TestRoamingMessageHandler : Roaming<Terminus, C2Map_TestRoamingMessage>
{
    protected override async FTask Run(Terminus terminus, C2Map_TestRoamingMessage message)
    {
        Log.Debug($"C2Map_TestRoamingMessageHandler message:{message.Tag} SceneType:{terminus.Scene.SceneType} SceneId:{terminus.Scene.RuntimeId}");
        await FTask.CompletedTask;
    }
}