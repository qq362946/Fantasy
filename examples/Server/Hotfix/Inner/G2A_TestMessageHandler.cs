using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public class G2A_TestMessageHandler : Route<Scene,G2A_TestMessage>
{
    protected override async FTask Run(Scene entity, G2A_TestMessage message)
    {
        
        
        Log.Debug($"G2A_TestMessageHandler :{message.Tag}");
        await FTask.CompletedTask;
    }
}