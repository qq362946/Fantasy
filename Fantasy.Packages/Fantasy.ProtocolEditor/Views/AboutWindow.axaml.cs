using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Fantasy.ProtocolEditor.Views;

public partial class AboutWindow : Window
{
    private const string GitHubUrl = "https://github.com/qq362946/Fantasy";
    private const string QQGroup = "569888673";

    public AboutWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 点击 GitHub 链接，在浏览器中打开
    /// </summary>
    private void OnGitHubLinkClick(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            OpenUrl(GitHubUrl);
        }
        catch (Exception ex)
        {
            // 忽略错误，避免影响程序运行
            Console.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    /// <summary>
    /// 复制 QQ 群号到剪贴板
    /// </summary>
    private async void OnCopyQQGroupClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(QQGroup);

                // 修改按钮文本为"已复制"，1秒后恢复
                if (sender is Button button)
                {
                    var originalContent = button.Content;
                    button.Content = "已复制 ✓";
                    button.IsEnabled = false;

                    await System.Threading.Tasks.Task.Delay(1000);

                    button.Content = originalContent;
                    button.IsEnabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy to clipboard: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 在默认浏览器中打开 URL（跨平台）
    /// </summary>
    private void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS
                Process.Start("open", url);
            }
        }
        catch
        {
            // 如果上述方法失败，尝试通用方法
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
