using System.Diagnostics;

namespace Fantasy.Exporter;

/// <summary>
/// 定义日志记录功能的接口。
/// </summary>
public interface ILog
{
    /// <summary>
    /// 记录跟踪级别的日志消息。
    /// </summary>
    /// <param name="message">日志消息。</param>
    void Trace(string message);
    /// <summary>
    /// 记录警告级别的日志消息。
    /// </summary>
    /// <param name="message">日志消息。</param>
    void Warning(string message);
    /// <summary>
    /// 记录信息级别的日志消息。
    /// </summary>
    /// <param name="message">日志消息。</param>
    void Info(string message);
    /// <summary>
    /// 记录调试级别的日志消息。
    /// </summary>
    /// <param name="message">日志消息。</param>
    void Debug(string message);
    /// <summary>
    /// 记录错误级别的日志消息。
    /// </summary>
    /// <param name="message">日志消息。</param>
    void Error(string message);
    /// <summary>
    /// 记录跟踪级别的格式化日志消息。
    /// </summary>
    /// <param name="message">日志消息模板。</param>
    /// <param name="args">格式化参数。</param>
    void Trace(string message, params object[] args);
    /// <summary>
    /// 记录警告级别的格式化日志消息。
    /// </summary>
    /// <param name="message">日志消息模板。</param>
    /// <param name="args">格式化参数。</param>
    void Warning(string message, params object[] args);
    /// <summary>
    /// 记录信息级别的格式化日志消息。
    /// </summary>
    /// <param name="message">日志消息模板。</param>
    /// <param name="args">格式化参数。</param>
    void Info(string message, params object[] args);
    /// <summary>
    /// 记录调试级别的格式化日志消息。
    /// </summary>
    /// <param name="message">日志消息模板。</param>
    /// <param name="args">格式化参数。</param>
    void Debug(string message, params object[] args);
    /// <summary>
    /// 记录错误级别的格式化日志消息。
    /// </summary>
    /// <param name="message">日志消息模板。</param>
    /// <param name="args">格式化参数。</param>
    void Error(string message, params object[] args);
}

public static class Log
{
    private static readonly ILog LogCore;

    static Log()
    {
        LogCore = new ConsoleLog();
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
    /// 记录信息级别的格式化日志消息。
    /// </summary>
    /// <param name="message">日志消息模板。</param>
    /// <param name="args">格式化参数。</param>
    public static void Info(string message, params object[] args)
    {
        LogCore.Info(string.Format(message, args));
    }
}

public class ConsoleLog : ILog
{
    public void Info(string message)
    {
        Console.WriteLine(message);
    }
    
    public void Error(string message)
    {
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{message}\n{new StackTrace(1, true)}");
        Console.ForegroundColor = color;
    }
    
    public void Trace(string message)
    {
        throw new NotImplementedException();
    }

    public void Warning(string message)
    {
        throw new NotImplementedException();
    }

    public void Debug(string message)
    {
        throw new NotImplementedException();
    }

    public void Trace(string message, params object[] args)
    {
        throw new NotImplementedException();
    }

    public void Warning(string message, params object[] args)
    {
        throw new NotImplementedException();
    }

    public void Info(string message, params object[] args)
    {
        throw new NotImplementedException();
    }

    public void Debug(string message, params object[] args)
    {
        throw new NotImplementedException();
    }

    public void Error(string message, params object[] args)
    {
        throw new NotImplementedException();
    }
}