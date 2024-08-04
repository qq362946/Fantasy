#if FANTASY_NET
using System.Linq.Expressions;
#pragma warning disable CS8625

namespace Fantasy;

/// <summary>
/// 表示用于执行各种数据库操作的数据库接口。
/// </summary>
public interface IDateBase
{
    /// <summary>
    /// 初始化数据库连接。
    /// </summary>
    IDateBase Initialize(Scene scene, string connectionString, string dbName);
    /// <summary>
    /// 在指定的集合中检索类型 <typeparamref name="T"/> 的实体数量。
    /// </summary>
    FTask<long> Count<T>(string collection = null) where T : Entity;
    /// <summary>
    /// 在指定的集合中检索满足给定筛选条件的类型 <typeparamref name="T"/> 的实体数量。
    /// </summary>
    FTask<long> Count<T>(Expression<Func<T, bool>> filter, string collection = null) where T : Entity;
    /// <summary>
    /// 检查指定集合中是否存在类型 <typeparamref name="T"/> 的实体。
    /// </summary>
    FTask<bool> Exist<T>(string collection = null) where T : Entity;
    /// <summary>
    /// 检查指定集合中是否存在满足给定筛选条件的类型 <typeparamref name="T"/> 的实体。
    /// </summary>
    FTask<bool> Exist<T>(Expression<Func<T, bool>> filter, string collection = null) where T : Entity;
    /// <summary>
    /// 从指定集合中检索指定 ID 的类型 <typeparamref name="T"/> 的实体，不锁定。
    /// </summary>
    FTask<T> QueryNotLock<T>(long id, string collection = null) where T : Entity;
    /// <summary>
    /// 从指定集合中检索指定 ID 的类型 <typeparamref name="T"/> 的实体。
    /// </summary>
    FTask<T> Query<T>(long id, string collection = null) where T : Entity;
    /// <summary>
    /// 按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体数量和日期。
    /// </summary>
    FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string collection = null) where T : Entity;
    /// <summary>
    /// 按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体数量和日期。
    /// </summary>
    FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, string collection = null) where T : Entity;
    /// <summary>
    /// 分页查询指定集合中满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表。
    /// </summary>
    FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string collection = null) where T : Entity;
    /// <summary>
    /// 分页查询指定集合中满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表，仅返回指定列的数据。
    /// </summary>
    FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, string collection = null) where T : Entity;
    /// <summary>
    /// 从指定集合中按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表，按指定字段排序。
    /// </summary>
    FTask<List<T>> QueryByPageOrderBy<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression, bool isAsc = true, string collection = null) where T : Entity;
    /// <summary>
    /// 检索满足给定筛选条件的类型 <typeparamref name="T"/> 的第一个实体，从指定集合中。
    /// </summary>
    FTask<T?> First<T>(Expression<Func<T, bool>> filter, string collection = null) where T : Entity;
    /// <summary>
    /// 查询指定集合中满足给定 JSON 查询字符串的类型 <typeparamref name="T"/> 的第一个实体，仅返回指定列的数据。
    /// </summary>
    FTask<T> First<T>(string json, string[] cols, string collection = null) where T : Entity;
    /// <summary>
    /// 从指定集合中按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表，按指定字段排序。
    /// </summary>
    FTask<List<T>> QueryOrderBy<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByExpression, bool isAsc = true, string collection = null) where T : Entity;
    /// <summary>
    /// 从指定集合中按页查询满足给定筛选条件的类型 <typeparamref name="T"/> 的实体列表。
    /// </summary>
    FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, string collection = null) where T : Entity;
    /// <summary>
    /// 查询指定 ID 的多个集合，将结果存储在给定的实体列表中。
    /// </summary>
    FTask Query(long id, List<string> collectionNames, List<Entity> result);
    /// <summary>
    /// 根据给定的 JSON 查询字符串查询指定集合中的类型 <typeparamref name="T"/> 实体列表。
    /// </summary>
    FTask<List<T>> QueryJson<T>(string json, string collection = null) where T : Entity;
    /// <summary>
    /// 根据给定的 JSON 查询字符串查询指定集合中的类型 <typeparamref name="T"/> 实体列表，仅返回指定列的数据。
    /// </summary>
    FTask<List<T>> QueryJson<T>(string json, string[] cols, string collection = null) where T : Entity;
    /// <summary>
    /// 根据给定的 JSON 查询字符串查询指定集合中的类型 <typeparamref name="T"/> 实体列表，通过指定的任务 ID 进行标识。
    /// </summary>
    FTask<List<T>> QueryJson<T>(long taskId, string json, string collection = null) where T : Entity;
    /// <summary>
    /// 查询指定集合中满足给定筛选条件的类型 <typeparamref name="T"/> 实体列表，仅返回指定列的数据。
    /// </summary>
    FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, string[] cols, string collection = null) where T : class;
    /// <summary>
    /// 保存类型 <typeparamref name="T"/> 实体到指定集合中，如果集合不存在将自动创建。
    /// </summary>
    FTask Save<T>(T entity, string collection = null) where T : Entity, new();
    /// <summary>
    /// 保存一组实体到数据库中，根据实体列表的 ID 进行区分和存储。
    /// </summary>
    FTask Save(long id, List<Entity> entities);
    /// <summary>
    /// 通过事务会话将类型 <typeparamref name="T"/> 实体保存到指定集合中，如果集合不存在将自动创建。
    /// </summary>
    FTask Save<T>(object transactionSession, T entity, string collection = null) where T : Entity;
    /// <summary>
    /// 向指定集合中插入一个类型 <typeparamref name="T"/> 实体，如果集合不存在将自动创建。
    /// </summary>
    FTask Insert<T>(T entity, string collection = null) where T : Entity, new();
    /// <summary>
    /// 批量插入一组类型 <typeparamref name="T"/> 实体到指定集合中，如果集合不存在将自动创建。
    /// </summary>
    FTask InsertBatch<T>(IEnumerable<T> list, string collection = null) where T : Entity, new();
    /// <summary>
    /// 通过事务会话，批量插入一组类型 <typeparamref name="T"/> 实体到指定集合中，如果集合不存在将自动创建。
    /// </summary>
    FTask InsertBatch<T>(object transactionSession, IEnumerable<T> list, string collection = null) where T : Entity, new();
    /// <summary>
    /// 通过事务会话，根据指定的 ID 从数据库中删除指定类型 <typeparamref name="T"/> 实体。
    /// </summary>
    FTask<long> Remove<T>(object transactionSession, long id, string collection = null) where T : Entity, new();
    /// <summary>
    /// 根据指定的 ID 从数据库中删除指定类型 <typeparamref name="T"/> 实体。
    /// </summary>
    FTask<long> Remove<T>(long id, string collection = null) where T : Entity, new();
    /// <summary>
    /// 通过事务会话，根据给定的筛选条件和 ID 从数据库中删除指定类型 <typeparamref name="T"/> 实体。
    /// </summary>
    FTask<long> Remove<T>(long id,object transactionSession, Expression<Func<T, bool>> filter, string collection = null) where T : Entity, new();
    /// <summary>
    /// 根据给定的筛选条件和 ID 从数据库中删除指定类型 <typeparamref name="T"/> 实体。
    /// </summary>
    FTask<long> Remove<T>(long id, Expression<Func<T, bool>> filter, string collection = null) where T : Entity, new();
    /// <summary>
    /// 根据给定的筛选条件计算指定集合中类型 <typeparamref name="T"/> 实体某个属性的总和。
    /// </summary>
    FTask<long> Sum<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sumExpression, string collection = null) where T : Entity;
    /// <summary>
    /// 在指定的集合中创建索引，以提高类型 <typeparamref name="T"/> 实体的查询性能。
    /// </summary>
    FTask CreateIndex<T>(string collection, params object[] keys) where T : Entity;
    /// <summary>
    /// 在默认集合中创建索引，以提高类型 <typeparamref name="T"/> 实体的查询性能。
    /// </summary>
    FTask CreateIndex<T>(params object[] keys) where T : Entity;
    /// <summary>
    /// 创建指定类型 <typeparamref name="T"/> 的数据库，用于存储实体。
    /// </summary>
    FTask CreateDB<T>() where T : Entity;
    /// <summary>
    /// 根据指定类型创建数据库，用于存储实体。
    /// </summary>
    FTask CreateDB(Type type);
}
#endif