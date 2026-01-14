using System.Threading;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;
using static System.Enum;

namespace Fantasy.ProtocolExportTool.Interactive;

public sealed class ProtocolExportWizard
{
    public async Task<ProtocolExportConfig> RunAsync(string? protocolDir, string? serverDir, string? clientDir, string? exportType, CancellationToken ct = default)
    {
        while (true)
        {
            var isHaveExportType = TryParse<ProtocolExportType>(exportType, out var protocolExportType);

            var config = new ProtocolExportConfig()
            {
                ProtocolDir = string.IsNullOrEmpty(protocolDir) ? AnsiConsole.Ask<string>("[cyan]协议目录:[/]").Trim() : protocolDir,
                ServerDir = string.IsNullOrEmpty(serverDir) ? AnsiConsole.Ask<string>("[cyan]服务器代码目录:[/]").Trim()  : serverDir,
                ClientDir = string.IsNullOrEmpty(clientDir) ? AnsiConsole.Ask<string>("[cyan]客户端代码目录:[/]").Trim()  : clientDir,
                ExportType = isHaveExportType
                    ? protocolExportType
                    : AnsiConsole.Prompt(new SelectionPrompt<ProtocolExportType>().Title("[cyan]导出目标:[/]")
                        .AddChoices(ProtocolExportType.Server, ProtocolExportType.Client, ProtocolExportType.All)
                        .UseConverter(GetProtocolExportTypeText))
            };

            if (await AnsiConsole.ConfirmAsync("\n[yellow]是否确定执行？[/]", true, ct))
            {
                return config;
            }

            AnsiConsole.MarkupLine("[red]已取消。[/]");
        }
    }

    private static string GetProtocolExportTypeText(ProtocolExportType exportType)
    {
        return exportType switch
        {
            ProtocolExportType.Server => "Server(仅导出服务端)",
            ProtocolExportType.Client => "Client (仅导出客户端)",
            ProtocolExportType.All => "All (客户端和服务端)",
            _ => exportType.ToString()
        };
    }
}