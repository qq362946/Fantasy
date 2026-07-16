#if FANTASY_NET
using Fantasy.Platform.Net;
#endif
using Fantasy;
using Serilog;
using Serilog.Events;

// 依赖包（添加到 .csproj）：
//   dotnet add package Serilog
//   dotnet add package Serilog.Sinks.Console
//   dotnet add package Serilog.Sinks.File

namespace MyGame
{
    /// <summary>
    /// 基于 Serilog 的 ILog 实现示例。
    /// 按需调整 LoggerConfiguration 中的 sink 和最低日志级别。
    /// </summary>
    public sealed class SerilogLog : ILog
    {
        private readonly Serilog.Core.Logger _logger;

        public SerilogLog()
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File("logs/server-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

#if FANTASY_NET
        public void Initialize(string appId, ProcessMode processMode)
        {
            _logger.Information(
                "Logger initialized, appId: {AppId}, mode: {ProcessMode}",
                appId,
                processMode);
        }
#endif

        public void Trace(string message)   => Write(LogEventLevel.Verbose, message);
        public void Debug(string message)   => Write(LogEventLevel.Debug, message);
        public void Info(string message)    => Write(LogEventLevel.Information, message);
        public void Warning(string message) => Write(LogEventLevel.Warning, message);
        public void Error(string message)   => Write(LogEventLevel.Error, message);

        public void Trace(string sceneName, string message)   => Write(LogEventLevel.Verbose, sceneName, message);
        public void Debug(string sceneName, string message)   => Write(LogEventLevel.Debug, sceneName, message);
        public void Info(string sceneName, string message)    => Write(LogEventLevel.Information, sceneName, message);
        public void Warning(string sceneName, string message) => Write(LogEventLevel.Warning, sceneName, message);
        public void Error(string sceneName, string message)   => Write(LogEventLevel.Error, sceneName, message);

        public void Trace(string message, params object[] args)   => Write(LogEventLevel.Verbose, message, args);
        public void Debug(string message, params object[] args)   => Write(LogEventLevel.Debug, message, args);
        public void Info(string message, params object[] args)    => Write(LogEventLevel.Information, message, args);
        public void Warning(string message, params object[] args) => Write(LogEventLevel.Warning, message, args);
        public void Error(string message, params object[] args)   => Write(LogEventLevel.Error, message, args);

        public void Trace(string sceneName, string message, params object[] args)   => Write(LogEventLevel.Verbose, sceneName, message, args);
        public void Debug(string sceneName, string message, params object[] args)   => Write(LogEventLevel.Debug, sceneName, message, args);
        public void Info(string sceneName, string message, params object[] args)    => Write(LogEventLevel.Information, sceneName, message, args);
        public void Warning(string sceneName, string message, params object[] args) => Write(LogEventLevel.Warning, sceneName, message, args);
        public void Error(string sceneName, string message, params object[] args)   => Write(LogEventLevel.Error, sceneName, message, args);

        private void Write(LogEventLevel level, string message, params object[] args)
        {
            _logger.Write(level, message, args);
        }

        private void Write(LogEventLevel level, string sceneName, string message, params object[] args)
        {
            _logger.ForContext("SceneName", sceneName).Write(level, message, args);
        }
    }
}
