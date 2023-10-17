#if FANTASY_NET
namespace Fantasy.Core;

/// <summary>
/// 定义包含 Fantasy 系统设置的静态类。
/// </summary>
public static class Define
{
#if FANTASY_NET
    
    /// <summary>
    /// Excel服务器二进制数据文件夹
    /// </summary>
    public static string ExcelServerBinaryDirectory;
    #region Network
    /// <summary>
    /// 会话空闲检查间隔。
    /// </summary>
    public static int SessionIdleCheckerInterval;
    /// <summary>
    /// 会话空闲检查超时时间。
    /// </summary>
    public static int SessionIdleCheckerTimeout;

    #endregion

#endif
}
#endif