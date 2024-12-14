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
                // var session = scene.GetSession(_addressableSceneRunTimeId);
                // var sceneNetworkMessagingComponent = scene.NetworkMessagingComponent;
                //
                // for (int i = 0; i < 1000; i++)
                // {
                //     sceneNetworkMessagingComponent.SendInnerRoute(_addressableSceneRunTimeId,new G2A_TestMessage()
                //     {
                //         Tag = $"{i}"
                //     });
                // }
                //
                // Log.Debug($"Send G2A_TestMessage 1000");
                
                
                // var tasks = new List<FTask>(2000);
                // var session = scene.GetSession(_addressableSceneRunTimeId);
                // var sceneNetworkMessagingComponent = scene.NetworkMessagingComponent;
                // var g2ATestRequest = new G2A_TestRequest();
                //
                // async FTask Call()
                // {
                //     await sceneNetworkMessagingComponent.CallInnerRouteBySession(session,_addressableSceneRunTimeId,g2ATestRequest);
                // }
                //
                // for (int i = 0; i < int.MaxValue; i++)
                // {
                //     tasks.Clear();
                //     for (int j = 0; j < tasks.Capacity; ++j)
                //     {
                //         tasks.Add(Call());
                //     }
                //     await FTask.WaitAll(tasks);
                // }
                break;
            }
        }

        await FTask.CompletedTask;
    }
}