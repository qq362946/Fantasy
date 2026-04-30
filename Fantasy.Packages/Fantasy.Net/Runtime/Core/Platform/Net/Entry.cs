#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CommandLine;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Serialize;
// ReSharper disable FunctionNeverReturns
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.Platform.Net;

/// <summary>
/// Fantasy.Net 应用程序入口
/// </summary>
/// <exception cref="Exception">当命令行格式异常时抛出。</exception>
/// <exception cref="NotSupportedException">不支持的 ProcessType 类型异常。</exception>
public static class Entry
{
    private static readonly List<Process> ProcessList = new List<Process>();
    /// <summary>
    /// 启动Fantasy.Net
    /// </summary>
    public static async FTask Start(ILog log = null)
    {
        // 初始化
        await Initialize(log);
        // 启动Process
        StartProcess().Coroutine();
        // 设置当前程序已经在运行中
        ProgramDefine.IsAppRunning = true;
        await FTask.CompletedTask;
        while (true)
        {
            ThreadScheduler.Update();
            Thread.Sleep(1);
        }
    }
    
    private static async FTask StartProcess()
    {
        if (ProgramDefine.StartupGroup != 0)
        {
            foreach (var processConfig in ProcessConfigData.Instance.ForEachByStartupGroup((uint)ProgramDefine.StartupGroup))
            {
                var process = await Process.Create(processConfig.Id);
                if (process != null)
                {
                    ProcessList.Add(process);
                }
            }

            return;
        }
        
        switch (ProgramDefine.RuntimeMode)
        {
            case ProcessMode.Develop:
            {
                foreach (var processConfig in ProcessConfigData.Instance.List)
                {
                    var process = await Process.Create(processConfig.Id);
                    if (process != null)
                    {
                        ProcessList.Add(process);
                    }
                }
                return;
            }
            case ProcessMode.Release:
            {
                var process = await Process.Create(ProgramDefine.ProcessId);
                if (process != null)
                {
                    ProcessList.Add(process);
                }
                return;
            }
        }        
    }

    private static void LogFantasyVersion()
    {
        Log.Info($"\r\n" +
        $"\r\n==========================================================================\r\n \r\n" +
        $"  ███████╗ █████╗ ███╗   ██╗████████╗ █████╗  ██████╗ ██╗   ██╗\r\n" +
        $"  ██╔════╝██╔══██╗████╗  ██║╚══██╔══╝██╔══██╗██╔════╝ ╚██╗ ██╔╝\r\n" +
        $"  █████╗  ███████║██╔██╗ ██║   ██║   ███████║╚█████╗   ╚████╔╝ \r\n" +
        $"  ██╔══╝  ██╔══██║██║╚██╗██║   ██║   ██╔══██║╚════██║   ╚██╔╝  \r\n" +
        $"  ██║     ██║  ██║██║ ╚████║   ██║   ██║  ██║██████╔╝    ██║   \r\n" +
        $"  ╚═╝     ╚═╝  ╚═╝╚═╝  ╚═══╝   ╚═╝   ╚═╝  ╚═╝╚═════╝     ╚═╝   \r\n" +
        $"                                                            \r\n" +
        $" Version : {ProgramDefine.VERSION}\r\n" +
        $"==========================================================================\r\n");
    }

    /// <summary>
    /// 框架初始化
    /// </summary>
    /// <param name="log">日志实例</param>
    /// <returns></returns>
    private static async FTask Initialize(ILog log = null)
    {
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
        Log.Initialize(ProgramDefine.ProcessId.ToString(), log);
        LogFantasyVersion();
        // 注册当前框架内部程序集到框架中
        typeof(Entry).Assembly.EnsureLoaded();
        // 加载Fantasy.config配置文件
        await ConfigLoader.InitializeFromXml(Path.Combine(AppContext.BaseDirectory, "Fantasy.config"));
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
        // 初始化序列化
        await SerializerManager.Initialize();
        // 精度处理（只针对Windows下有作用、其他系统没有这个问题、一般也不会用Windows来做服务器的）
        WinPeriod.Initialize();
    }

    /// <summary>
    /// 关闭 Fantasy
    /// </summary>
    public static async FTask Close()
    {
        foreach (var process in ProcessList)
        {
            await process.Close();
        }
        
        await AssemblyManifest.Dispose();
        SerializerManager.Dispose();
        // 设置当前程序已经在停止中
        ProgramDefine.IsAppRunning = false;
    }
}
#endif