using System;
using System.IO;
using System.Text.Json;
using Fantasy.ProtocolEditor.Models;

namespace Fantasy.ProtocolEditor.Services;

/// <summary>
/// 配置服务，用于保存和加载工作区配置
/// </summary>
public class ConfigService
{
    private const string ConfigFileName = "workspace.json";
    private const string LastConfigPathFileName = "last-config-path.txt";

    private static readonly string LastConfigPathFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        LastConfigPathFileName);

    /// <summary>
    /// 默认配置文件路径，位于进程当前目录。
    /// </summary>
    public static string DefaultConfigFilePath { get; } =
        Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);

    /// <summary>
    /// 当前使用的配置文件路径。
    /// </summary>
    public static string CurrentConfigFilePath { get; private set; } = DefaultConfigFilePath;

    /// <summary>
    /// 切换当前使用的配置文件。
    /// </summary>
    public static void SetConfigFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Config file path cannot be empty.", nameof(filePath));
        }

        CurrentConfigFilePath = Path.GetFullPath(filePath);
    }

    /// <summary>
    /// 保存上次使用的配置文件路径。
    /// </summary>
    public static void SaveLastConfigFilePath()
    {
        try
        {
            var configDirectory = Path.GetDirectoryName(LastConfigPathFilePath);
            if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            File.WriteAllText(LastConfigPathFilePath, CurrentConfigFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save last config path: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试恢复上次使用的配置文件路径。
    /// </summary>
    public static bool TryUseLastConfigFile(out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            if (!File.Exists(LastConfigPathFilePath))
            {
                return false;
            }

            var filePath = File.ReadAllText(LastConfigPathFilePath).Trim();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            if (!TryValidateConfigFile(filePath, out errorMessage))
            {
                return false;
            }

            SetConfigFilePath(filePath);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"读取上次配置文件记录失败：{ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 确保当前配置文件存在，不存在时创建默认配置。
    /// </summary>
    public static void EnsureConfigFile()
    {
        if (File.Exists(CurrentConfigFilePath))
        {
            return;
        }

        SaveConfig(new WorkspaceConfig());
    }

    /// <summary>
    /// 验证指定文件是否为合法工作区配置文件。
    /// </summary>
    public static bool TryValidateConfigFile(string filePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "配置文件路径为空";
                return false;
            }

            if (!File.Exists(filePath))
            {
                errorMessage = $"配置文件不存在：{filePath}";
                return false;
            }

            var json = File.ReadAllText(filePath);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errorMessage = "配置文件根节点必须是 JSON 对象";
                return false;
            }

            var root = document.RootElement;
            var hasKnownWorkspaceField =
                root.TryGetProperty(nameof(WorkspaceConfig.WorkspacePath), out _) ||
                root.TryGetProperty(nameof(WorkspaceConfig.OpenedTabs), out _) ||
                root.TryGetProperty(nameof(WorkspaceConfig.ActiveTabFilePath), out _) ||
                root.TryGetProperty("serverOutputDirectory", out _) ||
                root.TryGetProperty("clientOutputDirectory", out _) ||
                root.TryGetProperty("exportToServer", out _) ||
                root.TryGetProperty("exportToClient", out _);
            if (!hasKnownWorkspaceField)
            {
                errorMessage = "未找到工作区配置字段";
                return false;
            }

            var config = JsonSerializer.Deserialize<WorkspaceConfig>(json);
            if (config == null)
            {
                errorMessage = "配置文件内容为空或格式不正确";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = $"配置文件不是合法 JSON：{ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"读取配置文件失败：{ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 保存工作区配置
    /// </summary>
    public static void SaveConfig(WorkspaceConfig config)
    {
        SaveConfigAs(CurrentConfigFilePath, config);
    }

    /// <summary>
    /// 保存工作区配置到指定文件，不切换当前配置路径。
    /// </summary>
    public static void SaveConfigAs(string filePath, WorkspaceConfig config)
    {
        try
        {
            var configDirectory = Path.GetDirectoryName(filePath);

            // 确保配置目录存在
            if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // 序列化配置为 JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(config, options);

            // 写入文件
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 加载工作区配置
    /// </summary>
    public static WorkspaceConfig? LoadConfig()
    {
        try
        {
            // 检查配置文件是否存在
            if (!File.Exists(CurrentConfigFilePath))
            {
                return null;
            }

            // 读取文件
            var json = File.ReadAllText(CurrentConfigFilePath);

            // 反序列化
            var config = JsonSerializer.Deserialize<WorkspaceConfig>(json);

            return config;
        }
        catch (Exception ex)
        {
            // 忽略加载错误，返回 null
            Console.WriteLine($"Failed to load config: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 清除配置文件
    /// </summary>
    public static void ClearConfig()
    {
        try
        {
            if (File.Exists(CurrentConfigFilePath))
            {
                File.Delete(CurrentConfigFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear config: {ex.Message}");
        }
    }
}
