using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if FANTASY_NET
using Fantasy.Platform.Net;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#endif

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy
{
    /// <summary>
    /// 提供日志记录功能的静态类。
    /// </summary>
    public static partial class Log
    {
        private const string DefaultLogSceneName = "Server";
        private static ILog _logCore;

        /// <summary>
        /// 初始化Log系统
        /// </summary>
#if FANTASY_NET
        public static void Initialize(string appId, ILog log = null)
#else
        public static void Initialize(ILog log = null)
#endif
        {
            if (log == null)
            {
#if FANTASY_NET
                _logCore = new ConsoleLog();
#endif
#if FANTASY_UNITY
                _logCore = new UnityLog();
#endif
                return;
            }

            _logCore = log;
#if FANTASY_NET
            _logCore.Initialize(appId, ProgramDefine.RuntimeMode);
#endif
        }

        /// <summary>
        /// 记录跟踪级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Trace(string msg)
        {
            var st = new StackTrace(1, true);
            _logCore.Trace($"{msg}\n{st}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Trace(Scene scene, string msg)
        {
            var st = new StackTrace(1, true);
            _logCore.Trace(GetLogSceneName(scene), $"{msg}\n{st}");
        }

        /// <summary>
        /// 记录调试级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Debug(string msg)
        {
            _logCore.Debug(msg);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(Scene scene, string msg)
        {
            _logCore.Debug(GetLogSceneName(scene), msg);
        }

        /// <summary>
        /// 记录信息级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Info(string msg)
        {
            _logCore.Info(msg);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(Scene scene, string msg)
        {
            _logCore.Info(GetLogSceneName(scene), msg);
        }

        /// <summary>
        /// 记录跟踪级别的日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void TraceInfo(string msg)
        {
            var st = new StackTrace(1, true);
            _logCore.Trace($"{msg}\n{st}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TraceInfo(Scene scene, string msg)
        {
            var st = new StackTrace(1, true);
            _logCore.Trace(GetLogSceneName(scene), $"{msg}\n{st}");
        }

        /// <summary>
        /// 记录警告级别的日志消息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Warning(string msg)
        {
            _logCore.Warning(msg);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(Scene scene, string msg)
        {
            _logCore.Warning(GetLogSceneName(scene), msg);
        }

        /// <summary>
        /// 记录错误级别的日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="msg">日志消息。</param>
        public static void Error(string msg)
        {
            var st = new StackTrace(1, true);
            _logCore.Error($"{msg}\n{st}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(Scene scene, string msg)
        {
            var st = new StackTrace(1, true);
            _logCore.Error(GetLogSceneName(scene), $"{msg}\n{st}");
        }

        /// <summary>
        /// 记录异常的错误级别的日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="e">异常对象。</param>
        public static void Error(Exception e)
        {
            if (e.Data.Contains("StackTrace"))
            {
                _logCore.Error($"{e.Data["StackTrace"]}\n{e}");
                return;
            }
            var str = e.ToString();
            _logCore.Error(str);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(Scene scene, Exception e)
        {
            if (e.Data.Contains("StackTrace"))
            {
                _logCore.Error(GetLogSceneName(scene), $"{e.Data["StackTrace"]}\n{e}");
                return;
            }
            var str = e.ToString();
            _logCore.Error(GetLogSceneName(scene), str);
        }

        /// <summary>
        /// 记录跟踪级别的格式化日志消息，并附带调用栈信息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Trace(string message, params object[] args)
        {
            var st = new StackTrace(1, true);
            _logCore.Trace($"{string.Format(message, args)}\n{st}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Trace(Scene scene, string message, params object[] args)
        {
            var st = new StackTrace(1, true);
            _logCore.Trace(GetLogSceneName(scene), $"{string.Format(message, args)}\n{st}");
        }

        /// <summary>
        /// 记录警告级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Warning(string message, params object[] args)
        {
            _logCore.Warning(string.Format(message, args));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(Scene scene, string message, params object[] args)
        {
            _logCore.Warning(GetLogSceneName(scene), message, args);
        }

        /// <summary>
        /// 记录信息级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Info(string message, params object[] args)
        {
            _logCore.Info(string.Format(message, args));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(Scene scene, string message, params object[] args)
        {
            _logCore.Info(GetLogSceneName(scene), message, args);
        }

        /// <summary>
        /// 记录调试级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public static void Debug(string message, params object[] args)
        {
            _logCore.Debug(string.Format(message, args));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(Scene scene, string message, params object[] args)
        {
            _logCore.Debug(GetLogSceneName(scene), message, args);
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
            _logCore.Error(s);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(Scene scene, string message, params object[] args)
        {
            var st = new StackTrace(1, true);
            var s = string.Format(message, args) + '\n' + st;
            _logCore.Error(GetLogSceneName(scene), s);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetLogSceneName(Scene scene)
        {
#if FANTASY_NET
            return string.IsNullOrEmpty(scene.LogSceneName) ? DefaultLogSceneName : scene.LogSceneName;
#else
            return DefaultLogSceneName;
#endif
        }
    }
}
