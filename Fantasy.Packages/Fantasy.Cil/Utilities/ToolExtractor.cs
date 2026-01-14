using System.IO.Compression;
using System.Reflection;
using Fantasy.Cli.Language;
using Spectre.Console;

namespace Fantasy.Cli.Utilities;

/// <summary>
/// 将嵌入式工具资源提取到目标目录
/// </summary>
public class ToolExtractor
{
    private readonly Assembly _assembly;
    
    public ToolExtractor()
    {
        _assembly = Assembly.GetExecutingAssembly();
    }

    /// <summary>
    /// 将 ProtocolExportTool 工具提取到目标目录
    /// </summary>
    /// <param name="targetDirectory"></param>
    /// <param name="ct"></param>
    /// <param name="askOverwrite">是否询问覆盖确认（默认true）。新项目初始化时应设为false。</param>
    public async Task ExtractProtocolExportToolAsync(string targetDirectory, CancellationToken ct = default, bool askOverwrite = true)
    {
        var loc = LocalizationManager.Current;
        var toolsDir = Path.Combine(targetDirectory, "Tools");
        var toolDir = Path.Combine(toolsDir, "ProtocolExportTool");

        if (!Directory.Exists(toolsDir))
        {
            // 如果 Tools 目录不存在，则创建该目录
            Directory.CreateDirectory(toolsDir);
        }

        // 检查工具是否已存在
        if (askOverwrite && Directory.Exists(toolDir) && Directory.GetFileSystemEntries(toolDir).Length > 0)
        {
            var overwrite = AnsiConsole.Confirm(
                loc.ProtocolExportToolOverwriteConfirm(toolDir),
                false
            );

            if (!overwrite)
            {
                AnsiConsole.MarkupLine(loc.SkippedProtocolExportTool);
                return;
            }

            // 清理现有目录
            Directory.Delete(toolDir, true);
        }

        if (!Directory.Exists(toolDir))
        {
            Directory.CreateDirectory(toolDir);
        }

        await ExtractToolZipAsync("ProtocolExportTool", toolDir, ct);

        // 使 shell 脚本在 Unix 系统上可执行
        if (!OperatingSystem.IsWindows())
        {
            var runScript = Path.Combine(toolDir, "Run.sh");

            if (File.Exists(runScript))
            {
                await MakeExecutableAsync(runScript);
            }

            var toolExe = Path.Combine(toolDir, "Fantasy.Tools.NetworkProtocol");

            if (File.Exists(toolExe))
            {
                await MakeExecutableAsync(toolExe);
            }
        }

        AnsiConsole.MarkupLine(loc.ProtocolExportToolExtracted(toolDir));
    }

    /// <summary>
    /// 将 Fantasy.Net 框架提取到目标目录
    /// </summary>
    /// <param name="targetDirectory"></param>
    /// <param name="ct"></param>
    /// <param name="askOverwrite">是否询问覆盖确认（默认true）。新项目初始化时应设为false。</param>
    public async Task ExtractFantasyNetAsync(string targetDirectory, CancellationToken ct = default, bool askOverwrite = true)
    {
        var loc = LocalizationManager.Current;
        // 检查目标目录是否已有 Fantasy.Net 相关文件
        var toolsDir = Path.Combine(targetDirectory, "Tools");
        var fantasyNetDir = Path.Combine(toolsDir, "Fantasy.Net");

        if (!Directory.Exists(toolsDir))
        {
            // 如果 Tools 目录不存在，则创建该目录
            Directory.CreateDirectory(toolsDir);
        }

        if (Directory.Exists(fantasyNetDir) && Directory.GetFileSystemEntries(fantasyNetDir).Length > 0)
        {
            if (askOverwrite)
            {
                var overwrite = AnsiConsole.Confirm(
                    loc.FantasyNetOverwriteConfirm(fantasyNetDir),
                    false
                );

                if (!overwrite)
                {
                    AnsiConsole.MarkupLine(loc.SkippedFantasyNet);
                    return;
                }
            }

            // 清理现有目录
            Directory.Delete(fantasyNetDir, true);
        }

        await ExtractToolZipAsync("Fantasy.Net", fantasyNetDir, ct);

        AnsiConsole.MarkupLine(loc.FantasyNetExtracted(fantasyNetDir));
    }

    /// <summary>
    /// 将 Fantasy.Unity 框架提取到目标目录
    /// </summary>
    /// <param name="targetDirectory"></param>
    /// <param name="ct"></param>
    /// <param name="askOverwrite">是否询问覆盖确认（默认true）。新项目初始化时应设为false。</param>
    public async Task ExtractFantasyUnityAsync(string targetDirectory, CancellationToken ct = default, bool askOverwrite = true)
    {
        var loc = LocalizationManager.Current;
        // 检查目标目录是否已有 Fantasy.Unity 相关文件
        var toolsDir = Path.Combine(targetDirectory, "Tools");
        var fantasyUnityDir = Path.Combine(toolsDir, "Fantasy.Unity");

        if (!Directory.Exists(toolsDir))
        {
            // 如果 Tools 目录不存在，则创建该目录
            Directory.CreateDirectory(toolsDir);
        }

        if (Directory.Exists(fantasyUnityDir) && Directory.GetFileSystemEntries(fantasyUnityDir).Length > 0)
        {
            if (askOverwrite)
            {
                var overwrite = AnsiConsole.Confirm(
                    loc.FantasyUnityOverwriteConfirm(fantasyUnityDir),
                    false
                );

                if (!overwrite)
                {
                    AnsiConsole.MarkupLine(loc.SkippedFantasyUnity);
                    return;
                }
            }

            // 清理现有目录
            Directory.Delete(fantasyUnityDir, true);
        }

        await ExtractToolZipAsync("Fantasy.Unity", fantasyUnityDir, ct);

        AnsiConsole.MarkupLine(loc.FantasyUnityExtracted(fantasyUnityDir));
    }

