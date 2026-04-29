using System.CommandLine;
using Fantasy.ProtocolExportTool.Commands;

var rootCommand = new RootCommand("Fantasy 网络协议导出工具 2026.0.1002")
{
    new ProtocolExportCommand(),
    new McpServerCommand()
};

return await rootCommand.Parse(args).InvokeAsync();
