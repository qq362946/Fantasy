#if FANTASY_NET
using System.Collections.Concurrent;
using Fantasy.Async;
using Fantasy.IdFactory;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
namespace Fantasy.Platform.Net;

/// <summary>
/// 一个进程的实例
/// </summary>
public sealed class Process : IDisposable
{
    /// <summary>
    /// 当前进程的Id
    /// </summary>
    public readonly uint Id;
    /// <summary>
    /// 进程关联的MachineId
    /// </summary>
    public readonly uint MachineId;
    private readonly ConcurrentDictionary<uint, Scene> _processScenes = new ConcurrentDictionary<uint, Scene>();
    private static readonly ConcurrentDictionary<uint, Scene> Scenes = new ConcurrentDictionary<uint, Scene>();
    private Process() {}
    private Process(uint id, uint machineId)
    {
        Id = id;
        MachineId = machineId;
    }
    
    internal bool IsProcess(ref long routeId)
    {
        var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(ref routeId);
        return _processScenes.ContainsKey(sceneId);
    }
    
    internal bool IsProcess(ref uint sceneId)
    {
        return _processScenes.ContainsKey(sceneId);
    }
    
    internal void AddSceneToProcess(Scene scene)
    {
        _processScenes.TryAdd(scene.SceneConfigId, scene);
    }

    internal void RemoveSceneToProcess(Scene scene, bool isDispose)
    {
        _processScenes.Remove(scene.SceneConfigId, out _);

        if (isDispose)
        {
            scene.Dispose();
        }
    }
    
    internal bool TryGetSceneToProcess(long routeId, out Scene scene)
    {
        var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(ref routeId);
        return _processScenes.TryGetValue(sceneId, out scene);
    }

    internal bool TryGetSceneToProcess(uint sceneId, out Scene scene)
    {
        return _processScenes.TryGetValue(sceneId, out scene);
    }
    /// <summary>
    /// 销毁方法
    /// </summary>
    public void Dispose()
    {
        if (_processScenes.IsEmpty)
        {
            return;
        }
        
        var sceneQueue = new Queue<Scene>();
            
        foreach (var (_, scene) in _processScenes)
        {
            sceneQueue.Enqueue(scene);
        }

        while (sceneQueue.TryDequeue(out var removeScene))
        {
            removeScene.Dispose();
        }

        _processScenes.Clear();
    }

    internal static async FTask<Process?> Create(uint processConfigId)
    {
        if (!ProcessConfigData.Instance.TryGet(processConfigId, out var processConfig))
        {
            Log.Error($"not found processConfig by Id:{processConfigId}");
            return null;
        }

        if (!MachineConfigData.Instance.TryGet(processConfig.MachineId, out var machineConfig))
        {
            Log.Error($"not found machineConfig by Id:{processConfig.MachineId}");
            return null;
        }

        var process = new Process(processConfigId, processConfig.MachineId);
        var sceneConfigs = SceneConfigData.Instance.GetByProcess(processConfigId);

        foreach (var sceneConfig in sceneConfigs)
        {
            await Scene.Create(process, machineConfig, sceneConfig);
        }

        Log.Info($"Process:{processConfigId} Startup Complete SceneCount:{sceneConfigs.Count}");
        return process;
    }
    
    internal bool IsInAppliaction(ref uint sceneId)
    {
        return _processScenes.ContainsKey(sceneId);
    }

    internal static void AddScene(Scene scene)
    {
        Scenes.TryAdd(scene.SceneConfigId, scene);
    }

    internal static void RemoveScene(Scene scene, bool isDispose)
    {
        Scenes.Remove(scene.SceneConfigId, out _);

        if (isDispose)
        {
            scene.Dispose();
        }
    }
    
    internal static bool TryGetScene(long routeId, out Scene scene)
    {
        var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(ref routeId);
        return Scenes.TryGetValue(sceneId, out scene);
    }

    internal static bool TryGetScene(uint sceneId, out Scene scene)
    {
        return Scenes.TryGetValue(sceneId, out scene);
    }
}
#endif
