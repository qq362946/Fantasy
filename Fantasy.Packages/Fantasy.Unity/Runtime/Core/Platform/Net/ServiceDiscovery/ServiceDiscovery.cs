#if FANTASY_NET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.IdFactory;

namespace Fantasy;

/// <summary>
/// Fantasy.Net服务发现入口。
/// </summary>
public static class ServiceDiscovery
{
    /// <summary>
    /// 当前进程共享的服务发现缓存；服务发现未启用时为空。
    /// </summary>
    private static ServiceDiscoveryCache? _cache;
    
    /// <summary>
    /// 当前进程是否已经启用服务发现。
    /// </summary>
    internal static bool IsEnabled => Volatile.Read(ref _cache) != null;

    /// <summary>
    /// 初始化服务发现缓存。
    /// </summary>
    /// <param name="client">Control Center HTTP 客户端。</param>
    /// <param name="cacheDurationSeconds">查询快照在本地缓存的秒数。</param>
    /// <param name="namespaceId">当前进程所属的 Namespace ID。</param>
    internal static void Initialize(ControlCenterClient client, int cacheDurationSeconds, uint namespaceId)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentOutOfRangeException.ThrowIfZero(namespaceId);

        if (cacheDurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cacheDurationSeconds));
        }

        Volatile.Write(ref _cache,
            new ServiceDiscoveryCache(
                client,
                cacheDurationSeconds,
                namespaceId));
    }

    /// <summary>
    /// 清理当前服务发现入口。
    /// </summary>
    internal static void Reset()
    {
        Volatile.Write(ref _cache, null);
    }

    /// <summary>
    /// 将本机 Scene 从服务发现中摘除，但不销毁 Scene。
    /// Root Scene 摘除时会同时摘除其全部 SubScene。
    /// 适合计划缩容时先停止新流量，再迁移状态并关闭 Scene。
    /// </summary>
    /// <param name="scene">需要停止对外发现的本机 Scene。</param>
    /// <returns>Control Center 确认下线后完成。</returns>
    public static FTask SetSceneOfflineAsync(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (scene.IsDisposed || scene.Address == 0)
        {
            throw new InvalidOperationException("Cannot take a disposed Scene offline.");
        }

        _ = GetCache();
        return Platform.Net.Entry.UnregisterServiceSceneAsync(scene, true);
    }
    
    /// <summary>
    /// 按Scene类型名称查询当前在线实例。
    /// </summary>
    /// <param name="sceneType">已在配置中声明的 Scene 类型名称。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID，不能与 <paramref name="worldId"/> 同时指定。</param>
    /// <returns>当前查询范围内的只读实例快照。</returns>
    public static FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverAsync(
        string sceneType,
        uint? worldId = null,
        uint? worldGroupId = null)
    {
        return GetCache().DiscoverAsync(sceneType, worldId, worldGroupId);
    }
    
    /// <summary>
    /// 按SceneType常量查询当前在线实例。
    /// 例如：ServiceDiscovery.DiscoverAsync(SceneType.Map)。
    /// </summary>
    /// <param name="sceneType">源生成的 SceneType 整数常量。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID，不能与 <paramref name="worldId"/> 同时指定。</param>
    /// <returns>当前查询范围内的只读实例快照。</returns>
    public static FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverAsync(
        int sceneType,
        uint? worldId = null,
        uint? worldGroupId = null)
    {
        return GetCache().DiscoverAsync(sceneType, worldId, worldGroupId);
    }
    
    /// <summary>
    /// 查询指定 Root Scene 下面、指定类型的在线 SubScene。
    /// </summary>
    /// <param name="parentAddress">父 Root Scene 的运行时 Address。</param>
    /// <param name="sceneType">SubScene 类型名称。</param>
    /// <returns>在线 SubScene 的只读缓存快照。</returns>
    public static FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverSubScenesAsync( long parentAddress, string sceneType)
    {
        return GetCache().DiscoverSubScenesAsync(parentAddress, sceneType);
    }

    /// <summary>
    /// 使用源生成的 SceneType 常量查询指定 Root Scene 下的在线 SubScene。
    /// </summary>
    /// <param name="parentAddress">父 Root Scene 的运行时 Address。</param>
    /// <param name="sceneType">源生成的 SceneType 整数常量。</param>
    /// <returns>在线 SubScene 的只读缓存快照。</returns>
    public static FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverSubScenesAsync( long parentAddress, int sceneType)
    {
        return GetCache().DiscoverSubScenesAsync(parentAddress, sceneType);
    }
    
    /// <summary>
    /// 随机选择一个在线Scene，并返回它的Address。
    /// 没有在线实例时返回0。
    /// </summary>
    /// <param name="sceneType">已在配置中声明的 Scene 类型名称。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID，不能与 <paramref name="worldId"/> 同时指定。</param>
    /// <returns>随机实例的运行时 Address；没有在线实例时为 0。</returns>
    public static async FTask<long> DiscoverAddressAsync(
        string sceneType,
        uint? worldId = null,
        uint? worldGroupId = null)
    {
        var instances = await DiscoverAsync(sceneType, worldId, worldGroupId);
        return GetAddress(SelectOne(instances));
    }
    
    /// <summary>
    /// 随机选择一个在线Scene，并返回它的Address。
    /// </summary>
    /// <param name="sceneType">源生成的 SceneType 整数常量。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID，不能与 <paramref name="worldId"/> 同时指定。</param>
    /// <returns>随机实例的运行时 Address；没有在线实例时为 0。</returns>
    public static async FTask<long> DiscoverAddressAsync(
        int sceneType,
        uint? worldId = null,
        uint? worldGroupId = null)
    {
        var instances = await DiscoverAsync(sceneType, worldId, worldGroupId);
        return GetAddress(SelectOne(instances));
    }
    
    /// <summary>
    /// 随机选择指定 Root Scene 下面的一个在线 SubScene。
    /// </summary>
    public static async FTask<long> DiscoverSubSceneAddressAsync(long parentAddress, string sceneType)
    {
        var instances =
            await DiscoverSubScenesAsync(
                parentAddress,
                sceneType);

        return GetAddress(SelectOne(instances));
    }

    /// <summary>
    /// 使用 SceneType 常量随机选择指定 Root Scene 下的在线 SubScene。
    /// </summary>
    public static async FTask<long> DiscoverSubSceneAddressAsync(long parentAddress, int sceneType)
    {
        var instances =
            await DiscoverSubScenesAsync(
                parentAddress,
                sceneType);

        return GetAddress(SelectOne(instances));
    }
    
    /// <summary>
    /// 使用一致性哈希选择一个在线Scene，并返回它的Address。
    /// 没有在线实例时返回0。
    /// </summary>
    /// <param name="sceneType">已在配置中声明的 Scene 类型名称。</param>
    /// <param name="routingKey">参与 Rendezvous Hash 的稳定业务键。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID，不能与 <paramref name="worldId"/> 同时指定。</param>
    /// <returns>选中实例的运行时 Address；没有在线实例时为 0。</returns>
    public static async FTask<long> DiscoverAddressByHashAsync(
        string sceneType,
        long routingKey,
        uint? worldId = null,
        uint? worldGroupId = null)
    {
        var instances = await DiscoverAsync(sceneType, worldId, worldGroupId);
        return GetAddress( SelectByHash(instances, routingKey));
    }
    
    /// <summary>
    /// 使用一致性哈希选择一个在线Scene，并返回它的Address。
    /// </summary>
    /// <param name="sceneType">源生成的 SceneType 整数常量。</param>
    /// <param name="routingKey">参与 Rendezvous Hash 的稳定业务键。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID，不能与 <paramref name="worldId"/> 同时指定。</param>
    /// <returns>选中实例的运行时 Address；没有在线实例时为 0。</returns>
    public static async FTask<long> DiscoverAddressByHashAsync(
        int sceneType,
        long routingKey,
        uint? worldId = null,
        uint? worldGroupId = null)
    {
        var instances = await DiscoverAsync(sceneType, worldId, worldGroupId);
        return GetAddress(SelectByHash(instances, routingKey));
    }
    
    /// <summary>
    /// 使用 Rendezvous Hash 选择指定 Root Scene 下的在线 SubScene。
    /// </summary>
    /// <param name="parentAddress">父 Root Scene 的运行时 Address。</param>
    /// <param name="sceneType">SubScene 类型名称。</param>
    /// <param name="routingKey">稳定的业务路由键。</param>
    /// <returns>选中的 SubScene Address；没有实例时返回 0。</returns>
    public static async FTask<long> DiscoverSubSceneAddressByHashAsync(long parentAddress, string sceneType, long routingKey)
    {
        var instances =
            await DiscoverSubScenesAsync(
                parentAddress,
                sceneType);

        return GetAddress(SelectByHash(instances, routingKey));
    }

    /// <summary>
    /// 使用 SceneType 常量和 Rendezvous Hash 选择在线 SubScene。
    /// </summary>
    public static async FTask<long> DiscoverSubSceneAddressByHashAsync(long parentAddress, int sceneType, long routingKey)
    {
        var instances =
            await DiscoverSubScenesAsync(
                parentAddress,
                sceneType);

        return GetAddress(SelectByHash(instances, routingKey));
    }
    
    /// <summary>
    /// 从只读缓存快照中随机选择一个实例。
    /// 不创建新数组或集合。
    /// </summary>
    /// <param name="instances">当前查询返回的实例快照。</param>
    /// <returns>随机实例；快照为空时返回空。</returns>
    private static ServiceEndpointContract? SelectOne(IReadOnlyList<ServiceEndpointContract> instances)
    {
        return instances.Count == 0
            ? null
            : instances[Random.Shared.Next(instances.Count)];
    }
    
    /// <summary>
    /// 使用Rendezvous Hash选择节点。
    /// 不创建哈希环、数组或临时集合。
    /// </summary>
    /// <param name="instances">当前查询返回的实例快照。</param>
    /// <param name="routingKey">稳定的业务路由键。</param>
    /// <returns>得分最高的实例；快照为空时返回空。</returns>
    private static ServiceEndpointContract? SelectByHash(IReadOnlyList<ServiceEndpointContract> instances, long routingKey)
    {
        if (instances.Count == 0)
        {
            return null;
        }

        // 使用框架已有的稳定哈希算法。
        // 不能使用string.GetHashCode，因为不同进程的结果可能不同。
        var routingKeyHash = unchecked((uint)HashCodeHelper.Hash32(in routingKey));

        ServiceEndpointContract? selected = null;
        uint selectedScore = 0;
        ulong selectedNodeId = 0;

        for (var i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];

            // Address 是实际逻辑服务节点标识。
            // Root Scene 和 SubScene 都具有唯一且稳定的 Address。
            var nodeId = unchecked((ulong)instance.Address);
            var score = unchecked((uint)HashCodeHelper.Hash32(in nodeId, routingKeyHash));

            if (selected == null || score > selectedScore || score == selectedScore && nodeId < selectedNodeId)
            {
                selected = instance;
                selectedScore = score;
                selectedNodeId = nodeId;
            }
        }

        return selected;
    }
    
    /// <summary>
    /// 缓存 Scene 对应的网络端点并返回其运行时 Address。
    /// </summary>
    /// <param name="endpoint">服务发现选中的端点。</param>
    /// <returns>端点的运行时 Address；端点为空时返回 0。</returns>
    private static long GetAddress(ServiceEndpointContract? endpoint)
    {
        if (endpoint == null)
        {
            return 0;
        }

        if (endpoint.SceneId == 0 || endpoint.Address == 0)
        {
            throw new InvalidOperationException("Discovered Scene endpoint is invalid.");
        }

        return endpoint.Address;
    }
    
    /// <summary>
    /// 根据 Root Scene 或 SubScene Address，
    /// 查询并缓存其所属 Root Scene 的网络端点。
    /// </summary>
    /// <param name="address">
    /// Root Scene 或 SubScene 的 RuntimeId Address。
    /// </param>
    /// <returns>Address 所属 Root Scene 的网络端点。</returns>
    public static FTask<ServiceEndpointContract> ResolveAddressAsync(long address)
    {
        ArgumentOutOfRangeException.ThrowIfZero(address);

        var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(address);

        if (sceneId == 0)
        {
            throw new ArgumentException($"Address {address} 中没有有效的 SceneId。", nameof(address));
        }

        return GetCache().ResolveSceneAsync(sceneId);
    }
    
    /// <summary>
    /// 根据 Root Scene 或 SubScene Address，
    /// 查询并缓存其所属 Root Scene 的网络端点。
    /// </summary>
    /// <param name="sceneId">
    /// Root Scene 或 SubScene 的 RuntimeId Address。
    /// </param>
    /// <returns>Address 所属 Root Scene 的网络端点。</returns>
    public static FTask<ServiceEndpointContract> ResolveAddressAsync(uint sceneId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sceneId);

        return GetCache().ResolveSceneAsync(sceneId);
    }
    
    /// <summary>
    /// 根据SceneId读取已发现的网络端点。
    /// 未开启服务发现或尚未发现时返回false。
    /// </summary>
    /// <param name="sceneId">Scene 配置 ID。</param>
    /// <param name="endpoint">已缓存的服务端点。</param>
    /// <returns>找到端点时返回 true。</returns>
    internal static bool TryGetEndpoint(uint sceneId, out ServiceEndpointContract endpoint)
    {
        var cache = Volatile.Read(ref _cache);

        if (cache != null)
        {
            return cache.TryGetEndpoint(sceneId, out endpoint);
        }

        endpoint = null!;
        return false;
    }
    
    /// <summary>
    /// 获取当前服务发现缓存，并在功能未启用时立即报告配置错误。
    /// </summary>
    /// <returns>当前进程共享的服务发现缓存。</returns>
    private static ServiceDiscoveryCache GetCache()
    {
        return Volatile.Read(ref _cache) ?? throw new InvalidOperationException("Service discovery is not enabled.");
    }
}

