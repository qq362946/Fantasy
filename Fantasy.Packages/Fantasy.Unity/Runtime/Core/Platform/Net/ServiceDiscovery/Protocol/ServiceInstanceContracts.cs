#if FANTASY_NET
using System;
using System.Collections.Generic;

namespace Fantasy;

/// <summary>
/// Scene 启动成功后发送的注册请求。
/// </summary>
public sealed class RegisterInstanceRequest
{
    /// <summary>
    /// 协议结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = ServiceDiscoveryProtocol.SchemaVersion;
    
    /// <summary>
    /// 服务实例唯一标识。
    /// 后面建议使用：
    /// MachineId:ProcessId:SceneId:启动标识
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Scene 配置 ID。
    /// </summary>
    public uint SceneId { get; set; }
    
    /// <summary>
    /// Fantasy内部消息路由地址。
    /// </summary>
    public long Address { get; set; }

    /// <summary>
    /// Scene 类型字符串，例如 Gate、Map、Chat。
    /// </summary>
    public string SceneType { get; set; } = string.Empty;

    /// <summary>
    /// Scene 所属的 World ID。
    /// </summary>
    public uint WorldId { get; set; }

    /// <summary>
    /// Scene 所属的进程配置 ID。
    /// </summary>
    public uint ProcessId { get; set; }
    
    /// <summary>
    /// 服务网络主机，可以是IP或域名。
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Scene 内网监听端口。
    /// </summary>
    public int InnerPort { get; set; }

    /// <summary>
    /// Scene 外网监听端口；没有外网监听时为 0。
    /// </summary>
    public int OuterPort { get; set; }
    
    /// <summary>
    /// 当前服务器程序版本。
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 可扩展元数据。
    /// 例如 region、weight、environment 等。
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// 实例租约时长，单位为秒。
    /// </summary>
    public int? LeaseSeconds { get; set; }
}

/// <summary>
/// SubScene 创建完成后发送的注册请求。
/// SubScene 不提供独立网络端口，连接信息从父 Root Scene 继承。
/// </summary>
public sealed class RegisterSubSceneRequest
{
    /// <summary>
    /// 协议结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = ServiceDiscoveryProtocol.SchemaVersion;

    /// <summary>
    /// 当前 SubScene 实例的唯一标识。
    /// 建议格式：
    /// ParentInstanceId:sub:Address
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// SubScene 所属的 Root Scene 实例 ID。
    /// 必须对应一个已经注册且在线的 Root Scene。
    /// </summary>
    public string ParentInstanceId { get; set; } = string.Empty;

    /// <summary>
    /// SubScene 的运行时 Address。
    /// 向该 Address 发送消息时，通过父 Root Scene 的网络连接进行路由。
    /// </summary>
    public long Address { get; set; }

    /// <summary>
    /// SubScene 类型名称，例如 Dungeon、Battle、Room。
    /// </summary>
    public string SceneType { get; set; } = string.Empty;

    /// <summary>
    /// SubScene 租约时长，单位为秒。
    /// 为空时使用 Control Center 默认租约。
    /// </summary>
    public int? LeaseSeconds { get; set; }
}

/// <summary>
/// 服务实例心跳请求。
/// </summary>
public sealed class HeartbeatRequest
{
    /// <summary>
    /// 协议结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = ServiceDiscoveryProtocol.SchemaVersion;

    /// <summary>
    /// 需要续约的服务实例 ID。
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 本次续约使用的租约时长；为空时由 Control Center 使用默认值。
    /// </summary>
    public int? LeaseSeconds { get; set; }
}

/// <summary>
/// 同一进程中多个服务实例的批量心跳请求。
/// </summary>
public sealed class BatchHeartbeatRequest
{
    /// <summary>
    /// 协议结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = ServiceDiscoveryProtocol.SchemaVersion;

    /// <summary>
    /// 本批次需要续约的服务实例 ID。
    /// </summary>
    public string[] InstanceIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 本批次使用的租约时长；为空时由 Control Center 使用默认值。
    /// </summary>
    public int? LeaseSeconds { get; set; }
}

/// <summary>
/// 批量心跳结果。这里返回的实例需要重新注册。
/// </summary>
public sealed class BatchHeartbeatResponse
{
    /// <summary>
    /// 协议结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = ServiceDiscoveryProtocol.SchemaVersion;

    /// <summary>
    /// Control Center 未识别或已过期、需要重新注册的实例 ID。
    /// </summary>
    public string[] RejectedInstanceIds { get; set; } = Array.Empty<string>();
}

/// <summary>
/// 服务发现只返回建立连接所需的字段。
/// </summary>
public sealed class ServiceEndpointContract
{
    /// <summary>
    /// 当前启动周期内的服务实例唯一标识。
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 用于建立网络连接的 Root Scene 配置 ID。
    /// 当前实例为 SubScene 时，该字段是父 Root Scene 的 Scene 配置 ID。
    /// </summary>
    public uint SceneId { get; set; }
    
    /// <summary>
    /// 实际消息目标的运行时 Address。
    /// 当前实例为 SubScene 时，该字段是 SubScene 自身的 Address。
    /// </summary>
    public long Address { get; set; }
    
    /// <summary>
    /// 是否为动态 SubScene 实例。
    /// </summary>
    public bool IsSubScene { get; set; }

    /// <summary>
    /// 父 Root Scene 的实例 ID。
    /// Root Scene 该字段为空。
    /// </summary>
    public string ParentInstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 父 Root Scene 的运行时 Address。
    /// Root Scene 该字段为 0。
    /// </summary>
    public long ParentAddress { get; set; }

    /// <summary>
    /// 当前实例所属的 Namespace。
    /// </summary>
    public uint NamespaceId { get; set; }

    /// <summary>
    /// 当前实例所属的 WorldGroup。
    /// </summary>
    public uint WorldGroupId { get; set; }

    /// <summary>
    /// 当前实例所属的 World ID。
    /// </summary>
    public uint WorldId { get; set; }

    /// <summary>
    /// 当前实例所属的进程配置 ID。
    /// </summary>
    public uint ProcessId { get; set; }

    /// <summary>
    /// 服务网络主机，可以是IP或域名。
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Scene 内网监听端口。
    /// </summary>
    public int InnerPort { get; set; }

    /// <summary>
    /// Scene 外网监听端口；没有外网监听时为 0。
    /// </summary>
    public int OuterPort { get; set; }
}

/// <summary>
/// 服务发现接口响应。
/// </summary>
public sealed class DiscoverServicesResponse
{
    /// <summary>
    /// 协议结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = ServiceDiscoveryProtocol.SchemaVersion;

    /// <summary>
    /// 返回时对应的控制中心配置版本。
    /// </summary>
    public long Revision { get; set; }

    /// <summary>
    /// 满足查询范围且租约仍有效的服务端点。
    /// </summary>
    public List<ServiceEndpointContract> Instances { get; set; } = new();
}
#endif
