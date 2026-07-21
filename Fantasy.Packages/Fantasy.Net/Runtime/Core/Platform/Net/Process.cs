#if FANTASY_NET
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fantasy.Async;
using Fantasy.IdFactory;
#if FANTASY_WEBGL || UNITY_WEBGL
using FCloseTask = Fantasy.Async.FTask;
#else
using FCloseTask = Fantasy.Async.FThreadTask;
#endif
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
namespace Fantasy.Platform.Net;

/// <summary>
/// 一个进程的实例
/// </summary>
public sealed class Process
{
    /// <summary>
    /// 当前进程的Id
    /// </summary>
    public readonly uint Id;
    /// <summary>
    /// 进程所属的Namespace ID
    /// </summary>
    public readonly uint NamespaceId;
    /// <summary>
    /// 进程关联的MachineId
    /// </summary>
    public readonly uint MachineId;
    private readonly ConcurrentDictionary<uint, Scene> _processScenes = new ConcurrentDictionary<uint, Scene>();
    private static readonly ConcurrentDictionary<uint, Scene> Scenes = new ConcurrentDictionary<uint, Scene>();
    private Process() {}
    private Process(uint id, uint namespaceId, uint machineId)
    {
        Id = id;
        NamespaceId = namespaceId;
        MachineId = machineId;
    }
    
    internal bool IsProcess(ref long address)
    {
        var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(ref address);
        return _processScenes.ContainsKey(sceneId);
    }
    
    internal bool IsProcess(ref uint sceneId)
    {
        return _processScenes.ContainsKey(sceneId);
    }
    
    internal void AddSceneToProcess(Scene scene)
    {
        if (!_processScenes.TryAdd(scene.SceneConfigId, scene))
        {
            throw new InvalidOperationException(
                $"Duplicate Scene ID: {scene.SceneConfigId}.");
        }
    }

    internal void RemoveSceneToProcess(Scene scene)
    {
        _processScenes.TryRemove(new KeyValuePair<uint, Scene>(scene.SceneConfigId, scene));
    }
    
    internal bool TryGetSceneToProcess(long address, out Scene scene)
    {
        var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(ref address);
        return _processScenes.TryGetValue(sceneId, out scene);
    }

    internal bool TryGetSceneToProcess(uint sceneId, out Scene scene)
    {
        return _processScenes.TryGetValue(sceneId, out scene);
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public async FCloseTask Close()
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
            await removeScene.Close();
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

        var process = new Process(processConfigId, processConfig.NamespaceId, processConfig.MachineId);
        var sceneConfigs = SceneConfigData.Instance.GetByProcess(processConfigId);

        try
        {
            foreach (var sceneConfig in sceneConfigs)
            {
                await Scene.Create(process, machineConfig, sceneConfig);
            }
        }
        catch
        {
            await process.Close();
            throw;
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
        if (!Scenes.TryAdd(scene.SceneConfigId, scene))
        {
            throw new InvalidOperationException(
                $"Duplicate Scene ID: {scene.SceneConfigId}.");
        }
    }

    internal static void RemoveScene(Scene scene, bool isDispose)
    {
        if (!Scenes.TryRemove(new KeyValuePair<uint, Scene>(scene.SceneConfigId, scene)))
        {
            return;
        }

        if (isDispose)
        {
            scene.Dispose();
        }
    }
    
    internal static bool TryGetScene(long address, out Scene scene)
    {
        var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(ref address);
        return Scenes.TryGetValue(sceneId, out scene);
    }

    internal static bool TryGetScene(uint sceneId, out Scene scene)
    {
        return Scenes.TryGetValue(sceneId, out scene);
    }
}
#endif
