#if FANTASY_NET
using System.Reflection;
using CommandLine;
using NLog;

namespace Fantasy;

/// <summary>
/// Fantasy.Net 应用程序入口
/// </summary>
/// <exception cref="Exception">当命令行格式异常时抛出。</exception>
/// <exception cref="NotSupportedException">不支持的 ProcessType 类型异常。</exception>
public static class Entry
{
    /// <summary>
    /// 启动Fantasy.Net
    /// </summary>
    /// <param name="assemblies"></param>
    public static async FTask Start(params Assembly[] assemblies)
    {
        // 解析命令行参数
        Parser.Default.ParseArguments<CommandLineOptions>(Environment.GetCommandLineArgs())
            .WithNotParsed(error => throw new Exception("Command line format error!"))
            .WithParsed(option =>
            {
                ProcessDefine.Options = option;
                ProcessDefine.InnerNetwork = Enum.Parse<NetworkProtocolType>(option.InnerNetwork);
            });
        // 非Benchmark模式、根据不同的运行模式来选择日志的方式
        switch (ProcessDefine.Options.Mode)
        {
            case "Develop":
            {
                LogManager.Configuration.RemoveRuleByName("ServerDebug");
                LogManager.Configuration.RemoveRuleByName("ServerTrace");
                LogManager.Configuration.RemoveRuleByName("ServerInfo");
                LogManager.Configuration.RemoveRuleByName("ServerWarn");
                LogManager.Configuration.RemoveRuleByName("ServerError");
                break;
            }
            case "Release":
            {
                LogManager.Configuration.RemoveRuleByName("ConsoleTrace");
                LogManager.Configuration.RemoveRuleByName("ConsoleDebug");
                LogManager.Configuration.RemoveRuleByName("ConsoleInfo");
                LogManager.Configuration.RemoveRuleByName("ConsoleWarn");
                LogManager.Configuration.RemoveRuleByName("ConsoleError");
                break;
            }
        }
        // 检查启动参数,后期可能有机器人等不同的启动参数
        switch (ProcessDefine.Options.ProcessType)
        {
            case "Game":
            {
                break;
            }
            default:
            {
                throw new NotSupportedException($"ProcessType is {ProcessDefine.Options.ProcessType} Unrecognized!");
            }
        }
        // 初始化程序集管理系统
        AssemblySystem.Initialize(assemblies);
        // Mongo初始化
        MongoHelper.Initialize();
        // 初始化ProtoBuffHelper
        ProtoBuffHelper.Initialize();
        // 精度处理（只针对Windows下有作用、其他系统没有这个问题、一般也不会用Windows来做服务器的）
        WinPeriod.Initialize();
        // 启动Process
        StartProcess().Coroutine();
        await FTask.CompletedTask;
        while (true)
        {
            ThreadScheduler.Update();
            Thread.Yield();
        }
    }

    private static async FTask StartProcess()
    {
        switch (ProcessDefine.Options.Mode)
        {
            case "Develop":
            {
                foreach (var processConfig in ProcessConfigData.Instance.List)
                {
                    await Process.Create(processConfig.Id);
                }
                
                return;
            }
            case "Release":
            {
                await Process.Create(ProcessDefine.Options.ProcessId);
                return;
            }
        }
    }
    
    /// <summary>
    /// 关闭 Fantasy
    /// </summary>
    public static void Close()
    {
        AssemblySystem.Dispose();
    }
}
#endif