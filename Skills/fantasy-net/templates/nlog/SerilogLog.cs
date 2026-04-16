#if FANTASY_NET
using Fantasy.Platform.Net;
#endif
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
        public void Initialize(ProcessMode processMode)
        {
            _logger.Information("Logger initialized, mode: {ProcessMode}", processMode);
        }
#endif

        public void Trace(string message)   => _logger.Verbose(message);
        public void Debug(string message)   => _logger.Debug(message);
        public void Info(string message)    => _logger.Information(message);
        public void Warning(string message) => _logger.Warning(message);
        public void Error(string message)   => _logger.Error(message);

        public void Trace(string message, params object[] args)   => _logger.Verbose(message, args);
        public void Debug(string message, params object[] args)   => _logger.Debug(message, args);
        public void Info(string message, params object[] args)    => _logger.Information(message, args);
        public void Warning(string message, params object[] args) => _logger.Warning(message, args);
        public void Error(string message, params object[] args)   => _logger.Error(message, args);
    }
}
