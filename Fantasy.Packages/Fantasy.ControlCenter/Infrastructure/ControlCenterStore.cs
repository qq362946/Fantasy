using Fantasy.ControlCenter.Models;

namespace Fantasy.ControlCenter.Infrastructure;

/// <summary>
/// 控制中心门面：写配置时落库并刷新快照，所有读请求直接访问内存。
/// </summary>
public sealed class ControlCenterStore(
    ControlCenterDatabase database,
    ControlCenterRepository repository,
    ServiceRegistry registry)
{
    private readonly SemaphoreSlim _mutationLock = new(1, 1);
    private ControlCenterSnapshot _snapshot = ControlCenterSnapshot.Empty;

    public string DatabasePath => database.DatabasePath;

    private ControlCenterSnapshot Snapshot => Volatile.Read(ref _snapshot);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await repository.InitializeAsync(cancellationToken);
        await ReloadSnapshotAsync(cancellationToken);
    }

    public Task<IReadOnlyList<NamespaceDefinition>> GetNamespacesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot.Topology.Namespaces);

    public async Task SaveNamespaceAsync(
        NamespaceDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(definition.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Name);

        definition.Name = definition.Name.Trim();
        await MutateAsync(
            token => repository.SaveNamespaceAsync(definition, token),
            (topology, revision) => topology with
            {
                Namespaces = Upsert(
                    topology.Namespaces,
                    definition,
                    static (left, right) => left.Id == right.Id,
                    static (left, right) => left.Id.CompareTo(right.Id)),
                Revision = revision
            },
            cancellationToken);
    }

    public Task DeleteNamespaceAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(
            token =>
            {
                EnsureNamespaceUnused(id, Snapshot.Topology);
                return repository.DeleteNamespaceAsync(id, token);
            },
            (topology, revision) => topology with
            {
                Namespaces = Remove(
                    topology.Namespaces,
                    id,
                    static (item, key) => item.Id == key),
                Revision = revision
            },
            cancellationToken);

    public Task<IReadOnlyList<MachineDefinition>> GetMachinesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot.Topology.Machines);

    public async Task SaveMachineAsync(MachineDefinition machine, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(machine.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(machine.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(machine.OuterIp);
        ArgumentException.ThrowIfNullOrWhiteSpace(machine.OuterBindIp);
        ArgumentException.ThrowIfNullOrWhiteSpace(machine.InnerBindIp);

        machine.Name = machine.Name.Trim();
        machine.OuterIp = machine.OuterIp.Trim();
        machine.OuterBindIp = machine.OuterBindIp.Trim();
        machine.InnerBindIp = machine.InnerBindIp.Trim();
        await MutateAsync(
            token => repository.SaveMachineAsync(machine, token),
            (topology, revision) => topology with
            {
                Machines = Upsert(
                    topology.Machines,
                    machine,
                    static (left, right) => left.Id == right.Id,
                    static (left, right) => left.Id.CompareTo(right.Id)),
                Revision = revision
            },
            cancellationToken);
    }

    public Task DeleteMachineAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(
            token => repository.DeleteMachineAsync(id, token),
            (topology, revision) => topology with
            {
                Machines = Remove(topology.Machines, id, static (item, key) => item.Id == key),
                Revision = revision
            },
            cancellationToken);

    public Task<IReadOnlyList<ProcessDefinition>> GetProcessesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot.Topology.Processes);

    public async Task SaveProcessAsync(ProcessDefinition process, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(process.Id);
        ArgumentOutOfRangeException.ThrowIfZero(process.NamespaceId);
        ArgumentOutOfRangeException.ThrowIfZero(process.MachineId);
        ArgumentException.ThrowIfNullOrWhiteSpace(process.Name);

        process.Name = process.Name.Trim();
        await MutateAsync(
            token =>
            {
                var topology = Snapshot.Topology;
                EnsureNamespaceExists(process.NamespaceId, topology);
                ValidateProcessNamespace(process, topology);
                return repository.SaveProcessAsync(process, token);
            },
            (topology, revision) => topology with
            {
                Processes = Upsert(
                    topology.Processes,
                    process,
                    static (left, right) => left.Id == right.Id,
                    static (left, right) => left.Id.CompareTo(right.Id)),
                Revision = revision
            },
            cancellationToken);
    }

    public Task DeleteProcessAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(
            token => repository.DeleteProcessAsync(id, token),
            (topology, revision) => topology with
            {
                Processes = Remove(topology.Processes, id, static (item, key) => item.Id == key),
                Revision = revision
            },
            cancellationToken);

    public Task<IReadOnlyList<WorldGroupDefinition>> GetWorldGroupsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot.Topology.WorldGroups);

    public async Task SaveWorldGroupAsync(
        WorldGroupDefinition group,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(group.Id);
        ArgumentOutOfRangeException.ThrowIfZero(group.NamespaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(group.Name);

        group.Name = group.Name.Trim();
        await MutateAsync(
            token =>
            {
                var topology = Snapshot.Topology;
                EnsureNamespaceExists(group.NamespaceId, topology);
                ValidateWorldGroupNamespace(group, topology);
                return repository.SaveWorldGroupAsync(group, token);
            },
            (topology, revision) => topology with
            {
                WorldGroups = Upsert(
                    topology.WorldGroups,
                    group,
                    static (left, right) => left.Id == right.Id,
                    static (left, right) => left.Id.CompareTo(right.Id)),
                Revision = revision
            },
            cancellationToken);
    }

    public Task DeleteWorldGroupAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(
            token =>
            {
                EnsureWorldGroupUnused(id, Snapshot.Topology);
                return repository.DeleteWorldGroupAsync(id, token);
            },
            (topology, revision) => topology with
            {
                WorldGroups = Remove(
                    topology.WorldGroups,
                    id,
                    static (item, key) => item.Id == key),
                Revision = revision
            },
            cancellationToken);

    public Task<IReadOnlyList<WorldDefinition>> GetWorldsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot.Topology.Worlds);

    public async Task SaveWorldAsync(WorldDefinition world, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(world.Id);
        ArgumentOutOfRangeException.ThrowIfZero(world.GroupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(world.Name);

        world.Name = world.Name.Trim();
        await MutateAsync(
            token =>
            {
                var topology = Snapshot.Topology;
                EnsureWorldGroupExists(world.GroupId, topology);
                ValidateWorldNamespace(world, topology);
                return repository.SaveWorldAsync(world, token);
            },
            (topology, revision) => topology with
            {
                Worlds = Upsert(
                    topology.Worlds,
                    world,
                    static (left, right) => left.Id == right.Id,
                    static (left, right) => left.Id.CompareTo(right.Id)),
                Revision = revision
            },
            cancellationToken);
    }

    public Task DeleteWorldAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(
            token => repository.DeleteWorldAsync(id, token),
            (topology, revision) => topology with
            {
                Worlds = Remove(topology.Worlds, id, static (item, key) => item.Id == key),
                Databases = Remove(topology.Databases, id, static (item, key) => item.WorldId == key),
                Revision = revision
            },
            cancellationToken);

    public Task<IReadOnlyList<DatabaseDefinition>> GetDatabasesAsync(
        uint? worldId = null,
        CancellationToken cancellationToken = default)
    {
        var databases = Snapshot.Topology.Databases;
        if (!worldId.HasValue)
        {
            return Task.FromResult(databases);
        }

        var result = new List<DatabaseDefinition>();
        foreach (var definition in databases)
        {
            if (definition.WorldId == worldId.Value)
            {
                result.Add(definition);
            }
        }

        return Task.FromResult<IReadOnlyList<DatabaseDefinition>>(result);
    }

    public async Task<long> SaveDatabaseAsync(
        DatabaseDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(definition.WorldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.DbName);

        definition.DbType = NormalizeDatabaseType(definition.DbType);
        definition.DbName = definition.DbName.Trim();
        definition.DbConnection ??= string.Empty;
        return await MutateAsync(
            async token =>
            {
                var result = await repository.SaveDatabaseAsync(definition, token);
                return (result.Id, result.Revision);
            },
            (topology, id, revision) => topology with
            {
                Databases = UpsertDatabase(topology.Databases, definition),
                Revision = revision
            },
            cancellationToken);
    }

    public Task DeleteDatabaseAsync(
        long id,
        uint? worldId = null,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            token => repository.DeleteDatabaseAsync(id, worldId, token),
            (topology, revision) => topology with
            {
                Databases = Remove(
                    topology.Databases,
                    id,
                    (item, key) => item.Id == key && (!worldId.HasValue || item.WorldId == worldId.Value)),
                Revision = revision
            },
            cancellationToken);

    public Task<IReadOnlyList<SceneDefinition>> GetScenesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot.Topology.Scenes);

    public async Task SaveSceneAsync(SceneDefinition scene, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(scene.Id);
        ArgumentOutOfRangeException.ThrowIfZero(scene.ProcessId);
        ArgumentOutOfRangeException.ThrowIfZero(scene.WorldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scene.SceneType);
        ArgumentException.ThrowIfNullOrWhiteSpace(scene.RuntimeMode);

        if (scene.InnerPort is < 1 or > 65535 || scene.OuterPort is < 0 or > 65535)
        {
            throw new InvalidOperationException("端口必须在有效范围内。");
        }

        if (scene.OuterPort > 0 && string.IsNullOrWhiteSpace(scene.NetworkProtocol))
        {
            throw new InvalidOperationException("配置外网端口时必须指定网络协议。");
        }

        if (scene.OuterPort > 0 && scene.OuterPort == scene.InnerPort)
        {
            throw new InvalidOperationException("同一个 Scene 的内外网端口不能相同。");
        }

        scene.SceneType = scene.SceneType.Trim();
        scene.RuntimeMode = scene.RuntimeMode.Trim();
        scene.NetworkProtocol = scene.NetworkProtocol.Trim();

        await MutateAsync(async token =>
        {
            ValidateSceneReferencesAndPorts(scene, Snapshot.Topology);
            return await repository.SaveSceneAsync(scene, token);
        }, (topology, revision) => topology with
        {
            Scenes = Upsert(
                topology.Scenes,
                scene,
                static (left, right) => left.Id == right.Id,
                static (left, right) => left.Id.CompareTo(right.Id)),
            Revision = revision
        }, cancellationToken);
    }

    public Task DeleteSceneAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(
            token => repository.DeleteSceneAsync(id, token),
            (topology, revision) => topology with
            {
                Scenes = Remove(topology.Scenes, id, static (item, key) => item.Id == key),
                Revision = revision
            },
            cancellationToken);

    public Task<TopologySnapshot> GetTopologyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot.Topology);

    public RuntimeConfigSnapshot GetRuntimeConfig() => Snapshot.RuntimeConfig;

    public bool TryGetRuntimeConfig(
        uint processId,
        out RuntimeConfigSnapshot runtimeConfig) =>
        Snapshot.RuntimeConfigsByProcessId.TryGetValue(processId, out runtimeConfig!);

    public Task<RuntimeConfigSnapshot> GetRuntimeConfigAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(GetRuntimeConfig());

    public ControlCenterSummary GetSummary()
    {
        var topology = Snapshot.Topology;
        var instances = registry.Count();
        return new ControlCenterSummary(
            topology.Namespaces.Count,
            topology.Machines.Count,
            topology.Processes.Count,
            topology.Worlds.Count,
            topology.Scenes.Count,
            instances.Online,
            instances.Offline,
            topology.Revision);
    }

    public Task<ControlCenterSummary> GetSummaryAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(GetSummary());

    public bool RegisterInstance(RegisterInstanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ServiceDiscoveryProtocol.ValidateSchemaVersion(request.SchemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InstanceId);
        ArgumentOutOfRangeException.ThrowIfZero(request.SceneId);
        ArgumentOutOfRangeException.ThrowIfZero(request.Address);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Host);
        if (request.InnerPort is < 1 or > 65535 || request.OuterPort is < 0 or > 65535)
        {
            throw new InvalidOperationException("端口必须在有效范围内。");
        }

        var snapshot = Snapshot;
        if (!snapshot.EnabledScenes.TryGetValue(request.SceneId, out var scene))
        {
            return false;
        }

        registry.Register(request, scene);

        // 配置恰好在注册期间切换时，再按新拓扑校验一次实例。
        var current = Snapshot;
        if (!ReferenceEquals(snapshot, current))
        {
            registry.ApplyTopology(current.EnabledScenes);
        }

        return true;
    }

    public Task<bool> RegisterInstanceAsync(
        RegisterInstanceRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(RegisterInstance(request));
    
    /// <summary>
    /// 注册一个属于指定 Root Scene 的 SubScene。
    /// </summary>
    public bool RegisterSubScene(RegisterSubSceneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ServiceDiscoveryProtocol.ValidateSchemaVersion(request.SchemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InstanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ParentInstanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SceneType);
        ArgumentOutOfRangeException.ThrowIfZero(request.Address);

        var instanceId = request.InstanceId.Trim();
        var parentInstanceId = request.ParentInstanceId.Trim();

        if (string.Equals(
                instanceId,
                parentInstanceId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "SubScene 实例 ID 不能与父 Root Scene 实例 ID 相同。");
        }

        return registry.RegisterSubScene(request);
    }

    public bool Heartbeat(HeartbeatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ServiceDiscoveryProtocol.ValidateSchemaVersion(request.SchemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InstanceId);
        return registry.Heartbeat(request.InstanceId, request.LeaseSeconds);
    }

    public Task<bool> HeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Heartbeat(request));

    public BatchHeartbeatResponse Heartbeat(BatchHeartbeatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ServiceDiscoveryProtocol.ValidateSchemaVersion(request.SchemaVersion);
        ArgumentNullException.ThrowIfNull(request.InstanceIds);
        if (request.InstanceIds.Length is 0 or > ServiceDiscoveryProtocol.MaxBatchHeartbeatCount)
        {
            throw new ArgumentException(
                $"批量心跳必须包含 1 到 {ServiceDiscoveryProtocol.MaxBatchHeartbeatCount} 个实例。");
        }

        return new BatchHeartbeatResponse
        {
            RejectedInstanceIds = registry.Heartbeat(request.InstanceIds, request.LeaseSeconds)
        };
    }

    public bool SetInstanceOffline(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return registry.SetOffline(instanceId);
    }

    public Task<bool> SetInstanceOfflineAsync(
        string instanceId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SetInstanceOffline(instanceId));

    public IReadOnlyList<ServiceInstanceView> GetInstances() => registry.GetInstances();

    public Task<IReadOnlyList<ServiceInstanceView>> GetInstancesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(GetInstances());

    public DiscoverServicesResponse Discover(
        string sceneType,
        uint namespaceId,
        uint? worldId,
        uint? worldGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneType);

        if (namespaceId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(namespaceId), "Namespace ID 必须大于 0。");
        }

        if (worldId.HasValue && worldGroupId.HasValue)
        {
            throw new ArgumentException("worldId 和 worldGroupId 不能同时指定。");
        }

        if (worldId == 0 || worldGroupId == 0)
        {
            throw new ArgumentOutOfRangeException(
                worldId == 0 ? nameof(worldId) : nameof(worldGroupId),
                "查询范围 ID 必须大于 0。");
        }

        var snapshot = Snapshot;
        return new DiscoverServicesResponse
        {
            Revision = snapshot.Topology.Revision,
            Instances = registry.Discover(sceneType.Trim(), namespaceId, worldId, worldGroupId)
        };
    }

    public Task<DiscoverServicesResponse> DiscoverAsync(
        string sceneType,
        uint namespaceId,
        uint? worldId,
        uint? worldGroupId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Discover(sceneType, namespaceId, worldId, worldGroupId));
    
    /// <summary>
    /// 根据 NamespaceId 和 SceneId 精确查询一个在线 Root Scene。
    /// </summary>
    public DiscoverServicesResponse ResolveScene( uint sceneId, uint namespaceId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sceneId);
        ArgumentOutOfRangeException.ThrowIfZero(namespaceId);

        var snapshot = Snapshot;
        var endpoint = registry.ResolveRootScene(namespaceId, sceneId);

        return new DiscoverServicesResponse
        {
            Revision = snapshot.Topology.Revision,
            Instances = endpoint == null
                ? []
                : [endpoint]
        };
    }
    
    /// <summary>
    /// 查询指定父 Root Scene 下面的在线 SubScene。
    /// </summary>
    public DiscoverServicesResponse DiscoverSubScenes(
        long parentAddress,
        string sceneType)
    {
        ArgumentOutOfRangeException.ThrowIfZero(parentAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneType);

        var snapshot = Snapshot;

        return new DiscoverServicesResponse
        {
            Revision = snapshot.Topology.Revision,
            Instances = registry.DiscoverSubScenes(
                parentAddress,
                sceneType.Trim())
        };
    }

    public long GetRevision() => Snapshot.Topology.Revision;

    public Task<long> GetRevisionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(GetRevision());

    private async Task MutateAsync(
        Func<CancellationToken, Task<long>> mutation,
        Func<TopologySnapshot, long, TopologySnapshot> update,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var topology = Snapshot.Topology;
            var revision = await mutation(cancellationToken);
            PublishSnapshot(update(topology, revision));
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private async Task<T> MutateAsync<T>(
        Func<CancellationToken, Task<(T Result, long Revision)>> mutation,
        Func<TopologySnapshot, T, long, TopologySnapshot> update,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var topology = Snapshot.Topology;
            var (result, revision) = await mutation(cancellationToken);
            PublishSnapshot(update(topology, result, revision));
            return result;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private async Task ReloadSnapshotAsync(CancellationToken cancellationToken)
    {
        PublishSnapshot(await repository.LoadTopologyAsync(cancellationToken));
    }

    private void PublishSnapshot(TopologySnapshot topology)
    {
        var snapshot = ControlCenterSnapshot.Build(topology);
        Volatile.Write(ref _snapshot, snapshot);
        registry.ApplyTopology(snapshot.EnabledScenes);
    }

    private static IReadOnlyList<T> Upsert<T>(
        IReadOnlyList<T> items,
        T value,
        Func<T, T, bool> same,
        Comparison<T> comparison)
    {
        var result = new List<T>(items.Count + 1);
        foreach (var item in items)
        {
            if (!same(item, value))
            {
                result.Add(item);
            }
        }

        result.Add(value);
        result.Sort(comparison);
        return result;
    }

    private static IReadOnlyList<T> Remove<T, TKey>(
        IReadOnlyList<T> items,
        TKey key,
        Func<T, TKey, bool> same)
    {
        var result = new List<T>(items.Count);
        foreach (var item in items)
        {
            if (!same(item, key))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static IReadOnlyList<DatabaseDefinition> UpsertDatabase(
        IReadOnlyList<DatabaseDefinition> items,
        DatabaseDefinition value)
    {
        var result = new List<DatabaseDefinition>(items.Count + 1);
        foreach (var item in items)
        {
            if (item.Id == value.Id)
            {
                continue;
            }

            result.Add(value.IsDefault && item.WorldId == value.WorldId && item.IsDefault
                ? new DatabaseDefinition
                {
                    Id = item.Id,
                    WorldId = item.WorldId,
                    DbType = item.DbType,
                    DbName = item.DbName,
                    DbConnection = item.DbConnection,
                    IsDefault = false
                }
                : item);
        }

        result.Add(value);
        result.Sort(static (left, right) =>
        {
            var world = left.WorldId.CompareTo(right.WorldId);
            return world != 0 ? world : left.Id.CompareTo(right.Id);
        });
        return result;
    }

    private static void EnsureNamespaceExists(uint namespaceId, TopologySnapshot topology)
    {
        foreach (var definition in topology.Namespaces)
        {
            if (definition.Id == namespaceId)
            {
                return;
            }
        }

        throw new InvalidOperationException($"Namespace {namespaceId} 不存在。");
    }

    private static void EnsureNamespaceUnused(uint namespaceId, TopologySnapshot topology)
    {
        foreach (var process in topology.Processes)
        {
            if (process.NamespaceId == namespaceId)
            {
                throw new InvalidOperationException(
                    $"Namespace {namespaceId} 仍被 Process {process.Id} 使用，不能删除。");
            }
        }

        foreach (var group in topology.WorldGroups)
        {
            if (group.NamespaceId == namespaceId)
            {
                throw new InvalidOperationException(
                    $"Namespace {namespaceId} 仍被 WorldGroup {group.Id} 使用，不能删除。");
            }
        }
    }

    private static void EnsureWorldGroupExists(uint groupId, TopologySnapshot topology)
    {
        foreach (var group in topology.WorldGroups)
        {
            if (group.Id == groupId)
            {
                return;
            }
        }

        throw new InvalidOperationException($"WorldGroup {groupId} 不存在。");
    }

    private static void EnsureWorldGroupUnused(uint groupId, TopologySnapshot topology)
    {
        foreach (var world in topology.Worlds)
        {
            if (world.GroupId == groupId)
            {
                throw new InvalidOperationException(
                    $"WorldGroup {groupId} 仍被 World {world.Id} 使用，不能删除。");
            }
        }
    }

    private static void ValidateProcessNamespace(ProcessDefinition process, TopologySnapshot topology)
    {
        foreach (var scene in topology.Scenes)
        {
            if (scene.ProcessId == process.Id && GetWorldNamespace(scene.WorldId, topology) != process.NamespaceId)
            {
                throw new InvalidOperationException(
                    $"Process {process.Id} 仍有关联 Scene {scene.Id}，不能移动到其他 Namespace。");
            }
        }
    }

    private static void ValidateWorldGroupNamespace(WorldGroupDefinition group, TopologySnapshot topology)
    {
        foreach (var world in topology.Worlds)
        {
            if (world.GroupId != group.Id)
            {
                continue;
            }

            ValidateWorldScenesNamespace(world.Id, group.NamespaceId, topology);
        }
    }

    private static void ValidateWorldNamespace(WorldDefinition world, TopologySnapshot topology)
    {
        uint namespaceId = 0;
        foreach (var group in topology.WorldGroups)
        {
            if (group.Id == world.GroupId)
            {
                namespaceId = group.NamespaceId;
                break;
            }
        }

        ValidateWorldScenesNamespace(world.Id, namespaceId, topology);
    }

    private static void ValidateWorldScenesNamespace(
        uint worldId,
        uint namespaceId,
        TopologySnapshot topology)
    {
        foreach (var scene in topology.Scenes)
        {
            if (scene.WorldId == worldId && GetProcessNamespace(scene.ProcessId, topology) != namespaceId)
            {
                throw new InvalidOperationException(
                    $"World {worldId} 仍有关联 Scene {scene.Id}，不能移动到其他 Namespace。");
            }
        }
    }

    private static uint GetProcessNamespace(uint processId, TopologySnapshot topology)
    {
        foreach (var process in topology.Processes)
        {
            if (process.Id == processId)
            {
                return process.NamespaceId;
            }
        }

        return 0;
    }

    private static uint GetWorldNamespace(uint worldId, TopologySnapshot topology)
    {
        uint groupId = 0;
        foreach (var world in topology.Worlds)
        {
            if (world.Id == worldId)
            {
                groupId = world.GroupId;
                break;
            }
        }

        foreach (var group in topology.WorldGroups)
        {
            if (group.Id == groupId)
            {
                return group.NamespaceId;
            }
        }

        return 0;
    }

    private static void ValidateSceneReferencesAndPorts(SceneDefinition scene, TopologySnapshot topology)
    {
        ProcessDefinition? targetProcess = null;
        foreach (var process in topology.Processes)
        {
            if (process.Id == scene.ProcessId)
            {
                targetProcess = process;
                break;
            }
        }

        if (targetProcess is null)
        {
            throw new InvalidOperationException($"进程 {scene.ProcessId} 不存在。");
        }

        WorldDefinition? targetWorld = null;
        foreach (var world in topology.Worlds)
        {
            if (world.Id == scene.WorldId)
            {
                targetWorld = world;
                break;
            }
        }

        if (targetWorld is null)
        {
            throw new InvalidOperationException($"World {scene.WorldId} 不存在。");
        }

        WorldGroupDefinition? targetWorldGroup = null;
        foreach (var group in topology.WorldGroups)
        {
            if (group.Id == targetWorld.GroupId)
            {
                targetWorldGroup = group;
                break;
            }
        }

        if (targetWorldGroup is null)
        {
            throw new InvalidOperationException($"WorldGroup {targetWorld.GroupId} 不存在。");
        }

        if (targetProcess.NamespaceId != targetWorldGroup.NamespaceId)
        {
            throw new InvalidOperationException(
                $"Scene {scene.Id} 的 Process 和 World 必须属于同一个 Namespace。");
        }

        foreach (var existing in topology.Scenes)
        {
            if (existing.Id == scene.Id || !PortsOverlap(existing, scene))
            {
                continue;
            }

            foreach (var process in topology.Processes)
            {
                if (process.Id == existing.ProcessId && process.MachineId == targetProcess.MachineId)
                {
                    throw new InvalidOperationException($"端口与同一台机器上的 Scene {existing.Id} 冲突。");
                }
            }
        }
    }

    private static bool PortsOverlap(SceneDefinition left, SceneDefinition right) =>
        left.InnerPort == right.InnerPort ||
        right.OuterPort > 0 && left.InnerPort == right.OuterPort ||
        left.OuterPort > 0 && left.OuterPort == right.InnerPort ||
        left.OuterPort > 0 && right.OuterPort > 0 && left.OuterPort == right.OuterPort;

    private static string NormalizeDatabaseType(string databaseType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseType);
        return databaseType.Trim().ToLowerInvariant() switch
        {
            "mongodb" or "mongo" => "MongoDB",
            "postgresql" or "postgres" or "pgsql" or "pg" => "PostgreSQL",
            _ => throw new InvalidOperationException($"不支持的数据库类型：{databaseType}。")
        };
    }
}
