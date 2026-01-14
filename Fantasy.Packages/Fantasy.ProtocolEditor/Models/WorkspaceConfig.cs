using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fantasy.ProtocolEditor.Models;

/// <summary>
/// 工作区配置，用于保存和恢复工作区状态
/// </summary>
public class WorkspaceConfig
{
    /// <summary>
    /// 工作区路径
    /// </summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>
    /// 打开的标签页列表
    /// </summary>
    public List<TabConfig> OpenedTabs { get; set; } = new();

    /// <summary>
    /// 当前激活的标签页路径
    /// </summary>
    public string ActiveTabFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 服务器输出目录
    /// </summary>
    [JsonPropertyName("serverOutputDirectory")]
    public string ServerOutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 客户端输出目录
    /// </summary>
    [JsonPropertyName("clientOutputDirectory")]
    public string ClientOutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 是否导出到服务器
    /// </summary>
    [JsonPropertyName("exportToServer")]
    public bool ExportToServer { get; set; } = true;

    /// <summary>
    /// 是否导出到客户端
    /// </summary>
    [JsonPropertyName("exportToClient")]
    public bool ExportToClient { get; set; } = true;
}

/// <summary>
/// 标签页配置
/// </summary>
public class TabConfig
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 光标位置
    /// </summary>
    public int CaretOffset { get; set; }

    /// <summary>
    /// 编辑器类型
    /// </summary>
    public EditorType EditorType { get; set; } = EditorType.TextEditor;
}
