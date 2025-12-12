using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.CodeCompletion;
using Fantasy.ProtocolEditor.ViewModels;
using Fantasy.ProtocolEditor.Services;
using Avalonia.Media;
using Fantasy.ProtocolEditor.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Fantasy.ProtocolEditor.Views;

public partial class MainWindow : Window
{
    private TextMarkerService? _textMarkerService;
    private CurrentLineHighlightRenderer? _currentLineRenderer;
    private readonly ProtobufValidator _validator = new();
    private DispatcherTimer? _validationTimer;
    private readonly CompletionService _completionService = new();
    private CompletionWindow? _completionWindow;

    // 拖拽相关
    private Point _dragStartPoint;
    private bool _isDragging = false;
    private Models.EditorTab? _draggingTab = null;

    // 查找相关
    private List<int> _findResults = new();
    private int _currentFindIndex = -1;

    // 上次验证错误数量
    private int _lastErrorCount = 0;
    // 上次错误输出的起始位置（用于删除旧错误信息）
    private int _lastErrorOutputPosition = -1;

    // 当前打开的右键菜单（用于防止多个菜单同时显示）
    private ContextMenu? _currentContextMenu = null;

    public MainWindow()
    {
        InitializeComponent();

        // 添加快捷键支持
        this.KeyDown += OnKeyDown;

        // 初始化验证计时器（延迟验证，避免频繁验证）
        _validationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _validationTimer.Tick += OnValidationTimerTick;

        // 监听 ActiveTab 变化
        this.DataContextChanged += OnDataContextChanged;

        // 添加全局拖放事件处理
        AddHandler(DragDrop.DragOverEvent, OnTabDragOver);
        AddHandler(DragDrop.DropEvent, OnTabDrop);

        // 添加窗口关闭事件，保存配置
        this.Closing += OnWindowClosing;

        // 添加窗口加载完成事件，加载配置
        this.Loaded += OnWindowLoaded;
    }

