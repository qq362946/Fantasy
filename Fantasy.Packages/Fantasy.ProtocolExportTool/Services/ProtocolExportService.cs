using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Generators;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Services;

/// <summary>
/// 协议导出服务 - 核心导出逻辑，可被命令行和MCP服务器共用
/// </summary>
public class ProtocolExportService
{
    /// <summary>
    /// 执行协议导出
    /// </summary>
    /// <param name="config">导出配置</param>
    /// <param name="ct">取消令牌</param>
    /// <param name="silent">是否静默模式（不显示交互式UI）</param>
    /// <returns>导出结果</returns>
    public async Task<ExportResult> ExportAsync(ProtocolExportConfig config, CancellationToken ct = default, bool silent = false)
    {
        var result = new ExportResult { Success = false };

        // 验证目录
        if (!Directory.Exists(config.ProtocolDir))
        {
            result.ErrorMessage = $"协议目录不存在: {config.ProtocolDir}";
            return result;
        }

        if (!string.IsNullOrEmpty(config.ServerDir) && !Directory.Exists(config.ServerDir))
        {
            try
            {
                Directory.CreateDirectory(config.ServerDir);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"创建服务器目录失败: {ex.Message}";
                return result;
            }
        }

        if (!string.IsNullOrEmpty(config.ClientDir) && !Directory.Exists(config.ClientDir))
        {
            try
            {
                Directory.CreateDirectory(config.ClientDir);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"创建客户端目录失败: {ex.Message}";
                return result;
            }
        }

        if (string.IsNullOrEmpty(config.ServerDir) && string.IsNullOrEmpty(config.ClientDir))
        {
            result.ErrorMessage = "必须指定服务器目录或客户端目录至少一个";
            return result;
        }

        try
        {
            if (silent)
            {
                // 静默模式：直接使用Console输出，避免与Progress冲突
                Console.WriteLine("正在导出协议...");
                await new ProtocolGenerator().GenerateAsync(config, ct);
                Console.WriteLine("协议导出成功!");
            }
            else
            {
                // 交互模式：使用进度条
                await new ProtocolGenerator().GenerateAsync(config, ct);
                AnsiConsole.MarkupLine("[green]协议导出成功![/]");
            }

            result.Success = true;
            result.Message = "协议导出成功";
        }
        catch (OperationCanceledException)
        {
            result.ErrorMessage = "导出已取消";
            if (!silent)
                AnsiConsole.MarkupLine("[yellow]导出已取消[/]");
            else
                Console.WriteLine("导出已取消");
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            if (!silent)
                AnsiConsole.MarkupLine($"[red]导出失败: {ex.Message}[/]");
            else
                Console.WriteLine($"导出失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 从配置文件加载导出配置
    /// </summary>
    /// <param name="settingsFileName">配置文件路径，默认为 ExporterSettings.json</param>
    /// <param name="dllBaseDir">DLL所在目录，用于解析配置中的相对路径。为空时使用配置文件目录</param>
    public static async Task<ProtocolExportConfig?> LoadConfigFromFileAsync(string settingsFileName = "ExporterSettings.json", string? dllBaseDir = null)
    {
        try
        {
            // 解析配置文件为绝对路径
            var configFullPath = Path.GetFullPath(settingsFileName);
            var configDir = Path.GetDirectoryName(configFullPath) ?? "";

            if (!File.Exists(configFullPath))
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 找不到配置文件 '{configFullPath}'。");
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(configFullPath);
            var settings = JsonSerializer.Deserialize<ExporterSettings>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (settings?.Export == null)
            {
                AnsiConsole.MarkupLine($"[red]错误:[/] 配置文件 '{configFullPath}' 格式不正确。");
                return null;
            }

            // 基准目录：使用配置文件所在目录
            string baseDir = configDir;

            // 解析各目录为绝对路径
            string ResolveToAbsolute(string? path)
            {
                if (string.IsNullOrEmpty(path)) return "";
                if (Path.IsPathRooted(path)) return path;
                return Path.GetFullPath(Path.Combine(baseDir, path));
            }

            var config = new ProtocolExportConfig
            {
                ProtocolDir = ResolveToAbsolute(settings.Export.NetworkProtocolDirectory.Value),
                ServerDir = ResolveToAbsolute(settings.Export.NetworkProtocolServerDirectory.Value),
                ClientDir = ResolveToAbsolute(settings.Export.NetworkProtocolClientDirectory.Value),
                ExportType = ProtocolExportType.All
            };

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

/// <summary>
/// 导出结果
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
