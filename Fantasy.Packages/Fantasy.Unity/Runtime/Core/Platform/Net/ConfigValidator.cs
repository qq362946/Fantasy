#if FANTASY_NET
using Fantasy.IdFactory;
using Fantasy.Network;

namespace Fantasy.Platform.Net;

/// <summary>
/// Fantasy运行时配置统一校验器。
/// XML配置和Control Center配置必须统一通过此处校验，
/// 避免两套配置加载逻辑分别维护校验规则。
/// </summary>
internal static class ConfigValidator
{
    /// <summary>
    /// 校验完整的服务器拓扑配置。
    /// 该方法只校验，不修改配置，也不会发布到全局ConfigData。
    /// </summary>
    internal static void Validate( List<MachineConfig> machines, List<ProcessConfig> processes, List<WorldConfig> worlds, List<SceneConfig> scenes)
    {
        ArgumentNullException.ThrowIfNull(machines);
        ArgumentNullException.ThrowIfNull(processes);
        ArgumentNullException.ThrowIfNull(worlds);
        ArgumentNullException.ThrowIfNull(scenes);
        
        if (machines.Count == 0)
        {
            throw new InvalidOperationException("Configuration must contain at least one Machine.");
        }

        if (processes.Count == 0)
        {
            throw new InvalidOperationException("Configuration must contain at least one Process.");
        }

        if (worlds.Count == 0)
        {
            throw new InvalidOperationException("Configuration must contain at least one World.");
        }

        if (scenes.Count == 0)
        {
            throw new InvalidOperationException("Configuration must contain at least one Scene.");
        }
        
        var machineIds = new HashSet<uint>(machines.Count);
        var worldIds = new HashSet<uint>(worlds.Count);
        var idFactoryType = IdFactoryHelper.GetIdFactoryType();
        var processMachines = new Dictionary<uint, uint>(processes.Count);
        var processNamespaces = new Dictionary<uint, uint>(processes.Count);
        var worldNamespaces = new Dictionary<uint, uint>(worlds.Count);
        
        ValidateMachines(machines, machineIds);
        ValidateProcesses(processes, machineIds, processMachines, processNamespaces);
        ValidateWorlds(worlds, worldIds, worldNamespaces, idFactoryType);
        ValidateScenes(scenes, processMachines, processNamespaces, worldNamespaces, idFactoryType);
    }
    
