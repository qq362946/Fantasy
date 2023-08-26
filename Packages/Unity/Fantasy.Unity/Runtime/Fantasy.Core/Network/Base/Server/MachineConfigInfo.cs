#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 机器配置信息的类。
/// </summary>
public class MachineConfigInfo
{
    /// <summary>
    /// 获取或设置机器的唯一标识。
    /// </summary>
    public uint Id;
    /// <summary>
    /// 获取或设置外部IP地址。
    /// </summary>
    public string OuterIP;
    /// <summary>
    /// 获取或设置外部绑定IP地址。
    /// </summary>
    public string OuterBindIP;
    /// <summary>
    /// 获取或设置内部绑定IP地址。
    /// </summary>
    public string InnerBindIP;
    /// <summary>
    /// 获取或设置管理端口。
    /// </summary>
    public int ManagementPort;
}
#endif
