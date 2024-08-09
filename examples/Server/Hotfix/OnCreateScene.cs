namespace Fantasy;

public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    private static long _addressableSceneRunTimeId;
    public override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Addressable:
            {
                _addressableSceneRunTimeId = scene.RunTimeId;
                // Log.Debug($"_addressableSceneRunTimeId={_addressableSceneRunTimeId}");
                break;
            }
            case SceneType.Gate:
            {
                // var tasks = new List<FTask>(1000);
                // var session = scene.GetSession(_addressableSceneRunTimeId);
                // var sceneNetworkMessagingComponent = scene.NetworkMessagingComponent;
                // session.Call(new G2A_TestRequest());
                // async FTask Call()
                // {
                //     await sceneNetworkMessagingComponent.CallInnerRouteBySession(session,_addressableSceneRunTimeId,new G2A_TestRequest());
                // }
                // for (int i = 0; i < 100000000000; i++)
                // {
                //     tasks.Clear();
                //     for (int j = 0; j < tasks.Capacity; ++j)
                //     {
                //         tasks.Add(Call());
                //     }
                //     await FTask.WhenAll(tasks);
                // }
                break;
            }
        }

        await FTask.CompletedTask;
    }
}