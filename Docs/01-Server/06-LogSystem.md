# 日志系统使用指南

本文档介绍如何在 Fantasy Framework 中使用日志系统,包括:
- 使用框架内置的日志扩展(NLog)
- 实现自定义日志系统
- 将日志系统注册到框架中

---

## 目录

- [日志系统概述](#日志系统概述)
- [使用内置 NLog 扩展](#使用内置-nlog-扩展)
- [实现自定义日志系统](#实现自定义日志系统)
- [注册日志到框架](#注册日志到框架)
- [日志 API 使用](#日志-api-使用)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 日志系统概述

Fantasy Framework 提供了灵活的日志抽象接口 `ILog`,允许您:

1. **使用内置扩展**:框架提供了 `Fantasy.NLog` 包,基于流行的 NLog 库
2. **实现自定义日志**:实现 `ILog` 接口,集成您喜欢的日志库或自定义逻辑
3. **零依赖**:如果不提供日志实例,框架会使用内置的 `ConsoleLog` 控制台日志

### ILog 接口定义

```csharp
namespace Fantasy
{
    /// <summary>
    /// 定义日志记录功能的接口。
    /// </summary>
    public interface ILog
    {
#if FANTASY_NET
        /// <summary>
        /// 初始化日志系统
        /// </summary>
        /// <param name="processMode">进程模式(Develop/Release)</param>
        void Initialize(ProcessMode processMode);
#endif
        // 基本日志方法
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);

        // 格式化日志方法
        void Trace(string message, params object[] args);
        void Debug(string message, params object[] args);
        void Info(string message, params object[] args);
        void Warning(string message, params object[] args);
        void Error(string message, params object[] args);
    }
}
```

---

## 使用内置 NLog 扩展

Fantasy Framework 提供了基于 NLog 的日志扩展包 `Fantasy.NLog`,开箱即用。

### 1. 添加 Fantasy.NLog 扩展

Fantasy.NLog 位于框架源码的 `Fantasy.Packages/Fantasy.NLog` 目录,可以通过以下三种方式使用:

**方式1:通过 CLI 命令安装(推荐)**

使用 Fantasy CLI 工具快速添加 NLog 扩展:

```bash
fantasy add NLog
```

该命令会自动执行以下操作:
- ✅ 复制 `NLog.cs` 实现文件到项目目录
- ✅ 复制 `NLog.config` 配置文件到项目根目录
- ✅ 复制 `NLog.xsd` 架构文件(用于 IDE 智能提示)
- ✅ 自动配置 `.csproj` 以确保配置文件被复制到输出目录
- ✅ 自动安装 NLog NuGet 依赖包

> **💡 提示:** 如果尚未安装 Fantasy CLI 工具，请查看 [Fantasy CLI 安装指南](../../Fantasy.Packages/Fantasy.Cil/README.md#安装) 了解如何安装。


**方式2:通过项目引用**

在你的项目文件(如 `Server.csproj`)中添加项目引用:

```xml
<!-- Server.csproj -->
<ItemGroup>
    <ProjectReference Include="../Fantasy.Packages/Fantasy.NLog/Fantasy.NLog.csproj" />
</ItemGroup>
```

**方式3:手动复制文件**

将以下文件从 `Fantasy.Packages/Fantasy.NLog/` 复制到你的项目中:

```
YourProject/
├── NLog.cs                    # NLog 实现(必需)
├── NLog.config                # NLog 配置文件(必需)
└── NLog.xsd                   # XML Schema(可选,用于 IDE 智能提示)
```

**步骤:**

1. 复制 `NLog.cs` 到项目根目录或单独的文件夹(如 `Logging/`)
2. 复制 `NLog.config` 到项目根目录
3. 确保 `NLog.config` 设置为"始终复制"或"如果较新则复制"

```xml
<!-- 在 .csproj 中添加 -->
<ItemGroup>
    <None Update="NLog.config">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

4. 通过 NuGet 安装 NLog 依赖包:

```bash
dotnet add package NLog
```

### 2. 配置 NLog.config

在你的项目根目录创建 `NLog.config` 文件:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true">

    <!-- 定义输出目标 -->
    <targets>
        <!-- 控制台输出 -->
        <target xsi:type="ColoredConsole"
                name="ConsoleTarget"
                layout="${longdate} [${level:uppercase=true}] ${message} ${exception:format=tostring}" />

        <!-- 文件输出 -->
        <target xsi:type="File"
                name="FileTarget"
                fileName="logs/${shortdate}.log"
                layout="${longdate} [${level:uppercase=true}] ${message} ${exception:format=tostring}"
                archiveAboveSize="10485760"
                maxArchiveFiles="30" />
    </targets>

    <!-- 日志规则 -->
    <rules>
        <!-- Develop 模式:输出到控制台 -->
        <logger name="*" minlevel="Trace" writeTo="ConsoleTarget" ruleName="ConsoleTrace" />
        <logger name="*" minlevel="Debug" writeTo="ConsoleTarget" ruleName="ConsoleDebug" />
        <logger name="*" minlevel="Info" writeTo="ConsoleTarget" ruleName="ConsoleInfo" />
        <logger name="*" minlevel="Warn" writeTo="ConsoleTarget" ruleName="ConsoleWarn" />
        <logger name="*" minlevel="Error" writeTo="ConsoleTarget" ruleName="ConsoleError" />

        <!-- Release 模式:输出到文件 -->
        <logger name="*" minlevel="Trace" writeTo="FileTarget" ruleName="ServerTrace" />
        <logger name="*" minlevel="Debug" writeTo="FileTarget" ruleName="ServerDebug" />
        <logger name="*" minlevel="Info" writeTo="FileTarget" ruleName="ServerInfo" />
        <logger name="*" minlevel="Warn" writeTo="FileTarget" ruleName="ServerWarn" />
        <logger name="*" minlevel="Error" writeTo="FileTarget" ruleName="ServerError" />
    </rules>
</nlog>
```

**配置说明:**
- **Develop 模式**:日志输出到控制台,便于开发调试
- **Release 模式**:日志输出到文件,便于生产环境追踪
- NLog 实现会根据 `ProcessMode` 自动移除不需要的规则(见下文源码解析)

### 3. 在启动代码中使用

```csharp
using Fantasy;

try
{
    // 1. 初始化程序集
    AssemblyHelper.Initialize();

    // 2. 创建 NLog 日志实例
    var logger = new Fantasy.NLog("Server");

    // 3. 启动框架并传入日志实例
    await Fantasy.Platform.Net.Entry.Start(logger);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"服务器启动失败:{ex}");
    Environment.Exit(1);
}
```

### 4. NLog 实现源码解析

`Fantasy.NLog` 的实现位于 `/Fantasy.Packages/Fantasy.NLog/NLog.cs`:

```csharp
using Fantasy.Platform.Net;
using NLog;

namespace Fantasy
{
    /// <summary>
    /// 使用 NLog 实现的日志记录器。
    /// </summary>
    public class NLog : ILog
    {
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
        /// 初始化方法,根据运行模式调整日志规则
        /// </summary>
        /// <param name="processMode">进程模式</param>
        public void Initialize(ProcessMode processMode)
        {
            // 根据运行模式选择日志输出方式
            switch (processMode)
            {
                case ProcessMode.Develop:
                {
                    // Develop 模式:移除文件日志规则,仅保留控制台输出
                    LogManager.Configuration.RemoveRuleByName("ServerDebug");
                    LogManager.Configuration.RemoveRuleByName("ServerTrace");
                    LogManager.Configuration.RemoveRuleByName("ServerInfo");
                    LogManager.Configuration.RemoveRuleByName("ServerWarn");
                    LogManager.Configuration.RemoveRuleByName("ServerError");
                    break;
                }
                case ProcessMode.Release:
                {
                    // Release 模式:移除控制台日志规则,仅保留文件输出
                    LogManager.Configuration.RemoveRuleByName("ConsoleTrace");
                    LogManager.Configuration.RemoveRuleByName("ConsoleDebug");
                    LogManager.Configuration.RemoveRuleByName("ConsoleInfo");
                    LogManager.Configuration.RemoveRuleByName("ConsoleWarn");
                    LogManager.Configuration.RemoveRuleByName("ConsoleError");
                    break;
                }
            }
        }

        // 实现 ILog 接口的各个方法
        public void Trace(string message) => _logger.Trace(message);
        public void Warning(string message) => _logger.Warn(message);
        public void Info(string message) => _logger.Info(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Error(string message) => _logger.Error(message);
        public void Fatal(string message) => _logger.Fatal(message);

        // 格式化日志方法
        public void Trace(string message, params object[] args) => _logger.Trace(message, args);
        public void Warning(string message, params object[] args) => _logger.Warn(message, args);
        public void Info(string message, params object[] args) => _logger.Info(message, args);
        public void Debug(string message, params object[] args) => _logger.Debug(message, args);
        public void Error(string message, params object[] args) => _logger.Error(message, args);
        public void Fatal(string message, params object[] args) => _logger.Fatal(message, args);
    }
}
```

**关键特性:**
- ✅ 根据 `ProcessMode` 动态调整日志规则
- ✅ Develop 模式:仅输出到控制台,便于开发调试
- ✅ Release 模式:仅输出到文件,减少性能开销
- ✅ 完整实现 `ILog` 接口的所有方法

---

## 实现自定义日志系统

如果你想使用其他日志库(如 Serilog、Log4Net)或实现自定义逻辑,只需实现 `ILog` 接口。

### 示例1:简单的控制台日志

```csharp
using Fantasy.Platform.Net;

namespace MyProject
{
    /// <summary>
    /// 简单的控制台日志实现
    /// </summary>
    public class SimpleConsoleLog : ILog
    {
        public void Initialize(ProcessMode processMode)
        {
            // 可以在这里根据运行模式做初始化
            Console.WriteLine($"日志系统初始化,运行模式:{processMode}");
        }

        public void Trace(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[TRACE] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            Console.ResetColor();
        }

        public void Debug(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            Console.ResetColor();
        }

        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            Console.ResetColor();
        }

        public void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            Console.ResetColor();
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            Console.ResetColor();
        }

        // 格式化日志方法
        public void Trace(string message, params object[] args) => Trace(string.Format(message, args));
        public void Debug(string message, params object[] args) => Debug(string.Format(message, args));
        public void Info(string message, params object[] args) => Info(string.Format(message, args));
        public void Warning(string message, params object[] args) => Warning(string.Format(message, args));
        public void Error(string message, params object[] args) => Error(string.Format(message, args));
    }
}
```

### 示例2:文件日志实现

```csharp
using Fantasy.Platform.Net;

namespace MyProject
{
    /// <summary>
    /// 简单的文件日志实现
    /// </summary>
    public class FileLog : ILog
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public FileLog(string logDirectory = "logs")
        {
            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 按日期创建日志文件
            var fileName = $"{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(logDirectory, fileName);
        }

        public void Initialize(ProcessMode processMode)
        {
            WriteLog("INFO", $"日志系统初始化,运行模式:{processMode}");
        }

        private void WriteLog(string level, string message)
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

            // 线程安全的文件写入
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }

        public void Trace(string message) => WriteLog("TRACE", message);
        public void Debug(string message) => WriteLog("DEBUG", message);
        public void Info(string message) => WriteLog("INFO", message);
        public void Warning(string message) => WriteLog("WARN", message);
        public void Error(string message) => WriteLog("ERROR", message);

        // 格式化日志方法
        public void Trace(string message, params object[] args) => Trace(string.Format(message, args));
        public void Debug(string message, params object[] args) => Debug(string.Format(message, args));
        public void Info(string message, params object[] args) => Info(string.Format(message, args));
        public void Warning(string message, params object[] args) => Warning(string.Format(message, args));
        public void Error(string message, params object[] args) => Error(string.Format(message, args));
    }
}
```

### 示例3:集成 Serilog

```csharp
using Fantasy.Platform.Net;
using Serilog;
using Serilog.Events;

namespace MyProject
{
    /// <summary>
    /// 使用 Serilog 的日志实现
    /// </summary>
    public class SerilogLog : ILog
    {
        private readonly Serilog.Core.Logger _logger;

        public SerilogLog()
        {
            // 配置 Serilog
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/fantasy-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public void Initialize(ProcessMode processMode)
        {
            _logger.Information("日志系统初始化,运行模式:{ProcessMode}", processMode);
        }

        public void Trace(string message) => _logger.Verbose(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Info(string message) => _logger.Information(message);
        public void Warning(string message) => _logger.Warning(message);
        public void Error(string message) => _logger.Error(message);

        // 格式化日志方法
        public void Trace(string message, params object[] args) => _logger.Verbose(message, args);
        public void Debug(string message, params object[] args) => _logger.Debug(message, args);
        public void Info(string message, params object[] args) => _logger.Information(message, args);
        public void Warning(string message, params object[] args) => _logger.Warning(message, args);
        public void Error(string message, params object[] args) => _logger.Error(message, args);
    }
}
```

---

## 注册日志到框架

无论使用内置 NLog 还是自定义日志,注册方式都相同。

### 启动时注册

在 `Entry.Start()` 方法中传入日志实例:

```csharp
using Fantasy;

try
{
    // 1. 初始化程序集
    AssemblyHelper.Initialize();

    // 2. 创建日志实例(选择以下任一方式)

    // 方式1:使用 NLog
    var logger = new Fantasy.NLog("Server");

    // 方式2:使用自定义控制台日志
    // var logger = new SimpleConsoleLog();

    // 方式3:使用文件日志
    // var logger = new FileLog("logs");

    // 方式4:使用 Serilog
    // var logger = new SerilogLog();

    // 方式5:使用框架内置控制台日志(传入 null 或省略参数)
    // await Fantasy.Platform.Net.Entry.Start();

    // 3. 启动框架并传入日志实例
    await Fantasy.Platform.Net.Entry.Start(logger);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"服务器启动失败:{ex}");
    Environment.Exit(1);
}
```

### 框架如何处理日志

框架在 `Entry.Start()` 方法中初始化日志系统:

```csharp
// Fantasy.Platform.Net.Entry.cs (简化版)
public static async FTask Start(ILog log = null)
{
    // 初始化
    await Initialize(log);
    // 启动进程
    StartProcess().Coroutine();
    // ...
}

private static async FTask Initialize(ILog log = null)
{
    // 初始化日志系统
    Log.Initialize(log);  // 如果 log 为 null,会使用内置的 ConsoleLog

    // 显示框架版本信息
    LogFantasyVersion();

    // 加载配置文件
    await ConfigLoader.InitializeFromXml(Path.Combine(AppContext.BaseDirectory, "Fantasy.config"));

    // 解析命令行参数
    // ...

    // 调用日志的 Initialize 方法(如果实现了)
    // 注意:这里会传入运行模式(Develop/Release)
}
```

**关键流程:**
1. `Entry.Start(log)` 接收日志实例
2. 调用 `Log.Initialize(log)` 注册日志
3. 如果 `log` 为 `null`,框架使用内置的 `ConsoleLog`
4. 调用 `log.Initialize(processMode)` 传入运行模式

---

## 日志 API 使用

注册日志后,可以在代码的任何地方使用 `Log` 静态类:

```csharp
using Fantasy;

public class MyComponent : Entity
{
    protected override void Awake()
    {
        // 基本日志
        Log.Trace("这是 Trace 日志");
        Log.Debug("这是 Debug 日志");
        Log.Info("这是 Info 日志");
        Log.Warning("这是 Warning 日志");
        Log.Error("这是 Error 日志");

        // 格式化日志
        var userId = 12345;
        var userName = "Player1";
        Log.Info("用户 {0} (ID:{1}) 登录成功", userName, userId);

        // 在异常处理中使用
        try
        {
            // 可能抛出异常的代码
        }
        catch (Exception ex)
        {
            Log.Error($"发生错误:{ex.Message}");
            Log.Error($"堆栈跟踪:{ex.StackTrace}");
        }
    }
}
```

### 日志级别说明

| 级别 | 用途 | 使用场景 |
|------|------|----------|
| **Trace** | 最详细的调试信息 | 函数调用跟踪、变量值记录 |
| **Debug** | 调试信息 | 开发阶段的调试输出 |
| **Info** | 普通信息 | 正常业务流程记录 |
| **Warning** | 警告信息 | 潜在问题,但不影响运行 |
| **Error** | 错误信息 | 发生错误,需要关注 |

---

## 最佳实践

### 1. 根据环境选择日志实现

```csharp
ILog logger;

#if DEBUG
    // 开发环境:使用控制台日志,便于调试
    logger = new SimpleConsoleLog();
#else
    // 生产环境:使用 NLog,输出到文件
    logger = new Fantasy.NLog("Server");
#endif

await Fantasy.Platform.Net.Entry.Start(logger);
```

### 2. 记录关键业务逻辑

```csharp
public async FTask<bool> UserLogin(string username, string password)
{
    Log.Info($"用户 {username} 尝试登录");

    // 验证逻辑
    if (!ValidateUser(username, password))
    {
        Log.Warning($"用户 {username} 登录失败:密码错误");
        return false;
    }

    Log.Info($"用户 {username} 登录成功");
    return true;
}
```

### 3. 异常处理中使用日志

```csharp
try
{
    await SomeAsyncOperation();
}
catch (Exception ex)
{
    Log.Error($"操作失败:{ex.Message}");
    Log.Debug($"详细堆栈:{ex}");
    throw;
}
```

### 4. 性能敏感场景的日志优化

```csharp
// ❌ 不推荐:频繁调用字符串拼接
for (int i = 0; i < 10000; i++)
{
    Log.Debug($"处理第 {i} 个元素:{elements[i]}");  // 每次循环都会创建字符串
}

// ✅ 推荐:使用条件编译或日志级别控制
#if DEBUG
for (int i = 0; i < 10000; i++)
{
    Log.Debug($"处理第 {i} 个元素:{elements[i]}");
}
#endif

// ✅ 或者:仅在关键位置记录日志
Log.Debug($"开始处理 {elements.Length} 个元素");
for (int i = 0; i < elements.Length; i++)
{
    // 处理逻辑...
}
Log.Debug("处理完成");
```

### 5. 日志分类管理

```csharp
// 为不同模块使用不同的日志名称
public class DatabaseModule
{
    private static readonly ILog DbLog = new Fantasy.NLog("Database");

    public void Connect()
    {
        DbLog.Info("正在连接数据库...");
    }
}

public class NetworkModule
{
    private static readonly ILog NetLog = new Fantasy.NLog("Network");

    public void StartServer()
    {
        NetLog.Info("正在启动网络服务器...");
    }
}
```

---

## 常见问题

### Q1: 如何切换日志输出目标?

**答:**

如果使用 NLog,可以修改 `NLog.config` 文件。如果使用自定义日志,在实现 `ILog` 接口时添加逻辑。

**示例:运行时动态切换**

```csharp
public class DynamicLog : ILog
{
    private ILog _currentLogger;

    public DynamicLog(bool useFileLog)
    {
        _currentLogger = useFileLog ? new FileLog() : new SimpleConsoleLog();
    }

    public void SwitchToFileLog()
    {
        _currentLogger = new FileLog();
    }

    public void SwitchToConsoleLog()
    {
        _currentLogger = new SimpleConsoleLog();
    }

    // 委托给当前日志实现
    public void Info(string message) => _currentLogger.Info(message);
    // ... 其他方法
}
```

### Q2: 日志文件太大怎么办?

**答:**

使用日志库的自动归档功能(如 NLog、Serilog)或实现自定义文件滚动逻辑。

**NLog 配置示例:**

```xml
<target xsi:type="File"
        name="FileTarget"
        fileName="logs/${shortdate}.log"
        archiveFileName="logs/archive/{#}.log"
        archiveAboveSize="10485760"     <!-- 10MB -->
        archiveNumbering="Rolling"
        maxArchiveFiles="30" />         <!-- 保留30个归档文件 -->
```

### Q3: 如何在 Unity 客户端使用日志?

**答:**

Unity 客户端也支持 `ILog` 接口,可以实现一个 Unity 专用的日志类:

```csharp
using Fantasy;
using UnityEngine;

namespace MyGame
{
    public class UnityLog : ILog
    {
        public void Trace(string message) => Debug.Log($"[TRACE] {message}");
        public void Debug(string message) => UnityEngine.Debug.Log($"[DEBUG] {message}");
        public void Info(string message) => UnityEngine.Debug.Log($"[INFO] {message}");
        public void Warning(string message) => UnityEngine.Debug.LogWarning(message);
        public void Error(string message) => UnityEngine.Debug.LogError(message);

        // 格式化方法
        public void Trace(string message, params object[] args) => Trace(string.Format(message, args));
        public void Debug(string message, params object[] args) => Debug(string.Format(message, args));
        public void Info(string message, params object[] args) => Info(string.Format(message, args));
        public void Warning(string message, params object[] args) => Warning(string.Format(message, args));
        public void Error(string message, params object[] args) => Error(string.Format(message, args));
    }
}
```

### Q4: 生产环境如何优化日志性能?

**建议:**

1. **调整日志级别**:生产环境禁用 Trace 和 Debug 级别
2. **异步日志**:使用 NLog 或 Serilog 的异步写入功能
3. **条件编译**:使用 `#if DEBUG` 限制开发日志
4. **避免过度日志**:不在高频循环中记录日志

**NLog 异步配置:**

```xml
<targets async="true">
    <target xsi:type="File" name="FileTarget" ... />
</targets>
```

### Q5: 如何记录日志到数据库?

**答:**

实现一个自定义的 `ILog`,在方法中写入数据库:

```csharp
public class DatabaseLog : ILog
{
    private readonly IDatabase _database;

    public DatabaseLog(IDatabase database)
    {
        _database = database;
    }

    public void Info(string message)
    {
        _database.ExecuteNonQuery(
            "INSERT INTO Logs (Level, Message, Timestamp) VALUES (@level, @message, @timestamp)",
            new { level = "INFO", message, timestamp = DateTime.UtcNow }
        );
    }

    // ... 其他方法
}
```

---

## 总结

Fantasy Framework 提供了灵活的日志抽象:

### 核心特点

1. **接口抽象**:通过 `ILog` 接口实现日志解耦
2. **内置扩展**:`Fantasy.NLog` 包提供开箱即用的 NLog 集成
3. **易于扩展**:实现 `ILog` 接口即可集成任意日志库
4. **零依赖**:不提供日志实例时使用内置 `ConsoleLog`
5. **模式感知**:`Initialize(ProcessMode)` 根据运行模式调整日志行为

### 快速开始

```csharp
// 1. 使用 NLog(推荐生产环境)
var logger = new Fantasy.NLog("Server");
await Fantasy.Platform.Net.Entry.Start(logger);

// 2. 使用自定义日志
var logger = new MyCustomLog();
await Fantasy.Platform.Net.Entry.Start(logger);

// 3. 使用默认控制台日志(开发测试)
await Fantasy.Platform.Net.Entry.Start();
```

### 相关文档

- 📖 阅读 [编写启动代码](02-WritingStartupCode.md) 学习如何启动框架
- ⚙️ 阅读 [服务器配置](01-ServerConfiguration.md) 学习配置文件
- 🎯 阅读 [配置系统使用指南](05-ConfigUsage.md) 学习如何使用配置
- 🌐 阅读 [网络协议目录结构说明](07-NetworkProtocol.md) 学习如何定义和管理网络协议

---
