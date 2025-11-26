using System.Collections.Generic;
using Fantasy.Network;

namespace Fantasy.ProtocolExportTool.Models;

/// <summary>
/// 协议序列化设置
/// </summary>
public sealed class ProtocolSettings
{
    /// <summary>
    /// 协议类型名称 (ProtoBuf, Bson, 或自定义协议名)
    /// </summary>
    public string ProtocolName { get; init; } = "ProtoBuf";

    /// <summary>
    /// OpCode 协议类型
    /// </summary>
    public uint OpCodeType { get; init; } = OpCodeProtocolType.ProtoBuf;

    /// <summary>
    /// 类级别特性 (如 [ProtoContract], 自定义特性, null 表示无特性)
    /// </summary>
    public string? ClassAttribute { get; init; } = "[ProtoContract]";

    /// <summary>
    /// 字段成员特性 (如 ProtoMember, BsonElement, null 表示无特性)
    /// </summary>
    public string? MemberAttribute { get; init; } = "ProtoMember";

    /// <summary>
    /// 忽略字段特性 (如 [ProtoIgnore], [BsonIgnore])
    /// </summary>
    public string IgnoreAttribute { get; init; } = "[ProtoIgnore]";

    /// <summary>
    /// 序列化索引起始值
    /// </summary>
    public int KeyStartIndex { get; init; } = 1;

    /// <summary>
    /// 需要引入的命名空间
    /// </summary>
    public HashSet<string> RequiredNamespaces { get; init; } = [];

    /// <summary>
    /// 创建默认 ProtoBuf 设置
    /// </summary>
    public static ProtocolSettings CreateProtoBuf()
    {
        return new ProtocolSettings
        {
            ProtocolName = "ProtoBuf",
            OpCodeType = OpCodeProtocolType.ProtoBuf,
            ClassAttribute = "[ProtoContract]",
            MemberAttribute = "ProtoMember",
            IgnoreAttribute = "[ProtoIgnore]",
            KeyStartIndex = 1
        };
    }

    /// <summary>
    /// 创建 Bson 设置
    /// </summary>
    public static ProtocolSettings CreateBson()
    {
        return new ProtocolSettings
        {
            ProtocolName = "Bson",
            OpCodeType = OpCodeProtocolType.Bson,
            ClassAttribute = null,
            MemberAttribute = null,
            IgnoreAttribute = "[BsonIgnore]",
            KeyStartIndex = 1
        };
    }

    /// <summary>
    /// 创建 MemoryPack 设置
    /// </summary>
    public static ProtocolSettings CreateMemoryPack()
    {
        return new ProtocolSettings
        {
            ProtocolName = "MemoryPack",
            OpCodeType = OpCodeProtocolType.MemoryPack,
            ClassAttribute = "[MemoryPackable]",
            MemberAttribute = "MemoryPackOrder",
            IgnoreAttribute = "[MemoryPackIgnore]",
            KeyStartIndex = 1
        };
    }
}