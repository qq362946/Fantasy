#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 表示一个游戏世界。
/// </summary>
public sealed class World
{
    /// <summary>
    /// 获取游戏世界的唯一标识。
    /// </summary>
    public uint Id { get; private init; }
    /// <summary>
    /// 获取游戏世界的数据库接口。
    /// </summary>
    public IDateBase DateBase { get; private init; }
    /// <summary>
    /// 获取游戏世界的配置信息。
    /// </summary>
    public WorldConfigInfo Config => ConfigTableManage.WorldConfigInfo(Id);
    /// <summary>
    /// 用于存储已创建的游戏世界实例
    /// </summary>
    private static readonly Dictionary<uint, World> Worlds = new();

    /// <summary>
    /// 使用指定的配置信息创建一个游戏世界实例。
    /// </summary>
    /// <param name="worldConfigInfo">游戏世界的配置信息。</param>
    public World(WorldConfigInfo worldConfigInfo)
    {
        Id = worldConfigInfo.Id;
        var dbType = worldConfigInfo.DbType.ToLower();
        
        switch (dbType)
        {
            case "mongodb":
            {
                DateBase = new MongoDataBase();
                DateBase.Initialize(worldConfigInfo.DbConnection, worldConfigInfo.DbName);
                break;
            }
            default:
                throw new Exception("No supported database");
        }
    }

    /// <summary>
    /// 创建一个指定唯一标识的游戏世界实例。
    /// </summary>
    /// <param name="id">游戏世界的唯一标识。</param>
    /// <returns>游戏世界实例。</returns>
    public static World Create(uint id)
    {
        if (Worlds.TryGetValue(id, out var world))
        {
            return world;
        }

        var worldConfigInfo = ConfigTableManage.WorldConfigInfo(id);
        world = new World(worldConfigInfo);
        Worlds.Add(id, world);
        return world;
    }
}
#endif