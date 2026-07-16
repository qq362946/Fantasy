using Fantasy.Platform.Net;
using System.Runtime.CompilerServices;
using NLog;
using NLog.Targets;
using NLog.Targets.Wrappers;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Fantasy
{
    /// <summary>
    /// 使用 NLog 实现的日志记录器。
    /// </summary>
    public sealed class NLog : ILog
    {
        private const string DefaultLogSceneName = "Log";
        private readonly Logger _logger; // NLog 日志记录器实例

        /// <summary>
        /// 初始化 NLog 实例。
        /// </summary>
        /// <param name="name">日志记录器的名称。</param>
        public NLog(string name)
        {
            // 获取指定名称的 NLog 日志记录器
            _logger = LogManager.GetLogger(name);
        }

        /// <summary>
        /// 初始化方法
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="processMode"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(string appId, ProcessMode processMode)
        {
            LogManager.Configuration.Variables["appId"] = string.IsNullOrEmpty(appId) || appId=="0" ? "Develop" : appId;
            ApplyFileFlushOptions(processMode);
            
            if (processMode == ProcessMode.Release)
            {
                LogManager.Configuration.RemoveRuleByName("ConsoleTrace");
                LogManager.Configuration.RemoveRuleByName("ConsoleDebug");
                LogManager.Configuration.RemoveRuleByName("ConsoleInfo");
                LogManager.Configuration.RemoveRuleByName("ConsoleWarn");
                LogManager.Configuration.RemoveRuleByName("ConsoleError");
            }
            
            LogManager.ReconfigExistingLoggers();
        }
        
        private static void ApplyFileFlushOptions(ProcessMode processMode)
        {
            var isDevelop = processMode == ProcessMode.Develop;
            
            foreach (var target in LogManager.Configuration.AllTargets)
            {
                ApplyFileFlushOptions(target, isDevelop);
            }
        }
        
        private static void ApplyFileFlushOptions(Target target, bool isDevelop)
        {
            switch (target)
            {
                case FileTarget fileTarget:
                {
                    fileTarget.AutoFlush = isDevelop;
                    fileTarget.OpenFileFlushTimeout = isDevelop ? 1 : 60;
                    return;
                }
                case WrapperTargetBase { WrappedTarget: not null } wrapperTarget:
                {
                    ApplyFileFlushOptions(wrapperTarget.WrappedTarget, isDevelop);
                    return;
                }
            }
        }

        /// <summary>
        /// 记录跟踪级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string message)
        {
            Write(LogLevel.Trace, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录警告级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message)
        {
            Write(LogLevel.Warn, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录信息级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message)
        {
            Write(LogLevel.Info, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录调试级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message)
        {
            Write(LogLevel.Debug, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录错误级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message)
        {
            Write(LogLevel.Error, DefaultLogSceneName, message);
        }

        /// <summary>
        /// 记录严重错误级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fatal(string message)
        {
            Write(LogLevel.Fatal, DefaultLogSceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string sceneName, string message)
        {
            Write(LogLevel.Trace, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string sceneName, string message)
        {
            Write(LogLevel.Warn, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string sceneName, string message)
        {
            Write(LogLevel.Info, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string sceneName, string message)
        {
            Write(LogLevel.Debug, sceneName, message);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string sceneName, string message)
        {
            Write(LogLevel.Error, sceneName, message);
        }

        /// <summary>
        /// 记录跟踪级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string message, params object[] args)
        {
            Write(LogLevel.Trace, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录警告级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message, params object[] args)
        {
            Write(LogLevel.Warn, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录信息级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message, params object[] args)
        {
            Write(LogLevel.Info, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录调试级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message, params object[] args)
        {
            Write(LogLevel.Debug, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录错误级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message, params object[] args)
        {
            Write(LogLevel.Error, DefaultLogSceneName, message, args);
        }

        /// <summary>
        /// 记录严重错误级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fatal(string message, params object[] args)
        {
            Write(LogLevel.Fatal, DefaultLogSceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Trace, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Warn, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Info, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Debug, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string sceneName, string message, params object[] args)
        {
            Write(LogLevel.Error, sceneName, message, args);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(LogLevel level, string sceneName, string message, params object[] args)
        {
            var logEvent = LogEventInfo.Create(level, _logger.Name, null, message, args);
            logEvent.Properties["sceneName"] = sceneName;
            _logger.Log(logEvent);
        }
    }
}
