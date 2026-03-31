#if FANTASY_NET
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy.Database;

/// <summary>
/// 数据库自定义连接参数
/// </summary>
public sealed class DataBaseCustomConfig
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
#endif
