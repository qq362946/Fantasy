#if FANTASY_NET
using System.Collections.Generic;

namespace Fantasy;

/// <summary>
/// 控制中心提供给 Fantasy.Net 的完整运行时配置快照。
/// </summary>
public sealed class RuntimeConfigSnapshot
{
    /// <summary>
    /// 协议结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = ServiceDiscoveryProtocol.SchemaVersion;
    
    /// <summary>
    /// 当前配置数据版本。
    /// 每次后台配置变化后，该值应当增加。
    /// </summary>
    public long Revision { get; set; }
    
    /// <summary>
    /// 启用的服务器配置。
    /// </summary>
    public List<MachineConfigContract> Machines { get; set; } = [];

    /// <summary>
    /// 启用的进程配置。
    /// </summary>
    public List<ProcessConfigContract> Processes { get; set; } = [];

    /// <summary>
    /// 启用的 World 配置。
    /// 数据库作为 World 的子配置返回。
    /// </summary>
    public List<WorldConfigContract> Worlds { get; set; } = [];

    /// <summary>
    /// 启用的 Scene 配置。
    /// </summary>
    public List<SceneConfigContract> Scenes { get; set; } = [];
}

/// <summary>
/// 服务器运行时配置。
/// </summary>
public sealed class MachineConfigContract
{
    /// <summary>
    /// 服务器配置 ID。
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// 仅用于日志和后台展示。
    /// Fantasy.Net 当前的 MachineConfig 暂时不使用该字段。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 对外公布的网络地址。
    /// </summary>
    public string OuterIp { get; set; } = string.Empty;

    /// <summary>
    /// 外网监听绑定地址。
    /// </summary>
    public string OuterBindIp { get; set; } = string.Empty;

    /// <summary>
    /// 内网监听绑定地址，同时作为服务发现的内网连接主机。
    /// </summary>
    public string InnerBindIp { get; set; } = string.Empty;
}

/// <summary>
/// 进程运行时配置。
/// </summary>
public sealed class ProcessConfigContract
{
    /// <summary>
    /// 进程配置 ID。
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// 进程所属的 Namespace ID。
    /// </summary>
    public uint NamespaceId { get; set; }

    /// <summary>
    /// 进程所在的服务器配置 ID。
    /// </summary>
    public uint MachineId { get; set; }

    /// <summary>
    /// 仅用于日志和后台展示。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 进程启动分组，用于筛选本次启动需要加载的进程。
    /// </summary>
    public uint StartupGroup { get; set; }
}

/// <summary>
/// World 运行时配置。
/// </summary>
public sealed class WorldConfigContract
{
    /// <summary>
    /// World 配置 ID。
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// World 所属的 Namespace ID。
    /// </summary>
    public uint NamespaceId { get; set; }

    /// <summary>
    /// 当前 World 所属的 WorldGroup。
    /// </summary>
    public uint GroupId { get; set; }

    /// <summary>
    /// World 显示名称。
    /// </summary>
    public string WorldName { get; set; } = string.Empty;

    /// <summary>
    /// 当前 World 的数据库配置。
    /// </summary>
    public List<DatabaseConfigContract> Databases { get; set; } = new();
}

/// <summary>
/// 数据库运行时配置。
/// </summary>
public sealed class DatabaseConfigContract
{
    /// <summary>
    /// 数据库类型，例如 MongoDB。
    /// </summary>
    public string DbType { get; set; } = string.Empty;

    /// <summary>
    /// 数据库名称。
    /// </summary>
    public string DbName { get; set; } = string.Empty;

    /// <summary>
    /// 数据库连接字符串；为空时框架不建立连接。
    /// </summary>
    public string DbConnection { get; set; } = string.Empty;

    /// <summary>
    /// 是否为当前 World 的默认数据库。
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// Scene 运行时配置。
/// </summary>
public sealed class SceneConfigContract
{
    /// <summary>
    /// Scene 配置 ID。
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Scene 所属的进程配置 ID。
    /// </summary>
    public uint ProcessId { get; set; }

    /// <summary>
    /// Scene 所属的 World 配置 ID。
    /// </summary>
    public uint WorldId { get; set; }

    /// <summary>
    /// Scene 类型名称，例如 Gate、Map、Chat。
    /// 协议层只传递字符串，不依赖源生成的 SceneType 类。
    /// </summary>
    public string SceneType { get; set; } = string.Empty;

    /// <summary>
    /// Scene 运行模式，例如 Develop 或 Release。
    /// </summary>
    public string RuntimeMode { get; set; } = string.Empty;

    /// <summary>
    /// Scene 使用的网络协议名称。
    /// </summary>
    public string NetworkProtocol { get; set; } = string.Empty;

    /// <summary>
    /// Scene 外网监听端口；没有外网监听时为 0。
    /// </summary>
    public int OuterPort { get; set; }

    /// <summary>
    /// Scene 内网监听端口。
    /// </summary>
    public int InnerPort { get; set; }
}
#endif
