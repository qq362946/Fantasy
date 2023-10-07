#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 服务器配置信息。
/// </summary>
public class ServerConfigInfo
{
    /// <summary>
    /// 获取或设置服务器的唯一标识符。
    /// </summary>
    public uint Id;
    /// <summary>
    /// 获取或设置服务器所在的机器标识符。
    /// </summary>
    public uint MachineId;
    /// <summary>
    /// 获取或设置服务器的内部端口。
    /// </summary>
    public int InnerPort;
}
#endif
