using Fantasy.Async;
using Fantasy.Database;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    public enum TestWhat
    {
        FastDeploy,
        Init,
        Insert,
        Save,
        Query,
        QueryWithIndexes,
        JoinQuery
    }

    [FantasyDbSet("FantasyDbSetExampleEntity", Description = "Fantasy Table Test root Entity")]
    public class FantasyDbSetExampleRoot : Entity
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;

        public async FTask StartTest<T>(TestWhat testItem,int dutyId)where T:class,IDatabase 
        {
            var componentA = AddComponent<FantasyDbSetComponentA>();
            var componentB = AddComponent<FantasyDbSetComponentB>();

            var child01 = componentA.AddComponent<FantasyDbSetChild>();
            var child02 = componentA.AddComponent<FantasyDbSetChild>();
            var child03 = componentA.AddComponent<FantasyDbSetChild>();

            child01.AddComponent<FantasyDbSetComponentA>();
            child02.AddComponent<FantasyDbSetComponentB>();

            var componentC = child03.AddComponent<FantasyDbSetComponentC>();
            var grandChild = componentC.AddComponent<FantasyDbSetGrandchild>();
            grandChild.AddComponent<FantasyDbSetComponentA>();
            grandChild.AddComponent<FantasyDbSetComponentB>();
            grandChild.AddComponent<FantasyDbSetComponentC>();

            Log.Debug("[FantasyDbSet] 测试: 实体树已构造,");
            Log.Debug("\r\n        ///     Root\r\n        ///     ├─ ComponentA\r\n        ///     │  ├─ Child01\r\n        ///     │  │  └─ ComponentA\r\n        ///     │  ├─ Child02\r\n        ///     │  │  └─ ComponentB\r\n        ///     │  └─ Child03\r\n        ///     │     └─ ComponentC\r\n        ///     │        └─ Grandchild\r\n        ///     │           ├─ ComponentA\r\n        ///     │           ├─ ComponentB\r\n        ///     │           └─ ComponentC\r\n        ///     └─ ComponentB");

            var db = Scene.World.GetDatabase<T>(dutyId);

            if (db == null)
            {
                Log.Error($"{typeof(T)}不存在,无法进行测试");
                return;
            }

            switch (testItem)
            {
                case TestWhat.FastDeploy:
                {
                    await db.FastDeploy();
                    break;
                }
                case TestWhat.Insert:
                {
                        using var scope = db.Use(out IDbSession? dbSession);

                        if (dbSession == null)
                            throw new("Failed to use dbSession.");

                        var entity = Entity.Create<FantasyDbSetExampleRoot>(Scene, true, true);
                        await dbSession.Save(entity, "FantasyDbSetExampleEntity");
                      
                        break;
                }
                case TestWhat.Save:
                {

                    break;
                }
                case TestWhat.Query:
                {

                    break;
                }
                case TestWhat.QueryWithIndexes:
                {

                    break;
                }
                case TestWhat.JoinQuery:
                {

                    break;
                }
            }
        }

        public void TestAPI() {
            if (Scene.SceneConfigId == 1001)
            {
                var mongo = Scene.World.GetDatabase<MongoDb>(2);
                var pgSQL = Scene.World.GetDatabase<PostgreSQL>(0);

                //if(mongo!=null)
                //{
                //    //mongo.开始测试(限流锁测试.信号量锁住Id).Coroutine();
                //    mongo.开始测试(限流锁测试.信号量锁).Coroutine();
                //    mongo.开始测试(限流锁测试.随机数锁).Coroutine();
                //    //mongo.开始信号量锁有效性测试().Coroutine();
                //}

                if (pgSQL != null)
                {
                    Log.Debug("《 PgSQL API 大测试》");

                    //using (pgSQL.Use(out IDbSession? session, useSessionFromPool: false))
                    //{
                    //    if (session == null)
                    //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                    //    else
                    //        Log.Debug("已创建非池化的会话");
                    //}

                    //Log.Warning("↑看看非池化的会话有没有Dispose呢?");

                    using (pgSQL.Use(out IDbSession? session, useSessionFromPool: true))
                    {
                        if (session == null)
                            Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                        else
                            Log.Debug("已创建池化的会话");
                    }

                    Log.Warning("↑看看池化的会话有没有Dispose呢?");

                    //using (pgSQL.Use(out PgSession? session, useSessionFromPool: false))
                    //{
                    //    if (session == null)
                    //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                    //    else
                    //        Log.Debug("已创建PgSession强类型非池化会话");
                    //}

                    //Log.Warning("↑看看PgSession强类型非池化的会话有没有Dispose呢?");

                    //using (pgSQL.Use(out PgSession? session, useSessionFromPool: true))
                    //{
                    //    if (session == null)
                    //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                    //    else
                    //        Log.Debug("已创建池化的PgSession强类型会话");
                    //}

                    //Log.Warning("↑看看PgSession池化的强类型的会话有没有Dispose呢?");

                    //await pgSQL.Invoke(async (session) =>
                    //{
                    //    if (session == null)
                    //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                    //    else
                    //        Log.Debug("Invoke测试(Session默认池化)");
                    //    await FTask.CompletedTask;
                    //});
                    //Log.Warning("↑看看Invoke的PgSession(默认池化)有没有Dispose呢?");
                }
            }
        }
    }
}