    /// <summary>
    /// 窗口加载完成事件，加载工作区配置
    /// </summary>
    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        // 在窗口和所有控件完全加载后，再加载配置
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.LoadWorkspaceConfig();
                // 设置 TreeView 的容器准备事件，用于同步 IsExpanded 状态
                SetupTreeViewItemBinding();
            }
        }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// 设置 TreeViewItem 的 IsExpanded 绑定
    /// </summary>
    private void SetupTreeViewItemBinding()
    {
        if (FileTreeView == null) return;

        // 监听 TreeView 的 Loaded 事件，确保容器已生成
        FileTreeView.Loaded += (s, e) =>
        {
            SyncTreeViewItemExpansion(FileTreeView);
        };
    }

    /// <summary>
    /// 递归同步 TreeViewItem 的 IsExpanded 状态
    /// </summary>
    private void SyncTreeViewItemExpansion(Control control)
    {
        if (control is TreeViewItem treeViewItem && treeViewItem.DataContext is FileTreeNode node)
        {
            // 同步展开状态
            treeViewItem.IsExpanded = node.IsExpanded;

            // 监听节点的 PropertyChanged 事件
            node.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(FileTreeNode.IsExpanded))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (treeViewItem.IsExpanded != node.IsExpanded)
                        {
                            treeViewItem.IsExpanded = node.IsExpanded;
                        }
                    });
                }
            };

            // 监听 TreeViewItem 的 IsExpanded 变化，同步回模型
            treeViewItem.PropertyChanged += (s, args) =>
            {
                if (args.Property == TreeViewItem.IsExpandedProperty)
                {
                    var isExpanded = (bool)(args.NewValue ?? false);
                    if (node.IsExpanded != isExpanded)
                    {
                        node.IsExpanded = isExpanded;
                    }
                }
            };
        }

        // 递归处理子控件
        foreach (var child in control.GetVisualChildren())
        {
            if (child is Control childControl)
            {
                SyncTreeViewItemExpansion(childControl);
            }
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ActiveTab))
                {
                    OnActiveTabChanged(vm.ActiveTab);
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.OutputText))
                {
                    // 输出文本改变时，自动滚动到底部
                    Dispatcher.UIThread.Post(() =>
                    {
                        OutputScrollViewer?.ScrollToEnd();
                    }, DispatcherPriority.Background);
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.FileTreeNodes))
                {
                    // 文件树变化时，重新同步 TreeViewItem 的展开状态
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (FileTreeView != null)
                        {
                            SyncTreeViewItemExpansion(FileTreeView);
                        }
                    }, DispatcherPriority.Background);
                }
            };
        }
    }

    /// <summary>
    /// 激活标签变化时，切换编辑器文档
    /// </summary>
    private void OnActiveTabChanged(Models.EditorTab? tab)
    {
        if (tab == null)
            return;

        // 如果 TextEditor 还没初始化，延迟执行
        if (TextEditor == null || TextEditor.Document == null)
        {
            Dispatcher.UIThread.Post(() => OnActiveTabChanged(tab), Avalonia.Threading.DispatcherPriority.Loaded);
            return;
        }

        // 保存当前标签的光标位置
        if (DataContext is MainWindowViewModel vm)
        {
            foreach (var openTab in vm.OpenedTabs)
            {
                if (openTab != tab && openTab.Document == TextEditor.Document)
                {
                    openTab.CaretOffset = TextEditor.CaretOffset;
                    break;
                }
            }
        }

        // 取消订阅旧文档的事件
        if (TextEditor.Document != null)
        {
            TextEditor.Document.TextChanged -= OnDocumentTextChanged;
        }

        // 切换到新标签的文档
        TextEditor.Document = tab.Document;

        // 重置撤销栈，避免 Ctrl+Z 清空文本
        TextEditor.Document.UndoStack.ClearAll();

        // 订阅新文档的文本变化事件
        if (TextEditor.Document != null)
        {
            TextEditor.Document.TextChanged += OnDocumentTextChanged;
        }

        // 恢复光标位置
        TextEditor.CaretOffset = tab.CaretOffset;

        // 更新 ViewModel 当前文件路径
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CurrentFilePath = tab.FilePath;
        }

        // 更新标签样式
        Dispatcher.UIThread.Post(() => UpdateTabStyles());

        // 重新初始化当前行高亮和错误标记服务
        if (_textMarkerService != null && TextEditor.Document != null)
        {
            _textMarkerService = new TextMarkerService(TextEditor.Document);
            TextEditor.TextArea.TextView.BackgroundRenderers.Clear();
            TextEditor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);

            if (_currentLineRenderer != null)
            {
                TextEditor.TextArea.TextView.BackgroundRenderers.Add(_currentLineRenderer);
            }
        }

        // 重置错误输出位置（切换标签时）
        _lastErrorOutputPosition = -1;
        _lastErrorCount = 0;

        // 验证新文档
        ValidateDocument();
    }

    /// <summary>
    /// TextEditor 加载完成事件
    /// </summary>
    private void TextEditor_Loaded(object? sender, RoutedEventArgs e)
    {
        if (TextEditor != null && TextEditor.TextArea != null)
        {
            // 订阅 TextArea 的 Loaded 事件
            TextEditor.TextArea.Loaded += TextArea_Loaded;

            // 如果 TextArea 已经加载了，直接初始化
            if (TextEditor.TextArea.IsLoaded)
            {
                InitializeTextEditor();
            }
        }
    }

    /// <summary>
    /// TextArea 加载完成后初始化
    /// </summary>
    private void TextArea_Loaded(object? sender, RoutedEventArgs e)
    {
        InitializeTextEditor();
    }

    /// <summary>
    /// 初始化 TextEditor（创建空文档并设置语法高亮）
    /// </summary>
    private void InitializeTextEditor()
    {
        if (TextEditor == null) return;

        // 创建空文档
        var document = new AvaloniaEdit.Document.TextDocument();
        document.Text = "// 从左侧面板选择一个 .proto 文件进行编辑";
        TextEditor.Document = document;

        // 加载自定义 Protobuf 语法高亮
        LoadProtobufSyntaxHighlighting();

        // 初始化错误标记服务
        _textMarkerService = new TextMarkerService(document);
        TextEditor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);

        // 初始化当前行高亮渲染器（垂直内边距 4px）
        _currentLineRenderer = new CurrentLineHighlightRenderer(TextEditor, verticalPadding: 4);
        TextEditor.TextArea.TextView.BackgroundRenderers.Add(_currentLineRenderer);

        TextEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.Parse("#6599BB")); // 调整选中背景
        TextEditor.TextArea.SelectionForeground = new SolidColorBrush(Colors.White);      // 调整选中文本颜色

        // 订阅光标位置变化事件，重绘当前行
        TextEditor.TextArea.Caret.PositionChanged += (s, e) => TextEditor.TextArea.TextView.InvalidateVisual();

        // 订阅文本变化事件，触发语法验证
        document.TextChanged += OnDocumentTextChanged;

        // 订阅文本输入事件，触发代码补全
        TextEditor.TextArea.TextEntered += OnTextEntered;
        TextEditor.TextArea.TextEntering += OnTextEntering;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.OutputText += "编辑器已就绪。\n";
        }
    }

    /// <summary>
    /// 加载 Protobuf 语法高亮定义
    /// </summary>
    private void LoadProtobufSyntaxHighlighting()
    {
        if (TextEditor == null) return;

        try
        {
            // 从嵌入资源加载 .xshd 语法定义文件
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "Fantasy.ProtocolEditor.Resources.Protobuf.xshd";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.OutputText += "警告：无法加载 Protobuf.xshd 资源。\n";
                }
                return;
            }

            using var reader = new XmlTextReader(stream);
            var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            // 应用语法高亮
            TextEditor.SyntaxHighlighting = definition;

            if (DataContext is MainWindowViewModel vm2)
            {
                vm2.OutputText += "Protobuf 语法高亮加载成功。\n";
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OutputText += $"警告：无法加载语法高亮：{ex.Message}\n";
            }
        }
    }

    /// <summary>
    /// 处理快捷键
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+O 或 Cmd+O 打开文件夹
        if ((e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta) && e.Key == Key.O)
        {
            OnOpenFolderMenuClick(null, new RoutedEventArgs());
            e.Handled = true;
        }
        // Ctrl+S 或 Cmd+S 保存
        else if ((e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta) && e.Key == Key.S)
        {
            SaveAllOpened();
            e.Handled = true;
        }
        // Ctrl+F 或 Cmd+F 查找
        else if ((e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta) && e.Key == Key.F)
        {
            ShowFindPanel(false);
            e.Handled = true;
        }
        // Ctrl+H 或 Cmd+H 替换
        else if ((e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta) && e.Key == Key.H)
        {
            ShowFindPanel(true);
            e.Handled = true;
        }
        // Ctrl+G 或 Cmd+G 跳转到行
        else if ((e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta) && e.Key == Key.G)
        {
            ShowGoToLineDialog();
            e.Handled = true;
        }
        // Esc 关闭查找面板
        else if (e.Key == Key.Escape && FindPanel?.IsVisible == true)
        {
            CloseFindPanel();
            e.Handled = true;
        }
        // Ctrl+Z  撤销
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Z)
        {
            if (TextEditor.CanUndo)
                TextEditor.Undo();

            e.Handled = true;
            return;
        }
        // Ctrl+Y  重做
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Y)
        {
            if (TextEditor.CanRedo)
                TextEditor.Redo();

            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// 撤销
    /// </summary>
    private void OnUndoClick(object? sender, RoutedEventArgs e)
    {
        if (TextEditor.CanUndo)
            TextEditor.Undo();
    }

    /// <summary>
    /// 重做
    /// </summary>
    private void OnRedoClick(object? sender, RoutedEventArgs e)
    {
        if (TextEditor.CanRedo)
            TextEditor.Redo();
    }

    /// <summary>
    /// 保存
    /// </summary>
    private void OnSaveMenuClick(object? sender, RoutedEventArgs e)
    {
        SaveAllOpened(); 
    }

    /// <summary>
    /// 菜单打开文件夹点击事件
    /// </summary>
    private async void OnOpenFolderMenuClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folder = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Select Network Protocol Folder",
                AllowMultiple = false
            });

            if (folder.Count > 0 && DataContext is MainWindowViewModel viewModel)
            {
                var folderPath = folder[0].Path.LocalPath;
                viewModel.LoadWorkspaceFolder(folderPath);
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.OutputText += $"打开文件夹时出错：{ex.Message}\n";
            }
        }
    }

    /// <summary>
    /// 保存当前文件
    /// </summary>
    public void SaveTabFile(EditorTab tab,string outputSaveInfo = "已保存")
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (tab == null || string.IsNullOrEmpty(tab.FilePath))
        {
            viewModel.OutputText += "没有可保存的文件。\n";
            return;
        }

        try
        {
            if (TextEditor != null && TextEditor.Document != null && tab.IsModified)
            {
                System.IO.File.WriteAllText(tab.FilePath, TextEditor.Document.Text);
                tab.IsModified = false;
                if(!string.IsNullOrEmpty(outputSaveInfo))
                    viewModel.OutputText += $"{outputSaveInfo}：{tab.FileName}\n";
            }
        }
        catch (Exception ex)
        {
            viewModel.OutputText += $"保存文件时出错：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 保存所有打开的文件
    /// </summary>
    public void SaveAllOpened(string outputSaveInfo = "已保存")
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (viewModel.OpenedTabs == null || viewModel.OpenedTabs.Count == 0)
        {
            viewModel.OutputText += "没有可保存的文件。\n";
            return;
        }

        foreach (var tab in viewModel.OpenedTabs)
        {
            if (string.IsNullOrEmpty(tab.FilePath))
            {
                viewModel.OutputText += $"跳过未保存文件：{tab.FileName}(路径为空)\n";
                continue;
            }

            try
            {
                var textToSave = tab.Document?.Text;
                if (textToSave != null && tab.IsModified)
                {
                    System.IO.File.WriteAllText(tab.FilePath, textToSave);
                    tab.IsModified = false;
                    if (!string.IsNullOrEmpty(outputSaveInfo))
                        viewModel.OutputText += $"{outputSaveInfo}：{tab.FileName}\n";
                }
            }
            catch (Exception ex)
            {
                viewModel.OutputText += $"保存 {tab.FileName} 时出错：{ex.Message}\n";
            }
        }
    }


    /// <summary>
    /// 加载文件到编辑器
    /// </summary>
    private void LoadFileToEditor(string filePath)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // 调用 ViewModel 的 OpenFile 方法，会自动处理标签页
            viewModel.OpenFile(filePath);
        }
    }

    /// <summary>
    /// 文档文本变化事件 - 触发延迟验证
    /// </summary>
    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        // 重启计时器（防止频繁验证）
        _validationTimer?.Stop();
        _validationTimer?.Start();

        if (DataContext is MainWindowViewModel vm)
        {
            var activeTab = vm.ActiveTab;
            if (activeTab != null)
            {
                // 标记当前活动的标签页为已修改
                activeTab.IsModified = true;
            }
        }
    }

    /// <summary>
    /// 验证计时器 Tick - 执行语法验证
    /// </summary>
    private void OnValidationTimerTick(object? sender, EventArgs e)
    {
        _validationTimer?.Stop();
        ValidateDocument();
    }

    /// <summary>
    /// 验证文档语法
    /// </summary>
    private void ValidateDocument()
    {
        if (TextEditor?.Document == null || _textMarkerService == null)
            return;

        // 清除旧的错误标记
        _textMarkerService.Clear();

        // 执行验证
        var text = TextEditor.Document.Text;
        var errors = _validator.Validate(text);

        // 添加错误标记
        foreach (var error in errors)
        {
            _textMarkerService.AddMarker(error.Offset, error.Length, error.Message);
        }

        // 刷新视图
        TextEditor.TextArea.TextView.Redraw();

        // 更新 MainWindowViewModel 信息输出
        if (DataContext is MainWindowViewModel vm)
        {
            //// 输出未保存提示
            //foreach (Models.EditorTab tab in vm.OpenedTabs) {
            //    if (tab.Document == TextEditor.Document)
            //    {
            //        vm.OutputText += tab.IsModified?  $"*\n" : "";                    
            //    }
            //}

            // 如果之前有错误输出，先删除它
            if (_lastErrorOutputPosition >= 0 && _lastErrorOutputPosition < vm.OutputText.Length)
            {
                vm.OutputText = vm.OutputText.Substring(0, _lastErrorOutputPosition);
            }

            if (errors.Count > 0)
            {
                // 记录错误输出的起始位置
                _lastErrorOutputPosition = vm.OutputText.Length;

                vm.OutputText += $"发现 {errors.Count} 个语法错误\n";
                foreach (var error in errors)
                {
                    vm.OutputText += $"  第 {error.Line + 1} 行：{error.Message}\n";
                }
                _lastErrorCount = errors.Count;
            }
            else
            {
                // 没有错误，重置位置
                _lastErrorOutputPosition = -1;
                _lastErrorCount = 0;
            }
        }
    }

    /// <summary>
    /// 文本输入事件 - 触发代码补全
    /// </summary>
    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        // 如果补全窗口已经打开，不要干扰
        if (_completionWindow != null)
            return;

        if (TextEditor?.Document == null)
            return;

        // 只在输入字母时触发补全检查
        if (!string.IsNullOrEmpty(e.Text) && char.IsLetter(e.Text[0]))
        {
            // 检查是否应该触发补全
            if (_completionService.ShouldTriggerCompletion(TextEditor.Document, TextEditor.CaretOffset))
            {
                ShowCompletionWindow();
            }
        }
    }

    /// <summary>
    /// 文本输入前事件 - 处理补全窗口的特殊按键
    /// </summary>
    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        // 暂时禁用，让 CompletionWindow 自己处理
        // if (e.Text?.Length > 0 && _completionWindow != null)
        // {
        //     // 如果输入的不是字母，关闭补全窗口
        //     if (!char.IsLetterOrDigit(e.Text[0]))
        //     {
        //         _completionWindow.Close();
        //     }
        // }
    }

    /// <summary>
    /// 显示代码补全窗口
    /// </summary>
    private void ShowCompletionWindow()
    {
        if (TextEditor == null || TextEditor.Document == null)
            return;

        // 如果已经有补全窗口打开，先关闭
        _completionWindow?.Close();

        // 获取补全项
        var completions = _completionService.GetCompletions(TextEditor.Document, TextEditor.CaretOffset);

        if (completions.Count == 0)
            return;

        // 创建补全窗口 - 使用最简单的方式
        _completionWindow = new CompletionWindow(TextEditor.TextArea);

        // 添加补全项
        var data = _completionWindow.CompletionList.CompletionData;
        foreach (var completion in completions)
        {
            data.Add(completion);
        }

        // 计算最长文本的宽度并设置窗口宽度
        var maxTextLength = completions.Max(c => c.Text.Length);
        // 每个字符大约 8 像素，加上内边距和边框
        var estimatedWidth = Math.Max(200, Math.Min(600, maxTextLength * 8 + 40));

        _completionWindow.MinWidth = estimatedWidth;
        _completionWindow.MaxWidth = 600;  // 最大宽度限制
        _completionWindow.Width = estimatedWidth;

        // 显示窗口
        _completionWindow.Show();

        // 窗口关闭时清理引用
        _completionWindow.Closed += (sender, args) => _completionWindow = null;
    }

    /// <summary>
    /// 标签页按下事件 - 开始拖拽或点击
    /// </summary>
    private void OnTabPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not Border border)
            return;

        var tab = border.DataContext as Models.EditorTab;
        if (tab == null)
            return;

        // 记录拖拽起始点
        _dragStartPoint = e.GetPosition(border);
        _draggingTab = tab;

        e.Handled = false; // 不阻止，让子元素也能处理
    }

    /// <summary>
    /// 标签页移动事件 - 检测拖拽
    /// </summary>
    private async void OnTabPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (_draggingTab == null || _isDragging)
            return;

        var currentPoint = e.GetPosition(border);
        var diff = currentPoint - _dragStartPoint;

        // 如果移动距离超过阈值，开始拖拽
        if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
        {
            _isDragging = true;

#pragma warning disable CS0618 // Type or member is obsolete
            var dragData = new DataObject();
            dragData.Set("EditorTab", _draggingTab);

            await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
#pragma warning restore CS0618 // Type or member is obsolete

            _isDragging = false;
            _draggingTab = null;
        }
    }

    /// <summary>
    /// 标签页释放事件 - 处理点击
    /// </summary>
    private void OnTabPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (sender is not Border border)
            return;

        // 如果没有发生拖拽，则视为点击
        if (!_isDragging && _draggingTab != null)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ActiveTab = _draggingTab;
                UpdateTabStyles();
            }
        }

        _draggingTab = null;
    }

    /// <summary>
    /// 更新所有标签的激活状态样式
    /// </summary>
    private void UpdateTabStyles()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // 遍历所有标签，更新背景色
        var itemsControl = this.FindControl<ItemsControl>("TabItemsControl");
        if (itemsControl == null)
            return;

        for (int i = 0; i < vm.OpenedTabs.Count; i++)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is Control control)
            {
                var border = control.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                if (border != null)
                {
                    var tab = vm.OpenedTabs[i];
                    border.Background = tab == vm.ActiveTab
                        ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E"))
                        : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2B2B2B"));
                }
            }
        }
    }

    /// <summary>
    /// 查找指定类型的父级控件
    /// </summary>
    private T? FindAncestor<T>(Visual? visual) where T : Visual
    {
        while (visual != null)
        {
            if (visual is T ancestor)
                return ancestor;
            visual = visual.GetVisualParent();
        }
        return null;
    }

    /// <summary>
    /// 拖拽悬停事件 - 显示拖放反馈
    /// </summary>
    private void OnTabDragOver(object? sender, DragEventArgs e)
    {
        // 查找拖放目标的 Border
        Border? targetBorder = null;
        if (e.Source is Visual visual)
        {
            targetBorder = visual as Border ?? FindAncestor<Border>(visual);
        }

        if (targetBorder?.DataContext is Models.EditorTab)
        {
            // 检查是否包含 EditorTab 数据
#pragma warning disable CS0618 // Type or member is obsolete
            if (e.Data.Contains("EditorTab"))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// 拖拽放置事件 - 执行标签排序
    /// </summary>
    private void OnTabDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // 查找拖放目标的 Border
        Border? targetBorder = null;
        if (e.Source is Visual visual)
        {
            targetBorder = visual as Border ?? FindAncestor<Border>(visual);
        }

        if (targetBorder == null)
            return;

        // 获取拖拽的标签
#pragma warning disable CS0618 // Type or member is obsolete
        var draggedTab = e.Data.Get("EditorTab") as Models.EditorTab;
#pragma warning restore CS0618 // Type or member is obsolete
        if (draggedTab == null)
            return;

        // 获取放置目标的标签
        var targetTab = targetBorder.DataContext as Models.EditorTab;
        if (targetTab == null || draggedTab == targetTab)
            return;

        // 获取索引
        int draggedIndex = vm.OpenedTabs.IndexOf(draggedTab);
        int targetIndex = vm.OpenedTabs.IndexOf(targetTab);

        if (draggedIndex < 0 || targetIndex < 0)
            return;

        // 移动标签
        vm.OpenedTabs.Move(draggedIndex, targetIndex);

        // 更新样式
        Dispatcher.UIThread.Post(() => UpdateTabStyles());

        e.Handled = true;
    }

    /// <summary>
    /// 标签页关闭按钮点击事件
    /// </summary>
    private void OnTabCloseClick(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not TextBlock textBlock)
            return;

        var tab = textBlock.DataContext as Models.EditorTab;
        if (tab == null)
            return;

        if (DataContext is MainWindowViewModel vm)
        {
            SaveTabFile(tab,"自动保存");
            vm.CloseTab(tab);
        }

        e.Handled = true;
    }

    /// <summary>
    /// 菜单查找点击事件
    /// </summary>
    private void OnFindMenuClick(object? sender, RoutedEventArgs e)
    {
        ShowFindPanel(false);
    }

    /// <summary>
    /// 菜单替换点击事件
    /// </summary>
    private void OnReplaceMenuClick(object? sender, RoutedEventArgs e)
    {
        ShowFindPanel(true);
    }

    /// <summary>
    /// 显示查找面板
    /// </summary>
    private void ShowFindPanel(bool showReplace)
    {
        if (FindPanel == null || FindTextBox == null || ReplaceTextBox == null || ReplaceButtonPanel == null)
            return;

        FindPanel.IsVisible = true;
        ReplaceTextBox.IsVisible = showReplace;
        ReplaceButtonPanel.IsVisible = showReplace;

        FindTextBox.Focus();

        // 如果编辑器有选中文本，自动填充到查找框
        if (TextEditor?.SelectedText?.Length > 0)
        {
            FindTextBox.Text = TextEditor.SelectedText;
            FindTextBox.SelectAll();
        }
    }

    /// <summary>
    /// 关闭查找面板
    /// </summary>
    private void CloseFindPanel()
    {
        if (FindPanel == null)
            return;

        FindPanel.IsVisible = false;
        _findResults.Clear();
        _currentFindIndex = -1;

        // 清除高亮
        ClearFindHighlights();

        // 焦点返回编辑器
        TextEditor?.Focus();
    }

    /// <summary>
    /// 关闭查找面板按钮点击
    /// </summary>
    private void OnCloseFindPanelClick(object? sender, RoutedEventArgs e)
    {
        CloseFindPanel();
    }

    /// <summary>
    /// 查找文本框按键事件
    /// </summary>
    private void OnFindTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Enter - 查找下一个
            FindNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Esc - 关闭查找面板
            CloseFindPanel();
            e.Handled = true;
        }
        else
        {
            // 文本改变时重新查找
            Dispatcher.UIThread.Post(() => PerformFind(), DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 查找上一个按钮点击
    /// </summary>
    private void OnFindPreviousClick(object? sender, RoutedEventArgs e)
    {
        FindPrevious();
    }

    /// <summary>
    /// 查找下一个按钮点击
    /// </summary>
    private void OnFindNextClick(object? sender, RoutedEventArgs e)
    {
        FindNext();
    }

    /// <summary>
    /// 执行查找
    /// </summary>
    private void PerformFind()
    {
        if (TextEditor?.Document == null || FindTextBox == null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            _findResults.Clear();
            _currentFindIndex = -1;
            UpdateFindResultText();
            ClearFindHighlights();
            return;
        }

        var searchText = FindTextBox.Text;
        var documentText = TextEditor.Document.Text;
        var caseSensitive = CaseSensitiveCheckBox?.IsChecked ?? false;

        _findResults.Clear();

        // 查找所有匹配
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int index = 0;
        while ((index = documentText.IndexOf(searchText, index, comparison)) >= 0)
        {
            _findResults.Add(index);
            index += searchText.Length;
        }

        // 找到最近的匹配
        if (_findResults.Count > 0)
        {
            var currentOffset = TextEditor.CaretOffset;
            _currentFindIndex = _findResults.FindIndex(offset => offset >= currentOffset);
            if (_currentFindIndex < 0)
                _currentFindIndex = 0;

            HighlightCurrentFind();
        }
        else
        {
            _currentFindIndex = -1;
        }

        UpdateFindResultText();
    }

    /// <summary>
    /// 查找下一个
    /// </summary>
    private void FindNext()
    {
        if (_findResults.Count == 0)
        {
            PerformFind();
            return;
        }

        _currentFindIndex = (_currentFindIndex + 1) % _findResults.Count;
        HighlightCurrentFind();
        UpdateFindResultText();
    }

    /// <summary>
    /// 查找上一个
    /// </summary>
    private void FindPrevious()
    {
        if (_findResults.Count == 0)
        {
            PerformFind();
            return;
        }

        _currentFindIndex = (_currentFindIndex - 1 + _findResults.Count) % _findResults.Count;
        HighlightCurrentFind();
        UpdateFindResultText();
    }

    /// <summary>
    /// 高亮当前查找结果
    /// </summary>
    private void HighlightCurrentFind()
    {
        if (TextEditor?.Document == null || FindTextBox?.Text == null || _currentFindIndex < 0 || _currentFindIndex >= _findResults.Count)
            return;

        var offset = _findResults[_currentFindIndex];
        var length = FindTextBox.Text.Length;

        // 选中文本
        TextEditor.Select(offset, length);

        // 滚动到视图
        var line = TextEditor.Document.GetLineByOffset(offset);
        TextEditor.ScrollToLine(line.LineNumber);

        // 确保光标可见
        TextEditor.CaretOffset = offset;
    }

    /// <summary>
    /// 清除查找高亮
    /// </summary>
    private void ClearFindHighlights()
    {
        if (TextEditor?.Document == null)
            return;

        TextEditor.Select(0, 0);
    }

    /// <summary>
    /// 更新查找结果文本
    /// </summary>
    private void UpdateFindResultText()
    {
        if (FindResultText == null)
            return;

        if (_findResults.Count == 0)
        {
            FindResultText.Text = string.IsNullOrEmpty(FindTextBox?.Text) ? "" : "No results";
        }
        else
        {
            FindResultText.Text = $"{_currentFindIndex + 1} of {_findResults.Count}";
        }
    }

    /// <summary>
    /// 替换当前匹配
    /// </summary>
    private void OnReplaceClick(object? sender, RoutedEventArgs e)
    {
        if (TextEditor?.Document == null || FindTextBox == null || ReplaceTextBox == null)
            return;

        if (string.IsNullOrEmpty(FindTextBox.Text))
            return;

        if (_findResults.Count == 0)
        {
            PerformFind();
            if (_findResults.Count == 0)
                return;
        }

        if (_currentFindIndex < 0 || _currentFindIndex >= _findResults.Count)
            return;

        var offset = _findResults[_currentFindIndex];
        var length = FindTextBox.Text.Length;
        var replaceText = ReplaceTextBox.Text ?? "";

        // 执行替换
        TextEditor.Document.Replace(offset, length, replaceText);

        // 重新查找
        PerformFind();

        if (DataContext is MainWindowViewModel vm)
        {
            vm.OutputText += $"已替换 1 处匹配\n";
        }
    }

    /// <summary>
    /// 替换所有匹配
    /// </summary>
    private void OnReplaceAllClick(object? sender, RoutedEventArgs e)
    {
        if (TextEditor?.Document == null || FindTextBox == null || ReplaceTextBox == null)
            return;

        if (string.IsNullOrEmpty(FindTextBox.Text))
            return;

        // 先查找所有匹配
        PerformFind();

        if (_findResults.Count == 0)
            return;

        var searchText = FindTextBox.Text;
        var replaceText = ReplaceTextBox.Text ?? "";
        var count = _findResults.Count;

        // 从后往前替换，避免偏移量变化
        for (int i = _findResults.Count - 1; i >= 0; i--)
        {
            var offset = _findResults[i];
            TextEditor.Document.Replace(offset, searchText.Length, replaceText);
        }

        // 清除查找结果
        _findResults.Clear();
        _currentFindIndex = -1;
        UpdateFindResultText();
        ClearFindHighlights();

        if (DataContext is MainWindowViewModel vm)
        {
            vm.OutputText += $"已替换 {count} 处匹配\n";
        }
    }

    /// <summary>
    /// 文件树项目指针释放事件 - 处理左键单击
    /// </summary>
    private void OnFileTreeItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border border)
            return;

        var node = border.DataContext as Models.FileTreeNode;
        if (node == null)
            return;

        var point = e.GetCurrentPoint(sender as Control);

        // 只处理左键释放
        if (point.Properties.PointerUpdateKind != Avalonia.Input.PointerUpdateKind.LeftButtonReleased)
            return;

        // 如果是文件夹，切换展开/折叠
        if (node.IsFolder)
        {
            // 找到对应的 TreeViewItem 并切换展开状态
            var treeViewItem = FindTreeViewItem(FileTreeView, node);
            if (treeViewItem != null)
            {
                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
            }
            e.Handled = true;
            return;
        }

        // 如果是文件
        var filePath = node.FullPath;
        if (string.IsNullOrEmpty(filePath))
        {
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.OutputText += $"选中无效, 因文件路径不存在：{filePath}\n";
            e.Handled = true;
            return;
        }

        // 检查文件是否存在
        if (!node.Exists || !System.IO.File.Exists(filePath))
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.OutputText += $"文件不存在：{node.Name}\n";
                viewModel.OutputText += "您可以右键点击创建此文件。\n";
            }
            e.Handled = true;
            return;
        }

        // 加载文件
        LoadFileToEditor(filePath);
        e.Handled = true;
    }

    /// <summary>
    /// 查找对应节点的 TreeViewItem
    /// </summary>
    private TreeViewItem? FindTreeViewItem(Control? control, FileTreeNode targetNode)
    {
        if (control == null) return null;

        if (control is TreeViewItem treeViewItem && treeViewItem.DataContext == targetNode)
        {
            return treeViewItem;
        }

        // 递归查找子控件
        foreach (var child in control.GetVisualChildren())
        {
            if (child is Control childControl)
            {
                var found = FindTreeViewItem(childControl, targetNode);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    /// <summary>
    /// 文件树项目指针按下事件 - 仅处理右键菜单
    /// </summary>
    private void OnFileTreeItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border)
            return;

        var node = border.DataContext as Models.FileTreeNode;
        if (node == null)
            return;

        var point = e.GetCurrentPoint(sender as Control);

        // 只处理右键点击
        if (point.Properties.IsRightButtonPressed)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            // 如果没有工作区路径，或者节点的 FullPath 为空，则不显示右键菜单
            if (string.IsNullOrEmpty(vm.WorkspacePath) || string.IsNullOrEmpty(node.FullPath))
                return;

            // 根据节点类型创建相应的右键菜单
            ContextMenu? contextMenu = null;

            if (node.Name == "Inner" || node.Name == "Outer")
            {
                // Inner/Outer 文件夹 - 创建"新建协议文件"菜单
                contextMenu = new ContextMenu();
                var newProtoFileMenuItem = new MenuItem { Header = "新建协议文件" };
                newProtoFileMenuItem.Click += (s, args) => OnNewProtoFileClick(node);
                contextMenu.Items.Add(newProtoFileMenuItem);

                var revealInFolder = new MenuItem { Header = "从文件夹找到..." };
                revealInFolder.Click += (s, args) => OpenInFileExplorer(node.FullPath);
                contextMenu.Items.Add(revealInFolder);
            }
            else if (node.Name == "RoamingType.Config" || node.Name == "RouteType.Config")
            {
                // RoamingType.Config/RouteType.Config - 只有不存在时才创建"创建配置文件"菜单
                if (!node.Exists)
                {
                    contextMenu = new ContextMenu();
                    var createConfigFileMenuItem = new MenuItem { Header = "创建配置文件" };
                    createConfigFileMenuItem.Click += (s, args) => OnCreateConfigFileClick(node);
                    contextMenu.Items.Add(createConfigFileMenuItem);

                    var revealInFolder = new MenuItem { Header = "从文件夹找到..." };
                    revealInFolder.Click += (s, args) => OpenInFileExplorer(node.FullPath);
                    contextMenu.Items.Add(revealInFolder);
                }
            }
            else if (!node.IsFolder && node.Name.EndsWith(".proto", StringComparison.OrdinalIgnoreCase))
            {
                // 协议文件 - 检查是否在 Inner 或 Outer 文件夹下
                var parentFolder = Path.GetFileName(Path.GetDirectoryName(node.FullPath));
                if (parentFolder == "Inner" || parentFolder == "Outer")
                {
                    contextMenu = new ContextMenu();

                    var newProtoFileMenuItem = new MenuItem { Header = "新建协议文件" };
                    newProtoFileMenuItem.Click += (s, args) => OnNewProtoFileClick(node);
                    contextMenu.Items.Add(newProtoFileMenuItem);

                    var revealInFolder = new MenuItem { Header = "从文件夹找到..." };
                    revealInFolder.Click += (s, args) => OpenInFileExplorer(node.FullPath);
                    contextMenu.Items.Add(revealInFolder);

                    var renameItem = new MenuItem { Header = "重命名" };
                    renameItem.Click += (s, args) => OnRenameNodeClick(node);
                    contextMenu.Items.Add(renameItem);

                    var removeProtoFileMenuItem = new MenuItem { Header = "删除协议文件" };
                    removeProtoFileMenuItem.Click += (s, args) => OnRemoveProtoFileClick(node);
                    contextMenu.Items.Add(removeProtoFileMenuItem);
                }
            }

            // 显示菜单
            if (contextMenu != null)
            {
                // 关闭之前打开的菜单（如果有）
                if (_currentContextMenu != null && _currentContextMenu.IsOpen)
                {
                    _currentContextMenu.Close();
                }

                // 保存当前菜单引用
                _currentContextMenu = contextMenu;

                // 监听菜单关闭事件，清空引用
                contextMenu.Closed += (s, args) =>
                {
                    if (_currentContextMenu == contextMenu)
                    {
                        _currentContextMenu = null;
                    }
                };

                contextMenu.PlacementTarget = border;
                contextMenu.Open(border);
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// 右键菜单打开时，根据节点类型动态显示/隐藏菜单项（已废弃，使用手动创建菜单的方式）
    /// </summary>
    private void OnContextMenuOpening_Deprecated(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not ContextMenu contextMenu)
        {
            e.Cancel = true;
            return;
        }

        // 获取右键点击的节点
        var stackPanel = contextMenu.PlacementTarget as StackPanel;
        if (stackPanel == null)
        {
            e.Cancel = true;
            return;
        }

        var node = stackPanel.DataContext as Models.FileTreeNode;
        if (node == null)
        {
            e.Cancel = true;
            return;
        }

        // 检查是否有工作区路径，没有工作区时不显示右键菜单
        if (DataContext is not MainWindowViewModel vm)
        {
            e.Cancel = true;
            return;
        }

        // 如果没有工作区路径，或者节点的 FullPath 为空，则不显示右键菜单
        if (string.IsNullOrEmpty(vm.WorkspacePath))
        {
            e.Cancel = true;
            return;
        }

        if (string.IsNullOrEmpty(node.FullPath))
        {
            e.Cancel = true;
            return;
        }

        // 查找菜单项
        var newProtoFileMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "NewProtoFileMenuItem");
        var createConfigFileMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "CreateConfigFileMenuItem");

        if (newProtoFileMenuItem == null || createConfigFileMenuItem == null)
        {
            e.Cancel = true;
            return;
        }

        // 根据节点类型显示/隐藏菜单项
        if (node.Name == "Inner" || node.Name == "Outer")
        {
            // Inner/Outer 文件夹 - 只显示"新建协议文件"
            newProtoFileMenuItem.IsVisible = true;
            createConfigFileMenuItem.IsVisible = false;
        }
        else if (node.Name == "RoamingType.Config" || node.Name == "RouteType.Config")
        {
            // RoamingType.Config/RouteType.Config - 只有不存在时才显示"创建配置文件"
            newProtoFileMenuItem.IsVisible = false;
            createConfigFileMenuItem.IsVisible = !node.Exists;

            // 如果文件已存在，取消显示右键菜单
            if (node.Exists)
            {
                e.Cancel = true;
            }
        }
        else
        {
            // 其他节点 - 不显示右键菜单
            e.Cancel = true;
        }
    }

    /// <summary>
    /// 新建协议文件（Inner/Outer 文件夹）
    /// </summary>
    private async void OnNewProtoFileClick(Models.FileTreeNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.FullPath))
            return;

        string? folderPath = node.FullPath;
        if (!node.IsFolder)
        {
            // 如果不是文件夹，获取其父文件夹路径
            folderPath = Path.GetDirectoryName(node.FullPath);
            if (string.IsNullOrEmpty(folderPath))
                return;
        }

        if (DataContext is not MainWindowViewModel vm)
            return;

        try
        {
            // 弹出对话框输入文件名
            var dialog = new Window
            {
                Title = "新建协议文件",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "请输入协议文件名（不含 .proto 扩展名）:", Margin = new Thickness(0, 0, 0, 10) });

            var textBox = new TextBox { Watermark = "例如: UserProtocol" };
            panel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0),
                Spacing = 10
            };

            var okButton = new Button { Content = "确定", Width = 80 };
            var cancelButton = new Button { Content = "取消", Width = 80 };

            okButton.Click += (s, args) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    dialog.Close(textBox.Text);
                }
            };

            cancelButton.Click += (s, args) => dialog.Close(null);

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;

            var result = await dialog.ShowDialog<string?>(this);

            if (string.IsNullOrWhiteSpace(result))
                return;

            // 创建文件
            var fileName = result.EndsWith(".proto") ? result : $"{result}.proto";
            var filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                vm.OutputText += $"错误：文件已存在：{fileName}\n";
                return;
            }

            // 创建文件夹（如果不存在）
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                if(node.IsFolder)
                    node.Exists = true;
            }

            // 创建空的 proto 文件
            File.WriteAllText(filePath, $"syntax = \"proto3\";\n\n// {fileName}\n");

            // 刷新文件树
            vm.LoadWorkspaceFolder(vm.WorkspacePath);

            // 打开新文件
            vm.OpenFile(filePath);

            vm.OutputText += $"已创建新协议文件：{fileName}\n";
        }
        catch (Exception ex)
        {
            vm.OutputText += $"创建文件时出错：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 从文件夹打开一个路径
    /// </summary>
    public void OpenInFileExplorer(string fullPath)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (string.IsNullOrEmpty(fullPath))
        {
            vm.OutputText += $"无法打开空路径\n";
            return; 
        }

        if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
        {
            vm.OutputText += $"路径不存在：{fullPath}\n";
            return;
        }

        var psi = new ProcessStartInfo { UseShellExecute = true };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // ** Windows 平台 **
            psi.FileName = "explorer.exe";

            if (Directory.Exists(fullPath))
            {
                psi.Arguments = fullPath;
            }
            else if (File.Exists(fullPath))
            {
                psi.Arguments = $"/select,\"{fullPath}\"";
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // ** macOS 平台 (Finder) **
            psi.FileName = "open";

            if (Directory.Exists(fullPath))
            {
                psi.Arguments = $"\"{fullPath}\"";
            }
            else
            if (File.Exists(fullPath))
            {
                psi.Arguments = $"-R \"{fullPath}\"";
            }
        }
        else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // ** Linux 平台 **
            psi.FileName = "xdg-open";

            string? directoryPath = Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                psi.Arguments = $"\"{directoryPath}\"";
            }
            else
            {
                // 如果无法获取目录 (如根目录文件)，尝试直接打开
                psi.Arguments = $"\"{fullPath}\"";
            }
        }
        else
        {
            vm.OutputText += $"未知平台, 无法打开文件系统。\n";
            return;
        }

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            vm.OutputText += $"打开当前路径发生错误：{ex}\n";
        }
    }

    /// <summary>
    /// 重命名
    /// </summary>
    private async void OnRenameNodeClick(Models.FileTreeNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.FullPath))
            return;

        if (DataContext is not MainWindowViewModel vm)
            return;

        var fullPath = node.FullPath;
        var isFolder = node.IsFolder || Directory.Exists(fullPath); 
        var oldName = Path.GetFileName(fullPath);
        var parentDir = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(parentDir))
            return;

        try
        {
            // 预处理：如果是文件，分离扩展名以便默认只修改名称部分
            string originalNameWithoutExt = oldName;
            string originalExt = string.Empty;
            if (!isFolder)
            {
                originalExt = Path.GetExtension(oldName); // 例如 ".proto"
                originalNameWithoutExt = Path.GetFileNameWithoutExtension(oldName);
            }

            // ==== 弹窗 ====
            var dialog = new Window
            {
                Title = "重命名",
                Width = 420,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(16) };

            panel.Children.Add(new TextBlock
            {
                Text = $"原名称：{oldName}",
                Margin = new Thickness(0, 0, 0, 8)
            });

            var textBox = new TextBox
            {
                Text = isFolder ? oldName : originalNameWithoutExt,
                Watermark = isFolder ? "请输入新文件夹名称" : "请输入新文件名（不包含扩展名，或可包含扩展名）"
            };

            panel.Children.Add(textBox);

            // 如果是文件且有扩展名，显示扩展名提示（可选）
            if (!isFolder && !string.IsNullOrEmpty(originalExt))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"扩展名：{originalExt}（如果输入的新名字不包含扩展名，将自动保留原扩展名）",
                    FontSize = 12,
                    Margin = new Thickness(0, 6, 0, 0)
                });
            }

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 10
            };

            var okButton = new Button { Content = "确定", Width = 80 };
            var cancelButton = new Button { Content = "取消", Width = 80 };

            okButton.Click += (s, args) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    dialog.Close(textBox.Text.Trim());
                }
            };

            cancelButton.Click += (s, args) => dialog.Close(null);

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            var newNameInput = await dialog.ShowDialog<string?>(this);
            if (string.IsNullOrWhiteSpace(newNameInput))
                return;

            var newNameTrim = newNameInput.Trim();

            // 如果名字没变，不处理
            string effectiveNewName = isFolder ? newNameTrim : (Path.HasExtension(newNameTrim) ? newNameTrim : newNameTrim + originalExt);
            if (effectiveNewName == oldName)
                return;

            // 校验非法文件名字符
            var invalidChars = Path.GetInvalidFileNameChars();
            if (effectiveNewName.IndexOfAny(invalidChars) >= 0)
            {
                vm.OutputText += $"错误：名称包含非法字符：{effectiveNewName}\n";
                return;
            }

            var newFullPath = Path.Combine(parentDir, effectiveNewName);

            // 校验目标是否已存在
            if (File.Exists(newFullPath) || Directory.Exists(newFullPath))
            {
                vm.OutputText += $"错误：目标已存在：{effectiveNewName}\n";
                return;
            }

            // 执行重命名（移动）
            if (isFolder)
            {
                Directory.Move(fullPath, newFullPath);
            }
            else
            {
                File.Move(fullPath, newFullPath);
            }

            // 刷新工作区文件树
            vm.LoadWorkspaceFolder(vm.WorkspacePath);

            try
            {
                if (!isFolder)
                {
                    vm.OpenFile(newFullPath);
                }
            }
            catch
            {
                // 忽略打开文件时可能的异常（不影响重命名成功）
            }

            vm.OutputText += $"已重命名：{oldName} → {effectiveNewName}\n";
        }
        catch (Exception ex)
        {
            vm.OutputText += $"重命名失败：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 删除协议文件
    /// </summary>
    private async void OnRemoveProtoFileClick(Models.FileTreeNode node)
    {
        if (node == null || node.IsFolder || string.IsNullOrEmpty(node.FullPath))
            return;

        if (DataContext is not MainWindowViewModel vm)
            return;

        try
        {
            // 确认删除
            var confirmDialog = new Window
            {
                Title = "确认删除",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock
            {
                Text = $"确定要删除协议文件吗？\n\n{node.Name}",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0),
                Spacing = 10
            };

            var confirmButton = new Button { Content = "确定", Width = 80 };
            var cancelButton = new Button { Content = "取消", Width = 80 };

            confirmButton.Click += (s, args) => confirmDialog.Close(true);
            cancelButton.Click += (s, args) => confirmDialog.Close(false);

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);

            confirmDialog.Content = panel;

            var result = await confirmDialog.ShowDialog<bool>(this);

            if (!result)
                return;

            // 检查文件是否存在
            if (!File.Exists(node.FullPath))
            {
                vm.OutputText += $"错误：未找到文件：{node.Name}\n";
                return;
            }

            // 如果文件在编辑器中打开，先关闭对应的标签页
            var openedTab = vm.OpenedTabs.FirstOrDefault(t => t.FilePath == node.FullPath);
            if (openedTab != null)
            {
                vm.CloseTab(openedTab);
            }

            // 删除文件
            File.Delete(node.FullPath);

            // 刷新文件树
            vm.LoadWorkspaceFolder(vm.WorkspacePath);

            vm.OutputText += $"已删除协议文件：{node.Name}\n";
        }
        catch (Exception ex)
        {
            vm.OutputText += $"移除文件时出错：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 创建配置文件（RoamingType.Config/RouteType.Config）
    /// </summary>
    private void OnCreateConfigFileClick(Models.FileTreeNode node)
    {
        if (node == null || node.IsFolder || string.IsNullOrEmpty(node.FullPath))
            return;

        if (DataContext is not MainWindowViewModel vm)
            return;

        try
        {
            // 检查文件是否已存在
            if (File.Exists(node.FullPath))
            {
                vm.OutputText += $"错误：文件已存在：{node.Name}\n";
                return;
            }

            // 创建默认内容
            string defaultContent = "";
            if (node.Name == "RoamingType.Config")
            {
                defaultContent = "# Roaming 类型配置\n# 在此添加漫游消息类型\n";
            }
            else if (node.Name == "RouteType.Config")
            {
                defaultContent = "# Route 类型配置\n# 在此添加路由消息类型\n";
            }

            // 创建文件
            File.WriteAllText(node.FullPath, defaultContent);

            // 更新节点状态
            node.Exists = true;

            // 刷新文件树
            vm.LoadWorkspaceFolder(vm.WorkspacePath);

            // 打开新文件
            vm.OpenFile(node.FullPath);

            vm.OutputText += $"已创建配置文件：{node.Name}\n";
        }
        catch (Exception ex)
        {
            vm.OutputText += $"创建配置文件时出错：{ex.Message}\n";
        }
    }

    /// <summary>
    /// 窗口关闭事件，保存工作区配置
    /// </summary>
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // 保存当前激活标签页的光标位置
            if (vm.ActiveTab != null && TextEditor != null)
            {
                vm.ActiveTab.CaretOffset = TextEditor.CaretOffset;
            }

            // 保存工作区配置
            vm.SaveWorkspaceConfig();
            // 保存打开的文件
            SaveAllOpened();
        }
    }

    /// <summary>
    /// 关于菜单点击事件
    /// </summary>
    private void OnAboutMenuClick(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog(this);
    }

    /// <summary>
    /// 清空输出按钮点击事件
    /// </summary>
    private void OnClearOutputClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OutputText = string.Empty;
        }
    }

    /// <summary>
    /// 菜单"跳转到行"点击事件
    /// </summary>
    private void OnGoToLineMenuClick(object? sender, RoutedEventArgs e)
    {
        ShowGoToLineDialog();
    }

    /// <summary>
    /// 显示跳转到行对话框
    /// </summary>
    private async void ShowGoToLineDialog()
    {
        if (TextEditor?.Document == null)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OutputText += "请先打开一个文件。\n";
            }
            return;
        }

        try
        {
            // 获取当前行号和总行数
            var currentLine = TextEditor.Document.GetLineByOffset(TextEditor.CaretOffset).LineNumber;
            var totalLines = TextEditor.Document.LineCount;

            // 创建对话框
            var dialog = new Window
            {
                Title = "跳转到行",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2B2B2B"))
            };

            var panel = new StackPanel { Margin = new Thickness(20) };

            var labelText = new TextBlock
            {
                Text = $"请输入行号 (1-{totalLines}):",
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(labelText);

            var textBox = new TextBox
            {
                Watermark = $"当前行: {currentLine}",
                Text = currentLine.ToString(),
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E")),
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC")),
                BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3E3E42"))
            };
            panel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0),
                Spacing = 10
            };

            var okButton = new Button
            {
                Content = "跳转",
                Width = 80,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#007ACC")),
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
            };
            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3E3E42")),
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC"))
            };

            okButton.Click += (s, args) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    dialog.Close(textBox.Text);
                }
            };

            cancelButton.Click += (s, args) => dialog.Close(null);

            // 添加回车键快捷键
            textBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        dialog.Close(textBox.Text);
                    }
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    dialog.Close(null);
                    args.Handled = true;
                }
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;

            // 聚焦并选中文本框
            dialog.Opened += (s, args) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };

            var result = await dialog.ShowDialog<string?>(this);

            if (string.IsNullOrWhiteSpace(result))
                return;

            // 解析行号
            if (!int.TryParse(result, out int lineNumber))
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.OutputText += $"错误：无效的行号：{result}\n";
                }
                return;
            }

            // 验证行号范围
            if (lineNumber < 1 || lineNumber > totalLines)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.OutputText += $"错误：行号超出范围 (1-{totalLines})：{lineNumber}\n";
                }
                return;
            }

            // 跳转到指定行
            GoToLine(lineNumber);
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OutputText += $"跳转到行时出错：{ex.Message}\n";
            }
        }
    }

    /// <summary>
    /// 跳转到指定行
    /// </summary>
    private void GoToLine(int lineNumber)
    {
        if (TextEditor?.Document == null)
            return;

        try
        {
            // 获取指定行
            var line = TextEditor.Document.GetLineByNumber(lineNumber);

            // 设置光标位置到行首
            TextEditor.CaretOffset = line.Offset;

            // 滚动到该行（居中显示）
            TextEditor.ScrollToLine(lineNumber);

            // 选中整行（可选，视觉效果更好）
            TextEditor.Select(line.Offset, line.Length);

            // 聚焦编辑器
            TextEditor.Focus();

            if (DataContext is MainWindowViewModel vm)
            {
                vm.OutputText += $"已跳转到第 {lineNumber} 行\n";
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OutputText += $"跳转到行时出错：{ex.Message}\n";
            }
        }
    }

    /// <summary>
    /// 退出菜单点击事件
    /// </summary>
    private void OnExitMenuClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}