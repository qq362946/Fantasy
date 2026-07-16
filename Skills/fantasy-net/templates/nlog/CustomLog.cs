#if FANTASY_NET
using Fantasy.Platform.Net;
#endif
using System;
using Fantasy;

namespace MyGame
{
    /// <summary>
    /// 自定义日志最小实现模板。
    /// 将类名替换为你的实际名称，在 Initialize() 中根据 processMode 做初始化（可为空）。
    /// </summary>
    public sealed class MyLog : ILog
    {
#if FANTASY_NET
        public void Initialize(string appId, ProcessMode processMode)
        {
            // 可按进程ID和运行模式设置日志级别或输出目标。
        }
#endif

        public void Trace(string message)   => Write("TRACE", "Log", message);
        public void Debug(string message)   => Write("DEBUG", "Log", message);
        public void Info(string message)    => Write("INFO", "Log", message);
        public void Warning(string message) => Write("WARN", "Log", message);
        public void Error(string message)   => Write("ERROR", "Log", message);

        public void Trace(string sceneName, string message)   => Write("TRACE", sceneName, message);
        public void Debug(string sceneName, string message)   => Write("DEBUG", sceneName, message);
        public void Info(string sceneName, string message)    => Write("INFO", sceneName, message);
        public void Warning(string sceneName, string message) => Write("WARN", sceneName, message);
        public void Error(string sceneName, string message)   => Write("ERROR", sceneName, message);

        public void Trace(string message, params object[] args)   => Write("TRACE", "Log", message, args);
        public void Debug(string message, params object[] args)   => Write("DEBUG", "Log", message, args);
        public void Info(string message, params object[] args)    => Write("INFO", "Log", message, args);
        public void Warning(string message, params object[] args) => Write("WARN", "Log", message, args);
        public void Error(string message, params object[] args)   => Write("ERROR", "Log", message, args);

        public void Trace(string sceneName, string message, params object[] args)   => Write("TRACE", sceneName, message, args);
        public void Debug(string sceneName, string message, params object[] args)   => Write("DEBUG", sceneName, message, args);
        public void Info(string sceneName, string message, params object[] args)    => Write("INFO", sceneName, message, args);
        public void Warning(string sceneName, string message, params object[] args) => Write("WARN", sceneName, message, args);
        public void Error(string sceneName, string message, params object[] args)   => Write("ERROR", sceneName, message, args);

        private static void Write(string level, string sceneName, string message, params object[] args)
        {
            var text = args.Length == 0 ? message : string.Format(message, args);
            Console.WriteLine($"[{level}] [{sceneName}] {text}");
        }
    }
}
