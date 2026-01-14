using System.Collections.Generic;

namespace Fantasy.ProtocolExportTool.Models;

public enum MessageType
{
    Message,
    Request,
    Response,
    RouteTypeMessage,
    RouteTypeRequest,
    RouteTypeResponse,
    RoamingMessage,
    RoamingRequest,
    RoamingResponse,
}

/// <summary>
/// 表示一个网络协议消息的完整定义
/// </summary>
public sealed class MessageDefinition
{
    /// <summary>
    /// 消息类名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实现的接口类型 (IMessage, IRequest, IResponse, IAddressableMessage, etc.)
    /// </summary>
    public string InterfaceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 类型消息
    /// </summary>
    public MessageType MessageType { get; set; }

    /// <summary>
    /// 响应类型 (仅适用于 Request 类型消息)
    /// </summary>
    public string? ResponseType { get; set; }

    /// <summary>
    /// 自定义路由类型 (RouteType 或 RoamingType)
    /// </summary>
    public string? CustomRouteType { get; set; }

    /// <summary>
    /// 协议设置 (ProtoBuf, Bson, 自定义序列化)
    /// </summary>
    public ProtocolSettings Protocol { get; set; } = new();

    /// <summary>
    /// 消息字段列表
    /// </summary>
    public List<FieldDefinition> Fields { get; set; } = new();

    /// <summary>
    /// OpCode 信息
    /// </summary>
    public OpcodeInfo? OpCode { get; set; }

    /// <summary>
    /// XML 注释文档
    /// </summary>
    public List<string> DocumentationComments { get; set; } = new();
    
    /// <summary>
    /// 定义常量/预编译宏指令
    /// </summary>
    public List<string> DefineConstants { get; set; } = new();

    /// <summary>
    /// 源文件路径 (用于错误报告)
    /// </summary>
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 源文件行号 (用于错误报告)
    /// </summary>
    public int SourceLineNumber { get; set; }

    /// <summary>
    /// 是否需要 OpCode
    /// </summary>
    public bool HasOpCode => !string.IsNullOrWhiteSpace(InterfaceType);
}
