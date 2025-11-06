#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Database;
using Fantasy.Entitas;
namespace Fantasy.SeparateTable;

/// <summary>
/// 分表组件扩展方法。
/// </summary>
public static class SeparateTableGeneratedExtensions
{
    /// <summary>
    /// 从数据库加载指定实体的所有分表数据，并自动建立父子关系。
    /// </summary>
    public static FTask LoadWithSeparateTables<T>(this T entity, IDatabase database) where T : Entity, new()
    {
        return entity.Scene.SeparateTableComponent.LoadWithSeparateTables(entity, database);
    }
    /// <summary>
    /// 将实体及其所有分表组件保存到数据库中。
    /// </summary>
    public static FTask PersistAggregate<T>(this T entity, IDatabase database) where T : Entity, new()
    {
        return entity.Scene.SeparateTableComponent.PersistAggregate(entity,database);
    }
}
#endif
