#if FANTASY_NET
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Helper;
using Fantasy.Serialize;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Channels;
using static System.Formats.Asn1.AsnWriter;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Database
{
    /// <summary>
    /// 这里是 Fantasy框架使用 Mongo 数据库的实现。
    /// ( 请注意"b"的小写性质, 容易与大写"B"的MongoDB命名空间产生混淆, 特此提醒。)
    /// </summary>
    public sealed partial class MongoDb : IDatabase, IRawHandler<IMongoDatabase>
    {
        private MongoClient _mongoClient;
        private MongoSession _dbSession;
        internal FTaskFlowLock FlowLock { get; private set; }
        internal const int FTaskCountLimit = 1024;

        /// <summary>
        /// Scene
        /// </summary>
        public Scene Scene { get; private set; }
        /// <summary>
        /// 序列化器
        /// </summary>
        public ISerialize Serializer { get; private set; }
        /// <summary>
        /// 所有集合名
        /// </summary>
        public readonly HashSet<string> Collections = new HashSet<string>();
        /// <summary>
        /// 获得当前数据的类型
        /// </summary>
        public DatabaseType GetDatabaseType { get; } = DatabaseType.MongoDB;
        /// <summary>
        /// 这个数据库的工作职责
        /// </summary>
        public int Duty { get; private set; }
        /// <summary>
        /// 获得Mongo数据库的原生操作柄
        /// </summary>
        public IMongoDatabase RawHandler { get; private set; }

        #region Basic

        /// <summary>
        /// 初始化 MongoDB 数据库连接并记录所有集合名。
        /// </summary>
        ///  <param name="scene">场景对象。</param>
        ///  <param name="worldServices">服务注册。</param>
        /// <param name="duty">工作职责。</param>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <param name="dbName">数据库名称。</param>
        /// <returns>初始化后的数据库实例。</returns>
        public IDatabase Initialize(Scene scene,ref ServiceCollection worldServices, int duty, string connectionString, string dbName)
        {
            this.Scene = scene;
            Duty = duty;

            if(DatabaseSetting.MongoDBCustomInitialize != null)
            {
                _mongoClient = DatabaseSetting.MongoDBCustomInitialize(new DatabaseCustomConfig()
                {
                    Scene = scene,
                    ConnectionString = connectionString,
                    DBName = dbName
                });
            }        
            else
            {
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.MaxConnectionPoolSize = 200;
                _mongoClient = new MongoClient(settings);
            }

            try
            {
                RawHandler = _mongoClient.GetDatabase(dbName);
                _dbSession = new MongoSession(this);

                long mongoDbTypeHash = GetType().TypeHandle.Value.ToInt64();

                FlowLock = ( FlowLock == null )
                        ? new FTaskFlowLock(FTaskCountLimit, scene.CoroutineLockComponent, mongoDbTypeHash)
                        : FlowLock.Reset(scene.CoroutineLockComponent, mongoDbTypeHash);

                // 记录所有集合名
                Collections.UnionWith(RawHandler.ListCollectionNames().ToList());
                if (Collections.Count == 0)
                    Log.Warning($" ( MongoDb has No collection in \"{dbName}\"!! ) ");
                else
                    Log.Info($"(MongoDb has {Collections.Count} collection(s) in \"{dbName}\". )");
            }
            catch (Exception ex){
                throw new Exception($"Mongo logic-database {dbName} connection failed:\n{ex}");
            }

            Serializer = SerializerManager.GetSerializer(FantasySerializerType.Bson);
            return this;
        }

        /// <summary>
        /// 异步销毁限流锁
        /// </summary>
        /// <returns></returns>
        public async FTask DisposeFlowLockAsync() {
            await FlowLock.DisposeAsync();
        }

        /// <summary>
        /// 销毁释放资源。
        /// </summary>
        public void Dispose()
        {
            DisposeFlowLockAsync().Coroutine(); // ← 销毁FTaskFlowLock, 注意这里是开了协程的
            // 清理资源。
            Serializer = null;
            RawHandler = null;
            FlowLock = null;
            Collections.Clear();
            _mongoClient.Dispose();
            _dbSession.Dispose();
        }
        #endregion

        #region Use Session
        /// <summary>
        /// 使用MongoDb, 返回一个MongoSession。
        /// 可以链式调用Mongo数据库操作。
        /// </summary>
        /// <returns></returns>
        public MongoSession Use()
        {
            return _dbSession;
        }

        /// <summary>
        /// 使用MongoDb, 传出一个MongoSession。这里实现了接口中的方法, 让MongoDb与一般的SQL数据库兼容。 
        /// 事实上, Mongo的连接与一般的SQL数据库不同，它不自动维护了一个连接池, 所以返回的Scope为空, 可以直接放心用out出来的IDbSession就可以了
        /// </summary>
        /// <param name="mongoSession"></param>
        /// <param name="useSessionFromPool">Session池化, Mongo 数据库无需关心</param>
        /// <returns></returns>
        public AsyncServiceScope Use(out IDbSession? mongoSession, bool useSessionFromPool = true)
        {
            mongoSession = _dbSession;
            return Scene.World.ServiceProvider.CreateAsyncScope();
        }

        /// <summary>
        /// 通过 IDbSession 操作数据库会话。 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="useSessionFromPool">Session池化, Mongo 数据库无需关心</param>
        public async FTask Invoke(Func<IDbSession, FTask> asyncFunc, bool useSessionFromPool = true)
        {
           await asyncFunc(_dbSession);
        }
        #endregion

        #region Deployment

        /// <summary>
        /// 快速部署, 一次性快速创建所有标记为 [Table] 或 [FTable] 的实体集合。
        /// (建议仅开发时使用,正式部署后请走严格的数据库迁移流程)
        /// </summary>
        /// <returns></returns>
        public async FTask FastDeploy()
        {
            await DbAttrHelper.ScanFantasyDbSetTypesAsync(async (type, tableName, attr) => {
                if (attr.IfSelectionContainsDbType(DatabaseType.MongoDB) == false)
                { 
                    Log.Error($"Failed to operated a FantasyDbSet which`s Attr Info had not contained DbSelection of {DatabaseType.MongoDB}");
                    return;
                }
                await CreateCollection(type, tableName);
            }
            );
        }

        /// <summary>
        /// 清空逻辑数据库。用于开发时将整个数据库全部清除(危险操作, 仅开发时快速迭代期使用，仅针对数据库超级用户进行)
        /// </summary>
        /// <returns></returns>
        public async FTask DropEverything()
        {
            //TODO 
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 创建数据库集合（如果不存在）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="Name">指定名。</param>
        public async FTask CreateCollection<T>(string Name = null) where T : Entity
        {
            if(string.IsNullOrWhiteSpace(Name))
            {
                Name = typeof(T).Name;
            }

            // 已经存在数据库表
            if (Collections.Contains(Name))
            {
                return;
            }

            await RawHandler.CreateCollectionAsync(Name);

            Collections.Add(Name);
        }

        /// <summary>
        /// 创建数据库集合（如果不存在）。
        /// </summary>
        /// <param name="type">实体类型。</param>
        /// <param name="Name">指定名。</param>
        public async FTask CreateCollection(Type type, string Name = null)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = type.Name;
            }
            if (Collections.Contains(Name))
            {
                return;
            }

            await RawHandler.CreateCollectionAsync(Name);

            Collections.Add(Name);
        }

        /// <summary>
        /// 创建指定类型 <typeparamref name="T"/> 的存储集，用于存储实体。
        /// </summary>
        public async FTask CreateDbSet<T>(string Name = null) where T : Entity
        {
            await CreateCollection<T>(Name);
        }
        /// <summary>
        /// 根据指定类型创建存储集，用于存储实体。
        /// </summary>
        public async FTask CreateDbSet(Type type, string Name = null)
        {
            await CreateCollection(type, Name);
        }

        #endregion

        #region Index

        /// <summary>
        /// 创建数据库索引。
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="keys"></param>
        /// <typeparam name="T"></typeparam>
        /// <code>
        /// 使用例子(可多个):
        /// 1 : Builders.IndexKeys.Ascending(d=>d.Id)
        /// 2 : Builders.IndexKeys.Descending(d=>d.Id).Ascending(d=>d.Name)
        /// 3 : Builders.IndexKeys.Descending(d=>d.Id),Builders.IndexKeys.Descending(d=>d.Name)
        /// </code>
        public async FTask CreateIndex<T>(string collection, params object[]? keys) where T : Entity
        {
            if (keys == null || keys.Length <= 0)
            {
                return;
            }

            var indexModels = new List<CreateIndexModel<T>>();

            foreach (object key in keys)
            {
                IndexKeysDefinition<T> indexKeysDefinition = (IndexKeysDefinition<T>)key;

                indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition));
            }

            await _dbSession.GetCollection<T>(collection).Indexes.CreateManyAsync(indexModels);
        }

        /// <summary>
        /// 创建数据库的索引。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="keys">索引键定义。</param>
        public async FTask CreateIndex<T>(params object[]? keys) where T : Entity
        {
            if (keys == null)
            {
                return;
            }

            List<CreateIndexModel<T>> indexModels = new List<CreateIndexModel<T>>();

            foreach (object key in keys)
            {
                IndexKeysDefinition<T> indexKeysDefinition = (IndexKeysDefinition<T>)key;

                indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition));
            }

            await _dbSession.GetCollection<T>().Indexes.CreateManyAsync(indexModels);
        }

        #endregion
    }
}
#endif