/// <summary>
/// 服务发现本地快照缓存。
/// 每个SceneType和查询范围组合只允许一个刷新请求。
/// </summary>
internal sealed class ServiceDiscoveryCache
{
    /// <summary>
    /// 用于刷新服务实例快照的 Control Center 客户端。
    /// </summary>
    private readonly ControlCenterClient _client;

    /// <summary>
    /// 当前进程所属的 Namespace ID，所有查询都会携带该隔离条件。
    /// </summary>
    private readonly uint _namespaceId;

    /// <summary>
    /// SceneType 整数常量到协议字符串名称的只读映射。
    /// </summary>
    private readonly Dictionary<int, string> _sceneTypeNames;

    /// <summary>
    /// 按 SceneType 和查询范围隔离的缓存条目。
    /// </summary>
    private readonly ConcurrentDictionary<DiscoveryKey, CacheEntry> _entries = new();

    /// <summary>
    /// 最近一次发现结果中的 Scene 网络端点索引。
    /// SubScene 使用父 Root Scene 的 SceneId 和网络端点。
    /// </summary>
    private readonly ConcurrentDictionary<uint, ServiceEndpointContract> _endpointsBySceneId = new();

    /// <summary>
    /// 本地查询快照有效期，单位为毫秒。
    /// </summary>
    private readonly long _cacheDurationMilliseconds;