    private static void ValidateMachines(List<MachineConfig> machines, HashSet<uint> machineIds)
    {
        for (var index = 0; index < machines.Count; index++)
        {
            var machine = machines[index];

            if (machine == null)
            {
                throw new InvalidOperationException($"Machine at index {index} cannot be null.");
            }

            if (machine.Id == 0)
            {
                throw new InvalidOperationException("Machine ID cannot be 0.");
            }

            if (!machineIds.Add(machine.Id))
            {
                throw new InvalidOperationException($"Duplicate Machine ID: {machine.Id}.");
            }

            if (string.IsNullOrWhiteSpace(machine.OuterIP))
            {
                throw new InvalidOperationException($"Machine {machine.Id}: OuterIP cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(machine.OuterBindIP))
            {
                throw new InvalidOperationException($"Machine {machine.Id}: OuterBindIP cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(machine.InnerBindIP))
            {
                throw new InvalidOperationException($"Machine {machine.Id}: InnerBindIP cannot be empty.");
            }
        }
    }
    
    private static void ValidateProcesses(
        List<ProcessConfig> processes,
        HashSet<uint> machineIds,
        Dictionary<uint, uint> processMachines,
        Dictionary<uint, uint> processNamespaces)
    {
        for (var index = 0; index < processes.Count; index++)
        {
            var process = processes[index];

            if (process == null)
            {
                throw new InvalidOperationException($"Process at index {index} cannot be null.");
            }

            if (process.Id == 0)
            {
                throw new InvalidOperationException("Process ID cannot be 0.");
            }

            if (process.MachineId == 0)
            {
                throw new InvalidOperationException($"Process {process.Id}: MachineId cannot be 0.");
            }

            if (!machineIds.Contains(process.MachineId))
            {
                throw new InvalidOperationException(
                    $"Process {process.Id}: Machine " +
                    $"{process.MachineId} does not exist.");
            }

            // TryAdd同时完成Process ID去重和Process到Machine的映射。
            if (!processMachines.TryAdd( process.Id, process.MachineId))
            {
                throw new InvalidOperationException($"Duplicate Process ID: {process.Id}.");
            }

            processNamespaces.Add(process.Id, process.NamespaceId);
        }
    }
    
    private static void ValidateWorlds(
        List<WorldConfig> worlds,
        HashSet<uint> worldIds,
        Dictionary<uint, uint> worldNamespaces,
        IdFactoryType idFactoryType)
    {
        for (var index = 0; index < worlds.Count; index++)
        {
            var world = worlds[index];

            if (world == null)
            {
                throw new InvalidOperationException($"World at index {index} cannot be null.");
            }

            if (world.Id == 0)
            {
                throw new InvalidOperationException("World ID cannot be 0.");
            }

            if (!worldIds.Add(world.Id))
            {
                throw new InvalidOperationException($"Duplicate World ID: {world.Id}.");
            }

            if ((world.NamespaceId == 0) != (world.GroupId == 0))
            {
                throw new InvalidOperationException(
                    $"World {world.Id}: NamespaceId and GroupId must either both be 0 or both be greater than 0.");
            }

            worldNamespaces.Add(world.Id, world.NamespaceId);

            if (string.IsNullOrWhiteSpace(world.WorldName))
            {
                throw new InvalidOperationException($"World {world.Id}: WorldName cannot be empty.");
            }

            if (idFactoryType == IdFactoryType.World && world.Id > byte.MaxValue)
            {
                throw new InvalidOperationException(
                    $"World {world.Id}: World ID must be less than or " +
                    $"equal to {byte.MaxValue} when IdFactoryType is World.");
            }

            ValidateDatabases(world);
        }
    }
    
    private static void ValidateDatabases(WorldConfig world)
    {
        var databases = world.DatabaseConfig;

        if (databases == null || databases.Length == 0)
        {
            return;
        }

        // 单数据库不存在名称冲突，不需要分配HashSet。
        HashSet<string>? databaseNames = null;

        if (databases.Length > 1)
        {
            databaseNames = new HashSet<string>(databases.Length, StringComparer.Ordinal);
        }

        for (var index = 0; index < databases.Length; index++)
        {
            var database = databases[index];

            if (database == null)
            {
                throw new InvalidOperationException(
                    $"World {world.Id}: Database at index {index} " +
                    "cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(database.DbName))
            {
                throw new InvalidOperationException($"World {world.Id}: DbName cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(database.DbType))
            {
                throw new InvalidOperationException($"World {world.Id}: DbType cannot be empty.");
            }

            if (databaseNames != null && !databaseNames.Add(database.DbName))
            {
                throw new InvalidOperationException(
                    $"World {world.Id}: Duplicate database name " +
                    $"'{database.DbName}'.");
            }
        }
    }
    
    private static void ValidateScenes(
        List<SceneConfig> scenes,
        Dictionary<uint, uint> processMachines,
        Dictionary<uint, uint> processNamespaces,
        Dictionary<uint, uint> worldNamespaces,
        IdFactoryType idFactoryType)
    {
        var sceneIds = new HashSet<uint>(scenes.Count);

        // 一个Scene最多占用InnerPort和OuterPort两个端口。
        // key高32位是MachineId，低32位是Port。
        var occupiedPorts = new Dictionary<ulong, uint>(scenes.Count * 2);

        for (var index = 0; index < scenes.Count; index++)
        {
            var scene = scenes[index];

            if (scene == null)
            {
                throw new InvalidOperationException($"Scene at index {index} cannot be null.");
            }

            if (scene.Id == 0)
            {
                throw new InvalidOperationException("Scene ID cannot be 0.");
            }

            if (!sceneIds.Add(scene.Id))
            {
                throw new InvalidOperationException($"Duplicate Scene ID: {scene.Id}.");
            }

            if (!processMachines.TryGetValue( scene.ProcessConfigId, out var machineId))
            {
                throw new InvalidOperationException(
                    $"Scene {scene.Id}: Process " +
                    $"{scene.ProcessConfigId} does not exist.");
            }

            if (!worldNamespaces.TryGetValue(scene.WorldConfigId, out var worldNamespaceId))
            {
                throw new InvalidOperationException(
                    $"Scene {scene.Id}: World " +
                    $"{scene.WorldConfigId} does not exist.");
            }

            var processNamespaceId = processNamespaces[scene.ProcessConfigId];
            if (processNamespaceId != worldNamespaceId)
            {
                throw new InvalidOperationException(
                    $"Scene {scene.Id}: Process {scene.ProcessConfigId} and World " +
                    $"{scene.WorldConfigId} must belong to the same Namespace.");
            }

            if (!IsValidSceneRuntimeMode(scene.SceneRuntimeMode))
            {
                throw new InvalidOperationException(
                    $"Scene {scene.Id}: Invalid SceneRuntimeMode " +
                    $"'{scene.SceneRuntimeMode}'.");
            }

            if (string.IsNullOrWhiteSpace(scene.SceneTypeString))
            {
                throw new InvalidOperationException($"Scene {scene.Id}: SceneTypeString cannot be empty.");
            }

            if (!Scene.SceneTypeDictionary.ContainsKey(
                    scene.SceneTypeString))
            {
                throw new InvalidOperationException(
                    $"Scene {scene.Id}: SceneType " +
                    $"'{scene.SceneTypeString}' is not defined.");
            }

            ValidateScenePorts(scene);
            ValidateSceneIdRange(scene, idFactoryType);

            if (scene.OuterPort > 0 && scene.InnerPort == scene.OuterPort)
            {
                throw new InvalidOperationException(
                    $"Scene {scene.Id}: InnerPort and OuterPort " +
                    $"cannot both use port {scene.InnerPort}.");
            }

            AddOccupiedPort(occupiedPorts, machineId, scene.InnerPort, scene.Id);

            if (scene.OuterPort > 0)
            {
                AddOccupiedPort( occupiedPorts, machineId, scene.OuterPort, scene.Id);
            }
        }
    }
    
    private static void ValidateScenePorts(SceneConfig scene)
    {
        if (scene.InnerPort <= 0 || scene.InnerPort > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"Scene {scene.Id}: InnerPort must be between " +
                $"1 and {ushort.MaxValue}.");
        }

        if (scene.OuterPort < 0 || scene.OuterPort > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"Scene {scene.Id}: OuterPort must be between " +
                $"0 and {ushort.MaxValue}.");
        }

        var networkProtocol = scene.NetworkProtocol;

        if (!string.IsNullOrEmpty(networkProtocol) && !IsValidNetworkProtocol(networkProtocol))
        {
            throw new InvalidOperationException(
                $"Scene {scene.Id}: Invalid NetworkProtocol " +
                $"'{networkProtocol}'.");
        }

        if (scene.OuterPort > 0 && string.IsNullOrEmpty(networkProtocol))
        {
            throw new InvalidOperationException(
                $"Scene {scene.Id}: NetworkProtocol is required " +
                "when OuterPort is greater than 0.");
        }
    }
    
    private static bool IsValidSceneRuntimeMode(string runtimeMode)
    {
        return runtimeMode == SceneRuntimeMode.MainThread ||
               runtimeMode == SceneRuntimeMode.MultiThread ||
               runtimeMode == SceneRuntimeMode.ThreadPool;
    }

    private static bool IsValidNetworkProtocol(string networkProtocol)
    {
        return networkProtocol == nameof(NetworkProtocolType.TCP) ||
               networkProtocol == nameof(NetworkProtocolType.KCP) ||
               networkProtocol == nameof(NetworkProtocolType.WebSocket) ||
               networkProtocol == nameof(NetworkProtocolType.HTTP);
    }
    
    private static void ValidateSceneIdRange( SceneConfig scene, IdFactoryType idFactoryType)
    {
        if (idFactoryType != IdFactoryType.World)
        {
            return;
        }

        // 先转换成ulong，避免WorldConfigId * 1000发生uint溢出。
        var minimum = (ulong)scene.WorldConfigId * 1000 + 1;
        var maximum = (ulong)scene.WorldConfigId * 1000 + 255;

        if (scene.Id < minimum || scene.Id > maximum)
        {
            throw new InvalidOperationException(
                $"Scene {scene.Id}: ID must be between " +
                $"{minimum} and {maximum} for World " +
                $"{scene.WorldConfigId}.");
        }
    }
    
    private static void AddOccupiedPort( Dictionary<ulong, uint> occupiedPorts, uint machineId, int port, uint sceneId)
    {
        var key = ((ulong)machineId << 32) | (uint)port;

        if (occupiedPorts.TryGetValue( key, out var conflictSceneId))
        {
            throw new InvalidOperationException(
                $"Scene {sceneId}: Port {port} conflicts with " +
                $"Scene {conflictSceneId} on Machine {machineId}.");
        }

        occupiedPorts.Add(key, sceneId);
    }
}
#endif
