#if FANTASY_NET
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Http;

namespace Fantasy;

/// <summary>
/// Fantasy.Net访问Control Center的HTTP客户端。
/// URL只在构造时解析一次，后续请求直接复用完整接口地址。
/// </summary>
internal sealed class ControlCenterClient
{
    /// <summary>
    /// 获取运行时配置快照的完整地址。
    /// </summary>
    private readonly string _runtimeConfigEndpoint;

    /// <summary>
    /// 注册服务实例的完整地址。
    /// </summary>
    private readonly string _registerInstanceEndpoint;
    
    /// <summary>
    /// 注册 SubScene 的完整地址。
    /// </summary>
    private readonly string _registerSubSceneEndpoint;

    /// <summary>
    /// 批量心跳的完整地址。
    /// </summary>
    private readonly string _batchHeartbeatEndpoint;

    /// <summary>
    /// 查询在线 Scene 的完整地址。
    /// </summary>
    private readonly string _discoverScenesEndpoint;
    
    /// <summary>
    /// 按 SceneId 精确查询在线 Root Scene 的接口地址前缀。
    /// </summary>
    private readonly string _resolveSceneEndpointPrefix;
    
    /// <summary>
    /// 查询指定 Root Scene 下在线 SubScene 的完整地址。
    /// </summary>
    private readonly string _discoverSubScenesEndpoint;

    /// <summary>
    /// 实例下线接口的地址前缀，调用时追加实例 ID 和 offline 路径。
    /// </summary>
    private readonly string _instanceOfflineEndpointPrefix;

    /// <summary>
    /// 预创建的 JSON Content-Type，供重复发送的心跳内容复用。
    /// </summary>
    private static readonly MediaTypeHeaderValue JsonContentType =
        new("application/json")
        {
            CharSet = "utf-8"
        };

