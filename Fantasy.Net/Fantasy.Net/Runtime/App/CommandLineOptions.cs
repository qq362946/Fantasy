#if FANTASY_NET
using CommandLine;

#pragma warning disable CS8618

namespace Fantasy;

/// <summary>
/// 命令行选项类，用于解析命令行参数。
/// </summary>
public class CommandLineOptions
{
    /// <summary>
    /// 进程Id，获取或设置进程的唯一标识符。
    /// </summary>
    [Option("AppId", Required = false, Default = (uint)0, HelpText = "Enter an AppId such as 1")]
    public uint AppId { get; set; }
    /// <summary>
    /// App类型，获取或设置应用程序的类型。
    /// Game - 游戏服务器App
    /// Robot - 机器人（暂未支持该功能）
    /// </summary>
    [Option('a', "AppType", Required = false, Default = "Game", HelpText = "Game")]
    public string AppType { get; set; }
    /// <summary>
    /// 服务器运行模式，获取或设置服务器的运行模式。
    /// Develop - 开发模式（所有Server都在一个进程中）
    /// Release - 发布模式（每个Server都在独立的进程中）
    /// </summary>
    [Option('m',"Mode", Required = true, Default = "Release", HelpText = "Develop:所有Server都在一个进程中,\nRelease:每个Server都在独立的进程中")]
    public string Mode { get; set; }
    /// <summary>
    /// 服务器内部网络协议
    /// TCP - 服务器内部之间通讯使用TCP协议
    /// KCP - 服务器内部之间通讯使用KCP协议
    /// </summary>
    [Option('n',"InnerNetwork", Required = false, Default = "KCP", HelpText = "TCP、KCP")]
    public string InnerNetwork { get; set; }
    /// <summary>
    /// 配置表文件夹路径。
    /// </summary>
    [Option('c',"ConfigTableBinaryDirectory", Required = true, Default = "", HelpText = "Configure the table binary folder path")]
    public string ConfigTableBinaryDirectory{ get; set; }
    /// <summary>
    /// 会话空闲检查超时时间。
    /// </summary>
    [Option('t',"SessionIdleCheckerTimeout", Required = false, Default = 8000, HelpText = "Session idle check timeout")]
    public int SessionIdleCheckerTimeout { get; set; }
    /// <summary>
    /// 会话空闲检查间隔。
    /// </summary>
    [Option('i',"SessionIdleCheckerInterval", Required = false, Default = 5000, HelpText = "Session idle check interval")]
    public int SessionIdleCheckerInterval { get; set; }
}
#endif