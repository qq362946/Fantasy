
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 标准的控制台Log
    /// </summary>
    public sealed class ConsoleLog : ILog
    {

        /// <summary>
        /// 记录跟踪级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        public void Trace(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// 记录警告级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        public void Warning(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// 记录信息级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        public void Info(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Gray;
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// 记录调试级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        public void Debug(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// 记录错误级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        public void Error(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// 记录严重错误级别的日志消息。
        /// </summary>
        /// <param name="message">日志消息。</param>
        public void Fatal(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// 记录跟踪级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public void Trace(string message, params object[] args)
        {
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine(message, args);
        }

        /// <summary>
        /// 记录警告级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public void Warning(string message, params object[] args)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(message, args);
        }

        /// <summary>
        /// 记录信息级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public void Info(string message, params object[] args)
        {
            System.Console.ForegroundColor = ConsoleColor.Gray;
            System.Console.WriteLine(message, args);
        }

        /// <summary>
        /// 记录调试级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public void Debug(string message, params object[] args)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            System.Console.WriteLine(message, args);
        }

        /// <summary>
        /// 记录错误级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public void Error(string message, params object[] args)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            System.Console.WriteLine(message, args);
        }

        /// <summary>
        /// 记录严重错误级别的格式化日志消息。
        /// </summary>
        /// <param name="message">日志消息模板。</param>
        /// <param name="args">格式化参数。</param>
        public void Fatal(string message, params object[] args)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(message, args);
        }
    }
}