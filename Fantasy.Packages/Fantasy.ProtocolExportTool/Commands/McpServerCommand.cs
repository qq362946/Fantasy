using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Models;
using Fantasy.ProtocolExportTool.Services;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Commands;

/// <summary>
/// MCP服务器命令 - 使用JSON-RPC over stdio实现MCP协议
/// </summary>
public class McpServerCommand : Command
{
    private const string ConfigPathEnvVar = "FANTASY_PROTOCOL_CONFIG";

    /// <summary>
    /// 从命令行参数中解析DLL所在目录
    /// 优先从命令行获取DLL路径，而不是使用Environment.ProcessPath（后者在MCP调用时不可靠）
    /// </summary>
    private static string GetDllDirectoryFromArgs(string[] args)
    {
        // 模式: dotnet "path/to/xxx.dll" mcp -c config.json
        // 或: "path/to/xxx.dll" mcp -c config.json
        
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            
            // 跳过 "dotnet" 关键字
            if (arg.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                continue;
                
            // 跳过命令关键字
            if (arg.Equals("mcp", StringComparison.OrdinalIgnoreCase) || arg.Equals("export", StringComparison.OrdinalIgnoreCase))
                continue;
                
            // 跳过参数名
            if (arg.Equals("-c", StringComparison.OrdinalIgnoreCase) || arg.Equals("--config", StringComparison.OrdinalIgnoreCase))
                continue;
                
            // 检查是否是 DLL 文件
            if (arg.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // 检查文件是否存在
                if (File.Exists(arg))
                {
                    return Path.GetDirectoryName(arg) ?? "";
                }
                
                // 也可能是相对路径，检查工作目录下是否存在
                var combined = Path.Combine(Directory.GetCurrentDirectory(), arg);
                if (File.Exists(combined))
                {
                    return Path.GetDirectoryName(combined) ?? "";
                }
            }
        }
        
        // 回退：使用当前工作目录
        return Directory.GetCurrentDirectory();
    }

    public McpServerCommand() : base("mcp", "启动MCP服务器模式，允许远程调用协议导出")
    {
        var configPathOption = new Option<string>("-c", "--config")
        {
            Description = "配置文件路径",
            DefaultValueFactory = _ => null
        };

        Add(configPathOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            // 直接从 args 获取 DLL 目录
            var args = parseResult.Tokens.Select(t => t.Value).ToArray();
            var dllDir = GetDllDirectoryFromArgs(args);
            
            AnsiConsole.MarkupLine($"[gray]检测到DLL目录: {dllDir}[/]");

            // 确定配置文件路径
            string? configPath = null;
            
            // 1. 优先使用环境变量
            var envConfigPath = Environment.GetEnvironmentVariable(ConfigPathEnvVar);
            if (!string.IsNullOrEmpty(envConfigPath))
            {
                configPath = envConfigPath;
                AnsiConsole.MarkupLine($"[green]✓[/] 使用环境变量配置: {ConfigPathEnvVar}={configPath}");
            }
            else
            {
                // 2. 检查 -c 参数
                var cmdLineConfigPath = parseResult.GetValue(configPathOption);
                if (!string.IsNullOrEmpty(cmdLineConfigPath))
                {
                    configPath = cmdLineConfigPath;
                }
                else
                {
                    // 3. 在DLL目录查找 ExporterSettings.json
                    var defaultConfigPath = Path.Combine(dllDir, "ExporterSettings.json");
                    if (File.Exists(defaultConfigPath))
                    {
                        configPath = defaultConfigPath;
                        AnsiConsole.MarkupLine($"[green]✓[/] 使用DLL目录默认配置: {configPath}");
                    }
                }
            }

            // 如果没有找到配置文件
            if (string.IsNullOrEmpty(configPath))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 找不到配置文件。");
                AnsiConsole.MarkupLine("[yellow]提示:[/] 请使用 -c 参数指定配置文件，或在DLL目录放置 ExporterSettings.json");
                return 1;
            }

            // 检查配置文件是否存在
            if (!File.Exists(configPath))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 配置文件不存在: {configPath}");
                return 1;
            }

            AnsiConsole.MarkupLine("[blue]╔════════════════════════════════════════════════╗[/]");
            AnsiConsole.MarkupLine("[blue]║  Fantasy Protocol Export MCP Server            ║[/]");
            AnsiConsole.MarkupLine("[blue]║  Version: 2026.0.1002                         ║[/]");
            AnsiConsole.MarkupLine("[blue]╚════════════════════════════════════════════════╝[/]");
            AnsiConsole.WriteLine();

