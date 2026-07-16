using System.Collections.Concurrent;
using Fantasy.ControlCenter.Models;
using Fantasy.ControlCenter.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fantasy.ControlCenter.Infrastructure;

/// <summary>
/// 服务实例是运行时临时状态：注册、心跳和发现全部在内存完成。
/// 控制中心重启后，服务端按正常流程重新注册。
/// </summary>
public sealed class ServiceRegistry(IOptions<ControlCenterOptions> options) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);
    // ponytail: 固定保留 5 分钟；只有不同部署需要不同窗口时再放进配置。
    private const long OfflineRetentionMilliseconds = 5 * 60 * 1000;
    private readonly int _defaultLeaseSeconds = options.Value.DefaultLeaseSeconds;
    private readonly object _registrationLock = new();
    private readonly ConcurrentDictionary<string, InstanceState> _instances = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<ServiceKey, ConcurrentDictionary<string, InstanceState>> _byNamespace = new();
    private readonly ConcurrentDictionary<ServiceKey, ConcurrentDictionary<string, InstanceState>> _byWorld = new();
    private readonly ConcurrentDictionary<ServiceKey, ConcurrentDictionary<string, InstanceState>> _byWorldGroup = new();
    /// <summary>
    /// 按 NamespaceId + SceneId 建立 Root Scene 精确索引。
    /// 用于服务器根据 SubScene Address 快速找到其所属 Root Scene 的网络端点。
    /// </summary>
    private readonly ConcurrentDictionary<RootSceneKey, InstanceState> _rootScenesById = new();
    
    /// <summary>
    /// 按父 Root Scene Address 和 SubScene 类型建立索引。
    /// SubScene 查询不会扫描全部实例。
    /// </summary>
    private readonly ConcurrentDictionary<SubSceneKey, ConcurrentDictionary<string, InstanceState>> _bySubScenes = new();

    internal void Register(RegisterInstanceRequest request, SceneRegistration scene)
    {
        var instanceId = request.InstanceId.Trim();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var state = new InstanceState(
            instanceId,
            scene,
            request.Address,
            request.Host.Trim(),
            request.InnerPort,
            request.OuterPort,
            request.Version?.Trim() ?? string.Empty,
            now,
            now + NormalizeLeaseMilliseconds(request.LeaseSeconds));

        // 注册远低于心跳频率，用一个短锁保证主索引和三个发现索引一起切换。
        lock (_registrationLock)
        {
            if (_instances.TryGetValue(instanceId, out var oldState))
            {
                oldState.MarkRemoved();
                RemoveFromIndexes(oldState);
            }

            _instances[instanceId] = state;
            
            // Root Scene 精确查询索引。
            _rootScenesById[new RootSceneKey(scene.NamespaceId, scene.SceneId) ] = state;
            
            _byNamespace
                .GetOrAdd(new ServiceKey(scene.SceneType, scene.NamespaceId, 0), static _ => new ConcurrentDictionary<string, InstanceState>(StringComparer.Ordinal))
                [instanceId] = state;
            _byWorld
                .GetOrAdd(new ServiceKey(scene.SceneType, scene.NamespaceId, scene.WorldId), static _ => new ConcurrentDictionary<string, InstanceState>(StringComparer.Ordinal))
                [instanceId] = state;
            _byWorldGroup
                .GetOrAdd(new ServiceKey(scene.SceneType, scene.NamespaceId, scene.WorldGroupId), static _ => new ConcurrentDictionary<string, InstanceState>(StringComparer.Ordinal))
                [instanceId] = state;
        }
    }
    
    /// <summary>
    /// 将 SubScene 注册到指定的在线 Root Scene 实例下面。
    /// SubScene 的网络连接、进程和 World 信息全部继承父 Root Scene。
    /// </summary>
    internal bool RegisterSubScene(RegisterSubSceneRequest request)
    {
        var instanceId = request.InstanceId.Trim();
        var parentInstanceId = request.ParentInstanceId.Trim();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var leaseExpires = now + NormalizeLeaseMilliseconds(request.LeaseSeconds);

        lock (_registrationLock)
        {
            // 父实例必须存在、必须是 Root Scene，并且当前租约有效。
            if (!_instances.TryGetValue(parentInstanceId, out var parent) ||
                parent.Endpoint.IsSubScene ||
                !parent.IsHealthy(now))
            {
                return false;
            }

            if (_instances.TryGetValue(instanceId, out var oldState))
            {
                // SubScene 不允许覆盖一个 Root Scene 实例。
                if (!oldState.Endpoint.IsSubScene)
                {
                    return false;
                }

                oldState.MarkRemoved();
                RemoveFromIndexes(oldState);
            }

            var state = new InstanceState(
                instanceId,
                request,
                parent,
                now,
                leaseExpires);

            _instances[instanceId] = state;

            _bySubScenes
                .GetOrAdd(
                    new SubSceneKey(parent.Endpoint.Address, state.SceneType),
                    static _ => new ConcurrentDictionary<string, InstanceState>(StringComparer.Ordinal))
                [instanceId] = state;

            return true;
        }
    }

    public bool Heartbeat(string instanceId, int? leaseSeconds)
    {
        if (!_instances.TryGetValue(instanceId.Trim(), out var state))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return state.Heartbeat(now, now + NormalizeLeaseMilliseconds(leaseSeconds));
    }

    public string[] Heartbeat(string[] instanceIds, int? leaseSeconds)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var leaseExpires = now + NormalizeLeaseMilliseconds(leaseSeconds);
        List<string>? rejected = null;
        foreach (var instanceId in instanceIds)
        {
            if (string.IsNullOrWhiteSpace(instanceId) ||
                !_instances.TryGetValue(instanceId.Trim(), out var state) ||
                !state.Heartbeat(now, leaseExpires))
            {
                (rejected ??= []).Add(instanceId ?? string.Empty);
            }
        }

        return rejected?.ToArray() ?? [];
    }

    public bool SetOffline(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId.Trim(), out var state))
        {
            return false;
        }

        return state.SetOffline(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public IReadOnlyList<ServiceInstanceView> GetInstances()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new List<ServiceInstanceView>(_instances.Count);
        foreach (var pair in _instances)
        {
            result.Add(pair.Value.ToView(now));
        }

        result.Sort(static (left, right) =>
        {
            var sceneType = string.CompareOrdinal(left.SceneType, right.SceneType);
            return sceneType != 0 ? sceneType : string.CompareOrdinal(left.InstanceId, right.InstanceId);
        });
        return result;
    }

    public List<ServiceEndpointContract> Discover(
        string sceneType,
        uint namespaceId,
        uint? worldId,
        uint? worldGroupId)
    {
        ConcurrentDictionary<string, InstanceState>? bucket;
        if (worldId.HasValue)
        {
            _byWorld.TryGetValue(new ServiceKey(sceneType, namespaceId, worldId.Value), out bucket);
        }
        else if (worldGroupId.HasValue)
        {
            _byWorldGroup.TryGetValue(new ServiceKey(sceneType, namespaceId, worldGroupId.Value), out bucket);
        }
        else
        {
            _byNamespace.TryGetValue(new ServiceKey(sceneType, namespaceId, 0), out bucket);
        }

        if (bucket is null || bucket.IsEmpty)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new List<ServiceEndpointContract>(bucket.Count);
        foreach (var pair in bucket)
        {
            var state = pair.Value;
            if (state.IsHealthy(now))
            {
                result.Add(state.Endpoint);
            }
        }

        return result;
    }
    
    /// <summary>
    /// 查询指定 Root Scene 下面、指定类型的在线 SubScene。
    /// </summary>
    public List<ServiceEndpointContract> DiscoverSubScenes( long parentAddress, string sceneType)
    {
        if (!_bySubScenes.TryGetValue( new SubSceneKey(parentAddress, sceneType), out var bucket) || bucket.IsEmpty)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = new List<ServiceEndpointContract>(bucket.Count);

        foreach (var pair in bucket)
        {
            var state = pair.Value;
            if (state.IsHealthy(now))
            {
                result.Add(state.Endpoint);
            }
        }

        return result;
    }
    
    /// <summary>
    /// 根据 NamespaceId 和 SceneId 精确查询一个在线 Root Scene。
    /// </summary>
    /// <param name="namespaceId">命名空间 ID。</param>
    /// <param name="sceneId">Root Scene 配置 ID。</param>
    /// <returns>
    /// Root Scene 在线且租约有效时返回网络端点；否则返回 null。
    /// </returns>
    public ServiceEndpointContract? ResolveRootScene(uint namespaceId, uint sceneId)
    {
        if (!_rootScenesById.TryGetValue(new RootSceneKey(namespaceId, sceneId), out var state))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return state.IsHealthy(now)
            ? state.Endpoint
            : null;
    }

    public (int Online, int Offline) Count()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var online = 0;
        var total = 0;
        foreach (var pair in _instances)
        {
            total++;
            var state = pair.Value;
            if (state.IsHealthy(now))
            {
                online++;
            }
        }

        return (online, total - online);
    }

    internal void ApplyTopology(IReadOnlyDictionary<uint, SceneRegistration> enabledScenes)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var pair in _instances)
        {
            var state = pair.Value;
            if (!enabledScenes.TryGetValue(state.Scene.SceneId, out var current) || current != state.Scene)
            {
                state.SetOffline(now);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                CleanupExpiredInstances(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private int NormalizeLeaseMilliseconds(int? leaseSeconds) =>
        Math.Clamp(leaseSeconds ?? _defaultLeaseSeconds, 5, 300) * 1000;

    private void CleanupExpiredInstances(long now)
    {
        // ponytail: 每分钟 O(n) 扫描；实例达到百万级且扫描可测地影响延迟时再换时间轮。
        foreach (var pair in _instances)
        {
            var state = pair.Value;
            if (!state.ShouldRemove(now, OfflineRetentionMilliseconds))
            {
                continue;
            }

            lock (_registrationLock)
            {
                if (!_instances.TryGetValue(pair.Key, out var current) ||
                    !ReferenceEquals(current, state) ||
                    !state.TryMarkRemoved(now, OfflineRetentionMilliseconds))
                {
                    continue;
                }

                _instances.TryRemove(pair.Key, out _);
                RemoveFromIndexes(state);
            }
        }
    }

    private void RemoveFromIndexes(InstanceState state)
    {
        if (state.Endpoint.IsSubScene)
        {
            var subSceneKey = new SubSceneKey(
                state.Endpoint.ParentAddress,
                state.SceneType);

            if (_bySubScenes.TryGetValue(subSceneKey, out var bucket))
            {
                bucket.TryRemove(state.InstanceId, out _);

                if (bucket.IsEmpty)
                {
                    _bySubScenes.TryRemove(subSceneKey, out _);
                }
            }

            return;
        }
        
        var rootSceneKey = new RootSceneKey(state.Scene.NamespaceId, state.Scene.SceneId);
        
        // 只有索引仍然指向当前旧实例时才删除。
        // 防止旧实例清理时误删后来注册的新实例。
        if (_rootScenesById.TryGetValue(
                rootSceneKey,
                out var currentState) &&
            ReferenceEquals(currentState, state))
        {
            _rootScenesById.TryRemove(rootSceneKey, out _);
        }

        var namespaceKey = new ServiceKey(state.SceneType, state.Scene.NamespaceId, 0);
        if (_byNamespace.TryGetValue(namespaceKey, out var typeBucket))
        {
            typeBucket.TryRemove(state.InstanceId, out _);
            if (typeBucket.IsEmpty)
            {
                _byNamespace.TryRemove(namespaceKey, out _);
            }
        }

        var worldKey = new ServiceKey(state.SceneType, state.Scene.NamespaceId, state.Scene.WorldId);
        if (_byWorld.TryGetValue(worldKey, out var serviceBucket))
        {
            serviceBucket.TryRemove(state.InstanceId, out _);
            if (serviceBucket.IsEmpty)
            {
                _byWorld.TryRemove(worldKey, out _);
            }
        }

        var worldGroupKey = new ServiceKey(state.SceneType, state.Scene.NamespaceId, state.Scene.WorldGroupId);
        if (_byWorldGroup.TryGetValue(worldGroupKey, out var worldGroupBucket))
        {
            worldGroupBucket.TryRemove(state.InstanceId, out _);
            if (worldGroupBucket.IsEmpty)
            {
                _byWorldGroup.TryRemove(worldGroupKey, out _);
            }
        }
    }

    private readonly record struct ServiceKey(string SceneType, uint NamespaceId, uint ScopeId);
    private readonly record struct SubSceneKey(long ParentAddress, string SceneType);
    /// <summary>
    /// Root Scene 精确查询键。
    /// 不同 Namespace 可以存在相同的 SceneId。
    /// </summary>
    private readonly record struct RootSceneKey(uint NamespaceId, uint SceneId);

    private sealed class InstanceState
    {
        private const long RemovedLease = long.MinValue;
        private long _lastHeartbeatUnixMilliseconds;
        private long _leaseExpiresUnixMilliseconds;
        private long _offlineSinceUnixMilliseconds;
        private int _offline;

        public InstanceState(
            string instanceId,
            SceneRegistration scene,
            long address,
            string host,
            int innerPort,
            int outerPort,
            string version,
            long startedAtUnixMilliseconds,
            long leaseExpiresUnixMilliseconds)
        {
            Scene = scene;
            SceneType = scene.SceneType;
            Version = version;
            StartedAtUnixMilliseconds = startedAtUnixMilliseconds;
            Endpoint = new ServiceEndpointContract
            {
                InstanceId = instanceId,
                SceneId = scene.SceneId,
                Address = address,
                NamespaceId = scene.NamespaceId,
                WorldGroupId = scene.WorldGroupId,
                WorldId = scene.WorldId,
                ProcessId = scene.ProcessId,
                Host = host,
                InnerPort = innerPort,
                OuterPort = outerPort
            };
            _lastHeartbeatUnixMilliseconds = startedAtUnixMilliseconds;
            _leaseExpiresUnixMilliseconds = leaseExpiresUnixMilliseconds;
        }
        
        /// <summary>
        /// 从父 Root Scene 创建 SubScene 注册状态。
        /// SubScene 不建立自己的网络监听，网络字段全部继承父实例。
        /// </summary>
        public InstanceState(
            string instanceId,
            RegisterSubSceneRequest request,
            InstanceState parent,
            long startedAtUnixMilliseconds,
            long leaseExpiresUnixMilliseconds)
        {
            Scene = parent.Scene;
            SceneType = request.SceneType.Trim();
            Parent = parent;
            Version = parent.Version;
            StartedAtUnixMilliseconds = startedAtUnixMilliseconds;

            var parentEndpoint = parent.Endpoint;

            Endpoint = new ServiceEndpointContract
            {
                InstanceId = instanceId,

                // 建立连接时使用父 Root Scene 的配置 ID。
                SceneId = parentEndpoint.SceneId,

                // 消息最终发送给 SubScene 自身。
                Address = request.Address,

                IsSubScene = true,
                ParentInstanceId = parent.InstanceId,
                ParentAddress = parentEndpoint.Address,

                NamespaceId = parentEndpoint.NamespaceId,
                WorldGroupId = parentEndpoint.WorldGroupId,
                WorldId = parentEndpoint.WorldId,
                ProcessId = parentEndpoint.ProcessId,
                Host = parentEndpoint.Host,
                InnerPort = parentEndpoint.InnerPort,
                OuterPort = parentEndpoint.OuterPort
            };

            _lastHeartbeatUnixMilliseconds = startedAtUnixMilliseconds;
            _leaseExpiresUnixMilliseconds = leaseExpiresUnixMilliseconds;
        }

        public string InstanceId => Endpoint.InstanceId;
        public SceneRegistration Scene { get; }
        public string SceneType { get; }
        public string Version { get; }
        
        /// <summary>
        /// SubScene 所属的父 Root Scene。
        /// Root Scene 为 null。
        /// </summary>
        private InstanceState? Parent { get; }
        
        public long StartedAtUnixMilliseconds { get; }
        public ServiceEndpointContract Endpoint { get; }

        public bool Heartbeat(long now, long leaseExpires)
        {
            var parent = Parent;

            // 父 Root Scene 离线后，SubScene 不允许独立续约。
            if ((parent is not null && !parent.IsHealthy(now)) ||
                Volatile.Read(ref _offline) != 0)
            {
                return false;
            }

            while (true)
            {
                var currentLease = Interlocked.Read(ref _leaseExpiresUnixMilliseconds);
                if (currentLease == RemovedLease)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _leaseExpiresUnixMilliseconds, leaseExpires, currentLease) == currentLease)
                {
                    break;
                }
            }

            Interlocked.Exchange(ref _lastHeartbeatUnixMilliseconds, now);
            return Volatile.Read(ref _offline) == 0 &&
                   Interlocked.Read(ref _leaseExpiresUnixMilliseconds) != RemovedLease &&
                   (parent is null || parent.IsHealthy(now));
        }

        public bool SetOffline(long now)
        {
            if (Interlocked.Read(ref _leaseExpiresUnixMilliseconds) == RemovedLease)
            {
                return false;
            }

            Interlocked.Exchange(ref _offlineSinceUnixMilliseconds, now);
            Volatile.Write(ref _offline, 1);
            return Interlocked.Read(ref _leaseExpiresUnixMilliseconds) != RemovedLease;
        }

        public void MarkRemoved() => Interlocked.Exchange(ref _leaseExpiresUnixMilliseconds, RemovedLease);

        public bool ShouldRemove(long now, long retentionMilliseconds)
        {
            var leaseExpires = Interlocked.Read(ref _leaseExpiresUnixMilliseconds);
            if (leaseExpires == RemovedLease)
            {
                return true;
            }

            var removeAfter = Volatile.Read(ref _offline) == 0
                ? leaseExpires + retentionMilliseconds
                : Interlocked.Read(ref _offlineSinceUnixMilliseconds) + retentionMilliseconds;
            return removeAfter <= now;
        }

        public bool TryMarkRemoved(long now, long retentionMilliseconds)
        {
            var offline = Volatile.Read(ref _offline);
            var leaseExpires = Interlocked.Read(ref _leaseExpiresUnixMilliseconds);
            if (leaseExpires == RemovedLease)
            {
                return true;
            }

            var removeAfter = offline == 0
                ? leaseExpires + retentionMilliseconds
                : Interlocked.Read(ref _offlineSinceUnixMilliseconds) + retentionMilliseconds;
            if (removeAfter > now || offline != Volatile.Read(ref _offline))
            {
                return false;
            }

            return Interlocked.CompareExchange(ref _leaseExpiresUnixMilliseconds, RemovedLease, leaseExpires) == leaseExpires;
        }

        public bool IsHealthy(long now)
        {
            if (Volatile.Read(ref _offline) != 0 ||
                Interlocked.Read(ref _leaseExpiresUnixMilliseconds) <= now)
            {
                return false;
            }

            var parent = Parent;
            return parent is null || parent.IsHealthy(now);
        }

        public ServiceInstanceView ToView(long now)
        {
            var lastHeartbeat = Interlocked.Read(ref _lastHeartbeatUnixMilliseconds);
            var leaseExpires = Interlocked.Read(ref _leaseExpiresUnixMilliseconds);
            var removed = leaseExpires == RemovedLease;
            if (removed)
            {
                leaseExpires = lastHeartbeat;
            }
            return new ServiceInstanceView
            {
                InstanceId = InstanceId,
                SceneId = Endpoint.SceneId,
                SceneType = SceneType,
                Address = Endpoint.Address,
                IsSubScene = Endpoint.IsSubScene,
                ParentInstanceId = Endpoint.ParentInstanceId,
                ParentAddress = Endpoint.ParentAddress,
                NamespaceId = Scene.NamespaceId,
                WorldGroupId = Scene.WorldGroupId,
                WorldId = Scene.WorldId,
                Host = Endpoint.Host,
                InnerPort = Endpoint.InnerPort,
                OuterPort = Endpoint.OuterPort,
                Version = Version,
                Status = !removed && IsHealthy(now)
                    ? "Online"
                    : "Offline",
                StartedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(StartedAtUnixMilliseconds),
                LastHeartbeatUtc = DateTimeOffset.FromUnixTimeMilliseconds(lastHeartbeat),
                LeaseExpiresAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(leaseExpires)
            };
        }
    }
}
