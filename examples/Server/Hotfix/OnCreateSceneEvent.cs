using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Event;
using Fantasy.Helper;
using Fantasy.Serialize;
using ProtoBuf;

namespace Fantasy;

public sealed class SubSceneTestComponent : Entity
{
    public override void Dispose()
    {
        Log.Debug("销毁SubScene下的SubSceneTestComponent");
        base.Dispose();
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
        
        // var epoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
        //
        // {
        //     var now = TimeHelper.Transition(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        //     var epochThisYear = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970;
        //     var time = (uint)((now - epochThisYear) / 1000);
        //     Log.Debug($"time = {time} now = {now} epochThisYear = {epochThisYear}");
        // }
        //
        // {
        //     var now = TimeHelper.Transition(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        //     var epochThisYear = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970;
        //     var time = (uint)((now - epochThisYear) / 1000);
        //     Log.Debug($"time = {time} now = {now} epochThisYear = {epochThisYear}");
        // }
        
        var scene = self.Scene;
        
        switch (scene.SceneType)
        {
            case 6666:
            {
                var subSceneTestComponent = scene.AddComponent<SubSceneTestComponent>();
                Log.Debug("增加了SubSceneTestComponent");
                scene.EntityComponent.CustomSystem(subSceneTestComponent,CustomSystemType.RunSystem);
                break;
            }
            case SceneType.Addressable:
            {
                // scene.AddComponent<AddressableManageComponent>(); 
                _addressableSceneRunTimeId = scene.RuntimeId;
                break;
            }
            case SceneType.Map:
            {
                Log.Debug($"Map Scene  SceneRuntimeId:{scene.RuntimeId}");
                break;
            }
            case SceneType.Chat:
            {
                break;
            }
            case SceneType.Gate:
            {
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
                // for (int i = 0; i < 100000000000; i++)
                // {
                //     tasks.Clear();
                //     for (int j = 0; j < tasks.Capacity; ++j)
                //     {
                //         tasks.Add(Call());
                //     }
                //     await FTask.WaitAll(tasks);
                // }
                
                // 执行自定义系统
                var testCustomSystemComponent = scene.AddComponent<TestCustomSystemComponent>();
                scene.EntityComponent.CustomSystem(testCustomSystemComponent, CustomSystemType.RunSystem);
                // // 测试配置表
                // var instanceList = UnitConfigData.Instance.List;
                // var unitConfig = instanceList[0];
                // Log.Debug(instanceList[0].Dic[1]);
                break;
            }
        }

        await FTask.CompletedTask;
    }
}