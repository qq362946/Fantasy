#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 表示游戏世界的配置信息。
/// </summary>
public class WorldConfigInfo
{
    /// <summary>
    /// 获取或设置游戏世界的唯一标识。
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// 获取或设置游戏世界的名称。
    /// </summary>
    public string WorldName { get; set; }

    /// <summary>
    /// 获取或设置游戏世界的数据库连接字符串。
    /// </summary>
    public string DbConnection { get; set; }

    /// <summary>
    /// 获取或设置游戏世界的数据库名称。
    /// </summary>
    public string DbName { get; set; }

    /// <summary>
    /// 获取或设置游戏世界的数据库类型。
    /// </summary>
    public string DbType { get; set; }
}
#endif