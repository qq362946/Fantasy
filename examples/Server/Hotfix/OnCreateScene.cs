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
                _authenticationSceneRunTimeId = scene.RunTimeId;
                Log.Debug($"Authentication RunTimeId:{scene.RunTimeId} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
                break;
            }
            case SceneType.Gate:
            {
                // List<FTask> list = new List<FTask>(1000); 
                // var sceneNetworkMessagingComponent = scene.NetworkMessagingComponent;
                // var session = scene.GetSession(_authenticationSceneRunTimeId);
                //
                // var message = new I_G2A_PingRequest();
                // async FTask Call()
                // {
                //     await sceneNetworkMessagingComponent.CallInnerRouteBySession(session,_authenticationSceneRunTimeId, message);
                // }
                //
                // for (int j = 0; j < 1000000000; ++j)
                // {
                //     list.Clear();
                //     for (int i = 0; i < list.Capacity; ++i)
                //     {
                //         list.Add(Call());
                //     }
                //
                //     await FTask.WhenAll(list);
                // }

                // Log.Debug($"Gate RunTimeId:{scene.RunTimeId} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
                // scene.NetworkMessagingComponent.SendInnerRoute(_authenticationSceneRunTimeId, new I_G2A_TestMessage()
                // {
                //     Name = "66666666"
                // });
                // var response = (I_A2G_TestResponse)await scene.NetworkMessagingComponent.CallInnerRoute(_authenticationSceneRunTimeId, new I_G2A_TestRequest()
                // {
                //     Name = "77777777"
                // });
                // Log.Debug($"Gate RunTimeId:{scene.RunTimeId} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId} Name:{response.Name}");
                break;
            }
        }

        await FTask.CompletedTask;
    }
}