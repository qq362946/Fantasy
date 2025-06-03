using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Roaming;

namespace Fantasy;

public sealed class C2Chat_TestRoamingMessageHandler : Roaming<Terminus, C2Chat_TestRoamingMessage>
{
    protected override async FTask Run(Terminus terminus, C2Chat_TestRoamingMessage message)
    {
        Log.Debug($"C2Chat_TestRoamingMessageHandler message:{message.Tag} SceneType:{terminus.Scene.SceneType} SceneId:{terminus.Scene.RuntimeId}");
        await FTask.CompletedTask;
    }
}