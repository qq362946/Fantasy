using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Roaming;

namespace Fantasy;

public sealed class Chat2M_TestMessageHandler : Roaming<Terminus, Chat2M_TestMessage>
{
    protected override async FTask Run(Terminus terminus, Chat2M_TestMessage message)
    {
        Log.Debug($"Chat2M_TestMessageHandler message:{message.Tag} SceneType:{terminus.Scene.SceneType} SceneId:{terminus.Scene.RuntimeId}");
        await FTask.CompletedTask;
    }
}