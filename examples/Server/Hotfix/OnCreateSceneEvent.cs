using System.Buffers;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using Fantasy.SeparateTable;
using Fantasy.Serialize;
using LightProto;

namespace Fantasy;

public sealed class SaveEntity : Entity
{

}

[SeparateTable(typeof(SaveEntity), "SubSceneTestComponent")]
public sealed class SubSceneTestComponent : Entity
{
    public override void Dispose()
    {
        Log.Debug("销毁SubScene下的SubSceneTestComponent");
        base.Dispose();
    }
}

public sealed class SubSceneTestComponentAwakeSystem : AwakeSystem<SubSceneTestComponent>
{
    protected override void Awake(SubSceneTestComponent self)
    {
        Log.Debug("SubSceneTestComponentAwakeSystem");
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
                Log.Debug($"Map Scene SceneRuntimeId:{scene.RuntimeId}");
                break;
            }
            case SceneType.Chat:
            {
                break;
            }
            case SceneType.Gate:
            {

               
                IBufferWriter<byte> buffer = new MemoryStreamBuffer();
                object message = new C2G_TestMessage()
                {
                    Tag = "1111"
                };
                
                
                Serializer.Serialize<global::Fantasy.C2G_TestMessage>(buffer, (global::Fantasy.C2G_TestMessage)message, global::Fantasy.C2G_TestMessage.ProtoWriter);
                
                var memoryStreamBuffer = (MemoryStreamBuffer)buffer;
                memoryStreamBuffer.Seek(0, SeekOrigin.Begin);
                var c2GTestMessage = Serializer.Deserialize<global::Fantasy.C2G_TestMessage>(memoryStreamBuffer);
                
                Log.Debug($"C2G_TestMessage:{c2GTestMessage.Tag}");

                //单泛型参数实体测试
                    Entity.Create<SubSceneTestComponent>(scene).AddComponent<GenericTest.TestEntity<SaveEntity>>();
                    //双泛型参数实体测试
                    Entity.Create<SubSceneTestComponent>(scene).AddComponent<GenericTest.TestEntity2<SaveEntity, SaveEntity>>();
                    // var saveEntity = Entity.Create<SaveEntity>(scene);
                    // saveEntity.AddComponent<SubSceneTestComponent>();
                    //
                    // await saveEntity.PersistAggregate(scene.World.Database);

                    // var saveEntity = await scene.World.Database.LoadWithSeparateTables<SaveEntity>(488710241381777422);
                    // var saveEntity = await scene.World.Database.Query<SaveEntity>(488710241381777422,true);
                    // await saveEntity.LoadWithSeparateTables(scene.World.Database);
                    // var a = 0;
                    //
                    // Log.Debug($"{saveEntity.GetComponent<SubSceneTestComponent>()!=null}");
                    // var saveEntity = Entity.Create<SaveEntity>(scene, true, false);
                    // saveEntity.AddComponent<SubSceneTestComponent>();
                    // await saveEntity.PersistAggregate(scene.World.Database);
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
                    break;
            }
        }
    }
}