    /// <summary>
    /// 创建当前进程的服务发现缓存，并冻结 SceneType 名称映射。
    /// </summary>
    /// <param name="client">Control Center HTTP 客户端。</param>
    /// <param name="cacheDurationSeconds">查询快照有效期，单位为秒。</param>
    /// <param name="namespaceId">当前进程所属的 Namespace ID。</param>
    internal ServiceDiscoveryCache(ControlCenterClient client, int cacheDurationSeconds, uint namespaceId)
    {
        _client = client;
        _namespaceId = namespaceId;
        _cacheDurationMilliseconds = cacheDurationSeconds * 1000L;
        
        var sceneTypes = Scene.SceneTypeDictionary;
        _sceneTypeNames = new Dictionary<int, string>(sceneTypes.Count);
        
        foreach (var sceneType in sceneTypes)
        {
            _sceneTypeNames.Add(
                sceneType.Value,
                sceneType.Key);
        }
    }
    
    /// <summary>
    /// 根据SceneId读取网络端点。
    /// </summary>
    /// <param name="sceneId">Scene 配置 ID。</param>
    /// <param name="endpoint">已缓存的服务端点。</param>
    /// <returns>找到端点时返回 true。</returns>
    internal bool TryGetEndpoint( uint sceneId, out ServiceEndpointContract endpoint)
    {
        return _endpointsBySceneId.TryGetValue(sceneId, out endpoint!);
    }
    
