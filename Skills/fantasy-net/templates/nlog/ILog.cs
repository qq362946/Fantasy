#if FANTASY_NET
using Fantasy.Platform.Net;
#endif

namespace Fantasy
{
    public partial interface ILog
    {
#if FANTASY_NET
        /// <summary>
        /// 框架启动时回调，根据运行模式（Develop/Release）调整日志行为
        /// </summary>
        void Initialize(ProcessMode processMode);
#endif
        void Trace(string message);
        void Warning(string message);
        void Info(string message);
        void Debug(string message);
        void Error(string message);

        void Trace(string message, params object[] args);
        void Warning(string message, params object[] args);
        void Info(string message, params object[] args);
        void Debug(string message, params object[] args);
        void Error(string message, params object[] args);
    }
}
