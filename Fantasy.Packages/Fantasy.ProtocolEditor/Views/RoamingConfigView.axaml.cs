using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Fantasy.ProtocolEditor.ViewModels;

namespace Fantasy.ProtocolEditor.Views;

public partial class RoamingConfigView : UserControl
{
    public RoamingConfigView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 复制名称到剪贴板
    /// </summary>
    private async void OnCopyNameClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        // 从 Tag 属性获取名称
        var name = button.Tag as string;
        if (string.IsNullOrEmpty(name))
            return;

        try
        {
            // 获取剪贴板
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(name);

                // 显示成功提示
                if (DataContext is RoamingConfigViewModel vm)
                {
                    var originalError = vm.ErrorMessage;
                    vm.ErrorMessage = $"已复制: {name}";

                    // 2秒后清除提示
                    await System.Threading.Tasks.Task.Delay(2000);

                    // 如果错误信息没有被改变，则清除
                    if (vm.ErrorMessage == $"已复制: {name}")
                    {
                        vm.ErrorMessage = string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (DataContext is RoamingConfigViewModel vm)
            {
                vm.ErrorMessage = $"复制失败: {ex.Message}";
            }
        }
    }
}
