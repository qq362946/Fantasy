namespace Fantasy;

public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    private static long _authenticationSceneRunTimeId;
    public override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Authentication:
            {
                break;
            }
            case SceneType.Gate:
            {
                break;
            }
        }

        await FTask.CompletedTask;
    }
}