    /// <summary>
    /// 创建 Control Center HTTP 客户端并预计算全部接口地址。
    /// </summary>
    /// <param name="baseUrl">Control Center 的 HTTP 或 HTTPS 根地址。</param>
    internal ControlCenterClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Control center URL cannot be empty.", nameof(baseUrl));
        }

        var normalizedBaseUrl = baseUrl.TrimEnd('/');

        if (!Uri.TryCreate( normalizedBaseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"Invalid control center URL: {baseUrl}");
        }

        if (!string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException("Control center URL cannot contain a query or fragment.");
        }

        normalizedBaseUrl = uri.AbsoluteUri.TrimEnd('/');

        _runtimeConfigEndpoint = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.RuntimeConfigRoute);
        _registerInstanceEndpoint = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.RegisterInstanceRoute);
        _registerSubSceneEndpoint = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.RegisterSubSceneRoute);
        _batchHeartbeatEndpoint = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.BatchHeartbeatRoute);
        _discoverScenesEndpoint = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.DiscoverScenesRoute);
        _resolveSceneEndpointPrefix = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.ResolveSceneRoutePrefix);
        _discoverSubScenesEndpoint = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.DiscoverSubScenesRoute);
        _instanceOfflineEndpointPrefix = string.Concat(normalizedBaseUrl, ServiceDiscoveryProtocol.ApiPrefix, "/instances/");
    }

    /// <summary>
    /// 获取Control Center运行时配置快照。
    /// </summary>
    /// <param name="processId">可选的进程配置 ID；指定后只获取该进程所需配置。</param>
    /// <returns>通过结构版本和进程范围校验的运行时配置快照。</returns>
    internal async FTask<RuntimeConfigSnapshot> GetRuntimeConfigAsync(uint? processId = null)
    {
        if (processId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }

        var endpoint = processId.HasValue
            ? string.Concat(_runtimeConfigEndpoint, "?processId=", processId.Value.ToString())
            : _runtimeConfigEndpoint;

        var snapshot = await HttpClientHelper.CallByGet<RuntimeConfigSnapshot>(endpoint);

        if (snapshot == null)
        {
            throw new InvalidOperationException("Control center returned an empty runtime configuration.");
        }

        ServiceDiscoveryProtocol.ValidateSchemaVersion(snapshot.SchemaVersion);

        if (processId.HasValue &&
            (snapshot.Processes == null ||
             snapshot.Processes.Count != 1 ||
             snapshot.Processes[0].Id != processId.Value))
        {
            throw new InvalidOperationException(
                $"Control center returned an invalid runtime configuration for Process {processId.Value}.");
        }

        return snapshot;
    }

    /// <summary>
    /// 注册一个Scene服务实例。
    /// 注册属于低频操作，不需要维护额外的批量注册协议。
    /// </summary>
    /// <param name="request">包含 Scene 地址、端口和租约信息的注册请求。</param>
    internal async FTask RegisterInstanceAsync(RegisterInstanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfZero(request.Address);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Host);

        using var content = CreateJsonContent(request);

        await HttpClientHelper.CallNotDeserializeByPost(_registerInstanceEndpoint, content);
    }
    
    /// <summary>
    /// 将 SubScene 注册到指定的父 Root Scene 实例下面。
    /// SubScene 的网络端点由 Control Center 从父实例继承。
    /// </summary>
    /// <param name="request">SubScene 注册请求。</param>
    internal async FTask RegisterSubSceneAsync( RegisterSubSceneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InstanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ParentInstanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SceneType);
        ArgumentOutOfRangeException.ThrowIfZero(request.Address);

        using var content = CreateJsonContent(request);

        await HttpClientHelper.CallNotDeserializeByPost(_registerSubSceneEndpoint, content);
    }

    /// <summary>
    /// 查询指定 Root Scene 下面、指定类型的在线 SubScene。
    /// </summary>
    /// <param name="parentAddress">父 Root Scene 的运行时 Address。</param>
    /// <param name="sceneType">SubScene 类型名称。</param>
    /// <returns>满足父子关系和 SceneType 条件的在线 SubScene。</returns>
    internal async FTask<DiscoverServicesResponse> DiscoverSubScenesAsync( long parentAddress, string sceneType)
    {
        ArgumentOutOfRangeException.ThrowIfZero(parentAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneType);

        var endpoint = string.Concat(
            _discoverSubScenesEndpoint,
            "?parentAddress=",
            parentAddress.ToString(),
            "&sceneType=",
            Uri.EscapeDataString(sceneType.Trim()));

        var response = await HttpClientHelper.CallByGet<DiscoverServicesResponse>(endpoint);

        if (response == null)
        {
            throw new InvalidOperationException("Control center returned an empty SubScene discovery response.");
        }

        ServiceDiscoveryProtocol.ValidateSchemaVersion(response.SchemaVersion);

        if (response.Instances == null)
        {
            throw new InvalidOperationException("Control center returned null SubScene instances.");
        }

        for (var i = 0; i < response.Instances.Count; i++)
        {
            var instance = response.Instances[i];

            if (instance == null ||
                !instance.IsSubScene ||
                instance.SceneId == 0 ||
                instance.Address == 0 ||
                instance.ParentAddress != parentAddress ||
                string.IsNullOrWhiteSpace(instance.ParentInstanceId) ||
                instance.NamespaceId == 0 ||
                instance.WorldGroupId == 0 ||
                instance.WorldId == 0 ||
                string.IsNullOrWhiteSpace(instance.Host) ||
                instance.InnerPort is < 1 or > 65535 ||
                instance.OuterPort is < 0 or > 65535)
            {
                throw new InvalidOperationException($"Control center returned an invalid SubScene instance at index {i}.");
            }
        }

        return response;
    }

    /// <summary>
    /// 将一个服务实例标记为下线。
    /// 只在服务器正常关闭时调用。
    /// </summary>
    /// <param name="instanceId">当前启动周期内的服务实例唯一标识。</param>
    internal async FTask SetInstanceOfflineAsync(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        var endpoint = string.Concat(
            _instanceOfflineEndpointPrefix,
            Uri.EscapeDataString(instanceId),
            "/offline");

        using var content = new StringContent(string.Empty);

        await HttpClientHelper.CallNotDeserializeByPost(endpoint, content);
    }

    /// <summary>
    /// 发送已经序列化好的批量心跳。
    /// 心跳内容固定，由ServiceDiscoveryWorker预先序列化并重复使用。
    /// </summary>
    /// <param name="requestJson">UTF-8 编码的 <see cref="BatchHeartbeatRequest"/> JSON。</param>
    /// <returns>需要重新注册的实例列表。</returns>
    internal async FTask<BatchHeartbeatResponse> BatchHeartbeatAsync(byte[] requestJson)
    {
        ArgumentNullException.ThrowIfNull(requestJson);
        
        if (requestJson.Length == 0)
        {
            throw new ArgumentException("Heartbeat request JSON cannot be empty.", nameof(requestJson));
        }
        
        using var content = new ByteArrayContent(requestJson);
        
        content.Headers.ContentType = JsonContentType;
        
        var response =
            await HttpClientHelper
                .CallByPost<BatchHeartbeatResponse>(
                    _batchHeartbeatEndpoint,
                    content);

        if (response == null)
        {
            throw new InvalidOperationException(
                "Control center returned an empty heartbeat response.");
        }

        ServiceDiscoveryProtocol.ValidateSchemaVersion(response.SchemaVersion);

        return response;
    }
    
    /// <summary>
    /// 查询当前在线的Scene服务实例。
    /// </summary>
    /// <param name="sceneType">
    /// Scene类型名称，例如Gate、Map、Chat。
    /// </param>
    /// <param name="namespaceId">
    /// 当前进程所属的Namespace ID。
    /// </param>
    /// <param name="worldId">
    /// 可选World ID；为null时查询所有World。
    /// </param>
    /// <param name="worldGroupId">
    /// 可选WorldGroup ID；不能与worldId同时指定。
    /// </param>
    /// <returns>满足 Namespace 和可选 World 范围的在线实例。</returns>
    internal async FTask<DiscoverServicesResponse> DiscoverScenesAsync(string sceneType, uint namespaceId, uint? worldId = null, uint? worldGroupId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( sceneType);
        ArgumentOutOfRangeException.ThrowIfZero(namespaceId);

        if (worldId.HasValue && worldGroupId.HasValue)
        {
            throw new ArgumentException("worldId and worldGroupId cannot both be specified.");
        }

        if (worldId == 0 || worldGroupId == 0)
        {
            throw new ArgumentOutOfRangeException(worldId == 0 ? nameof(worldId) : nameof(worldGroupId));
        }

        var normalizedSceneType = sceneType.Trim();
        var encodedSceneType = Uri.EscapeDataString(normalizedSceneType);
        var queryPrefix = string.Concat("?sceneType=", encodedSceneType, "&namespaceId=", namespaceId.ToString());

        var endpoint = worldId.HasValue
            ? string.Concat(_discoverScenesEndpoint, queryPrefix, "&worldId=", worldId.Value.ToString())
            : worldGroupId.HasValue
                ? string.Concat(_discoverScenesEndpoint, queryPrefix, "&worldGroupId=", worldGroupId.Value.ToString())
                : string.Concat(_discoverScenesEndpoint, queryPrefix);

        var response = await HttpClientHelper.CallByGet<DiscoverServicesResponse>( endpoint);

        if (response == null)
        {
            throw new InvalidOperationException(
                "Control center returned an empty " +
                "service discovery response.");
        }

        ServiceDiscoveryProtocol.ValidateSchemaVersion(response.SchemaVersion);

        if (response.Instances == null)
        {
            throw new InvalidOperationException("Control center returned null service instances.");
        }
        
        for (var i = 0; i < response.Instances.Count; i++)
        {
            var instance = response.Instances[i];

            if (instance == null ||
                instance.SceneId == 0 ||
                instance.Address == 0 ||
                instance.NamespaceId != namespaceId ||
                instance.WorldGroupId == 0 ||
                string.IsNullOrWhiteSpace(instance.Host) ||
                instance.InnerPort is < 1 or > 65535 ||
                instance.OuterPort is < 0 or > 65535)
            {
                throw new InvalidOperationException(
                    $"Control center returned an invalid " +
                    $"service instance at index {i}.");
            }
        }

        return response;
    }

    /// <summary>
    /// 根据 NamespaceId 和 SceneId 精确查询在线 Root Scene。
    /// </summary>
    /// <param name="sceneId">Root Scene 配置 ID。</param>
    /// <param name="namespaceId">当前进程所属 Namespace ID。</param>
    /// <returns>
    /// 包含零个或一个 Root Scene 端点的发现响应。
    /// </returns>
    internal async FTask<DiscoverServicesResponse> ResolveSceneAsync(uint sceneId, uint namespaceId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sceneId);
        ArgumentOutOfRangeException.ThrowIfZero(namespaceId);

        var endpoint = string.Concat(
            _resolveSceneEndpointPrefix,
            sceneId.ToString(),
            "?namespaceId=",
            namespaceId.ToString());

        var response =
            await HttpClientHelper
                .CallByGet<DiscoverServicesResponse>(endpoint);

        if (response == null)
        {
            throw new InvalidOperationException(
                "Control center returned an empty Root Scene response.");
        }

        ServiceDiscoveryProtocol.ValidateSchemaVersion(
            response.SchemaVersion);

        if (response.Instances == null)
        {
            throw new InvalidOperationException(
                "Control center returned null Root Scene instances.");
        }

        if (response.Instances.Count > 1)
        {
            throw new InvalidOperationException(
                $"Control center returned multiple Root Scenes " +
                $"for SceneId {sceneId}.");
        }

        // 没有在线实例属于正常查询结果。
        if (response.Instances.Count == 0)
        {
            return response;
        }

        var instance = response.Instances[0];

        if (instance == null ||
            instance.IsSubScene ||
            instance.SceneId != sceneId ||
            instance.Address == 0 ||
            instance.NamespaceId != namespaceId ||
            instance.ProcessId == 0 ||
            string.IsNullOrWhiteSpace(instance.InstanceId) ||
            string.IsNullOrWhiteSpace(instance.Host) ||
            instance.InnerPort is < 1 or > 65535 ||
            instance.OuterPort is < 0 or > 65535)
        {
            throw new InvalidOperationException(
                $"Control center returned an invalid Root Scene " +
                $"for SceneId {sceneId}.");
        }

        return response;
    }

    /// <summary>
    /// 使用框架 JSON 配置创建 UTF-8 请求内容。
    /// </summary>
    /// <typeparam name="T">请求 DTO 类型。</typeparam>
    /// <param name="value">需要序列化的请求对象。</param>
    /// <returns>由调用方负责释放的 HTTP 内容。</returns>
    private static StringContent CreateJsonContent<T>(T value)
    {
        return new StringContent(value.ToJson(), Encoding.UTF8, "application/json");
    }

}
#endif
