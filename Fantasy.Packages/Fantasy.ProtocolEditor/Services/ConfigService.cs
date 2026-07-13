using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Fantasy.ProtocolEditor.Models;
using Fantasy.ProtocolExportTool.Models;
using Fantasy.ProtocolExportTool.Services;

namespace Fantasy.ProtocolEditor.Services;

/// <summary>
/// 管理编辑器当前使用的协议工作区。
/// ExporterSettings.json 与 editor-state.json 固定保存在协议工作区根目录。
/// </summary>
public static class ConfigService
{
    public const string EditorStateFileName = "editor-state.json";
    private const string LastWorkspaceDirectoryFileName = "last-workspace-directory.txt";
    private static readonly TimeSpan FileLockTimeout = TimeSpan.FromSeconds(3);

    private static readonly JsonSerializerOptions EditorStateReadOptions = CreateEditorStateReadOptions();
    private static readonly JsonSerializerOptions EditorStateWriteOptions = CreateEditorStateWriteOptions();

    private static readonly string ApplicationDataDirectory = GetApplicationDataDirectory();
    private static readonly string LastWorkspaceDirectoryFilePath = Path.Combine(
        ApplicationDataDirectory,
        LastWorkspaceDirectoryFileName);

    public static string CurrentWorkspaceDirectory { get; private set; } = string.Empty;

    public static string CurrentExporterSettingsFilePath => string.IsNullOrWhiteSpace(CurrentWorkspaceDirectory)
        ? string.Empty
        : Path.Combine(CurrentWorkspaceDirectory, ExporterSettingsService.FileName);

    public static string CurrentEditorStateFilePath => string.IsNullOrWhiteSpace(CurrentWorkspaceDirectory)
        ? string.Empty
        : Path.Combine(CurrentWorkspaceDirectory, EditorStateFileName);

    public static bool HasCurrentWorkspace => !string.IsNullOrWhiteSpace(CurrentWorkspaceDirectory);

