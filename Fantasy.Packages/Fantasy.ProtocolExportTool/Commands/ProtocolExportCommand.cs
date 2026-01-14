using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Generators;
using Fantasy.ProtocolExportTool.Interactive;
using Fantasy.ProtocolExportTool.Models;
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
            Description = "静默模式，从 ExporterSettings.json 文件读取配置"
        };

        Add(protocolDirectoryOption);
        Add(serverOption);
        Add(clientOption);
        Add(exportTypeOption);
        Add(silentOption);

        SetAction(async context =>
        {
            var protocolDir = context.GetValue(protocolDirectoryOption);
            var serverDir = context.GetValue(serverOption);
            var clientDir = context.GetValue(clientOption);
            var exportType = context.GetValue(exportTypeOption);
            var isSilent = context.GetValue(silentOption);

            ProtocolExportConfig config;

            if (isSilent)
            {
                // 静默模式：从 ExporterSettings.json 加载配置
                var loadedConfig = await LoadConfigFromFileAsync();
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

            if (!isSilent && !Directory.Exists(config.ProtocolDir))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 网络协议所在的位置的目录 '{config.ProtocolDir}' 不已存在。");
                return 1;
            }

            if (!string.IsNullOrEmpty(config.ServerDir) && !Directory.Exists(config.ServerDir))
            {
                if (isSilent || await AnsiConsole.ConfirmAsync("[/] 导出到服务器的目录不存在是否要创建?"))
                {
                    Directory.CreateDirectory(config.ServerDir);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]错误:[/] 导出到服务器的目录 '{config.ServerDir}' 不已存在。");
                    return 1;
                }
            }

            if (!string.IsNullOrEmpty(config.ClientDir) && !Directory.Exists(config.ClientDir))
            {
                if (isSilent || await AnsiConsole.ConfirmAsync("[/] 导出到客户端的目录不存在是否要创建?"))
                {
                    Directory.CreateDirectory(config.ClientDir);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]错误:[/] 导出到客户端的目录 '{config.ClientDir}' 不已存在。");
                    return 1;
                }
            }

            if (string.IsNullOrEmpty(config.ServerDir) && string.IsNullOrEmpty(config.ClientDir))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 导出到客户端和服务端的目录至少要指定一个。");
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
                if (AnsiConsole.Confirm("显示堆栈跟踪？", false))
                {
                    AnsiConsole.WriteException(ex);
                }

                return 1;
            }
        });
    }

    private static async Task<ProtocolExportConfig?> LoadConfigFromFileAsync()
    {
        const string settingsFileName = "ExporterSettings.json";

        try
        {
            if (!File.Exists(settingsFileName))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 找不到配置文件 '{settingsFileName}'。");
                AnsiConsole.MarkupLine("[yellow]提示:[/] 请在当前目录下创建 ExporterSettings.json 文件。");
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(settingsFileName);
            var settings = JsonSerializer.Deserialize<ExporterSettings>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (settings?.Export == null)
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 配置文件 '{settingsFileName}' 格式不正确。");
                return null;
            }

            var config = new ProtocolExportConfig
            {
                ProtocolDir = settings.Export.NetworkProtocolDirectory.Value,
                ServerDir = settings.Export.NetworkProtocolServerDirectory.Value,
                ClientDir = settings.Export.NetworkProtocolClientDirectory.Value,
                ExportType = ProtocolExportType.All
            };

            AnsiConsole.MarkupLine("[green]成功:[/] 已从 ExporterSettings.json 加载配置。");
            // AnsiConsole.MarkupLine($"  协议目录: {config.ProtocolDir}");
            // AnsiConsole.MarkupLine($"  服务器目录: {config.ServerDir}");
            // AnsiConsole.MarkupLine($"  客户端目录: {config.ClientDir}");
            // AnsiConsole.WriteLine();

            return config;
        }
        catch (JsonException ex)
        {
            AnsiConsole.MarkupLine($"[red]错误:[/] 解析配置文件失败: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误:[/] 读取配置文件失败: {ex.Message}");
            return null;
        }
    }
}