using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Fantasy.ProtocolEditor.Models;

/// <summary>
/// 编辑器类型枚举
/// </summary>
public enum EditorType
{
    /// <summary>
    /// 文本编辑器（用于 .proto 等文本文件）
    /// </summary>
    TextEditor,

    /// <summary>
    /// 配置编辑器（用于 .Config 文件）
    /// </summary>
    ConfigEditor,

    /// <summary>
    /// 导出设置编辑器
    /// </summary>
    ExportSettings
}

/// <summary>
/// 编辑器标签页模型
/// </summary>
public partial class EditorTab : ObservableObject
{
    /// <summary>
    /// 文件完整路径
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// 文件名（显示在标签上）
    /// </summary>
    [ObservableProperty]
    private string _fileName = "Untitled";

    /// <summary>
    /// 文档对象
    /// </summary>
    [ObservableProperty]
    private TextDocument _document;

    /// <summary>
    /// 是否已修改
    /// </summary>
    [ObservableProperty]
    private bool _isModified = false;

    /// <summary>
    /// 编辑器类型
    /// </summary>
    [ObservableProperty]
    private EditorType _editorType = EditorType.TextEditor;

    /// <summary>
    /// 光标位置（用于切换标签时恢复）
    /// </summary>
    public int CaretOffset { get; set; } = 0;

    public EditorTab()
    {
        _document = new TextDocument();
    }

    public EditorTab(string filePath, string content)
    {
        _filePath = filePath;
        _fileName = System.IO.Path.GetFileName(filePath);
        _document = new TextDocument { Text = content };
        _editorType = EditorType.TextEditor;
    }

    public EditorTab(string filePath, EditorType editorType)
    {
        _filePath = filePath;
        _fileName = System.IO.Path.GetFileName(filePath);
        _document = new TextDocument();
        _editorType = editorType;
    }
}
