using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Generators;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;

namespace Fantasy.ProtocolEditor.Services;

public class ProtocolExportService
{
    /// <summary>
    /// 执行协议导出
    /// </summary>
    /// <param name="config">导出配置</param>
    /// <param name="progressCallback">进度回调，参数为进度消息</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    public async Task<(bool Success, string? ErrorMessage)> ExportAsync(
        ProtocolExportConfig config,
        Action<string>? progressCallback = null)
    {
        // 创建字符串写入器来捕获输出
        var outputWriter = new StringWriter();

        try
        {
            // 重定向 Spectre.Console 的输出
            var console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(outputWriter)
            });

            AnsiConsole.Console = console;

            var (isValid, validationError) = ValidateConfig(config);
            if (!isValid)
            {
                return (false, validationError);
            }

            EnsureOutputDirectories(
                "主协议",
                config.ServerDir,
                config.ClientDir,
                config.ExportType,
                progressCallback);

            for (var i = 0; i < config.PackageExports.Count; i++)
            {
                var package = config.PackageExports[i];
                EnsureOutputDirectories(
                    $"子包 {i + 1}",
                    package.ServerDir,
                    package.ClientDir,
                    package.ExportType,
                    progressCallback);
            }

            progressCallback?.Invoke("========================================");
            progressCallback?.Invoke("开始导出协议...");
            progressCallback?.Invoke($"协议目录: {config.ProtocolDir}");

            if (config.ExportType.HasFlag(ProtocolExportType.Server))
            {
                progressCallback?.Invoke($"服务器目录: {config.ServerDir}");
            }

            if (config.ExportType.HasFlag(ProtocolExportType.Client))
            {
                progressCallback?.Invoke($"客户端目录: {config.ClientDir}");
            }

            // 执行协议生成
            var generator = new ProtocolGenerator();
            await generator.GenerateAsync(config);

            // 获取捕获的输出
            var output = outputWriter.ToString();
            var hasFormatErrors = false;

            if (!string.IsNullOrEmpty(output))
            {
                // 移除 ANSI 控制字符（虽然已经禁用了颜色，但保留这个以防万一）
                var cleanOutput = RemoveAnsiCodes(output);

                // 检查是否有格式错误
                if (cleanOutput.Contains("The following format errors were found:") ||
                    cleanOutput.Contains("format errors"))
                {
                    hasFormatErrors = true;
                }

                // 使用 HashSet 去重，避免重复显示
                var displayedLines = new HashSet<string>();
                var lines = cleanOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine) && displayedLines.Add(trimmedLine))
                    {
                        progressCallback?.Invoke(trimmedLine);
                    }
                }
            }

            // 如果有格式错误，即使没有抛出异常，也标记为失败
            if (hasFormatErrors)
            {
                progressCallback?.Invoke("[失败] 协议导出失败：发现格式错误");
                progressCallback?.Invoke("========================================");
                return (false, "协议文件存在格式错误，请查看上述错误信息");
            }

            progressCallback?.Invoke("✓ 协议导出完成！");
            progressCallback?.Invoke("========================================");
            return (true, null);
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke($"[失败] 导出失败: {ex.Message}");
            progressCallback?.Invoke($"详细信息: {ex.StackTrace}");
            progressCallback?.Invoke("========================================");
            return (false, ex.ToString());
        }
        finally
        {
            // 释放资源
            outputWriter.Dispose();
        }
    }

    /// <summary>
    /// 移除 ANSI 控制字符（用于清理 Spectre.Console 的彩色输出）
    /// </summary>
    private static string RemoveAnsiCodes(string text)
    {
        // ANSI 转义序列的正则模式
        return Regex.Replace(text, @"\x1B\[[^@-~]*[@-~]", "");
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidateConfig(ProtocolExportConfig config)
    {
        var mainResult = ValidateTarget(
            "主协议",
            config.ProtocolDir,
            config.ServerDir,
            config.ClientDir,
            config.ExportType);
        if (!mainResult.IsValid)
        {
            return mainResult;
        }

        for (var i = 0; i < config.PackageExports.Count; i++)
        {
            var package = config.PackageExports[i];
            var packageResult = ValidateTarget(
                $"子包 {i + 1}",
                package.ProtocolDir,
                package.ServerDir,
                package.ClientDir,
                package.ExportType);
            if (!packageResult.IsValid)
            {
                return packageResult;
            }
        }

        return (true, null);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateTarget(
        string targetName,
        string protocolDirectory,
        string serverDirectory,
        string clientDirectory,
        ProtocolExportType exportType)
    {
        if ((exportType & ProtocolExportType.All) == 0)
        {
            return (false, $"{targetName}未启用任何导出目标");
        }

        if (string.IsNullOrWhiteSpace(protocolDirectory))
        {
            return (false, $"{targetName}协议目录不能为空");
        }

        if (!Directory.Exists(protocolDirectory))
        {
            return (false, $"{targetName}协议目录不存在: {protocolDirectory}");
        }

        if (exportType.HasFlag(ProtocolExportType.Server) && string.IsNullOrWhiteSpace(serverDirectory))
        {
            return (false, $"{targetName}已启用服务器导出，但服务器输出目录为空");
        }

        if (exportType.HasFlag(ProtocolExportType.Client) && string.IsNullOrWhiteSpace(clientDirectory))
        {
            return (false, $"{targetName}已启用客户端导出，但客户端输出目录为空");
        }

        return (true, null);
    }

    private static void EnsureOutputDirectories(
        string targetName,
        string serverDirectory,
        string clientDirectory,
        ProtocolExportType exportType,
        Action<string>? progressCallback)
    {
        if (exportType.HasFlag(ProtocolExportType.Server) && !Directory.Exists(serverDirectory))
        {
            progressCallback?.Invoke($"创建{targetName}服务器目录: {serverDirectory}");
            Directory.CreateDirectory(serverDirectory);
        }

        if (exportType.HasFlag(ProtocolExportType.Client) && !Directory.Exists(clientDirectory))
        {
            progressCallback?.Invoke($"创建{targetName}客户端目录: {clientDirectory}");
            Directory.CreateDirectory(clientDirectory);
        }
    }
}
