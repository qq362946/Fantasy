using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fantasy.ProtocolEditor.Models;

namespace Fantasy.ProtocolEditor.Services;

/// <summary>
/// 配置文件解析服务，用于解析和保存 RoamingType.Config 和 RouteType.Config 文件
/// </summary>
public static class ConfigParser
{
    /// <summary>
    /// 解析 RoamingType.Config 文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>RoamingType 条目列表</returns>
    public static List<RoamingTypeEntry> ParseRoamingConfig(string filePath)
    {
        var entries = new List<RoamingTypeEntry>();

        if (!File.Exists(filePath))
        {
            return entries;
        }

        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过空行和注释行
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
            {
                continue;
            }

            // 解析格式: MapRoamingType = 10001
            var parts = trimmedLine.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var name = parts[0].Trim();
                if (int.TryParse(parts[1].Trim(), out var value))
                {
                    entries.Add(new RoamingTypeEntry
                    {
                        Name = name,
                        Value = value
                    });
                }
            }
        }

        return entries;
    }

    /// <summary>
    /// 保存 RoamingType.Config 文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="entries">RoamingType 条目列表</param>
    public static void SaveRoamingConfig(string filePath, IEnumerable<RoamingTypeEntry> entries)
    {
        var sb = new StringBuilder();

        // 添加文件头注释
        sb.AppendLine("// Roaming协议定义(需要定义10000以上、因为10000以内的框架预留)");

        // 按值排序后写入
        var sortedEntries = entries.OrderBy(e => e.Value).ToList();

        foreach (var entry in sortedEntries)
        {
            if (entry.IsValid())
            {
                sb.AppendLine($"{entry.Name} = {entry.Value}");
            }
        }

        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// 验证 Roaming 类型值是否重复
    /// </summary>
    public static Dictionary<int, List<string>> FindDuplicateValues(IEnumerable<RoamingTypeEntry> entries)
    {
        return entries
            .GroupBy(e => e.Value)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Name).ToList());
    }

    /// <summary>
    /// 验证 Roaming 类型名称是否重复
    /// </summary>
    public static Dictionary<string, List<int>> FindDuplicateNames(IEnumerable<RoamingTypeEntry> entries)
    {
        return entries
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Value).ToList());
    }

    /// <summary>
    /// 解析 RouteType.Config 文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>RouteType 条目列表</returns>
    public static List<RouteTypeEntry> ParseRouteConfig(string filePath)
    {
        var entries = new List<RouteTypeEntry>();

        if (!File.Exists(filePath))
        {
            return entries;
        }

        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过空行和注释行
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
            {
                continue;
            }

            // 解析格式: PlayerRouteType = 1001
            var parts = trimmedLine.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var name = parts[0].Trim();
                if (int.TryParse(parts[1].Trim(), out var value))
                {
                    entries.Add(new RouteTypeEntry
                    {
                        Name = name,
                        Value = value
                    });
                }
            }
        }

        return entries;
    }

    /// <summary>
    /// 保存 RouteType.Config 文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="entries">RouteType 条目列表</param>
    public static void SaveRouteConfig(string filePath, IEnumerable<RouteTypeEntry> entries)
    {
        var sb = new StringBuilder();

        // 添加文件头注释
        sb.AppendLine("// Route协议定义(需要定义1000以上、因为1000以内的框架预留)");

        // 按值排序后写入
        var sortedEntries = entries.OrderBy(e => e.Value).ToList();

        foreach (var entry in sortedEntries)
        {
            if (entry.IsValid())
            {
                sb.AppendLine($"{entry.Name} = {entry.Value}");
            }
        }

        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// 验证 Route 类型值是否重复
    /// </summary>
    public static Dictionary<int, List<string>> FindDuplicateRouteValues(IEnumerable<RouteTypeEntry> entries)
    {
        return entries
            .GroupBy(e => e.Value)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Name).ToList());
    }

    /// <summary>
    /// 验证 Route 类型名称是否重复
    /// </summary>
    public static Dictionary<string, List<int>> FindDuplicateRouteNames(IEnumerable<RouteTypeEntry> entries)
    {
        return entries
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Value).ToList());
    }
}
