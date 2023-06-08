using Fantasy.Core.Network;

namespace Fantasy.Hotfix.Core;

public sealed class OnCreateScene_Configuration : AsyncEventSystem<OnCreateScene>
{
    public override async FTask Handler(OnCreateScene self)
    {
        var scene = self.SceneInfo.Scene;
        var sceneInfo = self.SceneInfo;
        var sceneType = SceneType.SceneDic[sceneInfo.SceneType];
        
        switch (sceneType)
        {
            case SceneType.Authentication:
            {
                break;
            }
            case SceneType.Addressable:
            {
                scene.AddComponent<AddressableManageComponent>();
                break;
            }
            case SceneType.Map:
            {
                scene.AddComponent<AccountManageComponent>();
                break;
            }
            case SceneType.Chat:
            {
                scene.AddComponent<AccountManageComponent>();
                break;
            }
        }

        await FTask.CompletedTask;
    }
}