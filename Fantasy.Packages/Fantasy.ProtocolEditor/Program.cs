using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;

namespace Fantasy.ProtocolEditor;

sealed class Program
{
    internal static string? StartupWorkspacePath { get; private set; }
    internal static string? StartupArgumentError { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var avaloniaArgs = ParseStartupArguments(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(avaloniaArgs);
    }

    private static string[] ParseStartupArguments(IReadOnlyList<string> args)
    {
        StartupWorkspacePath = null;
        StartupArgumentError = null;
        var avaloniaArgs = new List<string>();

        for (var i = 0; i < args.Count; i++)
        {
            var argument = args[i];
            string? configPath = null;

            if (string.Equals(argument, "--config", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Count || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    StartupWorkspacePath = null;
                    StartupArgumentError = "--config 参数缺少协议工作区路径。";
                    continue;
                }

                configPath = args[++i];
            }
            else if (argument.StartsWith("--config=", StringComparison.OrdinalIgnoreCase))
            {
                configPath = argument["--config=".Length..];
                if (string.IsNullOrWhiteSpace(configPath))
                {
                    StartupWorkspacePath = null;
                    StartupArgumentError = "--config 参数缺少协议工作区路径。";
                    continue;
                }
            }
            else
            {
                avaloniaArgs.Add(argument);
                continue;
            }

            try
            {
                StartupWorkspacePath = Path.GetFullPath(configPath.Trim());
                StartupArgumentError = null;
            }
            catch (Exception ex)
            {
                StartupWorkspacePath = null;
                StartupArgumentError = $"配置路径无效：{ex.Message}";
            }
        }

        return avaloniaArgs.ToArray();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
