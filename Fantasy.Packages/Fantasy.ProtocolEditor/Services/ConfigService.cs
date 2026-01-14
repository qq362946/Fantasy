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
    // 配置文件保存在程序运行目录
    private static readonly string ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory;

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "workspace.json");

    /// <summary>
    /// 保存工作区配置
    /// </summary>
    public static void SaveConfig(WorkspaceConfig config)
    {
        try
        {
            // 确保配置目录存在
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            // 序列化配置为 JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(config, options);

            // 写入文件
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            // 忽略保存错误，不影响程序运行
            Console.WriteLine($"Failed to save config: {ex.Message}");
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
            if (!File.Exists(ConfigFilePath))
            {
                return null;
            }

            // 读取文件
            var json = File.ReadAllText(ConfigFilePath);

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
            if (File.Exists(ConfigFilePath))
            {
                File.Delete(ConfigFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear config: {ex.Message}");
        }
    }
}
