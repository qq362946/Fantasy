using System;
using System.Diagnostics;

namespace Fantasy
{
    /// <summary>
    /// 提供日志记录功能的静态类。
    /// </summary>
    public static class Log
    {
        private static readonly ILog LogCore;

        static Log()
        {
#if FANTASY_NET
            LogCore = new NLog("Server");
#elif FANTASY_UNITY
            LogCore = new UnityLog();
#endif
        }

        /// <summary>
        /// 记录跟踪级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Trace(string msg)
        {
            var st = new StackTrace(1, true);
            LogCore.Trace($"{msg}\n{st}");
        }

        /// <summary>
        /// 记录调试级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Debug(string msg)
        {
            LogCore.Debug(msg);
        }

        /// <summary>
        /// 记录信息级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Info(string msg)
        {
            LogCore.Info(msg);
        }

        /// <summary>
        /// 记录跟踪级别的日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void TraceInfo(string msg)
        {
            var st = new StackTrace(1, true);
            LogCore.Trace($"{msg}\n{st}");
        }

        /// <summary>
        /// 记录警告级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Warning(string msg)
        {
            LogCore.Warning(msg);
        }

        /// <summary>
        /// 记录错误级别的日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Error(string msg)
        {
            var st = new StackTrace(1, true);
            LogCore.Error($"{msg}\n{st}");
        }

        /// <summary>
        /// 记录异常的错误级别的日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="e">异常对象。</param>
        public static void Error(Exception e)
        {
            if (e.Data.Contains("StackTrace"))
            {
                LogCore.Error($"{e.Data["StackTrace"]}\n{e}");
                return;
            }
            var str = e.ToString();
            LogCore.Error(str);
        }

        /// <summary>
        /// 记录跟踪级别的格式化日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Trace(string message, params object[] args)
        {
            var st = new StackTrace(1, true);
            LogCore.Trace($"{string.Format(message, args)}\n{st}");
        }

        /// <summary>
        /// 记录警告级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Warning(string message, params object[] args)
        {
            LogCore.Warning(string.Format(message, args));
        }

        /// <summary>
        /// 记录信息级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Info(string message, params object[] args)
        {
            LogCore.Info(string.Format(message, args));
        }

        /// <summary>
        /// 记录调试级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Debug(string message, params object[] args)
        {
            LogCore.Debug(string.Format(message, args));
        }

        /// <summary>
        /// 记录错误级别的格式化日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Error(string message, params object[] args)
        {
            var st = new StackTrace(1, true);
            var s = string.Format(message, args) + '\n' + st;
            LogCore.Error(s);
        }
    }
}