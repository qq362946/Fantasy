#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Serialize;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Npgsql;
using System.Reflection.Emit;
using System.Threading.Tasks;
using static Fantasy.Database.PgSession;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Database
{
    /// <summary>
    /// Fantasy框架使用 PostgreSQL 数据库。
    /// </summary>
    public sealed partial class PostgreSQL : IDatabase, IRawHandler<NpgsqlDataSource>
    {
        internal FTaskFlowLock FlowLock { get; private set; }
        internal const int FTaskCountLimit = 1024;

        /// <summary>
        /// 所在Scene
        /// </summary>
        public Scene Scene { get; private set; }
        /// <summary>
        /// 序列化器
        /// </summary>
        public ISerialize Serializer { get; private set; }
        /// <summary>
        /// 当前数据的类型
        /// </summary>
        public DatabaseType GetDatabaseType { get; } = DatabaseType.PostgreSQL;
        /// <summary>
        /// 这个数据库的工作职责
        /// </summary>
        public int Duty { get; private set; }
        /// <summary>
        /// PgSQL原生操作柄
        /// </summary>
        public NpgsqlDataSource RawHandler { get; private set; }

        #region Basic

        /// <summary>
        /// 初始化 PgSQL 数据库连接。
        /// </summary>
        /// <param name="duty">工作职责。</param>
        /// <param name="scene">场景对象。</param>
        /// <param name="worldServices">世界服务构建。</param>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <param name="dbName">数据库名称。</param>
        /// <returns>初始化后的数据库实例。</returns>
        public IDatabase Initialize(Scene scene, ref ServiceCollection worldServices,int duty, string connectionString, string dbName)
        {
            Scene = scene;
            Duty = duty;
            Serializer = SerializerManager.GetSerializer(FantasySerializerType.Bson);

            if (DatabaseSetting.PostgreSQLCustomInitialize != null)
            {
                RawHandler = DatabaseSetting.PostgreSQLCustomInitialize(new DatabaseCustomConfig()
                {
                    Scene = scene,
                    ConnectionString = connectionString,
                    DBName = dbName
                });
            }
            else try
            {
                RawHandler = NpgsqlDataSource.Create(connectionString);
            }
            catch(Exception e) {
                throw new Exception($" ( PgSQL Building ConnectionString Err : {connectionString} )\n {e}");
            }

            // 为World服务 注册 PgSession 池。
            // 注意, DbContext的池化会将Scope也一并连带, 这导致的直接结果是, 每次调用Open取池中的PgSession时, Scoped服务都是跟初始保持一致的,
            // 因此 池化的PgSession 不允许使用动态Scoped服务 (非动态Scoped服务无影响)。
            // 详情 请阅读 AddDbContextPool 微软官方的注释以获得更多理解。
            worldServices.AddDbContextPool<PgSession>(contextOptionsBuilder =>
            {
                contextOptionsBuilder.UseNpgsql(RawHandler).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // 默认关闭 ChangeTracking，让 EFCore 不跟踪实体
#if DEBUG
                contextOptionsBuilder.EnableSensitiveDataLogging(true);
#endif
            }, poolSize: FTaskCountLimit);

            //为World服务 注册非池化版本的PgSession。
            worldServices.AddDbContext<PgSessionUnPooled>(contextOptionsBuilder =>
            {
                contextOptionsBuilder.UseNpgsql(RawHandler).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // 默认关闭 ChangeTracking，让 EFCore 不跟踪实体
#if DEBUG
                contextOptionsBuilder.EnableSensitiveDataLogging(true);
#endif
            });

            //分别初始化池化Session和非池化Session连接( 这一步同时也会构建 Entity - Table映射模型 )
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<PgSession>();
                optionsBuilder.UseNpgsql(RawHandler);
                var pgSessionTest = new PgSession(optionsBuilder.Options);
                pgSessionTest.Database.OpenConnection();
                pgSessionTest.Dispose();

                var optionsBuilderForNonPooled = new DbContextOptionsBuilder<PgSessionUnPooled>();
                optionsBuilderForNonPooled.UseNpgsql(RawHandler);
                var pgSessionNonPooledTest = new PgSessionUnPooled(optionsBuilderForNonPooled.Options);
                pgSessionNonPooledTest.Database.OpenConnection();
                pgSessionNonPooledTest.Dispose();
            }
            catch (Exception ex) {
                throw new Exception($"( Failed to init PgSession. \"{GetConnectionInfoWithoutPassword()}\") :\n {ex} ");
            }

            Log.Info("(PgSQL is able to be connected.) \n " + GetConnectionInfoWithoutPassword());

            var pgDbHash = GetType().TypeHandle.Value.ToInt64();
            FlowLock = (FlowLock == null)
                    ? new FTaskFlowLock(FTaskCountLimit, scene.CoroutineLockComponent, pgDbHash)
                    : FlowLock.Reset(scene.CoroutineLockComponent, pgDbHash);

            return this;
        }

        /// <summary>
        /// 异步销毁限流锁
        /// </summary>
        /// <returns></returns>
        public async FTask DisposeFlowLockAsync()
        {
            await FlowLock.DisposeAsync();
        }

        /// <summary>
        /// 销毁释放资源。
        /// </summary>
        public void Dispose()
        {
            DisposeFlowLockAsync().Coroutine(); // ← 销毁FTaskFlowLock, 注意这里是开了协程的
            // 清理资源。
            Scene = null;
            Serializer = null;
            RawHandler?.Dispose();
            RawHandler = null;
            FlowLock = null;
        }
        #endregion

        #region  Use Session

        /// <summary>
        /// 使用数据库: 创建作用域并获取 Db对话实例。
        /// (这里的 SqlSession 超过 scope 会自动Dispose, 确保外部使用了using 管理作用域, 即可自动将SqlSession返还池中)
        /// </summary>
        /// <param name="session"> out：获取到的 数据库对话 实例（可能为 null）</param>
        /// <param name="useSessionFromPool"> 本次连接是否从池中取 DbSession, 默认为为true; 
        /// 解释如下 : DbSession 的父类是微软提供的 DbContext。使用DbContext分池化和实例化两种用法，
        /// 池化模式每次会尝试从DbContextPool当中取一个DbContext, 相比于每次都实例化, 池化的GC压力较小且性能略优,
        /// 在我们框架中, PgSQL 从池中取的是 PgSession; 
        /// 然而, 由于 PgSession 当中保存了一些数据库操作的状态, 池化模式在少数高并发的边缘情况可能存在副作用, 需特别留意, 
        /// 详情请参考微软EFCore官方手册对DbContextPool的说明以获得更多理解。 </param>
        /// <returns>创建的作用域（外部通过 using 释放）</returns>
        public AsyncServiceScope Use(out IDbSession? session,bool useSessionFromPool = true)
        {
            var scope = Scene.World.ServiceProvider.CreateAsyncScope();
            PgSession? pgSession = null; 
            try
            {
                if (useSessionFromPool)
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSession>();
                else
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSessionUnPooled>();

                pgSession.SetPg(this);
                session = pgSession;
            }
            catch (Exception ex)
            {
                Log.Error($"( Critical Emergency! PgSQL failed to get connection! ) \n " +
            $"{GetConnectionInfoWithoutPassword()}\n", ex);
                pgSession?.Dispose();
                session = null;
            }
            return scope;
        }

        /// <summary>
        /// 使用数据库: 创建作用域并获取 Db会话实例。SqlSession 类型版本。
        /// </summary>
        /// <param name="session">out：获取到的 SQL数据库对话 实例（可能为 null）</param>
        /// <param name="useSessionFromPool"> 本次连接是否从池中取 DbSession, 默认为为true; 
        /// 解释如下 : DbSession 的父类是微软提供的 DbContext。使用DbContext分池化和实例化两种用法，
        /// 池化模式每次会尝试从DbContextPool当中取一个DbContext, 相比于每次都实例化, 池化的GC压力较小且性能略优,
        /// 在我们框架中, PgSQL 从池中取的是 PgSession; 
        /// 然而, 由于 PgSession 当中保存了一些数据库操作的状态, 池化模式在少数高并发的边缘情况可能存在副作用, 需特别留意, 
        /// 详情请参考微软EFCore官方手册对DbContextPool的说明以获得更多理解。 </param>
        /// <returns>创建的作用域（外部通过 using 释放）</returns>
        /// <returns>作用域（外部通过 using 释放）</returns>
        public AsyncServiceScope Use(out PgSession? session, bool useSessionFromPool = true)
        {
            var scope = Scene.World.ServiceProvider.CreateAsyncScope();
            PgSession? pgSession = null;
            try
            {
                if (useSessionFromPool)
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSession>();
                else
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSessionUnPooled>();

                pgSession.SetPg(this);
                session = pgSession;
            }
            catch (Exception ex)
            {
                Log.Error($"( Critical Emergency! PgSQL failed to get connection! ) \n " +
          $"{GetConnectionInfoWithoutPassword()}\n", ex);
                pgSession?.Dispose();
                session = null;
            }
            return scope;
        }

        /// <summary>
        /// 直接传入一个函数操作数据库, 函数执行完会自动结束 PgSession；
        /// 注意: 这一方法写起来更方便, 但如果传入函数闭包了外部变量, 编译器将会生成DisplayClass, 导致一定的GC压力。 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="useSessionFromPool"> 本次连接是否从池中取 DbSession, 默认为为true; 
        /// 解释如下 : DbSession 的父类是微软提供的 DbContext。使用DbContext分池化和实例化两种用法，
        /// 池化模式每次会尝试从DbContextPool当中取一个DbContext, 相比于每次都实例化, 池化的GC压力较小且性能略优,
        /// 在我们框架中, PgSQL 从池中取的是 PgSession; 
        /// 然而, 由于 PgSession 当中保存了一些数据库操作的状态, 池化模式在少数高并发的边缘情况可能存在副作用, 需特别留意, 
        /// 详情请参考微软EFCore官方手册对DbContextPool的说明以获得更多理解。 </param>
        public async FTask Invoke(Func<PgSession?, FTask> asyncFunc, bool useSessionFromPool = true)
        {
            await using AsyncServiceScope scope = Scene.World.ServiceProvider.CreateAsyncScope();
            PgSession? pgSession = null;
            try
            {
                if (useSessionFromPool)
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSession>(); //TODO 需要验证一下这里拿到的PgSession是自动Connection还是手动的
                else
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSessionUnPooled>();

                pgSession.SetPg(this);
                await asyncFunc(pgSession);
            }
            catch (Exception ex)
            {
                Log.Error($"( Critical Emergency! PgSQL failed to get connection! ) \n " +
           $"{GetConnectionInfoWithoutPassword()}\n", ex);
                await asyncFunc(pgSession);
            }
        }

        /// <summary>
        /// 通过 IDbSession 操作数据库。直接传入一个函数, 执行完会自动结束 PgSession；
        /// 注意: 这一方法写起来更方便, 但如果传入函数闭包了外部变量, 编译器将会生成DisplayClass, 导致一定的GC压力。 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="useSessionFromPool"> 本次连接是否从池中取 DbSession, 默认为为true; 
        /// 解释如下 : DbSession 的父类是微软提供的 DbContext。使用DbContext分池化和实例化两种用法，
        /// 池化模式每次会尝试从DbContextPool当中取一个DbContext, 相比于每次都实例化, 池化的GC压力较小且性能略优,
        /// 在我们框架中, PgSQL 从池中取的是 PgSession; 
        /// 然而, 由于 PgSession 当中保存了一些数据库操作的状态, 池化模式在少数高并发的边缘情况可能存在副作用, 需特别留意, 
        /// 详情请参考微软EFCore官方手册对DbContextPool的说明以获得更多理解。 </param>
        public async FTask Invoke(Func<IDbSession?, FTask> asyncFunc, bool useSessionFromPool = true)
        {
            await using AsyncServiceScope scope = Scene.World.ServiceProvider.CreateAsyncScope();
            PgSession? pgSession = null;
            try
            {
                if (useSessionFromPool)
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSession>(); //TODO 需要验证一下这里拿到的PgSession是自动Connection还是手动的
                else
                    pgSession = scope.ServiceProvider.GetRequiredService<PgSessionUnPooled>();

                pgSession.SetPg(this);
                await asyncFunc(pgSession);
            }
            catch (Exception ex)
            {
                Log.Error($"( Critical Emergency! PgSQL failed to get connection! ) \n " +
               $"{GetConnectionInfoWithoutPassword()}\n", ex);
                await asyncFunc(pgSession);
            }
        }

        /// <summary>
        /// 获取Connection信息, 规避密码
        /// </summary>
        /// <returns></returns>
        public string GetConnectionInfoWithoutPassword() {
            var parts = RawHandler.ConnectionString?.Split(';') ?? Array.Empty<string>();

            string GetValue(string key) =>
                parts.FirstOrDefault(p => p.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                     ?.Substring(key.Length + 1) ?? "Unknown";

            var host = GetValue("Host");
            var port = GetValue("Port");
            var username = GetValue("Username");
            var database = GetValue("Database");
            return $"---------- ( Host={host}; Port={port}; Username={username}; Database={database} ) ----------";
        }

        #endregion

        #region Deployment

        /// <summary>
        /// 快速部署, 一次性快速创建所有标记为 [Table] 或 [FTable] 的实体的表。
        /// (建议仅开发时使用,正式部署后请走严格的数据库迁移流程)
        /// </summary>
        /// <returns></returns>
        public async FTask FastDeploy()
        {
            //TODO 暂时这样简单粗暴, 之后再改成像SqlSugar那样可以根据标签精细管理迁移策略
            await using (Use(out PgSession? pgSession))
            {
                if (pgSession != null)
                {
                    await pgSession.Database.EnsureCreatedAsync();
                }
                else
                    Log.Debug(" Failed to connect to pgSession");
            }
        }

        /// <summary>
        /// 清空逻辑数据库。用于开发时将整个数据库全部清除(危险操作, 仅开发时快速迭代期使用，仅针对数据库超级用户进行)
        /// </summary>
        /// <returns></returns>
        public async FTask DropEverything()
        {
            //TODO 暂时这样简单粗暴, 之后再改成像SqlSugar那样可以根据标签精细管理迁移策略
            await using (Use(out PgSession? pgSession))
            {
                if (pgSession != null)
                    await pgSession.Database.EnsureDeletedAsync();
                else
                    Log.Debug(" Failed to connect to pgSession");
            }
        }

        /// <summary>
        /// 创建表。无效。
        /// 与文档数据库不同, SQL 数据库不鼓励在运行时随意用代码建表，
        /// 因为这会在生产环境中导致严重的数据库版本管理困难。
        /// 如果是在开发阶段，可以适当使用 快速部署 或自动迁移来快速创建表以便快速迭代；
        /// 但如果业务已经上线，请严格遵循 EF Core 的流程：生成迁移 -> 同步数据库。
        /// </summary>
        [Obsolete(" (Invalid operation!!)  " +
       " SQL databases do not recommend creating tables at runtime using code arbitrarily, " +
       "as this can cause serious difficulties in database version management in production. \r\n" +
       "If you are in the development stage, you may appropriately use FastDeploy or automatic migrations to quickly create tables for testing purposes; " +
       "however, if your application is already in production, you should strictly follow the EF Core workflow: " +
       "generate migrations -> synchronize the database.\r\n", true)]
        public async FTask CreateDbSet<T>(string Name = null) where T : Entity
        {
            Log.Warning(" (Invalid operation!!)  " +
            " SQL databases do not recommend creating tables at runtime using code arbitrarily, " +
            "as this can cause serious difficulties in database version management in production. \r\n" +
            "If you are in the development stage, you may appropriately use FastDeploy or automatic migrations to quickly create tables for testing purposes; " +
            "however, if your application is already in production, you should strictly follow the EF Core workflow: " +
            "generate migrations -> synchronize the database.\r\n");
            await FTask.CompletedTask;
        }
        /// <summary>
        /// 创建表。无效。
        /// 与文档数据库不同, SQL 数据库不鼓励在运行时随意用代码建表，
        /// 因为这会在生产环境中导致严重的数据库版本管理困难。
        /// 如果是在开发阶段，可以适当使用 快速部署 或自动迁移来快速创建表以便快速迭代；
        /// 但如果业务已经上线，请严格遵循 EF Core 的流程：生成迁移 -> 同步数据库。
        /// </summary>
        [Obsolete(" (Invalid operation!!)  " +
               " SQL databases do not recommend creating tables at runtime using code arbitrarily, " +
               "as this can cause serious difficulties in database version management in production. \r\n" +
               "If you are in the development stage, you may appropriately use FastDeploy or automatic migrations to quickly create tables for testing purposes; " +
               "however, if your application is already in production, you should strictly follow the EF Core workflow: " +
               "generate migrations -> synchronize the database.\r\n", true)]
        public async FTask CreateDbSet(Type type, string Name = null)
        {
            Log.Warning(" (Invalid operation!!)  " +
            " SQL databases do not recommend creating tables at runtime using code arbitrarily, " +
            "as this can cause serious difficulties in database version management in production. \r\n" +
            "If you are in the development stage, you may appropriately use FastDeploy or automatic migrations to quickly create tables for testing purposes; " +
            "however, if your application is already in production, you should strictly follow the EF Core workflow: " +
            "generate migrations -> synchronize the database.\r\n");
            await FTask.CompletedTask;
        }

        #endregion

        #region Index

        /// <summary>
        /// 创建数据库索引（加锁）。
        /// </summary>
        /// <param name="table"></param>
        /// <param name="keys"></param>
        /// <typeparam name="T"></typeparam>
        /// <code>
        /// 使用例子(可多个):
        /// 1 : Builders.IndexKeys.Ascending(d=>d.Id)
        /// 2 : Builders.IndexKeys.Descending(d=>d.Id).Ascending(d=>d.Name)
        /// 3 : Builders.IndexKeys.Descending(d=>d.Id),Builders.IndexKeys.Descending(d=>d.Name)
        /// </code>
        public async FTask CreateIndex<T>(string table, params object[]? keys) where T : Entity
        {
            // if (keys == null || keys.Length <= 0)
            // {
            //     return;
            // }
            // 
            // var tableName = GetTableName<T>(table);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     for (int i = 0; i < keys.Length; i++)
            //     {
            //         var indexName = $"{tableName}_index_{i}";
            //         var columns = GetIndexColumns(keys[i]);
            //         var indexType = GetIndexType(keys[i]);
            //         
            //         using (var cmd = _connection.CreateCommand())
            //         {
            //             cmd.CommandText = $"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" {indexType} ({columns})";
            //             await cmd.ExecuteNonQueryAsync();
            //         }
            //     }
            //     await _connection.CloseAsync();
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 创建数据库的索引（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="keys">索引键定义。</param>
        public async FTask CreateIndex<T>(params object[]? keys) where T : Entity
        {
            // if (keys == null || keys.Length <= 0)
            // {
            //     return;
            // }
            // 
            // var tableName = GetTableName<T>();
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     for (int i = 0; i < keys.Length; i++)
            //     {
            //         var indexName = $"{tableName}_index_{i}";
            //         var columns = GetIndexColumns(keys[i]);
            //         var indexType = GetIndexType(keys[i]);
            //         
            //         using (var cmd = _connection.CreateCommand())
            //         {
            //             cmd.CommandText = $"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" {indexType} ({columns})";
            //             await cmd.ExecuteNonQueryAsync();
            //         }
            //     }
            //     await _connection.CloseAsync();
            // }
            await FTask.CompletedTask;
        }

        #endregion
    }
}
#endif
