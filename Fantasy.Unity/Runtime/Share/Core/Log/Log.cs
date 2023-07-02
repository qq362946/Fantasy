using System;
using System.Diagnostics;

namespace Fantasy
{
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

        public static void Trace(string msg)
        {
            var st = new StackTrace(1, true);
            LogCore.Trace($"{msg}\n{st}");
        }

        public static void Debug(string msg)
        {
            LogCore.Debug(msg);
        }

        public static void Info(string msg)
        {
            LogCore.Info(msg);
        }

        public static void TraceInfo(string msg)
        {
            var st = new StackTrace(1, true);
            LogCore.Trace($"{msg}\n{st}");
        }

        public static void Warning(string msg)
        {
            LogCore.Warning(msg);
        }

        public static void Error(string msg)
        {
            var st = new StackTrace(1, true);
            LogCore.Error($"{msg}\n{st}");
        }

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

        public static void Trace(string message, params object[] args)
        {
            var st = new StackTrace(1, true);
            LogCore.Trace($"{string.Format(message, args)}\n{st}");
        }

        public static void Warning(string message, params object[] args)
        {
            LogCore.Warning(string.Format(message, args));
        }

        public static void Info(string message, params object[] args)
        {
            LogCore.Info(string.Format(message, args));
        }

        public static void Debug(string message, params object[] args)
        {
            LogCore.Debug(string.Format(message, args));
        }

        public static void Error(string message, params object[] args)
        {
            var st = new StackTrace(1, true);
            var s = string.Format(message, args) + '\n' + st;
            LogCore.Error(s);
        }
    }
}