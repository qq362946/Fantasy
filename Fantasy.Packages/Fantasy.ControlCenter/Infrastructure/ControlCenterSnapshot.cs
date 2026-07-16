using Fantasy.ControlCenter.Models;


namespace Fantasy.ControlCenter.Infrastructure;

/// <summary>
/// 一次构建、整体替换的只读缓存。读路径不加锁，也不访问数据库。
/// </summary>
internal sealed class ControlCenterSnapshot
{
    private ControlCenterSnapshot(
        TopologySnapshot topology,
        RuntimeConfigSnapshot runtimeConfig,
        Dictionary<uint, RuntimeConfigSnapshot> runtimeConfigsByProcessId,
        Dictionary<uint, SceneRegistration> enabledScenes)
    {
        Topology = topology;
        RuntimeConfig = runtimeConfig;
        RuntimeConfigsByProcessId = runtimeConfigsByProcessId;
        EnabledScenes = enabledScenes;
    }

    public TopologySnapshot Topology { get; }

    public RuntimeConfigSnapshot RuntimeConfig { get; }

    public Dictionary<uint, RuntimeConfigSnapshot> RuntimeConfigsByProcessId { get; }

    public Dictionary<uint, SceneRegistration> EnabledScenes { get; }

    public static ControlCenterSnapshot Empty { get; } = Build(new TopologySnapshot([], [], [], [], [], [], [], 0));

    public static ControlCenterSnapshot Build(TopologySnapshot topology)
    {
        var runtime = new RuntimeConfigSnapshot
        {
            SchemaVersion = ServiceDiscoveryProtocol.SchemaVersion,
            Revision = topology.Revision,
            Machines = new List<MachineConfigContract>(topology.Machines.Count),
            Processes = new List<ProcessConfigContract>(topology.Processes.Count),
            Worlds = new List<WorldConfigContract>(topology.Worlds.Count),
            Scenes = new List<SceneConfigContract>(topology.Scenes.Count)
        };

        var enabledNamespaceIds = new HashSet<uint>(topology.Namespaces.Count);
        foreach (var definition in topology.Namespaces)
        {
            if (definition.Enabled)
            {
                enabledNamespaceIds.Add(definition.Id);
            }
        }

        var enabledMachineIds = new HashSet<uint>(topology.Machines.Count);
        foreach (var machine in topology.Machines)
        {
            if (!machine.Enabled)
            {
                continue;
            }

            enabledMachineIds.Add(machine.Id);
            runtime.Machines.Add(new MachineConfigContract
            {
                Id = machine.Id,
                Name = machine.Name,
                OuterIp = machine.OuterIp,
                OuterBindIp = machine.OuterBindIp,
                InnerBindIp = machine.InnerBindIp
            });
        }

        var enabledProcessNamespaces = new Dictionary<uint, uint>(topology.Processes.Count);
        foreach (var process in topology.Processes)
        {
            if (!process.Enabled ||
                !enabledNamespaceIds.Contains(process.NamespaceId) ||
                !enabledMachineIds.Contains(process.MachineId))
            {
                continue;
            }

            enabledProcessNamespaces.Add(process.Id, process.NamespaceId);
            runtime.Processes.Add(new ProcessConfigContract
            {
                Id = process.Id,
                NamespaceId = process.NamespaceId,
                MachineId = process.MachineId,
                Name = process.Name,
                StartupGroup = process.StartupGroup
            });
        }

        var enabledWorldGroups = new Dictionary<uint, uint>(topology.WorldGroups.Count);
        foreach (var group in topology.WorldGroups)
        {
            if (group.Enabled && enabledNamespaceIds.Contains(group.NamespaceId))
            {
                enabledWorldGroups.Add(group.Id, group.NamespaceId);
            }
        }

        var enabledWorlds = new Dictionary<uint, WorldConfigContract>(topology.Worlds.Count);
        foreach (var world in topology.Worlds)
        {
            if (!world.Enabled || !enabledWorldGroups.TryGetValue(world.GroupId, out var namespaceId))
            {
                continue;
            }

            var contract = new WorldConfigContract
            {
                Id = world.Id,
                NamespaceId = namespaceId,
                GroupId = world.GroupId,
                WorldName = world.Name,
                Databases = []
            };
            enabledWorlds.Add(world.Id, contract);
            runtime.Worlds.Add(contract);
        }

        foreach (var definition in topology.Databases)
        {
            if (!enabledWorlds.TryGetValue(definition.WorldId, out var world))
            {
                continue;
            }

            world.Databases.Add(new DatabaseConfigContract
            {
                DbType = definition.DbType,
                DbName = definition.DbName,
                DbConnection = definition.DbConnection,
                IsDefault = definition.IsDefault
            });
        }

        var enabledScenes = new Dictionary<uint, SceneRegistration>(topology.Scenes.Count);
        foreach (var scene in topology.Scenes)
        {
            if (!scene.Enabled ||
                !enabledProcessNamespaces.TryGetValue(scene.ProcessId, out var processNamespaceId) ||
                !enabledWorlds.TryGetValue(scene.WorldId, out var world) ||
                processNamespaceId != world.NamespaceId)
            {
                continue;
            }

            enabledScenes.Add(scene.Id, new SceneRegistration(
                scene.Id,
                scene.SceneType,
                world.NamespaceId,
                world.GroupId,
                scene.WorldId,
                scene.ProcessId));
            runtime.Scenes.Add(new SceneConfigContract
            {
                Id = scene.Id,
                ProcessId = scene.ProcessId,
                WorldId = scene.WorldId,
                SceneType = scene.SceneType,
                RuntimeMode = scene.RuntimeMode,
                NetworkProtocol = scene.NetworkProtocol,
                OuterPort = scene.OuterPort,
                InnerPort = scene.InnerPort
            });
        }

        return new ControlCenterSnapshot(
            topology,
            runtime,
            BuildProcessRuntimeConfigs(runtime),
            enabledScenes);
    }

