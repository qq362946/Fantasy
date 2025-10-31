#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Serialize;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Connections;
using Npgsql;
using System;
using System.Linq.Expressions;
// ReSharper disable InconsistentNaming
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

#pragma warning disable CS8625

namespace Fantasy.Database
{
    /// <summary>
    /// 数据库设置助手
    /// </summary>
    public static class DatabaseSetting
    {
        /// <summary>
        /// PostgreSQL 初始化自定义委托, 当设置了这个委托后，就不会自动创建PgSQL连接源, 把创建权交给自定义。
        /// </summary>
        public static Func<DatabaseCustomConfig, NpgsqlDataSource>? PostgreSQLCustomInitialize;
        /// <summary>
        /// MongoDB 初始化自定义委托，当设置了这个委托后，就不会自动创建MongoClient，把创建权交给自定义。
        /// </summary>
        public static Func<DatabaseCustomConfig, MongoClient>? MongoDBCustomInitialize;
    }

    /// <summary>
    /// 数据库自定义连接参数
    /// </summary>
    public sealed class DatabaseCustomConfig
    {
        /// <summary>
        /// 当前Scene
        /// </summary>
        public Scene Scene;
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString;
        /// <summary>
        /// 数据库名字
        /// </summary>
        public string DBName;
    }

    /// <summary>
    /// 原生操作柄接口
    /// </summary>
    public interface IRawHandler<H>
    {
        /// <summary>
        /// 原生操作柄
        /// </summary>
        public H RawHandler { get; }
    }

    /// <summary>
    /// 数据库基本接口。
    /// </summary>
    public interface IDatabase : IDisposable
    {
        /// <summary>
        /// 所在Scene
        /// </summary>
        public Scene Scene { get; }

        /// <summary>
        /// 获得当前数据库的类型
        /// </summary>
        public DatabaseType GetDatabaseType { get;}

        /// <summary>
        /// 获得当前数据库的工作职责标识
        /// </summary>
        public int Duty { get; }

        /// <summary>
        /// 序列化器
        /// </summary>
        public ISerialize Serializer { get; }

        /// <summary>
        /// 初始化数据库。
        /// </summary>
        IDatabase Initialize(Scene scene, ref ServiceCollection servicec, int duty, string connectionString, string dbName);
        
        /// <summary>
        /// 使用数据库会话, 返回一个作用域；out一个Session, 通过Session操作数据库。
        /// <param name="dbSession">dbSession 获得一个新的数据库会话</param>
        /// <param name="useSessionFromPool">useSessionFromPool 是否从池中获取会话</param>
        /// </summary>
        public AsyncServiceScope Use(out IDbSession? dbSession, bool useSessionFromPool = true);

        /// <summary>
        /// 传入异步函数, 通过委托直接操作数据库。
        /// 注意 ：如果传入的函数闭包了外部变量, 编译器将会自动生成DisplayClass, 导致一定的GC压力。
        /// </summary>
        /// <param name="asyncFunc">asyncFunc 传入一个异步FTask函数</param>
        /// <param name="useSessionFromPool">useSessionFromPool 是否从池中获取会话</param>
        /// <returns></returns>
        public FTask Invoke(Func<IDbSession?, FTask> asyncFunc, bool useSessionFromPool = true);

        /// <summary>
        /// 快速部署, 用于开发时 一次性快速创建所有标记了 [Table] 或 [FTable] 的存储集。
        /// </summary>
        FTask FastDeploy();

        /// <summary>
        /// 清空逻辑数据库。用于开发时 将整个数据库全部清除(危险操作, 仅开发时快速迭代期使用，仅针对数据库超级用户进行)
        /// </summary>
        FTask DropEverything();

        /// <summary>
        /// 在指定的存储集中创建索引，以提高类型 <typeparamref name="T"/> 实体的查询性能。
        /// </summary>
        FTask CreateIndex<T>(string name, params object[] keys) where T : Entity;

        /// <summary>
        /// 在默认存储集中创建索引，以提高类型 <typeparamref name="T"/> 实体的查询性能。
        /// </summary>
        FTask CreateIndex<T>(params object[] keys) where T : Entity;

        /// <summary>
        /// 创建指定类型 <typeparamref name="T"/> 的存储集，用于存储实体。
        /// </summary>
        FTask CreateDbSet<T>(string Name = null) where T : Entity;

        /// <summary>
        /// 根据指定类型创建存储集，用于存储实体。
        /// </summary>
        FTask CreateDbSet(Type type, string Name = null);
    }

