using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;

namespace Fantasy;

public sealed class UnitTransferOutSystem : TransferOutSystem<Unit>
{
    protected override async FTask Out(Unit self)
    {
        Log.Debug("UnitTransferOutSystem");
        await FTask.CompletedTask;
    }
}

public sealed class UnitTransferInSystem : TransferInSystem<Unit>
{
    protected override async FTask In(Unit self)
    {
        Log.Debug("UnitTransferInSystem");
        await FTask.CompletedTask;
    }
}

public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{

    private static long _addressableSceneRunTimeId;

    /// <summary>
    /// Handles the OnCreateScene event.
    /// </summary> 
    /// <param name="self">The OnCreateScene object.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        await FTask.CompletedTask;

        switch (scene.SceneType)
        {
            case 6666:
            {
                break;
            }
            case SceneType.Addressable:
            {
                _addressableSceneRunTimeId = scene.RuntimeId;
                break;
            }
            case SceneType.Map:
            {
                scene.AddComponent<PlayerUnitManageComponent>();
                break;
            }
            case SceneType.Chat:
            {
                break;
            }
            case SceneType.Gate:
            {
                scene.AddComponent<AccountManageComponent>();

                var unit = Entity.Create<Unit>(scene);
                
                await scene.EntityComponent.TransferOut(unit);
                await scene.EntityComponent.TransferIn(unit);
                break;
            }
        }
    }
}