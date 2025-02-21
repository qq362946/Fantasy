using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Event;

namespace Fantasy;

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
        
        switch (scene.SceneType)
        {
            case SceneType.Addressable:
            {
                // scene.AddComponent<AddressableManageComponent>(); 
                _addressableSceneRunTimeId = scene.RuntimeId;
                break;
            }
            case SceneType.Map:
            {
                break;
            }
            case SceneType.Chat:
            {
                break;
            }
            case SceneType.Gate:
            {
                
                var instanceList = UnitConfigData.Instance.List;
                break;
            }
        }

        await FTask.CompletedTask;
    }
}