            // 加载配置（使用DLL目录作为基准）
            var config = await ProtocolExportService.LoadConfigFromFileAsync(configPath, dllDir);
            if (config == null)
            {
                return 1;
            }

            AnsiConsole.MarkupLine("[green]✓[/] 已加载配置文件");
            AnsiConsole.MarkupLine($"  协议目录: {config.ProtocolDir}");
            AnsiConsole.MarkupLine($"  服务器目录: {config.ServerDir}");
            AnsiConsole.MarkupLine($"  客户端目录: {config.ClientDir}");
            AnsiConsole.WriteLine();

            // 首次执行导出
            AnsiConsole.MarkupLine("[blue]▶ 执行首次协议导出...[/]");
            var exportService = new ProtocolExportService();
            var result = await exportService.ExportAsync(config, cancellationToken, silent: true);

            if (!result.Success)
            {
                AnsiConsole.MarkupLine($"[red]✗ 首次导出失败:[/] {result.ErrorMessage}");
                AnsiConsole.MarkupLine("[yellow]继续启动MCP服务器...[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]✓[/] 首次导出成功");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]▶ 启动MCP服务器 (stdio模式)...[/]");
            AnsiConsole.MarkupLine("[gray]  服务器正在运行，等待MCP客户端连接...[/]");
            AnsiConsole.MarkupLine("[gray]  按 Ctrl+C 停止服务器[/]");
            AnsiConsole.WriteLine();

            // 启动MCP服务器（传入配置路径和DLL目录）
            var server = new McpJsonRpcServer(exportService, configPath, dllDir);
            await server.RunAsync(cancellationToken);

            return 0;
        });
    }
}

/// <summary>
/// JSON-RPC MCP服务器
/// </summary>
public class McpJsonRpcServer
{
    private readonly string _configPath;
    private readonly string _dllBaseDir;
    private readonly ProtocolExportService _exportService;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpJsonRpcServer(ProtocolExportService exportService, string configPath, string dllBaseDir)
    {
        _exportService = exportService;
        _configPath = configPath;
        _dllBaseDir = dllBaseDir;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task RunAsync(CancellationToken ct)
    {
        // 发送初始化通知
        await SendNotificationAsync("notifications/initialized", new JsonObject(), ct);

        // 使用StreamReader读取stdin
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 读取一行JSON-RPC消息
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var message = JsonNode.Parse(line);
                    if (message == null) continue;

                    // 处理消息
                    await ProcessMessageAsync(message, ct);
                }
                catch (JsonException ex)
                {
                    await SendErrorResponseAsync(null, -32700, $"Parse error: {ex.Message}", ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
    }

    private async Task ProcessMessageAsync(JsonNode message, CancellationToken ct)
    {
        var jsonRpc = message["jsonrpc"]?.GetValue<string>();
        if (jsonRpc != "2.0")
        {
            await SendErrorResponseAsync(null, -32600, "Invalid JSON-RPC version", ct);
            return;
        }

        var method = message["method"]?.GetValue<string>();
        var id = message["id"];

        // 处理请求
        if (method != null)
        {
            switch (method)
            {
                case "initialize":
                    await HandleInitializeAsync(id, ct);
                    break;

                case "tools/list":
                    await HandleToolsListAsync(id, ct);
                    break;

                case "tools/call":
                    await HandleToolsCallAsync(id, message["params"], ct);
                    break;

                default:
                    if (id != null)
                    {
                        await SendErrorResponseAsync(id, -32601, $"Method not found: {method}", ct);
                    }
                    break;
            }
        }
    }

    private async Task HandleInitializeAsync(JsonNode? id, CancellationToken ct)
    {
        var result = new JsonObject
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject { ["listChanged"] = true }
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "fantasy-protocol-to-code",
                ["version"] = "2026.0.1002",
                ["description"] = " Fantasy 框架的协议->代码类 导出工具，将 .proto 协议文件转换为 C# 消息类代码. 利用Fantasy框架开发 需要定义网络消息时请不要手写消息类!! 务必使用这个工具!! 以免出现消息类定义不完整的问题!!  "
            }
        };

        await SendResponseAsync(id, result, ct);
    }

