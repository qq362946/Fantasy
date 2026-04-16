#if FANTASY_NET
using Fantasy.Platform.Net;
#endif

namespace MyGame
{
    /// <summary>
    /// 自定义日志最小实现模板。
    /// 将类名替换为你的实际名称，在 Initialize() 中根据 processMode 做初始化（可为空）。
    /// </summary>
    public sealed class MyLog : ILog
    {
#if FANTASY_NET
        public void Initialize(ProcessMode processMode)
        {
            // 根据运行模式做初始化，例如设置日志级别或切换输出目标
        }
#endif

        public void Trace(string message)   => Console.WriteLine($"[TRACE] {message}");
        public void Debug(string message)   => Console.WriteLine($"[DEBUG] {message}");
        public void Info(string message)    => Console.WriteLine($"[INFO]  {message}");
        public void Warning(string message) => Console.WriteLine($"[WARN]  {message}");
        public void Error(string message)   => Console.WriteLine($"[ERROR] {message}");

        public void Trace(string message, params object[] args)   => Trace(string.Format(message, args));
        public void Debug(string message, params object[] args)   => Debug(string.Format(message, args));
        public void Info(string message, params object[] args)    => Info(string.Format(message, args));
        public void Warning(string message, params object[] args) => Warning(string.Format(message, args));
        public void Error(string message, params object[] args)   => Error(string.Format(message, args));
    }
}