    /// <summary>
    /// 将 NetworkProtocol 工具提取到目标目录
    /// </summary>
    /// <param name="targetDirectory"></param>
    /// <param name="ct"></param>
    /// <param name="askOverwrite">是否询问覆盖确认（默认true）。新项目初始化时应设为false。</param>
    public async Task ExtractNetworkProtocolAsync(string targetDirectory, CancellationToken ct = default, bool askOverwrite = true)
    {
        var loc = LocalizationManager.Current;
        var toolsDir = Path.Combine(targetDirectory, "Tools");
        var toolDir = Path.Combine(toolsDir, "NetworkProtocol");

        if (!Directory.Exists(toolsDir))
        {
            // 如果 Tools 目录不存在，则创建该目录
            Directory.CreateDirectory(toolsDir);
        }

        // 检查工具是否已存在
        if (askOverwrite && Directory.Exists(toolDir) && Directory.GetFileSystemEntries(toolDir).Length > 0)
        {
            var overwrite = AnsiConsole.Confirm(
                loc.NetworkProtocolOverwriteConfirm(toolDir),
                false
            );

            if (!overwrite)
            {
                AnsiConsole.MarkupLine(loc.SkippedNetworkProtocol);
                return;
            }

            // 清理现有目录
            Directory.Delete(toolDir, true);
        }

        if (!Directory.Exists(toolDir))
        {
            Directory.CreateDirectory(toolDir);
        }

        await ExtractToolZipAsync("NetworkProtocol", toolDir, ct);

        // 使 shell 脚本在 Unix 系统上可执行
        if (!OperatingSystem.IsWindows())
        {
            var runScript = Path.Combine(toolDir, "Run.sh");

            if (File.Exists(runScript))
            {
                await MakeExecutableAsync(runScript);
            }

            var toolExe = Path.Combine(toolDir, "Fantasy.Tools.NetworkProtocol");

            if (File.Exists(toolExe))
            {
                await MakeExecutableAsync(toolExe);
            }
        }

        AnsiConsole.MarkupLine(loc.NetworkProtocolExtracted(toolDir));
    }

    /// <summary>
    /// 将 NLog 组件提取到目标目录
    /// </summary>
    /// <param name="targetDirectory"></param>
    /// <param name="ct"></param>
    /// <param name="askOverwrite">是否询问覆盖确认（默认true）。新项目初始化时应设为false。</param>
    public Task ExtractNLogAsync(string targetDirectory, CancellationToken ct = default, bool askOverwrite = true)
    {
        var loc = LocalizationManager.Current;
        var toolsDir = Path.Combine(targetDirectory, "Tools");
        var nlogDir = Path.Combine(toolsDir, "NLog");

        if (!Directory.Exists(toolsDir))
        {
            // 如果 Tools 目录不存在，则创建该目录
            Directory.CreateDirectory(toolsDir);
        }

        return ExtractNLogByDirAsync(nlogDir, ct, askOverwrite);
    }
    
    /// <summary>
    /// 将 NLog 组件提取到目标目录
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="ct"></param>
    /// <param name="askOverwrite">是否询问覆盖确认（默认true）。新项目初始化时应设为false。</param>
    public async Task ExtractNLogByDirAsync(string dir, CancellationToken ct = default, bool askOverwrite = true)
    {
        var loc = LocalizationManager.Current;
        // 检查组件是否已存在
        if (askOverwrite && Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length > 0)
        {
            var overwrite = AnsiConsole.Confirm(
                loc.NLogOverwriteConfirm(dir),
                false
            );
            
            if (!overwrite)
            {
                AnsiConsole.MarkupLine(loc.SkippedNLog);
                return;
            }

            // 清理现有目录
            Directory.Delete(dir, true);
        }

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await ExtractToolZipAsync("NLog", dir, ct);

        AnsiConsole.MarkupLine(loc.NLogExtracted(dir));
    }

    /// <summary>
    /// 解压嵌入式工具 ZIP 压缩包
    /// </summary>
    /// <param name="toolName"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="ct"></param>
    private async Task ExtractToolZipAsync(string toolName, string targetDirectory, CancellationToken ct)
    {
        var resourceName = $"Fantasy.Cli.Resources.{toolName}.zip";
        await using var resourceStream = _assembly.GetManifestResourceStream(resourceName);

        if (resourceStream == null)
        {
            // 列出所有可用于调试的资源
            var availableResources = _assembly.GetManifestResourceNames();
            throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. " +
                $"Available resources: {string.Join(", ", availableResources)}"
            );
        }
        
        // 将 ZIP 文件解压到目标目录
        using var archive = new ZipArchive(resourceStream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(targetDirectory, overwriteFiles: true);
    }
    
    /// <summary>
    /// 使文件在 Unix 系统上可执行
    /// </summary>
    /// <param name="filePath"></param>
    private async Task MakeExecutableAsync(string filePath)
    {
        var loc = LocalizationManager.Current;
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(loc.MakeExecutableWarning(filePath, ex.Message));
        }
    }
}