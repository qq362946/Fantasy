using System.Collections.Generic;

namespace Fantasy.ProtocolEditor.Models;

/// <summary>
/// 编辑器本地状态。项目与导出配置统一保存在 ExporterSettings.json 中。
/// </summary>
public class EditorState
{
    /// <summary>
    /// 打开的标签页列表。
    /// </summary>
    public List<TabConfig> OpenedTabs { get; set; } = new();

    /// <summary>
    /// 当前激活的标签页路径。
    /// </summary>
    public string ActiveTabFilePath { get; set; } = string.Empty;
}

public class TabConfig
{
    public string FilePath { get; set; } = string.Empty;
    public int CaretOffset { get; set; }
    public EditorType EditorType { get; set; } = EditorType.TextEditor;
}