    /// <summary>
    /// 激活一个协议工作区。两份配置文件都位于工作区根目录，缺失时按需创建。
    /// </summary>
    public static bool TryActivateWorkspaceDirectory(
        string path,
        bool createExporterSettingsIfMissing,
        out bool exporterSettingsCreated,
        out bool editorStateCreated,
        out string errorMessage)
    {
        exporterSettingsCreated = false;
        editorStateCreated = false;
        errorMessage = string.Empty;

        try
        {
            var workspaceDirectory = NormalizeWorkspaceDirectory(path);
            if (!Directory.Exists(workspaceDirectory))
            {
                if (!createExporterSettingsIfMissing)
                {
                    errorMessage = $"协议工作区不存在：{workspaceDirectory}";
                    return false;
                }

                Directory.CreateDirectory(workspaceDirectory);
            }

            var exporterSettingsPath = Path.Combine(workspaceDirectory, ExporterSettingsService.FileName);
            ExporterSettings settings;
            if (!File.Exists(exporterSettingsPath))
            {
                if (!createExporterSettingsIfMissing)
                {
                    errorMessage = $"协议工作区中未找到 {ExporterSettingsService.FileName}：{workspaceDirectory}";
                    return false;
                }

                settings = ExporterSettingsService.CreateDefault();
                exporterSettingsCreated = true;
            }
            else if (!ExporterSettingsService.TryLoad(exporterSettingsPath, out var loadedSettings, out errorMessage))
            {
                return false;
            }
            else
            {
                settings = loadedSettings!;
            }

            // 协议目录就是工作区本身，避免再出现配置目录与协议目录两个概念。
            if (exporterSettingsCreated || settings.Export.NetworkProtocolDirectory.Value != ".")
            {
                settings.Export.NetworkProtocolDirectory.Value = ".";
                ExporterSettingsService.Save(exporterSettingsPath, settings);
            }

            var editorStatePath = Path.Combine(workspaceDirectory, EditorStateFileName);
            if (!File.Exists(editorStatePath))
            {
                SaveEditorStateToFile(editorStatePath, new EditorState());
                editorStateCreated = true;
            }
            else if (!TryLoadEditorStateFile(editorStatePath, out _, out errorMessage))
            {
                return false;
            }

            CurrentWorkspaceDirectory = workspaceDirectory;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static bool TryUseLastWorkspaceDirectory(out string errorMessage)
    {
        try
        {
            if (!File.Exists(LastWorkspaceDirectoryFilePath))
            {
                errorMessage = string.Empty;
                return false;
            }

            var recordedPath = ReadAllTextLocked(LastWorkspaceDirectoryFilePath).Trim();
            if (string.IsNullOrWhiteSpace(recordedPath))
            {
                errorMessage = string.Empty;
                return false;
            }

            return TryActivateWorkspaceDirectory(recordedPath, false, out _, out _, out errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"读取上次协议工作区失败：{ex.Message}";
            return false;
        }
    }

    public static void SaveLastWorkspaceDirectory()
    {
        if (!HasCurrentWorkspace)
        {
            return;
        }

        try
        {
            WriteAllTextAtomic(LastWorkspaceDirectoryFilePath, CurrentWorkspaceDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save last workspace directory: {ex.Message}");
        }
    }

    public static ExporterSettings LoadExporterSettings()
    {
        EnsureCurrentWorkspaceDirectory();
        return ExporterSettingsService.Load(CurrentExporterSettingsFilePath);
    }

    public static void SaveExporterSettings(ExporterSettings settings)
    {
        EnsureCurrentWorkspaceDirectory();
        ExporterSettingsService.Save(CurrentExporterSettingsFilePath, settings);
    }

    public static ProtocolExportConfig CreateProtocolExportConfig(ExporterSettings settings)
    {
        EnsureCurrentWorkspaceDirectory();
        return ExporterSettingsService.CreateExportConfig(settings, CurrentExporterSettingsFilePath);
    }

    public static EditorState LoadEditorState()
    {
        EnsureCurrentWorkspaceDirectory();

        if (!TryLoadEditorStateFile(CurrentEditorStateFilePath, out var state, out var errorMessage))
        {
            throw new InvalidDataException(errorMessage);
        }

        return state!;
    }

    public static void SaveEditorState(EditorState state)
    {
        EnsureCurrentWorkspaceDirectory();
        SaveEditorStateToFile(CurrentEditorStateFilePath, state);
    }

    public static string NormalizeWorkspaceDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("协议工作区不能为空。", nameof(path));
        }

        var fullPath = Path.GetFullPath(path.Trim());
        if (File.Exists(fullPath))
        {
            if (!string.Equals(
                    Path.GetFileName(fullPath),
                    ExporterSettingsService.FileName,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                throw new InvalidDataException($"请选择协议工作区，或工作区中的 {ExporterSettingsService.FileName}。");
            }

            return Path.GetDirectoryName(fullPath)
                   ?? throw new InvalidDataException("无法确定协议工作区目录。");
        }

        if (string.Equals(
                Path.GetFileName(fullPath),
                ExporterSettingsService.FileName,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return Path.GetDirectoryName(fullPath)
                   ?? throw new InvalidDataException("无法确定协议工作区目录。");
        }

        return fullPath;
    }

    private static bool TryLoadEditorStateFile(
        string filePath,
        out EditorState? state,
        out string errorMessage)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                state = null;
                errorMessage = $"找不到编辑器状态文件：{filePath}";
                return false;
            }

            var json = ReadAllTextLocked(filePath);
            state = JsonSerializer.Deserialize<EditorState>(json, EditorStateReadOptions);
            if (state == null)
            {
                errorMessage = "editor-state.json 内容为空或格式不正确。";
                return false;
            }

            state.OpenedTabs ??= [];
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            state = null;
            errorMessage = $"读取 editor-state.json 失败：{ex.Message}";
            return false;
        }
    }

    private static void SaveEditorStateToFile(string filePath, EditorState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.OpenedTabs ??= [];
        var json = JsonSerializer.Serialize(state, EditorStateWriteOptions);
        WriteAllTextAtomic(filePath, json);
    }

    private static void EnsureCurrentWorkspaceDirectory()
    {
        if (!HasCurrentWorkspace)
        {
            throw new InvalidOperationException("尚未选择协议工作区。");
        }
    }

    private static string GetApplicationDataDirectory()
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Path.GetTempPath();
        }

        return Path.Combine(baseDirectory, "Fantasy", "ProtocolEditor");
    }

    private static JsonSerializerOptions CreateEditorStateReadOptions()
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

    private static JsonSerializerOptions CreateEditorStateWriteOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string ReadAllTextLocked(string filePath)
    {
        string content = string.Empty;
        ExecuteWithFileLock(filePath, () => content = File.ReadAllText(filePath));
        return content;
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
        return $"Fantasy.ProtocolEditor.State.{Convert.ToHexString(hash)}";
    }
}
