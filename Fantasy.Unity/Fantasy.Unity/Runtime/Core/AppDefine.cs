#if FANTASY_NET
namespace Fantasy
{
    /// <summary>
    /// AppDefine
    /// </summary>
    public static class AppDefine
    {
        /// <summary>
        /// 命令行选项
        /// </summary>
        public static CommandLineOptions Options;
        /// <summary>
        /// App程序Id
        /// </summary>
        public static uint AppId => Options.AppId;
        /// <summary>
        /// 会话空闲检查超时时间。
        /// </summary>
        public static int SessionIdleCheckerTimeout => Options.SessionIdleCheckerTimeout;
        /// <summary>
        /// 会话空闲检查间隔。
        /// </summary>
        public static int SessionIdleCheckerInterval => Options.SessionIdleCheckerInterval;
        /// <summary>
        /// 配置表文件夹路径
        /// </summary>
        public static string ConfigTableBinaryDirectory => Options.ConfigTableBinaryDirectory;
        /// <summary>
        /// 内部网络通讯协议类型
        /// </summary>
        public static NetworkProtocolType InnerNetwork;
    }
}
#endif