using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantasy.ProtocolEditor.Models;
using Fantasy.ProtocolEditor.Services;

namespace Fantasy.ProtocolEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _sidebarWidth = "220";

    [ObservableProperty]
    private bool _isBottomPanelVisible = true;

    [ObservableProperty]
    private string _bottomPanelHeight = "180";

    [ObservableProperty]
    private string _outputText = "Fantasy Protocol Editor 已初始化...\n准备编辑协议文件。\n";

    [ObservableProperty]
    private TextDocument _editorDocument;

    [ObservableProperty]
    private bool _isEditorVisible = false;

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    /// <summary>
    /// 打开的标签页集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<EditorTab> _openedTabs = new();

    /// <summary>
    /// 当前激活的标签页
    /// </summary>
    [ObservableProperty]
    private EditorTab? _activeTab;

    /// <summary>
    /// RoamingConfig ViewModel
    /// </summary>
    [ObservableProperty]
    private RoamingConfigViewModel _roamingConfigViewModel = new();

    /// <summary>
    /// RouteConfig ViewModel
    /// </summary>
    [ObservableProperty]
    private RouteConfigViewModel _routeConfigViewModel = new();

    /// <summary>
    /// ExportSettings ViewModel
    /// </summary>
    [ObservableProperty]
    private ExportSettingsViewModel _exportSettingsViewModel = new();

    /// <summary>
    /// 是否显示配置编辑器（而不是文本编辑器）
    /// </summary>
    [ObservableProperty]
    private bool _isConfigEditorVisible;

    /// <summary>
    /// 工作区路径
    /// </summary>
    [ObservableProperty]
    private string _workspacePath = string.Empty;

    /// <summary>
    /// 文件树根节点集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FileTreeNode> _fileTreeNodes = new();

    /// <summary>
    /// 协议导出服务
    /// </summary>
    private readonly ProtocolExportService _exportService = new();

    public MainWindowViewModel()
    {
        // 初始化编辑器文档
        _editorDocument = new TextDocument();

        // 初始化默认的文件树结构（空状态）
        InitializeDefaultFileTree();

        // 注意：不在构造函数中加载配置，而是在 MainWindow 完全加载后调用
        // LoadWorkspaceConfig 将由 MainWindow.Loaded 事件触发
    }

    /// <summary>
    /// 初始化默认的文件树结构（无工作区时显示）
    /// </summary>
    private void InitializeDefaultFileTree()
    {
        FileTreeNodes.Clear();

        // Inner 固定节点
        var innerNode = new FileTreeNode("Inner", string.Empty, true, FileTreeNodeType.Fixed)
        {
            IsExpanded = true
        };
        FileTreeNodes.Add(innerNode);

        // Outer 固定节点
        var outerNode = new FileTreeNode("Outer", string.Empty, true, FileTreeNodeType.Fixed)
        {
            IsExpanded = true
        };
        FileTreeNodes.Add(outerNode);

        // RoamingType.Config 固定节点
        var roamingNode = new FileTreeNode("RoamingType.Config", string.Empty, false, FileTreeNodeType.Fixed)
        {
            Exists = false
        };
        FileTreeNodes.Add(roamingNode);

        // RouteType.Config 固定节点
        var routeNode = new FileTreeNode("RouteType.Config", string.Empty, false, FileTreeNodeType.Fixed)
        {
            Exists = false
        };
        FileTreeNodes.Add(routeNode);
    }

    /// <summary>
    /// 加载工作区文件夹
    /// </summary>
    public void LoadWorkspaceFolder(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                OutputText += $"错误：未找到文件夹：{folderPath}\n";
                return;
            }

            WorkspacePath = folderPath;
            FileTreeNodes.Clear();

            // Inner 固定节点
            var innerPath = Path.Combine(folderPath, "Inner");
            var innerNode = new FileTreeNode("Inner", innerPath, true, FileTreeNodeType.Fixed)
            {
                IsExpanded = true,
                Exists = Directory.Exists(innerPath)
            };

            // 扫描 Inner 文件夹下的 .proto 文件
            if (Directory.Exists(innerPath))
            {
                var innerProtoFiles = Directory.GetFiles(innerPath, "*.proto");
                foreach (var protoFile in innerProtoFiles)
                {
                    var fileName = Path.GetFileName(protoFile);
                    innerNode.Children.Add(new FileTreeNode(fileName, protoFile, false));
                }
            }
            FileTreeNodes.Add(innerNode);

            // Outer 固定节点
            var outerPath = Path.Combine(folderPath, "Outer");
            var outerNode = new FileTreeNode("Outer", outerPath, true, FileTreeNodeType.Fixed)
            {
                IsExpanded = true,
                Exists = Directory.Exists(outerPath)
            };

            // 扫描 Outer 文件夹下的 .proto 文件
            if (Directory.Exists(outerPath))
            {
                var outerProtoFiles = Directory.GetFiles(outerPath, "*.proto");
                foreach (var protoFile in outerProtoFiles)
                {
                    var fileName = Path.GetFileName(protoFile);
                    outerNode.Children.Add(new FileTreeNode(fileName, protoFile, false));
                }
            }
            FileTreeNodes.Add(outerNode);

            // RoamingType.Config 固定节点
            var roamingConfigPath = Path.Combine(folderPath, "RoamingType.Config");
            var roamingNode = new FileTreeNode("RoamingType.Config", roamingConfigPath, false, FileTreeNodeType.Fixed)
            {
                Exists = File.Exists(roamingConfigPath)
            };
            FileTreeNodes.Add(roamingNode);

            // RouteType.Config 固定节点
            var routeConfigPath = Path.Combine(folderPath, "RouteType.Config");
            var routeNode = new FileTreeNode("RouteType.Config", routeConfigPath, false, FileTreeNodeType.Fixed)
            {
                Exists = File.Exists(routeConfigPath)
            };
            FileTreeNodes.Add(routeNode);

            OutputText += $"已加载工作区：{folderPath}\n";
            OutputText += $"  Inner：{innerNode.Children.Count} 个协议文件\n";
            OutputText += $"  Outer：{outerNode.Children.Count} 个协议文件\n";
            OutputText += $"  RoamingType.Config：{(roamingNode.Exists ? "已存在" : "未找到")}\n";
            OutputText += $"  RouteType.Config：{(routeNode.Exists ? "已存在" : "未找到")}\n";
        }
        catch (Exception ex)
        {
            OutputText += $"加载工作区时出错：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 当 ActiveTab 改变时触发
    /// </summary>
    partial void OnActiveTabChanged(EditorTab? value)
    {
        if (value != null)
        {
            if (value.EditorType == EditorType.ConfigEditor)
            {
                // 切换到配置编辑器标签时，重新加载配置
                var fileName = Path.GetFileName(value.FilePath);
                if (fileName == "RoamingType.Config")
                {
                    RoamingConfigViewModel.LoadConfig(value.FilePath);
                }
                else if (fileName == "RouteType.Config")
                {
                    RouteConfigViewModel.LoadConfig(value.FilePath);
                }
            }
            else if (value.EditorType == EditorType.ExportSettings)
            {
                // 切换到导出设置标签时，加载导出配置
                var config = ConfigService.LoadConfig() ?? new WorkspaceConfig
                {
                    WorkspacePath = WorkspacePath
                };
                ExportSettingsViewModel.LoadSettings(config);
            }
        }
    }

    /// <summary>
    /// 打开文件（如果已打开则激活，否则创建新标签）
    /// </summary>
    public void OpenFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                OutputText += $"错误：未找到文件：{filePath}\n";
                return;
            }

            // 检查文件是否已经打开
            var existingTab = OpenedTabs.FirstOrDefault(t => t.FilePath == filePath);
            if (existingTab != null)
            {
                // 切换到已存在的标签
                ActiveTab = existingTab;
                return;
            }

            // 检查是否是配置文件
            var fileName = Path.GetFileName(filePath);
            if (fileName == "RoamingType.Config" || fileName == "RouteType.Config")
            {
                // 创建配置编辑器类型的标签页
                var configTab = new EditorTab(filePath, EditorType.ConfigEditor);
                OpenedTabs.Add(configTab);

                // 激活新标签
                ActiveTab = configTab;

                // 加载配置到对应的 ViewModel
                if (fileName == "RoamingType.Config")
                {
                    RoamingConfigViewModel.LoadConfig(filePath);
                }
                else if (fileName == "RouteType.Config")
                {
                    RouteConfigViewModel.LoadConfig(filePath);
                }

                return;
            }

            // 读取文件内容
            string content = File.ReadAllText(filePath);

            // 创建文本编辑器类型的标签页
            var newTab = new EditorTab(filePath, content);
            OpenedTabs.Add(newTab);

            // 激活新标签
            ActiveTab = newTab;
        }
        catch (Exception ex)
        {
            OutputText += $"打开文件时出错：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 关闭标签
    /// </summary>
    public void CloseTab(EditorTab tab)
    {
        if (tab == null) return;

        var index = OpenedTabs.IndexOf(tab);
        OpenedTabs.Remove(tab);

        // 如果关闭的是当前标签，切换到其他标签
        if (ActiveTab == tab)
        {
            if (OpenedTabs.Count > 0)
            {
                // 优先激活后一个，否则激活前一个
                ActiveTab = index < OpenedTabs.Count ? OpenedTabs[index] : OpenedTabs[OpenedTabs.Count - 1];
            }
            else
            {
                ActiveTab = null;
            }
        }
    }

    /// <summary>
    /// 加载文件到编辑器（兼容旧代码）
    /// </summary>
    public void LoadFile(string filePath)
    {
        OpenFile(filePath);
    }

    [RelayCommand]
    private void NewFile()
    {
        EditorDocument.Text = string.Empty;
        IsEditorVisible = true;
        CurrentFilePath = string.Empty;
        OutputText += "正在创建新协议文件...\n";
    }

    [RelayCommand]
    private void OpenFolder()
    {
        OutputText += "正在打开文件夹对话框...\n";
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            OutputText += "没有可保存的文件。请先使用「另存为」或打开一个文件。\n";
            return;
        }

        OutputText += "保存命令已触发。请从窗口中保存。\n";
        // 注意：实际保存操作需要在 MainWindow.axaml.cs 中实现
        // 因为需要访问 TextEditor 控件
    }

    [RelayCommand]
    private async Task ExportProtocols()
    {
        try
        {
            // 协议目录直接使用工作区路径
            if (string.IsNullOrWhiteSpace(WorkspacePath))
            {
                OutputText += "[错误] 请先打开工作区文件夹\n";
                return;
            }

            // 加载配置
            var workspaceConfig = ConfigService.LoadConfig() ?? new WorkspaceConfig
            {
                WorkspacePath = WorkspacePath
            };

            // 验证服务器和客户端输出目录是否已配置
            var exportToServer = workspaceConfig.ExportToServer;
            var exportToClient = workspaceConfig.ExportToClient;

            var hasServerConfig = !string.IsNullOrWhiteSpace(workspaceConfig.ServerOutputDirectory);
            var hasClientConfig = !string.IsNullOrWhiteSpace(workspaceConfig.ClientOutputDirectory);

            // 如果两个输出目录都没有配置，说明是第一次运行
            if (!hasServerConfig && !hasClientConfig)
            {
                OutputText += "[提示] 首次使用协议导出功能，需要先配置导出路径\n";
                OutputText += "[提示] 使用菜单「文件 → 导出设置」配置导出路径\n";
                OutputText += "\n";

                // 自动打开导出设置对话框
                OpenExportSettings();
                return;
            }

            // 如果配置了部分目录，显示警告
            if (!hasServerConfig)
            {
                OutputText += "[警告] 未配置服务器输出目录，将跳过服务器导出\n";
                exportToServer = false;
            }

            if (!hasClientConfig)
            {
                OutputText += "[警告] 未配置客户端输出目录，将跳过客户端导出\n";
                exportToClient = false;
            }

            if (!exportToServer && !exportToClient)
            {
                OutputText += "[错误] 请至少配置一个输出目录（服务器或客户端）\n";
                OutputText += "[提示] 使用菜单「文件 → 导出设置」配置导出路径\n";
                return;
            }

            // 构建导出配置
            var exportType = Fantasy.ProtocolExportTool.Models.ProtocolExportType.All;
            if (!exportToServer)
            {
                exportType = Fantasy.ProtocolExportTool.Models.ProtocolExportType.Client;
            }
            else if (!exportToClient)
            {
                exportType = Fantasy.ProtocolExportTool.Models.ProtocolExportType.Server;
            }

            var config = new Fantasy.ProtocolExportTool.Models.ProtocolExportConfig
            {
                ProtocolDir = WorkspacePath, // 使用工作区路径作为协议目录
                ServerDir = workspaceConfig.ServerOutputDirectory,
                ClientDir = workspaceConfig.ClientOutputDirectory,
                ExportType = exportType
            };

            // 验证配置
            var (isValid, errorMsg) = _exportService.ValidateConfig(config);
            if (!isValid)
            {
                OutputText += $"[错误] {errorMsg}\n";
                return;
            }

            // 执行导出
            var (success, error) = await _exportService.ExportAsync(config, msg =>
            {
                OutputText += $"{msg}\n";
            });
        }
        catch (Exception ex)
        {
            OutputText += $"[异常] 导出过程中发生错误: {ex.Message}\n";
        }
    }

    [RelayCommand]
    private void ValidateProtocols()
    {
        OutputText += "正在验证协议文件...\n";
    }

    [RelayCommand]
    private void OpenExportSettings()
    {
        // 检查是否已经打开了导出设置标签
        var existingTab = OpenedTabs.FirstOrDefault(t => t.EditorType == EditorType.ExportSettings);
        if (existingTab != null)
        {
            // 切换到已存在的标签
            ActiveTab = existingTab;
            return;
        }

        // 创建新的导出设置标签
        var exportTab = new EditorTab("导出设置", EditorType.ExportSettings)
        {
            FileName = "导出设置"
        };
        OpenedTabs.Add(exportTab);

        // 激活新标签
        ActiveTab = exportTab;

        // 加载导出配置
        var config = ConfigService.LoadConfig() ?? new WorkspaceConfig
        {
            WorkspacePath = WorkspacePath
        };
        ExportSettingsViewModel.LoadSettings(config);
    }

    /// <summary>
    /// 加载工作区配置
    /// </summary>
    public void LoadWorkspaceConfig()
    {
        try
        {
            var config = ConfigService.LoadConfig();
            if (config == null)
                return;

            // 恢复工作区路径
            if (!string.IsNullOrEmpty(config.WorkspacePath) && Directory.Exists(config.WorkspacePath))
            {
                LoadWorkspaceFolder(config.WorkspacePath);

                // 先收集所有要恢复的标签页信息
                var tabsToRestore = new List<(string FilePath, int CaretOffset)>();
                foreach (var tabConfig in config.OpenedTabs)
                {
                    if (File.Exists(tabConfig.FilePath))
                    {
                        tabsToRestore.Add((tabConfig.FilePath, tabConfig.CaretOffset));
                    }
                }

                // 恢复打开的标签页
                foreach (var tabInfo in tabsToRestore)
                {
                    OpenFile(tabInfo.FilePath);

                    // 恢复光标位置
                    var tab = OpenedTabs.FirstOrDefault(t => t.FilePath == tabInfo.FilePath);
                    if (tab != null)
                    {
                        tab.CaretOffset = tabInfo.CaretOffset;
                    }
                }

                // 恢复激活的标签页
                if (!string.IsNullOrEmpty(config.ActiveTabFilePath))
                {
                    var activeTab = OpenedTabs.FirstOrDefault(t => t.FilePath == config.ActiveTabFilePath);
                    if (activeTab != null)
                    {
                        ActiveTab = activeTab;
                    }
                    else if (OpenedTabs.Count > 0)
                    {
                        // 如果之前激活的标签页不存在，激活第一个
                        ActiveTab = OpenedTabs[0];
                    }
                }

                OutputText += "工作区配置加载成功。\n";
            }
        }
        catch (Exception ex)
        {
            OutputText += $"加载工作区配置失败：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 保存工作区配置
    /// </summary>
    public void SaveWorkspaceConfig()
    {
        try
        {
            // 先加载现有配置，保留导出设置
            var config = ConfigService.LoadConfig() ?? new WorkspaceConfig();

            // 更新工作区路径
            config.WorkspacePath = WorkspacePath;

            // 清空并重新保存打开的标签页
            config.OpenedTabs.Clear();
            foreach (var tab in OpenedTabs)
            {
                config.OpenedTabs.Add(new TabConfig
                {
                    FilePath = tab.FilePath,
                    CaretOffset = tab.CaretOffset,
                    EditorType = tab.EditorType
                });
            }

            // 保存激活的标签页
            if (ActiveTab != null)
            {
                config.ActiveTabFilePath = ActiveTab.FilePath;
            }

            // 保存配置（会保留导出设置字段）
            ConfigService.SaveConfig(config);
        }
        catch (Exception ex)
        {
            OutputText += $"保存工作区配置失败：{ex.Message}\n";
        }
    }
}