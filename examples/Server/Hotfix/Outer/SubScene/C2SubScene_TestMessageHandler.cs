using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public class C2SubScene_TestMessageHandler : Addressable<Unit, C2SubScene_TestMessage>
{
    protected override async FTask Run(Unit unit, C2SubScene_TestMessage message)
    {
        Log.Debug($"C2M_TestMessageHandler = {message.Tag} SceneType:{unit.Scene.SceneType}");
        await FTask.CompletedTask;
    }
}