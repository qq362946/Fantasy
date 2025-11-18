using System.Diagnostics;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using Fantasy.Helper;
using Fantasy.SeparateTable;
using static Fantasy.Helper.JsonHelper;

namespace Fantasy;

public sealed class SaveEntity : Entity
{
    
}

[SeparateTable(typeof(SaveEntity),"SubSceneTestComponent")]
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
                Log.Debug($"Map Scene  SceneRuntimeId:{scene.RuntimeId}");

                    //性能测试
                if(scene.World.Id == 1)
                    TestEntitySerialization(scene ,false).Coroutine();

                break;
            }
            case SceneType.Chat:
            {
                break;
            }
            case SceneType.Gate:
            {
                // var saveEntity = await scene.World.Database.Query<SaveEntity>(459439609634619405,true);
                // await saveEntity.LoadWithSeparateTables(scene.World.Database);
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

    #region Json序列化-反序列化测试
    public static async FTask TestEntitySerialization(Scene scene, bool logJson = false)
    {

        await FTask.CompletedTask;
        int loop = 88888; // 循环次数
        Stopwatch sw = new Stopwatch();

        // ---------------- NewtonSoft ----------------
        var serializerSettingsNewtonsoft = new JsonSettings
        {
            Library = Library.Newtonsoft,
            IsIndented = true,
            WriteTypeWhenNecessary = true,
            NoCycles = true,
            NoNull = true
        };

        double totalSerializeNewtonsoftSingle = 0;
        double totalDeserializeNewtonsoftSingle = 0;
        double totalSerializeNewtonsoftList = 0;
        double totalDeserializeNewtonsoftList = 0;

        string? jsonSingle = null;
        string? jsonList = null;

        for (int i = 0; i < loop; i++)
        {
            // 每次循环都创建新实体，避免重复使用同一对象
            var tEntity0 = Entity.Create<JSONTextEntityA>(scene, true, true);
            var tEntity1 = Entity.Create<JSONTextEntityB>(scene, true, true);
            var tEntity2 = Entity.Create<JSONTextEntityC>(scene, true, true);
            List<Entity> tEntityList = new() { tEntity0, tEntity1, tEntity2 };

            // 单个对象序列化
            sw.Restart();
            jsonSingle = tEntity0.ToJson(serializerSettingsNewtonsoft);
            sw.Stop();
            totalSerializeNewtonsoftSingle += sw.Elapsed.TotalMilliseconds;

            // 单个对象反序列化
            sw.Restart();
            var objSingle = jsonSingle.Deserialize<JSONTextEntityA>(serializerSettingsNewtonsoft);
            sw.Stop();
            totalDeserializeNewtonsoftSingle += sw.Elapsed.TotalMilliseconds;

            // 列表序列化
            sw.Restart();
            jsonList = tEntityList.ToJson(serializerSettingsNewtonsoft);
            sw.Stop();
            totalSerializeNewtonsoftList += sw.Elapsed.TotalMilliseconds;

            // 列表反序列化
            sw.Restart();
            var objList = jsonList.Deserialize<List<Entity>>(serializerSettingsNewtonsoft);
            sw.Stop();
            totalDeserializeNewtonsoftList += sw.Elapsed.TotalMilliseconds;

            if (i == 0 && logJson)
            {
                Log.Info("[Newtonsoft] 单个对象 JSON:");
                Log.Info(jsonSingle);
                Log.Info("[Newtonsoft] 列表 JSON:");
                Log.Info(jsonList);
            }
        }

        Log.Info($"[Newtonsoft] 平均单个对象序列化耗时: {totalSerializeNewtonsoftSingle / loop:F4} ms 总耗时 {Math.Round(totalSerializeNewtonsoftSingle, 2)} ms");
        Log.Info($"[Newtonsoft] 平均单个对象反序列化耗时: {totalDeserializeNewtonsoftSingle / loop:F4} ms 总耗时 {Math.Round(totalDeserializeNewtonsoftSingle, 2)} ms");
        Log.Info($"[Newtonsoft] 平均列表序列化耗时: {totalSerializeNewtonsoftList / loop:F4} ms 总耗时 {Math.Round(totalSerializeNewtonsoftList, 2)} ms");
        Log.Info($"[Newtonsoft] 平均列表反序列化耗时: {totalDeserializeNewtonsoftList / loop:F4} ms 总耗时 {Math.Round(totalDeserializeNewtonsoftList, 2)} ms");

        Log.Warning($"-----------------------{loop}次执行---------------------------");

        // ---------------- Microsoft Json ----------------
        var serializerSettingsMicrosoft = new JsonSettings
        {
            Library = Library.Microsoft,
            IsIndented = true,
            WriteTypeWhenNecessary = true,
            NoCycles = true,
            NoNull = true
        };

        double totalSerializeMicrosoftSingle = 0;
        double totalDeserializeMicrosoftSingle = 0;
        double totalSerializeMicrosoftList = 0;
        double totalDeserializeMicrosoftList = 0;

        for (int i = 0; i < loop; i++)
        {
            // 每次循环都创建新实体
            var tEntity0 = Entity.Create<JSONTextEntityA>(scene, true, true);
            var tEntity1 = Entity.Create<JSONTextEntityB>(scene, true, true);
            var tEntity2 = Entity.Create<JSONTextEntityC>(scene, true, true);
            List<Entity> tEntityList = new() { tEntity0, tEntity1, tEntity2 };

            // 单个对象序列化
            sw.Restart();
            jsonSingle = tEntity0.ToJson(serializerSettingsMicrosoft);
            sw.Stop();
            totalSerializeMicrosoftSingle += sw.Elapsed.TotalMilliseconds;

            // 单个对象反序列化
            sw.Restart();
            var objSingle = jsonSingle.Deserialize<JSONTextEntityA>(serializerSettingsMicrosoft);
            sw.Stop();
            totalDeserializeMicrosoftSingle += sw.Elapsed.TotalMilliseconds;

            // 列表序列化
            sw.Restart();
            jsonList = tEntityList.ToJson(serializerSettingsMicrosoft);
            sw.Stop();
            totalSerializeMicrosoftList += sw.Elapsed.TotalMilliseconds;

            // 列表反序列化
            sw.Restart();
            var objList = jsonList.Deserialize<List<Entity>>(serializerSettingsMicrosoft);
            sw.Stop();
            totalDeserializeMicrosoftList += sw.Elapsed.TotalMilliseconds;

            if (i == 0 && logJson)
            {
                Log.Info("[Microsoft] 单个对象 JSON:");
                Log.Info(jsonSingle);
                Log.Info("[Microsoft] 列表 JSON:");
                Log.Info(jsonList);
            }
        }

        Log.Info($"[.NET] 平均单个对象序列化耗时: {totalSerializeMicrosoftSingle / loop:F4} ms 总耗时 {Math.Round(totalSerializeMicrosoftSingle, 2)} ms");
        Log.Info($"[.NET] 平均单个对象反序列化耗时: {totalDeserializeMicrosoftSingle / loop:F4} ms 总耗时 {Math.Round(totalDeserializeMicrosoftSingle, 2)} ms");
        Log.Info($"[.NET] 平均列表序列化耗时: {totalSerializeMicrosoftList / loop:F4} ms 总耗时 {Math.Round(totalSerializeMicrosoftList, 2)} ms");
        Log.Info($"[.NET] 平均列表反序列化耗时: {totalDeserializeMicrosoftList / loop:F4} ms 总耗时 {Math.Round(totalDeserializeMicrosoftList, 2)} ms");
        Log.Warning($"-----------------------{loop}次执行---------------------------");
    }

    public class JSONTextEntityA : Entity
    {
        public string Name { get; set; } = "I am A";
    }

    public class JSONTextEntityB : Entity
    {
        public string Name { get; set; } = "I am B";
    }

    public class JSONTextEntityC : Entity
    {
        public string Name { get; set; } = "I am C";
    }
    #endregion
}