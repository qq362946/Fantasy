#if FANTASY_NET
using CommandLine;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Serialize;
// ReSharper disable FunctionNeverReturns

namespace Fantasy.Platform.Net;

/// <summary>
/// Fantasy.Net 应用程序入口
/// </summary>
/// <exception cref="Exception">当命令行格式异常时抛出。</exception>
/// <exception cref="NotSupportedException">不支持的 ProcessType 类型异常。</exception>
public static class Entry
{
    /// <summary>
    /// 框架初始化
    /// </summary>
    /// <param name="log">日志实例</param>
    /// <param name="assemblies">注册的Assembly</param>
    /// <returns></returns>
    public static async FTask Initialize(ILog? log, params System.Reflection.Assembly[] assemblies)
    {
        // 注册日志模块到框架
        if (log != null)
        {
            Log.Register(log);
        }
        Log.Info($"Fantasy Version:{ProgramDefine.VERSION}");
        // 加载Fantasy.config配置文件
        await ConfigLoader.InitializeFromXml(Path.Combine(AppContext.BaseDirectory, "Fantasy.config"));
        // 解析命令行参数
        Parser.Default.ParseArguments<CommandLineOptions>(Environment.GetCommandLineArgs())
            .WithNotParsed(error => throw new Exception("Command line format error!"))
            .WithParsed(option =>
            {
                ProgramDefine.ProcessId = option.ProcessId;
                ProgramDefine.ProcessType  = option.ProcessType;
                ProgramDefine.RuntimeMode = Enum.Parse<ProcessMode>(option.RuntimeMode);
                ProgramDefine.StartupGroup = option.StartupGroup;
            });
        // 初始化Log系统
        Log.Initialize();
        // 检查启动参数,后期可能有机器人等不同的启动参数
        switch (ProgramDefine.ProcessType)
        {
            case "Game":
            {
                break;
            }
            default:
            {
                throw new NotSupportedException($"ProcessType is {ProgramDefine.ProcessType} Unrecognized!");
            }
        }
        // 初始化程序集管理系统
        await AssemblySystem.InnerInitialize(assemblies);
        // 初始化序列化
        SerializerManager.Initialize();
        // 精度处理（只针对Windows下有作用、其他系统没有这个问题、一般也不会用Windows来做服务器的）
        WinPeriod.Initialize();
    }

    /// <summary>
    /// 框架初始化
    /// </summary>
    /// <param name="assemblies">注册的Assembly</param>
    public static FTask Initialize(params System.Reflection.Assembly[] assemblies)
    {
        return Initialize(null, assemblies);
    }

    /// <summary>
    /// 启动Fantasy.Net
    /// </summary>
    public static async FTask Start()
    {
        // 启动Process
        StartProcess().Coroutine();
        await FTask.CompletedTask;
        while (true)
        {
            ThreadScheduler.Update();
            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// 初始化并且启动框架
    /// </summary>
    /// <param name="assemblies"></param>
    public static async FTask Start(params System.Reflection.Assembly[] assemblies)
    {
        await Initialize(assemblies);
        await Start();
    }

    private static async FTask StartProcess()
    {
        if (ProgramDefine.StartupGroup != 0)
        {
            foreach (var processConfig in ProcessConfigData.Instance.ForEachByStartupGroup((uint)ProgramDefine.StartupGroup))
            {
                await Process.Create(processConfig.Id);
            }

            return;
        }
        
        switch (ProgramDefine.RuntimeMode)
        {
            case ProcessMode.Develop:
            {
                foreach (var processConfig in ProcessConfigData.Instance.List)
                {
                    await Process.Create(processConfig.Id);
                }
                
                return;
            }
            case ProcessMode.Release:
            {
                await Process.Create(ProgramDefine.ProcessId);
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
        SerializerManager.Dispose();
    }
}
#endif