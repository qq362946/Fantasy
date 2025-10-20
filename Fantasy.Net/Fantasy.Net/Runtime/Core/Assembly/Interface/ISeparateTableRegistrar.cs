#if FANTASY_NET
namespace Fantasy.Assembly;

/// <summary>
/// 分表注册器接口，用于自动注册标记了 SeparateTableAttribute 的实体类型。
/// 通过 Source Generator 自动生成实现类，替代运行时反射，提升性能。
/// </summary>
public interface ISeparateTableRegistrar
{
    /// <summary>
    /// 分表信息记录，包含父实体类型、子实体类型和数据库集合名称。
    /// </summary>
    /// <param name="RootType">父实体的类型，表示子实体属于哪个父实体。</param>
    /// <param name="EntityType">子实体的类型，即标记了 SeparateTableAttribute 的实体类型。</param>
    /// <param name="TableName">在数据库中使用的集合名称。</param>
    public sealed record SeparateTableInfo(Type RootType, Type EntityType, string TableName);

    /// <summary>
    /// 注册所有分表信息。
    /// 返回包含所有标记了 SeparateTableAttribute 的实体及其配置信息的列表。
    /// </summary>
    /// <returns>分表信息列表。</returns>
    List<SeparateTableInfo> Register();

    /// <summary>
    /// 反注册所有分表信息。
    /// 返回需要移除的分表信息列表。
    /// </summary>
    /// <returns>分表信息列表。</returns>
    List<SeparateTableInfo> UnRegister();
}


#endif
