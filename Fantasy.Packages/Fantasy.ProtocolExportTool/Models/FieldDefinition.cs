using System;
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
    /// 字段集合类型 (普通字段、List、Array、Map 等)
    /// </summary>
    public FieldCollectionType CollectionType { get; set; } = FieldCollectionType.None;

    /// <summary>
    /// Map 的 Key 类型 (仅当 CollectionType = Map 时有效)
    /// </summary>
    public string MapKeyType { get; set; } = string.Empty;

    /// <summary>
    /// Map 的 Value 类型 (仅当 CollectionType = Map 时有效)
    /// </summary>
    public string MapValueType { get; set; } = string.Empty;

    /// <summary>
    /// XML 注释文档
    /// </summary>
    public List<string> DocumentationComments { get; set; } = new();

    /// <summary>
    /// 源文件行号 (用于错误报告)
    /// </summary>
    public int SourceLineNumber { get; set; }

    /// <summary>
    /// 是否是重复字段 (包括 Repeated、RepeatedList、RepeatedArray)
    /// </summary>
    public bool IsRepeated => (CollectionType & FieldCollectionType.IsRepeated) != 0;

    /// <summary>
    /// 是否是 Map 类型字段
    /// </summary>
    public bool IsMap => CollectionType.HasFlag(FieldCollectionType.Map);

    /// <summary>
    /// 是否需要初始化 (Repeated 和 Map 需要初始化)
    /// </summary>
    public bool NeedsInitialization => CollectionType == FieldCollectionType.Repeated || IsMap;
}

/// <summary>
/// 字段集合类型枚举
/// </summary>
[Flags]
public enum FieldCollectionType : byte
{
    /// <summary>
    /// 普通单值字段
    /// </summary>
    None = 0,
    /// <summary>
    /// 普通类型
    /// </summary>
    Normal = 1 << 0,
    /// <summary>
    /// Map/Dictionary 容器类型
    /// </summary>
    Map = 1 << 1,    
    /// <summary>
    /// 重复字段标志 
    /// </summary>
    Repeated = 1 << 2, 
    /// <summary>
    /// List 容器类型 (不带初始化)
    /// </summary>
    RepeatedList = 1 << 3,
    /// <summary>
    /// Array 容器类型
    /// </summary>
    RepeatedArray = 1 << 4,
    /// <summary>
    /// 是否是重复字段标志 (用于 repeated、repeatedList、repeatedArray)
    /// </summary>
    IsRepeated = Repeated | RepeatedList | RepeatedArray,  
}
