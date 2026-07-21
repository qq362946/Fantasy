#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using CommandLine;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Serialize;
#if FANTASY_WEBGL || UNITY_WEBGL
using FCloseTask = Fantasy.Async.FTask;
#else
using FCloseTask = Fantasy.Async.FThreadTask;
#endif
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
    private static bool _isClosed;
    
    private static readonly SemaphoreSlim CloseSemaphore = new(1, 1);
    private static readonly List<Process> ProcessList = new List<Process>();

    /// <summary>
    /// 应用是否正在关闭。
    /// 用于阻止关闭期间产生新的服务注册。
    /// </summary>
    private static bool _isClosing;
    
    /// <summary>
    /// Worker 启动前已经创建完成的 SubScene。
    /// Root Scene 会由 Worker 根据进程配置直接收集，不需要放入这里。
    /// </summary>
    private static readonly ConcurrentDictionary<long, Scene> PendingServiceScenes = new();
    
    /// <summary>
    /// 当前OS进程的服务注册和批量心跳执行器。
    /// 未启用Control Center时为null。
    /// </summary>
    private static ServiceDiscoveryWorker? _serviceDiscoveryWorker;
    
    /// <summary>
    /// 启动Fantasy.Net
    /// </summary>
    public static async FTask Start(ILog log = null)
    {
        // 初始化
        await Initialize(log);

        try
        {
            // 启动Process
            var startProcessTask = StartProcess();
            
            while (!startProcessTask.IsCompleted)
            {
                ThreadScheduler.Update();
                Thread.Sleep(1);
            }
            
            await startProcessTask;
            
            if (ProcessList.Count == 0)
            {
                throw new InvalidOperationException("No Process was started.");
            }
            
            // 所有Process和Scene启动成功后，
            // 才能向Control Center注册服务实例。
            await StartServiceDiscovery();
        }
        catch
        {
            var closeTask = Close();

            // Close 内部的程序集卸载也可能投递到主线程，
            // 因此回滚期间必须继续推进主线程调度器。
            while (!closeTask.IsCompleted)
            {
                ThreadScheduler.Update();
                Thread.Sleep(1);
            }

            await closeTask;
            throw;
        }
        
        // 设置当前程序已经在运行中
        ProgramDefine.IsAppRunning = true;
        
        while (!Volatile.Read(ref _isClosed))
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

    /// <summary>
    /// 启动当前 OS 进程唯一的服务注册和批量心跳执行器。
    /// </summary>
    private static async FTask StartServiceDiscovery()
    {
        var controlCenter = ConfigLoader.ControlCenter;

        if (controlCenter == null || ProcessList.Count == 0)
        {
            return;
        }

        var namespaceId = ProcessList[0].NamespaceId;
        if (namespaceId == 0)
        {
            throw new InvalidOperationException("Control Center模式下Process必须配置Namespace ID。");
        }

        for (var i = 1; i < ProcessList.Count; i++)
        {
            if (ProcessList[i].NamespaceId != namespaceId)
            {
                throw new InvalidOperationException("同一个OS进程中启动的Process必须属于同一个Namespace。");
            }
        }

        ServiceDiscovery.Initialize(
            controlCenter,
            ProgramDefine.ControlCenterHeartbeatIntervalSeconds,
            namespaceId);

        var worker = new ServiceDiscoveryWorker(
            controlCenter,
            ProcessList,
            ProgramDefine.ControlCenterHeartbeatIntervalSeconds,
            ProgramDefine.ControlCenterLeaseSeconds);

        // 必须先发布 Worker。
        // 发布后新创建的 Scene 可以直接加入 Worker，不会再进入待注册集合。
        Volatile.Write(ref _serviceDiscoveryWorker, worker);

        // 处理 Root Scene 初始化期间创建的 SubScene。
        // Worker 尚未 Start，因此这里只构建一次最终快照，不发送重复请求。
        await DrainPendingServiceScenesAsync(worker);

        worker.Start();
    }
    
    /// <summary>
    /// Scene 创建成功后通知服务发现。
    /// 未启用 Control Center 时直接返回。
    /// </summary>
    internal static async FTask RegisterServiceSceneAsync(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (scene.IsDisposed || Volatile.Read(ref _isClosing) || Volatile.Read(ref _isClosed) || ConfigLoader.ControlCenter == null)
        {
            return;
        }

        var worker = Volatile.Read(ref _serviceDiscoveryWorker);

        if (worker != null)
        {
            await worker.RegisterSceneAsync(scene);
            return;
        }

        // Root Scene 会由 Worker 根据配置生成初始注册信息。
        // 这里只暂存 Worker 启动前动态创建的 SubScene。
        if (scene.SceneRuntimeType != SceneRuntimeType.SubScene)
        {
            return;
        }

        var address = scene.Address;
        PendingServiceScenes[address] = scene;

        // 防止添加 Pending 和发布 Worker 恰好同时发生。
        worker = Volatile.Read(ref _serviceDiscoveryWorker);

        if (worker == null || !IsPendingServiceScene(address, scene))
        {
            return;
        }

        await worker.RegisterSceneAsync(scene);

        // 如果注册期间 Scene 已经关闭，关闭流程会先删除 Pending。
        // 此时需要再次通知 Worker 下线。
        if (!TryRemovePendingServiceScene(address, scene))
        {
            await worker.UnregisterSceneAsync(scene);
        }
    }
    
    /// <summary>
    /// Scene 开始销毁时通知服务发现下线。
    /// </summary>
    internal static async FTask UnregisterServiceSceneAsync(Scene scene, bool throwOnFailure = false)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var address = scene.Address;
        if (address == 0)
        {
            return;
        }

        TryRemovePendingServiceScene(address, scene);

        var worker = Volatile.Read(ref _serviceDiscoveryWorker);

        if (worker != null)
        {
            await worker.UnregisterSceneAsync(scene, throwOnFailure);
            return;
        }

        if (throwOnFailure)
        {
            throw new InvalidOperationException("Service discovery worker is not running.");
        }
    }
    
    /// <summary>
    /// 将 Worker 启动前创建的 SubScene 转移到 Worker。
    /// </summary>
    private static async FTask DrainPendingServiceScenesAsync(ServiceDiscoveryWorker worker)
    {
        foreach (var pair in PendingServiceScenes)
        {
            var address = pair.Key;
            var scene = pair.Value;

            if (!IsPendingServiceScene(address, scene))
            {
                continue;
            }

            if (scene.IsDisposed)
            {
                TryRemovePendingServiceScene(address, scene);
                continue;
            }

            await worker.RegisterSceneAsync(scene);

            // 注册期间被关闭时，补发一次下线。
            if (!TryRemovePendingServiceScene(address, scene))
            {
                await worker.UnregisterSceneAsync(scene);
            }
        }
    }
    
    /// <summary>
    /// 判断指定 Pending 项是否仍然是当前 Scene。
    /// </summary>
    private static bool IsPendingServiceScene(long address, Scene scene)
    {
        return PendingServiceScenes.TryGetValue( address, out var current) && ReferenceEquals(current, scene);
    }

    /// <summary>
    /// 仅在 Key 和 Scene 引用都匹配时删除 Pending 项。
    /// </summary>
    private static bool TryRemovePendingServiceScene(long address, Scene scene)
    {
        return ((ICollection<KeyValuePair<long, Scene>>) PendingServiceScenes)
            .Remove(new KeyValuePair<long, Scene>(address, scene));
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
    public static async FCloseTask Close()
    {
        await CloseSemaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_isClosed)
            {
                return;
            }
            
            Volatile.Write(ref _isClosing, true);
            
            var serviceDiscoveryWorker = Interlocked.Exchange(ref _serviceDiscoveryWorker, null);

            PendingServiceScenes.Clear();
            
            if (serviceDiscoveryWorker != null)
            {
                await serviceDiscoveryWorker.StopAsync();
            }
            
            // Scene关闭前停止对外提供服务发现结果。
            ServiceDiscovery.Reset();
            
            foreach (var process in ProcessList)
            {
                await process.Close();
            }
            
            ProcessList.Clear();
            
            await AssemblyManifest.Dispose().ConfigureAwait(false);
            SerializerManager.Dispose();
            
            ThreadScheduler.DisposeBackgroundSchedulers();
            
            // 设置当前程序已经在停止中
            ProgramDefine.IsAppRunning = false;
            Volatile.Write(ref _isClosed, true);
        }
        finally
        {
            CloseSemaphore.Release();
        }
    }
}
#endif
