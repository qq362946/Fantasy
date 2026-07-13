using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Fantasy.ProtocolEditor.Views;

public partial class WorkspaceGuideWindow : Window
{
    public WorkspaceGuideWindow()
    {
        InitializeComponent();
    }

    private void OnSelectWorkspaceClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