    private static Dictionary<uint, RuntimeConfigSnapshot> BuildProcessRuntimeConfigs(
        RuntimeConfigSnapshot runtime)
    {
        var machinesById = new Dictionary<uint, MachineConfigContract>(runtime.Machines.Count);
        foreach (var machine in runtime.Machines)
        {
            machinesById.Add(machine.Id, machine);
        }

        var worldsById = new Dictionary<uint, WorldConfigContract>(runtime.Worlds.Count);
        foreach (var world in runtime.Worlds)
        {
            worldsById.Add(world.Id, world);
        }

        var scenesByProcessId = new Dictionary<uint, List<SceneConfigContract>>();
        foreach (var scene in runtime.Scenes)
        {
            if (!scenesByProcessId.TryGetValue(scene.ProcessId, out var scenes))
            {
                scenes = [];
                scenesByProcessId.Add(scene.ProcessId, scenes);
            }

            scenes.Add(scene);
        }

        var result = new Dictionary<uint, RuntimeConfigSnapshot>(runtime.Processes.Count);
        foreach (var process in runtime.Processes)
        {
            if (!machinesById.TryGetValue(process.MachineId, out var machine))
            {
                continue;
            }

            if (!scenesByProcessId.TryGetValue(process.Id, out var scenes) || scenes.Count == 0)
            {
                continue;
            }

            var worlds = new List<WorldConfigContract>(scenes.Count);
            var worldIds = new HashSet<uint>(scenes.Count);
            foreach (var scene in scenes)
            {
                if (worldIds.Add(scene.WorldId) && worldsById.TryGetValue(scene.WorldId, out var world))
                {
                    worlds.Add(world);
                }
            }

            result.Add(process.Id, new RuntimeConfigSnapshot
            {
                SchemaVersion = runtime.SchemaVersion,
                Revision = runtime.Revision,
                Machines = [machine],
                Processes = [process],
                Worlds = worlds,
                Scenes = scenes
            });
        }

        return result;
    }
}

internal readonly record struct SceneRegistration(
    uint SceneId,
    string SceneType,
    uint NamespaceId,
    uint WorldGroupId,
    uint WorldId,
    uint ProcessId);