    /// <summary>
    /// 根据 SceneId 查询并缓存一个在线 Root Scene。
    /// 同一个 SceneId 的并发请求复用现有 CacheEntry 刷新锁。
    /// </summary>
    internal async FTask<ServiceEndpointContract> ResolveSceneAsync(uint sceneId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sceneId);

        var instances = await DiscoverCoreAsync(CreateRootSceneKey(sceneId));

        if (instances.Count == 0)
        {
            throw new InvalidOperationException($"Scene {sceneId} 当前没有在线的 Root Scene 实例。");
        }

        return instances[0];
    }
    
    /// <summary>
    /// 按Scene类型名称查询。
    /// </summary>
    /// <param name="sceneType">已在配置中声明的 Scene 类型名称。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID。</param>
    /// <returns>当前查询范围内的只读实例快照。</returns>
    internal FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverAsync(
        string sceneType,
        uint? worldId,
        uint? worldGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneType);

        var normalizedSceneType = sceneType.Trim();

        if (!Scene.SceneTypeDictionary.ContainsKey(normalizedSceneType))
        {
            throw new InvalidOperationException($"SceneType '{normalizedSceneType}' is not declared.");
        }

        return DiscoverCoreAsync(
            CreateKey(
                normalizedSceneType,
                worldId,
                worldGroupId));
    }
    
    /// <summary>
    /// 按SceneType常量查询。
    /// 例如：SceneType.Map。
    /// </summary>
    /// <param name="sceneType">源生成的 SceneType 整数常量。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID。</param>
    /// <returns>当前查询范围内的只读实例快照。</returns>
    internal FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverAsync(
        int sceneType,
        uint? worldId,
        uint? worldGroupId)
    {
        if (!_sceneTypeNames.TryGetValue(sceneType, out var sceneTypeName))
        {
            throw new InvalidOperationException($"SceneType id '{sceneType}' is not declared.");
        }

        return DiscoverCoreAsync(
            CreateKey(
                sceneTypeName,
                worldId,
                worldGroupId));
    }
    
    /// <summary>
    /// 按类型名称查询指定 Root Scene 下的 SubScene。
    /// </summary>
    internal FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverSubScenesAsync(long parentAddress, string sceneType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneType);

        var normalizedSceneType = sceneType.Trim();

        if (!Scene.SceneTypeDictionary.ContainsKey(
                normalizedSceneType))
        {
            throw new InvalidOperationException($"SceneType '{normalizedSceneType}' is not declared.");
        }

        return DiscoverCoreAsync(
            CreateSubSceneKey(
                normalizedSceneType,
                parentAddress));
    }

    /// <summary>
    /// 按 SceneType 常量查询指定 Root Scene 下的 SubScene。
    /// </summary>
    internal FTask<IReadOnlyList<ServiceEndpointContract>> DiscoverSubScenesAsync(long parentAddress, int sceneType)
    {
        if (!_sceneTypeNames.TryGetValue(
                sceneType,
                out var sceneTypeName))
        {
            throw new InvalidOperationException($"SceneType id '{sceneType}' is not declared.");
        }

        return DiscoverCoreAsync(
            CreateSubSceneKey(
                sceneTypeName,
                parentAddress));
    }

    /// <summary>
    /// 执行 Root Scene 或 SubScene 的统一缓存查询。
    /// </summary>
    private async FTask<IReadOnlyList<ServiceEndpointContract>>
        DiscoverCoreAsync(DiscoveryKey key)
    {
        var entry = _entries.GetOrAdd(key, static _ => new CacheEntry());
        var now = Environment.TickCount64;
        var cached = Volatile.Read(ref entry.Snapshot);

        if (cached != null && now < cached.ExpiresAtMilliseconds)
        {
            return cached.Instances;
        }

        await entry.RefreshLock.WaitAsync();

        try
        {
            now = Environment.TickCount64;
            cached = Volatile.Read(ref entry.Snapshot);

            if (cached != null && now < cached.ExpiresAtMilliseconds)
            {
                return cached.Instances;
            }

            try
            {
                DiscoverServicesResponse response;

                switch (key.Scope)
                {
                    case DiscoveryScope.All:
                    {
                        response =
                            await _client.DiscoverScenesAsync(
                                key.SceneType,
                                _namespaceId);

                        break;
                    }
                    case DiscoveryScope.World:
                    {
                        response =
                            await _client.DiscoverScenesAsync(
                                key.SceneType,
                                _namespaceId,
                                checked((uint)key.ScopeId));

                        break;
                    }
                    case DiscoveryScope.WorldGroup:
                    {
                        response =
                            await _client.DiscoverScenesAsync(
                                key.SceneType,
                                _namespaceId,
                                worldGroupId:
                                checked((uint)key.ScopeId));

                        break;
                    }
                    case DiscoveryScope.SubScene:
                    {
                        response =
                            await _client.DiscoverSubScenesAsync(
                                key.ScopeId,
                                key.SceneType);

                        break;
                    }
                    case DiscoveryScope.RootScene:
                    {
                        response =
                            await _client.ResolveSceneAsync(
                                checked((uint)key.ScopeId),
                                _namespaceId);

                        break;
                    }
                    default:
                    {
                        throw new InvalidOperationException(
                            $"Unsupported discovery scope: {key.Scope}.");
                    }
                }

                var instances =
                    response.Instances.Count == 0
                        ? Array.Empty<ServiceEndpointContract>()
                        : response.Instances.ToArray();

                // 在发布快照前登记全部网络端点。
                // 调用者从 DiscoverAsync 返回列表中直接取 Address，
                // 后续 Scene.GetSession(Address) 也能够找到网络入口。
                for (var i = 0; i < instances.Length; i++)
                {
                    var endpoint = instances[i];
                    _endpointsBySceneId[endpoint.SceneId] = endpoint;
                }

                var refreshed = new CachedSnapshot(
                    instances,
                    Environment.TickCount64 +
                    _cacheDurationMilliseconds);

                Volatile.Write(
                    ref entry.Snapshot,
                    refreshed);

                return instances;
            }
            catch (Exception)
            {
                cached = Volatile.Read(ref entry.Snapshot);

                if (cached == null)
                {
                    throw;
                }

                // Control Center 暂时不可用时继续使用旧快照。
                var stale = new CachedSnapshot(
                    cached.Instances,
                    Environment.TickCount64 +
                    _cacheDurationMilliseconds);

                Volatile.Write(
                    ref entry.Snapshot,
                    stale);

                return stale.Instances;
            }
        }
        finally
        {
            entry.RefreshLock.Release();
        }
    }

    /// <summary>
    /// 校验查询范围并创建无分配的缓存键。
    /// </summary>
    /// <param name="sceneType">已规范化的 Scene 类型名称。</param>
    /// <param name="worldId">可选的 World ID。</param>
    /// <param name="worldGroupId">可选的 WorldGroup ID。</param>
    /// <returns>包含 SceneType、范围类型和范围 ID 的缓存键。</returns>
    private static DiscoveryKey CreateKey(string sceneType, uint? worldId, uint? worldGroupId)
    {
        if (worldId.HasValue && worldGroupId.HasValue)
        {
            throw new ArgumentException("worldId and worldGroupId cannot both be specified.");
        }

        if (worldId == 0 || worldGroupId == 0)
        {
            throw new ArgumentOutOfRangeException(
                worldId == 0 ? nameof(worldId) : nameof(worldGroupId));
        }

        return worldId.HasValue
            ? new DiscoveryKey(sceneType, worldId.Value, DiscoveryScope.World)
            : worldGroupId.HasValue
                ? new DiscoveryKey(sceneType, worldGroupId.Value, DiscoveryScope.WorldGroup)
                : new DiscoveryKey(sceneType, 0, DiscoveryScope.All);
    }
    
    /// <summary>
    /// 创建按 SceneId 隔离的 Root Scene 精确查询键。
    /// </summary>
    private static DiscoveryKey CreateRootSceneKey(uint sceneId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sceneId);

        return new DiscoveryKey(
            string.Empty,
            sceneId,
            DiscoveryScope.RootScene);
    }
    
    /// <summary>
    /// 创建按父 Root Scene 隔离的 SubScene 查询键。
    /// </summary>
    private static DiscoveryKey CreateSubSceneKey(string sceneType, long parentAddress)
    {
        ArgumentOutOfRangeException.ThrowIfZero(parentAddress);

        return new DiscoveryKey(
            sceneType,
            parentAddress,
            DiscoveryScope.SubScene);
    }

    /// <summary>
    /// 唯一标识一个 SceneType 与查询范围组合的缓存键。
    /// </summary>
    /// <param name="SceneType">Scene 类型名称。</param>
    /// <param name="ScopeId">World 或 WorldGroup ID；全局范围时为 0。</param>
    /// <param name="Scope">查询范围类型。</param>
    private readonly record struct DiscoveryKey(string SceneType, long ScopeId, DiscoveryScope Scope);

    /// <summary>
    /// 服务发现查询的隔离范围。
    /// </summary>
    private enum DiscoveryScope : byte
    {
        /// <summary>
        /// 当前 Namespace 下的全部 World。
        /// </summary>
        All,

        /// <summary>
        /// 指定 World。
        /// </summary>
        World,

        /// <summary>
        /// 指定 WorldGroup。
        /// </summary>
        WorldGroup,
        
        /// <summary>
        /// 按 SceneId 精确查询一个 Root Scene。
        /// </summary>
        RootScene,
        
        /// <summary>
        /// 指定 Root Scene 下面的 SubScene。
        /// </summary>
        SubScene
    }

    /// <summary>
    /// 保存一个查询范围的刷新锁和最新不可变快照。
    /// </summary>
    private sealed class CacheEntry
    {
        /// <summary>
        /// 保证同一个缓存键同时只有一个远端刷新请求。
        /// </summary>
        internal readonly SemaphoreSlim RefreshLock = new(1, 1);

        /// <summary>
        /// 通过 Volatile 读写发布的最新完整快照。
        /// </summary>
        internal CachedSnapshot? Snapshot;
    }

    /// <summary>
    /// 将实例数组和过期时间放进同一个对象，
    /// 通过一次Volatile.Write原子发布完整快照。
    /// </summary>
    private sealed class CachedSnapshot
    {
        /// <summary>
        /// 创建一个可原子发布的不可变缓存快照。
        /// </summary>
        /// <param name="instances">在线实例数组。</param>
        /// <param name="expiresAtMilliseconds">基于 <see cref="Environment.TickCount64"/> 的过期时间。</param>
        internal CachedSnapshot(ServiceEndpointContract[] instances, long expiresAtMilliseconds)
        {
            Instances = instances;
            ExpiresAtMilliseconds = expiresAtMilliseconds;
        }

        /// <summary>
        /// 当前查询返回的实例数组，发布后不再修改。
        /// </summary>
        internal ServiceEndpointContract[] Instances
        {
            get;
        }

        /// <summary>
        /// 基于 <see cref="Environment.TickCount64"/> 的快照过期时间。
        /// </summary>
        internal long ExpiresAtMilliseconds
        {
            get;
        }
    }
}
#endif
