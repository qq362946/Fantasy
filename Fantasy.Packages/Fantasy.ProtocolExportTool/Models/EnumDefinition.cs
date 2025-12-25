using System.Collections.Generic;

namespace Fantasy.ProtocolExportTool.Models;

/// <summary>
/// 枚举值定义
/// </summary>
public sealed class EnumValueDefinition
{
    /// <summary>
    /// 枚举值名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值（数字）
    /// </summary>
    public int Value { get; set; }

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
/// 表示一个枚举的完整定义
/// </summary>
public sealed class EnumDefinition
{
    /// <summary>
    /// 枚举名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值列表
    /// </summary>
    public List<EnumValueDefinition> Values { get; set; } = new();

    /// <summary>
    /// XML 注释文档
    /// </summary>
    public List<string> DocumentationComments { get; set; } = new();

    /// <summary>
    /// 源文件路径 (用于错误报告)
    /// </summary>
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 源文件行号 (用于错误报告)
    /// </summary>
    public int SourceLineNumber { get; set; }
}
