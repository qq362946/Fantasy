using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
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
                var loadedConfig = await ProtocolExportService.LoadConfigFromFileAsync("ExporterSettings.json", null);
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

            // 验证目录
            if (!isSilent && !Directory.Exists(config.ProtocolDir))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 网络协议所在的位置的目录 '{config.ProtocolDir}' 不已存在。");
                return 1;
            }

            if (!string.IsNullOrEmpty(config.ServerDir) && !Directory.Exists(config.ServerDir))
            {
                if (isSilent || await AnsiConsole.ConfirmAsync("[yellow]提示:[/] 导出到服务器的目录不存在是否要创建?"))
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
                if (isSilent || await AnsiConsole.ConfirmAsync("[yellow]提示:[/] 导出到客户端的目录不存在是否要创建?"))
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

            // 执行导出
            var exportService = new ProtocolExportService();
            var result = await exportService.ExportAsync(config, silent: isSilent);

            return result.Success ? 0 : 1;
        });
    }
}
