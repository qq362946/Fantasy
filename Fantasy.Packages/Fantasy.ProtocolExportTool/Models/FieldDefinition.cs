using System.Collections.Generic;

namespace Fantasy.ProtocolExportTool.Models;

/// <summary>
/// 表示消息中的一个字段定义
/// </summary>
public sealed class FieldDefinition
{
    /// <summary>
    /// 字段名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段类型 (如 int, string, List&lt;int&gt;, PlayerData 等)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 字段编号 (用于序列化)
    /// </summary>
    public int FieldNumber { get; set; }

    /// <summary>
    /// ProtoMember/BsonElement 等序列化索引
    /// </summary>
    public int KeyIndex { get; set; }

    /// <summary>
    /// 是否是重复字段 (集合类型)
    /// </summary>
    public bool IsRepeated { get; set; }

    /// <summary>
    /// 重复字段类型: "repeated" (List with initialization), "repeatedArray" (Array), "repeatedList" (List without initialization)
    /// </summary>
    public RepeatedFieldType RepeatedType { get; set; } = RepeatedFieldType.None;

    /// <summary>
    /// XML 注释文档
    /// </summary>
    public List<string> DocumentationComments { get; set; } = new();

    /// <summary>
    /// 源文件行号 (用于错误报告)
    /// </summary>
    public int SourceLineNumber { get; set; }
}

/// <summary>
/// 重复字段类型枚举
/// </summary>
public enum RepeatedFieldType
{
    /// <summary>
    /// 非重复字段
    /// </summary>
    None,

    /// <summary>
    /// repeated - List&lt;T&gt; with initialization (new List&lt;T&gt;())
    /// </summary>
    Repeated,

    /// <summary>
    /// repeatedArray - T[]
    /// </summary>
    RepeatedArray,

    /// <summary>
    /// repeatedList - List&lt;T&gt; without initialization
    /// </summary>
    RepeatedList
}
