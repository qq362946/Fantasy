#if FANTASY_NET

using Microsoft.Extensions.Configuration;
#pragma warning disable CS8604
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace Fantasy;

/// <summary>
/// FantasySettingsHelper 类用于加载和管理 Fantasy 系统的各项设置。
/// </summary>
public static class FantasySettingsHelper
{
    /// <summary>
    /// 初始化 Fantasy 系统的各项设置。
    /// </summary>
    public static void Initialize()
    {
        const string settingsName = "FantasySettings.json";
        var currentDirectory = Directory.GetCurrentDirectory();

        if (!File.Exists(Path.Combine(currentDirectory, settingsName)))
        {
            throw new FileNotFoundException($"not found {settingsName} in OutputDirectory");
        }

        var configurationRoot = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(settingsName).Build();
        // 加载网络配置
        LoadNetworkConfig(configurationRoot);
        // 加载Excel配置
        LoadExcelConfig(configurationRoot);
    }

    private static void LoadNetworkConfig(IConfigurationRoot root)
    {
        Define.SessionIdleCheckerInterval = Convert.ToInt32(root["Network:SessionIdleCheckerInterval:Value"]);
        Define.SessionIdleCheckerTimeout = Convert.ToInt32(root["Network:SessionIdleCheckerTimeout:Value"]);
    }

    private static void LoadExcelConfig(IConfigurationRoot root)
    {
        // Excel生成服务器二进制数据文件夹位置
        Define.ExcelServerBinaryDirectory = FileHelper.GetFullPath(root["Export:ExcelServerBinaryDirectory:Value"]);
    }
}
#endif