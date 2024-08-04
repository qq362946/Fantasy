#if FANTASY_NET
using System.Collections.Concurrent;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
namespace Fantasy;

public sealed class Process : IDisposable
{
    public readonly uint Id;
    public readonly uint MachineId;
    private readonly ConcurrentBag<uint> _processScenes = new ConcurrentBag<uint>();
    private readonly ConcurrentDictionary<uint, Scene> _scenes = new ConcurrentDictionary<uint, Scene>();
    private Process() {}
    private Process(uint id, uint machineId)
    {
        Id = id;
        MachineId = machineId;
    }

    public void Dispose()
    {
        if (!_scenes.IsEmpty)
        {
            var sceneQueue = new Queue<Scene>();

            foreach (var (_, scene) in _scenes)
            {
                sceneQueue.Enqueue(scene);
            }

            while (sceneQueue.TryDequeue(out var removeScene))
            {
                removeScene.Dispose();
            }

            _processScenes.Clear();
            _scenes.Clear();
        }
    }

    public static async FTask<Process?> Create(uint processConfigId)
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
            process._processScenes.Add(sceneConfig.Id);
        }

        Log.Info($"Process:{processConfigId} Startup Complete SceneCount:{sceneConfigs.Count}");
        return process;
    }

    public void AddScene(Scene scene)
    {
        _scenes.TryAdd(scene.SceneConfigId, scene);
    }

    public void RemoveScene(Scene scene, bool isDispose)
    {
        _scenes.Remove(scene.SceneConfigId, out _);

        if (isDispose)
        {
            scene.Dispose();
        }
    }

    public bool TryGetScene(uint sceneId, out Scene scene)
    {
        return _scenes.TryGetValue(sceneId, out scene);
    }

    public bool IsProcess(ref uint sceneId)
    {
        return _processScenes.Contains(sceneId);
    }
}
#endif
