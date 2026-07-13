using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Generators;
using Fantasy.ProtocolExportTool.Interactive;
using Fantasy.ProtocolExportTool.Models;
using Fantasy.ProtocolExportTool.Services;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Commands;

public class ProtocolExportCommand : Command
{
    public ProtocolExportCommand() : base("export", "执行网络协议导出")
    {
        var protocolDirectoryOption = new Option<string>("-n", "--name")
        {
            Description = "网络协议所在的位置"
        };

        var serverOption = new Option<string>("-s", "--server")
        {
            Description = "导出到服务器的位置"
        };

        var clientOption = new Option<string>("-c", "--client")
        {
            Description = "导出到客户端的位置"
        };

        var exportTypeOption = new Option<string>("-t", "--type")
        {
            Description = "导出的方式"
        };

        var silentOption = new Option<bool>("-S", "--silent")
        {
            Description = "静默模式，从配置文件读取导出设置"
        };

        var configOption = new Option<string>("--config")
        {
            Description = "静默模式下指定配置文件路径，默认读取当前目录的 ExporterSettings.json"
        };

        Add(protocolDirectoryOption);
        Add(serverOption);
        Add(clientOption);
        Add(exportTypeOption);
        Add(silentOption);
        Add(configOption);

        SetAction(async context =>
        {
            var protocolDir = context.GetValue(protocolDirectoryOption);
            var serverDir = context.GetValue(serverOption);
            var clientDir = context.GetValue(clientOption);
            var exportType = context.GetValue(exportTypeOption);
            var isSilent = context.GetValue(silentOption);
            var configPath = context.GetValue(configOption);

            ProtocolExportConfig config;

            if (isSilent)
            {
                // 静默模式：从配置文件加载
                var loadedConfig = await LoadConfigFromFileAsync(configPath);
                if (loadedConfig == null)
                {
                    return 1;
                }
                config = loadedConfig;
            }
            else
            {
                // 交互模式：使用向导
                config = await new ProtocolExportWizard().RunAsync(protocolDir, serverDir, clientDir, exportType);
            }

            if (!ValidateTargetDirectories(config, isSilent))
            {
                return 1;
            }

            try
            {
                await new ProtocolGenerator().GenerateAsync(config);
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] {ex.Message}");
                return 1;
            }
        });
    }

    private static async Task<ProtocolExportConfig?> LoadConfigFromFileAsync(string? configPath)
    {
        var settingsFileName = string.IsNullOrWhiteSpace(configPath)
            ? "ExporterSettings.json"
            : Path.GetFullPath(configPath);

        try
        {
            if (!File.Exists(settingsFileName))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 找不到配置文件 '{settingsFileName}'。");
                AnsiConsole.MarkupLine("[yellow]提示:[/] 可通过 --config 指定配置文件路径，或在当前目录下创建 ExporterSettings.json 文件。");
                return null;
            }

            var settings = await ExporterSettingsService.LoadAsync(settingsFileName);
            var config = ExporterSettingsService.CreateExportConfig(settings, settingsFileName);

            AnsiConsole.MarkupLine($"[green]成功:[/] 已从 '{settingsFileName}' 加载配置。");
            // AnsiConsole.MarkupLine($"  协议目录: {config.ProtocolDir}");
            // AnsiConsole.MarkupLine($"  服务器目录: {config.ServerDir}");
            // AnsiConsole.MarkupLine($"  客户端目录: {config.ClientDir}");
            // AnsiConsole.WriteLine();

            return config;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误:[/] 读取配置文件失败: {ex.Message}");
            return null;
        }
    }

    private static bool ValidateTargetDirectories(ProtocolExportConfig config, bool isSilent)
    {
        if (!ValidateSingleTarget(config.ProtocolDir, config.ServerDir, config.ClientDir, config.ExportType, isSilent, "主协议"))
        {
            return false;
        }

        foreach (var packageExport in config.PackageExports)
        {
            if (!ValidateSingleTarget(packageExport.ProtocolDir, packageExport.ServerDir, packageExport.ClientDir, packageExport.ExportType, isSilent, "子包协议"))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateSingleTarget(string protocolDir, string serverDir, string clientDir, ProtocolExportType exportType, bool isSilent, string label)
    {
        if (!Directory.Exists(protocolDir))
        {
            AnsiConsole.MarkupLine($"[red]错误:[/] {label}目录 '{protocolDir}' 不已存在。");
            return false;
        }

        if (exportType.HasFlag(ProtocolExportType.Server) && string.IsNullOrWhiteSpace(serverDir))
        {
            AnsiConsole.MarkupLine($"[red]错误:[/] {label}启用了服务端导出，但未配置服务端目录。");
            return false;
        }

        if (exportType.HasFlag(ProtocolExportType.Client) && string.IsNullOrWhiteSpace(clientDir))
        {
            AnsiConsole.MarkupLine($"[red]错误:[/] {label}启用了客户端导出，但未配置客户端目录。");
            return false;
        }

        if (exportType.HasFlag(ProtocolExportType.Server) && !Directory.Exists(serverDir))
        {
            if (isSilent)
            {
                Directory.CreateDirectory(serverDir);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 导出到服务器的目录 '{serverDir}' 不已存在。");
                return false;
            }
        }

        if (exportType.HasFlag(ProtocolExportType.Client) && !Directory.Exists(clientDir))
        {
            if (isSilent)
            {
                Directory.CreateDirectory(clientDir);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 导出到客户端的目录 '{clientDir}' 不已存在。");
                return false;
            }
        }

        if (exportType == 0)
        {
            AnsiConsole.MarkupLine($"[red]错误:[/] {label}至少要启用一个导出目标。");
            return false;
        }

        return true;
    }
}
