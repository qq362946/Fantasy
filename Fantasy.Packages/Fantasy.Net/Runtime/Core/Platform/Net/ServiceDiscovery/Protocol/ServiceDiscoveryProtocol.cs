#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// Fantasy 服务发现协议常量。
/// </summary>
public static class ServiceDiscoveryProtocol
{
    /// <summary>
    /// 当前支持的协议结构版本。
    /// 修改不兼容的协议结构时，需要增加该版本号。
    /// </summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// 验证远端返回的协议结构版本是否与当前客户端兼容。
    /// </summary>
    /// <param name="schemaVersion">远端返回的协议结构版本。</param>
    /// <exception cref="InvalidOperationException">版本不匹配时抛出。</exception>
    internal static void ValidateSchemaVersion(int schemaVersion)
    {
        if (schemaVersion == SchemaVersion)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported schema version {schemaVersion}, expected {SchemaVersion}.");
    }

    /// <summary>
    /// 单次批量心跳允许携带的最大实例数。
    /// </summary>
    public const int MaxBatchHeartbeatCount = 4096;
    
    /// <summary>
    /// API 统一前缀。
    /// </summary>
    public const string ApiPrefix = "/api/v1";
    
    /// <summary>
    /// 获取 Fantasy 运行时配置。
    /// </summary>
    public const string RuntimeConfigRoute = ApiPrefix + "/runtime/config";
    
    /// <summary>
    /// 注册服务实例。
    /// </summary>
    public const string RegisterInstanceRoute = ApiPrefix + "/instances/register";
    
    /// <summary>
    /// 将 SubScene 注册到指定 Root Scene 实例下面。
    /// </summary>
    public const string RegisterSubSceneRoute = ApiPrefix + "/instances/sub-scenes/register";

    /// <summary>
    /// 服务实例心跳。
    /// </summary>
    public const string HeartbeatRoute = ApiPrefix + "/instances/heartbeat";

    /// <summary>
    /// 批量续约同一进程中的服务实例。
    /// </summary>
    public const string BatchHeartbeatRoute = ApiPrefix + "/instances/heartbeat/batch";

    /// <summary>
    /// 查询服务实例。
    /// </summary>
    public const string DiscoverScenesRoute = ApiPrefix + "/discovery/scenes";
    
    /// <summary>
    /// 按 Scene 配置 ID 精确查询在线 Root Scene 的接口前缀。
    /// 完整地址示例：/api/v1/discovery/scenes/1001?namespaceId=1
    /// </summary>
    public const string ResolveSceneRoutePrefix = DiscoverScenesRoute + "/";
    
    /// <summary>
    /// 查询指定 Root Scene 下面的在线 SubScene。
    /// </summary>
    public const string DiscoverSubScenesRoute = ApiPrefix + "/discovery/sub-scenes";
}
#endif
