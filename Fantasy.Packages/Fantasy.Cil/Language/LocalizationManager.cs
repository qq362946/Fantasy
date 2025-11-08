namespace Fantasy.Cli.Language;

/// <summary>
/// CLI 的全球本地化管理器
/// </summary>
public static class LocalizationManager
{
    private static ILocalization? _current;

    /// <summary>
    /// 当前本地化实例
    /// </summary>
    public static ILocalization Current
    {
        get => _current ?? throw new InvalidOperationException("Localization not initialized. Call Initialize() first.");
        private set => _current = value;
    }

    /// <summary>
    /// 使用指定语言初始化本地化
    /// </summary>
    public static void Initialize(Language language)
    {
        Current = language switch
        {
            Language.Chinese => new LocalizationChinese(),
            Language.English => new LocalizationEnglish(),
            _ => new LocalizationEnglish()
        };
    }

    /// <summary>
    /// 检查本地化是否已初始化。
    /// </summary>
    public static bool IsInitialized => _current != null;
}