    /// <summary>
    /// 表示用于执行数据库 CRUD 操作的数据库会话接口
    /// </summary>
    public interface IDbSession : IDisposable,IAsyncDisposable
    {
        /// <summary>
        /// 在指定的存储集中检索类型 <typeparamref name="T"/> 的实体数量。
        /// </summary>
        FTask<long> Count<T>(string name = null) where T : Entity;
        /// <summary>
        /// 在指定的存储集中检索满足给定筛选条件的类型 <typeparamref name="T"/> 的实体数量。
        /// </summary>
        FTask<long> Count<T>(Expression<Func<T, bool>> filter, string name = null) where T : Entity;
        /// <summary>
        /// 快速估算实体
        /// </summary>
        FTask<long> FastCount<T>(string name = null) where T : Entity;
        /// <summary>
        /// 检查指定存储集中是否存在类型 <typeparamref name="T"/> 的实体。
        /// </summary>
        FTask<bool> Exist<T>(string name = null) where T : Entity;
        /// <summary>
        /// 检查指定存储集中是否存在满足给定筛选条件的类型 <typeparamref name="T"/> 的实体。
        /// </summary>
        FTask<bool> Exist<T>(Expression<Func<T, bool>> filter, string name = null) where T : Entity;
        /// <summary>
        /// 从指定存储集中检索指定 ID 的类型 <typeparamref name="T"/> 的实体，不锁定。
        /// </summary>
        FTask<T> QueryNotLock<T>(long id, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 从指定存储集中检索指定 ID 的类型 <typeparamref name="T"/> 的实体。
        /// </summary>
        FTask<T> Query<T>(long id, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体数量和日期。
        /// </summary>
        FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体数量和日期。
        /// </summary>
        FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 分页查询指定存储集中满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表。
        /// </summary>
        FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 分页查询指定存储集中满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表，仅返回指定列的数据。
        /// </summary>
        FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 从指定存储集中按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表，按指定字段排序。
        /// </summary>
        FTask<List<T>> QueryByPageOrderBy<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 检索满足给定筛选条件的类型 <typeparamref name="T"/> 的第一个实体，从指定存储集中。
        /// </summary>
        FTask<T?> First<T>(Expression<Func<T, bool>> filter, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 查询指定存储集中满足给定 JSON 查询字符串的类型 <typeparamref name="T"/> 的第一个实体，仅返回指定列的数据。
        /// </summary>
        FTask<T> First<T>(string json, string[] cols, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 从指定存储集中按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表，按指定字段排序。
        /// </summary>
        FTask<List<T>> QueryOrderBy<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 从指定存储集中按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表。
        /// </summary>
        FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 查询指定存储集中满足给定筛选条件的类型 <typeparamref name="T"/> 实体列表，仅返回指定字段的数据。
        /// </summary>
        FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>[] cols, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 查询指定 ID 的多个集合，将结果存储在给定的实体列表中。
        /// </summary>
        FTask Query(long id, List<string> nameNames, List<Entity> result, bool isDeserialize = false);
        /// <summary>
        /// 根据给定的 JSON 查询字符串查询指定存储集中的类型 <typeparamref name="T"/> 实体列表。
        /// </summary>
        FTask<List<T>> QueryJson<T>(string json, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 根据给定的 JSON 查询字符串查询指定存储集中的类型 <typeparamref name="T"/> 实体列表，仅返回指定列的数据。
        /// </summary>
        FTask<List<T>> QueryJson<T>(string json, string[] cols, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 根据给定的 JSON 查询字符串查询指定存储集中的类型 <typeparamref name="T"/> 实体列表，通过指定的任务 ID 进行标识。
        /// </summary>
        FTask<List<T>> QueryJson<T>(long taskId, string json, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 查询指定存储集中满足给定筛选条件的类型 <typeparamref name="T"/> 实体列表，仅返回指定列的数据。
        /// </summary>
        FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, string[] cols, bool isDeserialize = false, string name = null) where T : Entity;
        /// <summary>
        /// 保存类型 <typeparamref name="T"/> 实体到指定存储集中。
        /// </summary>
        FTask Save<T>(T entity, string name = null) where T : Entity, new();
        /// <summary>
        /// 保存一组实体到数据库中，根据实体列表的 ID 进行区分和存储。
        /// </summary>
        FTask Save(long id, List<Entity> entities);
        /// <summary>
        /// 通过事务会话将类型 <typeparamref name="T"/> 实体保存到指定存储集中。
        /// </summary>
        FTask Save<T>(object transactionSession, T entity, string name = null) where T : Entity;
        /// <summary>
        /// 向指定存储集中插入一个类型 <typeparamref name="T"/> 实体。
        /// </summary>
        FTask Insert<T>(T entity, string name = null) where T : Entity, new();
        /// <summary>
        /// 批量插入一组类型 <typeparamref name="T"/> 实体到指定存储集中。
        /// </summary>
        FTask InsertBatch<T>(IEnumerable<T> list, string name = null) where T : Entity, new();
        /// <summary>
        /// 通过事务会话，批量插入一组类型 <typeparamref name="T"/> 实体到指定存储集中。
        /// </summary>
        FTask InsertBatch<T>(object transactionSession, IEnumerable<T> list, string name = null) where T : Entity, new();
        /// <summary>
        /// 通过事务会话，根据指定的 ID 从数据库中删除指定类型 <typeparamref name="T"/> 实体。
        /// </summary>
        FTask<long> Remove<T>(object transactionSession, long id, string name = null) where T : Entity, new();
        /// <summary>
        /// 根据指定的 ID 从数据库中删除指定类型 <typeparamref name="T"/> 实体。
        /// </summary>
        FTask<long> Remove<T>(long id, string name = null) where T : Entity, new();
        /// <summary>
        /// 通过事务会话，根据给定的筛选条件从数据库中删除指定类型 <typeparamref name="T"/> 实体。
        /// </summary>
        FTask<long> Remove<T>(long coroutineLockQueueKey, object transactionSession, Expression<Func<T, bool>> filter, string name = null) where T : Entity, new();
        /// <summary>
        /// 根据给定的筛选条件从数据库中删除指定类型 <typeparamref name="T"/> 实体。
        /// </summary>
        FTask<long> Remove<T>(long coroutineLockQueueKey, Expression<Func<T, bool>> filter, string name = null) where T : Entity, new();
        /// <summary>
        /// 根据给定的筛选条件计算指定存储集中类型 <typeparamref name="T"/> 实体某个属性的总和。
        /// </summary>
        FTask<long> Sum<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sumExpression, string name = null) where T : Entity;
    }
}

#endif