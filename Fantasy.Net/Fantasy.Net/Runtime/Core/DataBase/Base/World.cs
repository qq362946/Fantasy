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
    public WorldConfig Config => WorldConfigData.Instance.Get(Id);
    /// <summary>
    /// 用于存储已创建的游戏世界实例
    /// </summary>
    private static readonly Dictionary<uint, World> Worlds = new();

    /// <summary>
    /// 使用指定的配置信息创建一个游戏世界实例。
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="worldConfigId"></param>
    public World(Scene scene, uint worldConfigId)
    {
        Id = worldConfigId;
        var worldConfig = Config;
        var dbType = worldConfig.DbType.ToLower();

        switch (dbType)
        {
            case "mongodb":
            {
                DateBase = new MongoDataBase();
                DateBase.Initialize(scene, worldConfig.DbConnection, worldConfig.DbName);
                break;
            }
            default:
                throw new Exception("No supported database");
        }
    }

    /// <summary>
    /// 创建一个指定唯一标识的游戏世界实例。
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="id">游戏世界的唯一标识。</param>
    /// <returns>游戏世界实例。</returns>
    public static World Create(Scene scene, uint id)
    {
        if (Worlds.TryGetValue(id, out var world))
        {
            return world;
        }

        world = new World(scene, id);
        Worlds.Add(id, world);
        return world;
    }
}
#endif