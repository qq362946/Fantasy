using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public class G2SubScene_SentMessageHandler : Route<Scene, G2SubScene_SentMessage>
{
    protected override async FTask Run(Scene scene, G2SubScene_SentMessage message)
    {
        Log.Debug($"接受到来自Gate的消息 SceneType:{scene.SceneType} Message:{message.Tag}");
        await FTask.CompletedTask;
    }
}