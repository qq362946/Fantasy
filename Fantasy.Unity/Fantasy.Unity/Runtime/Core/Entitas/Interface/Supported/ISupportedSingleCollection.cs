#if FANTASY_NET
namespace Fantasy;
// Entity是单一集合、保存到数据库的时候不会跟随父组件保存在一个集合里、会单独保存在一个集合里
// 需要配合SingleCollectionAttribute一起使用、如在Entity类头部定义SingleCollectionAttribute(typeOf(Unit))
// SingleCollectionAttribute用来定义这个Entity是属于哪个Entity的子集
/// <summary>
/// 定义实体支持单一集合存储的接口。当实体需要单独存储在一个集合中，并且在保存到数据库时不会与父组件一起保存在同一个集合中时，应实现此接口。
/// </summary>
public interface ISupportedSingleCollection { }
public static class SupportedSingleCollectionChecker<T> where T : Entity
{
    public static bool IsSupported { get; }

    static SupportedSingleCollectionChecker()
    {
        IsSupported = typeof(ISupportedSingleCollection).IsAssignableFrom(typeof(T));
    }
}
/// <summary>
/// 表示用于指定实体的单一集合存储属性。此属性用于配合 <see cref="ISupportedSingleCollection"/> 接口使用，
/// 用于定义实体属于哪个父实体的子集合，以及在数据库中使用的集合名称。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class SingleCollectionAttribute : Attribute
{
    /// <summary>
    /// 获取父实体的类型，指示此实体是属于哪个父实体的子集合。
    /// </summary>
    public readonly Type RootType;
    /// <summary>
    /// 获取在数据库中使用的集合名称。
    /// </summary>
    public readonly string CollectionName;
    /// <summary>
    /// 初始化 <see cref="SingleCollectionAttribute"/> 类的新实例，指定父实体类型和集合名称。
    /// </summary>
    /// <param name="rootType">父实体的类型。</param>
    /// <param name="collectionName">在数据库中使用的集合名称。</param>
    public SingleCollectionAttribute(Type rootType, string collectionName)
    {
        RootType = rootType;
        CollectionName = collectionName;
    }
}
#endif