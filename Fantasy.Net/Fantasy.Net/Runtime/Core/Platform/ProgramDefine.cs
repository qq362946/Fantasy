#if FANTASY_NET
using Fantasy.Network;
using Fantasy.Platform.Net;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#endif

namespace Fantasy
{
    /// <summary>
    /// 程序定义
    /// </summary>
    public static class ProgramDefine
    {
        /// <summary>
        /// Fantasy版本。
        /// </summary>
        public const string VERSION = "Fantasy 2.0 Beta";
        /// <summary>
        /// 消息体最大长度(字节)。
        /// </summary>
        public static int MaxMessageSize { get; internal set; }
#if FANTASY_NET
        /// <summary>
        /// App程序Id。
        /// </summary>
        public static uint ProcessId { get; internal set; }
        /// <summary>
        /// 应用程序的类型。
        /// </summary>
        public static string ProcessType { get; internal set; }
        /// <summary>
        /// 服务器运行模式，获取或设置服务器的运行模式。
        /// </summary>
        public static ProcessMode RuntimeMode { get; internal set; }
        /// <summary>
        /// 服务器启动组
        /// </summary>
        public static int StartupGroup { get; internal set; }
        /// <summary>
        /// 会话空闲检查超时时间。
        /// </summary>
        public static int SessionIdleCheckerTimeout { get; internal set; }
        /// <summary>
        /// 会话空闲检查间隔。
        /// </summary>
        public static int SessionIdleCheckerInterval { get; internal set; }
        /// <summary>
        /// 内部网络通讯协议类型。
        /// </summary>
        public static NetworkProtocolType InnerNetwork { get; internal set; }
#endif
    }
}