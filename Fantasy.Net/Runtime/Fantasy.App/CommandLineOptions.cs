#if FANTASY_NET
using CommandLine;
using Fantasy.Core;

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
    [Option("AppType", Required = true, Default = null, HelpText = "Game")]
    public string AppType { get; set; }
    /// <summary>
    /// 服务器运行模式，获取或设置服务器的运行模式。
    /// Develop - 开发模式（所有Server都在一个进程中）
    /// Release - 发布模式（每个Server都在独立的进程中）
    /// </summary>
    [Option("Mode", Required = true, Default = "Release", HelpText = "Develop:所有Server都在一个进程中,Release:每个Server都在独立的进程中")]
    public string Mode { get; set; }
    /// <summary>
    /// 服务器内部网络协议
    /// TCP - 服务器内部之间通讯使用TCP协议
    /// KCP - 服务器内部之间通讯使用KCP协议
    /// </summary>
    [Option("InnerNetwork", Required = false, Default = "TCP", HelpText = "TCP、KCP")]
    public string InnerNetwork { get; set; }
}
#endif