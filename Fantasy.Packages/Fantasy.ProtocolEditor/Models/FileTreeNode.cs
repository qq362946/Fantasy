using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Fantasy.ProtocolEditor.Models;

/// <summary>
/// 文件树节点
/// </summary>
public partial class FileTreeNode : ObservableObject
{
    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 完整路径（文件夹或文件）
    /// </summary>
    [ObservableProperty]
    private string _fullPath = string.Empty;

    /// <summary>
    /// 是否是文件夹
    /// </summary>
    [ObservableProperty]
    private bool _isFolder;

    /// <summary>
    /// 是否展开
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// 文件是否存在（用于固定显示但可能不存在的节点）
    /// </summary>
    [ObservableProperty]
    private bool _exists = true;

    /// <summary>
    /// 子节点
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FileTreeNode> _children = new();

    /// <summary>
    /// 节点类型（用于区分固定节点和普通节点）
    /// </summary>
    [ObservableProperty]
    private FileTreeNodeType _nodeType = FileTreeNodeType.Normal;

    public FileTreeNode()
    {
    }

    public FileTreeNode(string name, string fullPath, bool isFolder, FileTreeNodeType nodeType = FileTreeNodeType.Normal)
    {
        _name = name;
        _fullPath = fullPath;
        _isFolder = isFolder;
        _nodeType = nodeType;
        _isExpanded = isFolder; // 文件夹默认展开
    }
}

/// <summary>
/// 文件树节点类型
/// </summary>
public enum FileTreeNodeType
{
    /// <summary>
    /// 普通节点（从文件系统读取）
    /// </summary>
    Normal,

    /// <summary>
    /// 固定节点（即使不存在也显示）
    /// </summary>
    Fixed
}
