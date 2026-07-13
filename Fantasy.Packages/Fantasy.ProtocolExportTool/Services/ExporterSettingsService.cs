using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Models;

namespace Fantasy.ProtocolExportTool.Services;

/// <summary>
/// ExporterSettings.json 的共享加载、保存和路径解析服务。
/// 命令行工具与可视化编辑器必须通过该服务使用导出配置。
/// </summary>
public static class ExporterSettingsService
{
    public const string FileName = "ExporterSettings.json";
    private static readonly TimeSpan FileLockTimeout = TimeSpan.FromSeconds(3);

    private static readonly JsonSerializerOptions ReadOptions = CreateReadOptions();
    private static readonly JsonSerializerOptions WriteOptions = CreateWriteOptions();

    public static async Task<ExporterSettings> LoadAsync(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"找不到配置文件 '{fullPath}'。", fullPath);
        }

        var json = await File.ReadAllTextAsync(fullPath);
        return DeserializeAndNormalize(json, fullPath);
    }

    public static ExporterSettings Load(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"找不到配置文件 '{fullPath}'。", fullPath);
        }

        string json = string.Empty;
        ExecuteWithFileLock(fullPath, () => json = File.ReadAllText(fullPath));
        return DeserializeAndNormalize(json, fullPath);
    }

    public static bool TryLoad(string filePath, out ExporterSettings? settings, out string errorMessage)
    {
        try
        {
            settings = Load(filePath);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            settings = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    public static void Save(string filePath, ExporterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Normalize(settings);
        var json = JsonSerializer.Serialize(settings, WriteOptions);
        WriteAllTextAtomic(filePath, json);
    }

    public static ExporterSettings CreateDefault()
    {
        return new ExporterSettings
        {
            Export = new ExportSettings
            {
                NetworkProtocolDirectory = new SettingItem
                {
                    Value = ".",
                    Comment = "ProtoBuf文件所在的文件夹位置"
                },
                NetworkProtocolServerDirectory = new SettingItem
                {
                    Value = string.Empty,
                    Comment = "ProtoBuf生成到服务端的文件夹位置"
                },
                NetworkProtocolClientDirectory = new SettingItem
                {
                    Value = string.Empty,
                    Comment = "ProtoBuf生成到客户端的文件夹位置"
                },
                SharedOpCodeCacheFile = new SettingItem
                {
                    Value = "OpCode.Cache",
                    Comment = "主协议与子包共享的OpCode缓存文件"
                },
                ExportType = ProtocolExportType.All,
                PackageExports = []
            }
        };
    }

    /// <summary>
    /// 将持久化配置转换为导出配置。相对路径优先以配置文件目录为基准；
    /// 若旧配置仅在当前工作目录下能找到协议目录，则整组路径兼容使用当前工作目录。
    /// </summary>
    public static ProtocolExportConfig CreateExportConfig(ExporterSettings settings, string settingsFilePath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Normalize(settings);

        var settingsPath = Path.GetFullPath(settingsFilePath);
        var settingsDirectory = Path.GetDirectoryName(settingsPath) ?? Directory.GetCurrentDirectory();
        var pathBase = DeterminePathBase(settings.Export.NetworkProtocolDirectory.Value, settingsDirectory);

        return new ProtocolExportConfig
        {
            ProtocolDir = ResolvePath(settings.Export.NetworkProtocolDirectory.Value, pathBase),
            ServerDir = ResolveOptionalPath(settings.Export.NetworkProtocolServerDirectory.Value, pathBase),
            ClientDir = ResolveOptionalPath(settings.Export.NetworkProtocolClientDirectory.Value, pathBase),
            OpCodeCacheFile = ResolveOptionalPath(settings.Export.SharedOpCodeCacheFile.Value, pathBase),
            ExportType = settings.Export.ExportType,
            PackageExports = settings.Export.PackageExports.ConvertAll(package => new ProtocolExportTarget
            {
                ProtocolDir = ResolvePath(package.NetworkProtocolDirectory.Value, pathBase),
                ServerDir = ResolveOptionalPath(package.NetworkProtocolServerDirectory.Value, pathBase),
                ClientDir = ResolveOptionalPath(package.NetworkProtocolClientDirectory.Value, pathBase),
                ExportType = package.ExportType
            })
        };
    }

    public static string ResolvePath(string path, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmedPath = path.Trim();
        return Path.IsPathRooted(trimmedPath)
            ? Path.GetFullPath(trimmedPath)
            : Path.GetFullPath(Path.Combine(baseDirectory, trimmedPath));
    }

    private static string ResolveOptionalPath(string path, string baseDirectory)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : ResolvePath(path, baseDirectory);
    }

    private static string DeterminePathBase(string protocolDirectory, string settingsDirectory)
    {
        if (string.IsNullOrWhiteSpace(protocolDirectory) || Path.IsPathRooted(protocolDirectory))
        {
            return settingsDirectory;
        }

        var configRelativeProtocolPath = Path.GetFullPath(Path.Combine(settingsDirectory, protocolDirectory));
        if (Directory.Exists(configRelativeProtocolPath))
        {
            return settingsDirectory;
        }

        var workingDirectory = Directory.GetCurrentDirectory();
        var legacyProtocolPath = Path.GetFullPath(Path.Combine(workingDirectory, protocolDirectory));
        return Directory.Exists(legacyProtocolPath) ? workingDirectory : settingsDirectory;
    }

    private static ExporterSettings DeserializeAndNormalize(string json, string filePath)
    {
        var settings = JsonSerializer.Deserialize<ExporterSettings>(json, ReadOptions)
                       ?? throw new JsonException($"配置文件 '{filePath}' 内容为空或格式不正确。");
        Normalize(settings);
        return settings;
    }

    private static void Normalize(ExporterSettings settings)
    {
        settings.Export ??= new ExportSettings();
        settings.Export.NetworkProtocolDirectory ??= new SettingItem();
        settings.Export.NetworkProtocolServerDirectory ??= new SettingItem();
        settings.Export.NetworkProtocolClientDirectory ??= new SettingItem();
        settings.Export.SharedOpCodeCacheFile ??= new SettingItem();
        settings.Export.PackageExports ??= [];

        foreach (var package in settings.Export.PackageExports)
        {
            package.NetworkProtocolDirectory ??= new SettingItem();
            package.NetworkProtocolServerDirectory ??= new SettingItem();
            package.NetworkProtocolClientDirectory ??= new SettingItem();
        }
    }

    private static JsonSerializerOptions CreateReadOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static JsonSerializerOptions CreateWriteOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static void WriteAllTextAtomic(string filePath, string content)
    {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ExecuteWithFileLock(fullPath, () =>
        {
            var tempFilePath = Path.Combine(
                directory ?? Path.GetTempPath(),
                $".{Path.GetFileName(fullPath)}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");

            try
            {
                File.WriteAllText(tempFilePath, content, new UTF8Encoding(false));
                File.Move(tempFilePath, fullPath, true);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch
                {
                    // 临时文件清理失败不能掩盖原始保存结果。
                }
            }
        });
    }

    private static void ExecuteWithFileLock(string filePath, Action action)
    {
        using var mutex = new Mutex(false, GetFileMutexName(filePath));
        var lockTaken = false;

        try
        {
            try
            {
                lockTaken = mutex.WaitOne(FileLockTimeout);
            }
            catch (AbandonedMutexException)
            {
                lockTaken = true;
            }

            if (!lockTaken)
            {
                throw new IOException($"等待配置文件锁超时：{filePath}");
            }

            action();
        }
        finally
        {
            if (lockTaken)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private static string GetFileMutexName(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (OperatingSystem.IsWindows())
        {
            fullPath = fullPath.ToUpperInvariant();
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fullPath));
        return $"Fantasy.ProtocolExporter.{Convert.ToHexString(hash)}";
    }
}
