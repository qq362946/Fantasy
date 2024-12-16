#if FANTASY_NET
using CommandLine;
using Fantasy.Network;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy.Platform.Net;

/// <summary>
/// Process运行模式
/// </summary>
public enum ProcessMode
{
    /// <summary>
    /// 默认
    /// </summary>
    None =0,
    /// <summary>
    /// 开发模式
    /// </summary>
    Develop = 1,
    /// <summary>
    /// 发布模式
    /// </summary>
    Release = 2
}

internal sealed class CommandLineOptions
{
    /// <summary>
    /// 用于启动指定的进程，该进程的 ID 与 ProcessConfig 的 ID 相关联。此参数只能传递单个 ID，不支持传递多个 ID。
    /// </summary>
    [Option("pid", Required = false, Default = (uint)0, HelpText = "Enter an ProcessIdId such as 1")]
    public uint ProcessId { get; set; }
    /// <summary>
    /// Process类型，获取或设置应用程序的类型。
    /// Game - 游戏服务器Process
    /// Robot - 机器人（暂未支持该功能）
    /// </summary>
    [Option('a', "ProcessType", Required = false, Default = "Game", HelpText = "Game")]
    public string ProcessType { get; set; }
    /// <summary>
    /// 服务器运行模式，获取或设置服务器的运行模式。
    /// Develop - 开发模式（启动Process配置表中的所有Process）
    /// Release - 发布模式（根据ProcessId启动Process）
    /// </summary>
    [Option('m', "Mode", Required = true, Default = "Release", HelpText = "Develop:启动Process配置表中的所有Process,\nRelease:根据ProcessId启动Process")]
    public string Mode { get; set; }
    /// <summary>
    /// 服务器内部网络协议
    /// TCP - 服务器内部之间通讯使用TCP协议
    /// KCP - 服务器内部之间通讯使用KCP协议
    /// WebSocket - 服务器内部之间通讯使用WebSocket协议(不推荐、TCP或KCP)
    /// </summary>
    [Option('n', "InnerNetwork", Required = false, Default = "TCP", HelpText = "TCP、KCP、WebSocket")]
    public string InnerNetwork { get; set; }
    /// <summary>
    /// 会话空闲检查超时时间。
    /// </summary>
    [Option('t', "SessionIdleCheckerTimeout", Required = false, Default = 8000, HelpText = "Session idle check timeout")]
    public int SessionIdleCheckerTimeout { get; set; }
    /// <summary>
    /// 会话空闲检查间隔。
    /// </summary>
    [Option('i', "SessionIdleCheckerInterval", Required = false, Default = 5000, HelpText = "Session idle check interval")]
    public int SessionIdleCheckerInterval { get; set; }
    /// <summary>
    /// 启动组。
    /// </summary>
    [Option('g', "StartupGroup", Required = false, Default = 0, HelpText = "Used to start a group of Process")]
    public int StartupGroup { get; set; }
}

/// <summary>
/// AppDefine
/// </summary>
internal static class ProcessDefine
{
    /// <summary>
    /// 命令行选项
    /// </summary>
    public static CommandLineOptions Options;
    /// <summary>
    /// App程序Id
    /// </summary>
    public static uint ProcessId => Options.ProcessId;
    /// <summary>
    /// 会话空闲检查超时时间。
    /// </summary>
    public static int SessionIdleCheckerTimeout => Options.SessionIdleCheckerTimeout;
    /// <summary>
    /// 会话空闲检查间隔。
    /// </summary>
    public static int SessionIdleCheckerInterval => Options.SessionIdleCheckerInterval;
    /// <summary>
    /// 内部网络通讯协议类型
    /// </summary>
    public static NetworkProtocolType InnerNetwork;
}
#endif