    private async Task HandleToolsListAsync(JsonNode? id, CancellationToken ct)
    {
        var result = new JsonObject
        {
            ["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "export_protocols",
                    ["description"] = "导出Fantasy网络协议文件。根据.proto配置生成服务器和/或客户端的协议代码，返回实际导出的消息类存放路径等信息。",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["exportType"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "导出哪些: 'Client'(仅导出客户端消息类), 'Server'(仅服务端消息类), 'All'(双端,默认), 一般来说导出双端即可.",
                                ["enum"] = new JsonArray { "Client", "Server", "All" }
                            }
                        }
                    }
                }
            }
        };

        await SendResponseAsync(id, result, ct);
    }

    private async Task HandleToolsCallAsync(JsonNode? id, JsonNode? @params, CancellationToken ct)
    {
        var toolName = @params?["name"]?.GetValue<string>();
        var arguments = @params?["arguments"];

        if (toolName == null)
        {
            await SendErrorResponseAsync(id, -32602, "Missing tool name", ct);
            return;
        }

        // 重新加载配置以获取最新设置（使用DLL目录作为基准）
        var freshConfig = await ProtocolExportService.LoadConfigFromFileAsync(_configPath, _dllBaseDir);
        if (freshConfig == null)
        {
            await SendToolErrorAsync(id, $"无法加载配置文件: {_configPath}", ct);
            return;
        }

        switch (toolName)
        {
            case "export_protocols":
                await HandleExportProtocolsAsync(id, freshConfig, arguments, ct);
                break;

            default:
                await SendErrorResponseAsync(id, -32601, $"Tool not found: {toolName}", ct);
                break;
        }
    }

    private async Task HandleExportProtocolsAsync(JsonNode? id, ProtocolExportConfig config, JsonNode? arguments, CancellationToken ct)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]▶ 收到MCP导出请求[/]");

        // 解析 exportType 参数，默认 All
        var exportTypeStr = arguments?["exportType"]?.GetValue<string>() ?? "All";
        if (!Enum.TryParse<ProtocolExportType>(exportTypeStr, out var exportType))
        {
            exportType = ProtocolExportType.All;
        }

        // 应用导出类型
        config.ExportType = exportType;

        var result = await _exportService.ExportAsync(config, ct, silent: true);

        if (result.Success)
        {
            var exportTypeDisplay = exportType switch
            {
                ProtocolExportType.Client => "仅客户端",
                ProtocolExportType.Server => "仅服务端",
                ProtocolExportType.All => "双端",
                _ => "双端"
            };

            var content = $"✓ 协议导出成功\n\n" +
                         $"导出配置:\n" +
                         $"  导出类型: {exportTypeDisplay}\n" +
                         $"  协议目录: {config.ProtocolDir}\n" +
                         $"  服务器目录: {(string.IsNullOrEmpty(config.ServerDir) ? "(未配置)" : config.ServerDir)}\n" +
                         $"  客户端目录: {(string.IsNullOrEmpty(config.ClientDir) ? "(未配置)" : config.ClientDir)}";

            await SendToolResultAsync(id, content, false, ct);
        }
        else
        {
            await SendToolErrorAsync(id, $"✗ 协议导出失败\n\n错误: {result.ErrorMessage}", ct);
        }
    }

    private async Task SendResponseAsync(JsonNode? id, JsonObject result, CancellationToken ct)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result
        };

        await SendMessageAsync(response, ct);
    }

    private async Task SendErrorResponseAsync(JsonNode? id, int code, string message, CancellationToken ct)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };

        await SendMessageAsync(response, ct);
    }

    private async Task SendNotificationAsync(string method, JsonObject @params, CancellationToken ct)
    {
        var notification = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = @params
        };

        await SendMessageAsync(notification, ct);
    }

    private async Task SendToolResultAsync(JsonNode? id, string content, bool isError, CancellationToken ct)
    {
        var result = new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = content
                }
            },
            ["isError"] = isError
        };

        await SendResponseAsync(id, result, ct);
    }

    private async Task SendToolErrorAsync(JsonNode? id, string errorMessage, CancellationToken ct)
    {
        await SendToolResultAsync(id, errorMessage, true, ct);
    }

    private async Task SendMessageAsync(JsonObject message, CancellationToken ct)
    {
        var json = message.ToJsonString(_jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");

        await Console.OpenStandardOutput().WriteAsync(bytes, ct);
        await Console.Out.FlushAsync();
    }
}
