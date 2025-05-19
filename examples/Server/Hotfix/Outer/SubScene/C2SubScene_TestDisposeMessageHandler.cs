using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public class C2SubScene_TestDisposeMessageHandler : Addressable<Unit, C2SubScene_TestDisposeMessage>
{
    protected override async FTask Run(Unit unit, C2SubScene_TestDisposeMessage message)
    {
        var unitScene = unit.Scene;
        var unitSceneSceneType = unitScene.SceneType;
        unitScene.Dispose();
        Log.Debug($"{unitSceneSceneType} {unitScene.RuntimeId} is Dispose!");
        await FTask.CompletedTask;
    }
}