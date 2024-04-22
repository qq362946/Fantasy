using System.Reflection;
using CommandLine;
using NLog;
// ReSharper disable FunctionNeverReturns

namespace Fantasy;

/// <summary>
/// Fantasy.Net 应用程序入口类型
/// </summary>
public static class Entry
{
    /// <summary>
    /// 执行 Fantasy 应用程序的初始化操作。
    /// </summary>
    /// <exception cref="Exception">当命令行格式异常时抛出。</exception>
    /// <exception cref="NotSupportedException">不支持的 AppType 类型异常。</exception>
    public static async Task Start(params Assembly[] assemblies)
    {
        // 解析命令行参数
        Parser.Default.ParseArguments<CommandLineOptions>(Environment.GetCommandLineArgs())
            .WithNotParsed(error => throw new Exception("Command line format error!"))
            .WithParsed(option =>
            {
                AppDefine.Options = option;
                AppDefine.InnerNetwork = Enum.Parse<NetworkProtocolType>(option.InnerNetwork);
            });
        // 非Benchmark模式、根据不同的运行模式来选择日志的方式
        switch (AppDefine.Options.Mode)
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
        switch (AppDefine.Options.AppType)
        {
            case "Game":
            {
                break;
            }
            default:
            {
                throw new NotSupportedException($"AppType is {AppDefine.Options.AppType} Unrecognized!");
            }
        }
        // 初始化程序集管理系统
        AssemblySystem.Initialize(assemblies);
        // 初始化单例管理系统
        await SingletonSystem.Initialize();
        // 初始化ProtoBuffHelper
        ProtoBuffHelper.Initialize();
        // 精度处理（只针对Windows下有作用、其他系统没有这个问题、一般也不会用Windows来做服务器的）
        WinPeriod.Initialize();
        // 初始化服务器
        InitializeServer().Coroutine();
        while (true)
        {
            Thread.Yield();
            ThreadScheduler.Update();
        }
    }

    private static async FTask InitializeServer()
    {
        switch (AppDefine.Options.Mode)
        {
            case "Develop":
            {
                // 开发模式默认所有Server都在一个进程中、方便调试、但网络还都是独立的
                
                foreach (var serverConfig in ServerConfigData.Instance.List)
                {
                    await Server.Create(serverConfig.Id);
                }
                
                return;
            }
            case "Release":
            {
                // 发布模式只会启动启动参数传递的Server、也就是只会启动一个Server
                // 您可以做一个Server专门用于管理启动所有Server的工作
                await Server.Create(AppDefine.Options.AppId);
                return;
            }
        }
    }

    /// <summary>
    /// 关闭 Fantasy 应用程序，释放 SingletonSystem 中的实例和已加载的程序集。
    /// </summary>
    public static void Close()
    {
        SingletonSystem.Instance.Dispose();
        AssemblySystem.Dispose();
    }
}