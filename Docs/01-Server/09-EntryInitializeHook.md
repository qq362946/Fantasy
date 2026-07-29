# Entry 初始化钩子 (IEntryInitializeHook)

本指南将介绍如何使用 `IEntryInitializeHook` 在服务器启动早期注入自定义初始化逻辑。

## 前置步骤

在使用 Entry 初始化钩子之前，请确保已完成：

1. ✅ 已完成服务器启动代码的编写
2. ✅ 已配置好 `Fantasy.config` 文件
3. ✅ 理解框架的启动流程

如果你还没有完成这些步骤，请先阅读：
- [编写启动代码](02-WritingStartupCode.md)
- [Fantasy.config 配置文件详解](01-ServerConfiguration.md)
- [OnCreateScene 事件使用指南](04-OnCreateScene.md)

---

## 什么是 IEntryInitializeHook？

`IEntryInitializeHook` 是框架提供的**启动初始化钩子接口**，允许你在 `Entry.Initialize()` 期间注入自定义逻辑。

### 执行时机

```
Entry.Start() 启动流程:
┌─────────────────────────────────────────────────────┐
│ Entry.Initialize()                                  │
│   ├─ 1. 解析命令行参数                               │
│   ├─ 2. Log.Initialize()                           │
│   ├─ 3. typeof(Entry).Assembly.EnsureLoaded()      │
│   ├─ 4. ConfigLoader.InitializeFromXml()           │
│   │      ↓ 配置文件已加载完成                         │
│   ├─ 5. ⭐ IEntryInitializeHook.OnInitialize() ⭐   │
│   │      ↑ 你的自定义初始化逻辑在这里执行              │
│   ├─ 6. SerializerManager.Initialize()             │
│   └─ 7. WinPeriod.Initialize()                     │
├─────────────────────────────────────────────────────┤
│ StartProcess()                                      │
│   └─ Process.Create() → Scene.Create()             │
│        └─ OnCreateScene 事件触发                    │
└─────────────────────────────────────────────────────┘
```

**关键时机点:**
- ✅ 配置文件已加载 (`ConfigLoader.InitializeFromXml()` 完成)
- ✅ 日志系统已初始化 (`Log` 可用)
- ✅ 命令行参数已解析 (`ProgramDefine` 可用)
- ❌ 序列化器尚未初始化
- ❌ Scene 尚未创建
- ❌ 网络尚未启动

### 与 OnCreateScene 的区别

| 特性 | IEntryInitializeHook | OnCreateScene |
|------|---------------------|---------------|
| **执行时机** | 配置加载后、序列化器初始化前 | 每个 Scene 创建后 |
| **执行次数** | 整个进程启动时执行一次 | 每个 Scene 创建时执行一次 |
| **可访问内容** | 配置、日志、命令行参数 | Scene 实例、核心组件、网络 |
| **适用场景** | 全局验证、预加载、环境检查 | Scene 专属初始化、组件挂载 |

**选择建议:**
- 需要在**任何 Scene 创建之前**执行逻辑 → 使用 `IEntryInitializeHook`
- 需要为**特定 Scene** 初始化组件 → 使用 `OnCreateScene` 事件

---

## 什么时候使用 IEntryInitializeHook？

### ✅ 适用场景

1. **配置验证**
   ```csharp
   // 验证必需的配置项是否存在
   ValidateRequiredConfigurations();
   ```

2. **数据库连接检查**
   ```csharp
   // 启动前检查数据库是否可用
   await CheckDatabaseConnectivity();
   ```

3. **外部服务可用性检查**
   ```csharp
   // 验证第三方 API 是否可访问
   await ValidateExternalServices();
   ```

4. **环境特定设置**
   ```csharp
   // 根据运行模式加载不同配置
   if (ProgramDefine.RuntimeMode == ProcessMode.Develop) { ... }
   ```

5. **许可证验证**
   ```csharp
   // 验证许可证是否有效
   await ValidateLicense();
   ```

6. **预加载静态数据**
   ```csharp
   // 加载游戏静态配置表
   await PreloadGameDataTables();
   ```

### ❌ 不适用场景

这些应该使用**其他机制**：

| 不要用 Hook 做... | 应该使用... |
|------------------|-----------|
| Scene 初始化逻辑 | `OnCreateScene` 事件 |
| Entity/Component 创建 | `AwakeSystem` |
| 网络消息处理 | Message Handler |
| 定时任务 | `TimerComponent` |
| 事件订阅 | `EventSystem` |

---

## 创建初始化钩子

### 基础示例

在你的 **Hotfix** 或 **Entity** 项目中创建钩子实现：

```csharp
using Fantasy;

namespace YourNamespace;

/// <summary>
/// 游戏启动前的初始化钩子
/// </summary>
public class GameStartupHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // 只在 Game 进程类型执行
        if (ProgramDefine.ProcessType != "Game")
        {
            return;
        }
        
        Log.Info("执行游戏启动前检查...");
        
        // 1. 验证配置
        ValidateConfiguration();
        
        // 2. 检查数据库连接
        await CheckDatabaseConnection();
        
        // 3. 验证外部服务
        await CheckExternalServices();
        
        Log.Info("启动前检查完成");
    }
    
    private void ValidateConfiguration()
    {
        // 检查进程配置是否存在
        var processConfig = ProcessConfigData.Instance.Get(ProgramDefine.ProcessId);
        if (processConfig == null)
        {
            throw new InvalidOperationException(
                $"Process {ProgramDefine.ProcessId} not found in Fantasy.config");
        }
        
        Log.Info($"配置验证通过: ProcessId={processConfig.Id}");
    }
    
    private async Task CheckDatabaseConnection()
    {
        // 检查数据库连接
        Log.Info("检查数据库连接...");
        // 你的数据库检查逻辑
        await Task.CompletedTask;
    }
    
    private async Task CheckExternalServices()
    {
        // 检查外部 API 可用性
        Log.Info("检查外部服务...");
        // 你的 API 检查逻辑
        await Task.CompletedTask;
    }
}
```

### 重要说明

1. **无需手动注册** - 源代码生成器会自动发现并注册所有实现 `IEntryInitializeHook` 的类
2. **继承自 ICustomInterface** - `IEntryInitializeHook` 继承了 `Fantasy.Assembly.ICustomInterface`
3. **支持多个实现** - 可以创建多个钩子类，都会被自动执行
4. **执行顺序不保证** - 设计时确保钩子之间相互独立

---

## 实际应用示例

### 示例 1: 环境特定初始化

```csharp
public class EnvironmentSetupHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        switch (ProgramDefine.RuntimeMode)
        {
            case ProcessMode.Develop:
                Log.Info("开发模式 - 启用调试功能");
                EnableDevelopmentFeatures();
                break;
                
            case ProcessMode.Release:
                Log.Info("生产模式 - 严格验证");
                await ValidateProductionRequirements();
                break;
        }
    }
    
    private void EnableDevelopmentFeatures()
    {
        // 开发环境专用功能
        Log.Debug("启用详细日志");
        Log.Debug("禁用速率限制");
    }
    
    private async Task ValidateProductionRequirements()
    {
        // 生产环境必须通过的检查
        Log.Info("验证生产环境配置...");
        
        // 检查关键配置项
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_KEY")))
        {
            throw new InvalidOperationException("生产环境必须配置 API_KEY");
        }
        
        await Task.CompletedTask;
    }
}
```

### 示例 2: 多个独立钩子

```csharp
// 钩子 1: 数据库验证
public class DatabaseValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("验证数据库架构...");
        
        // 检查数据库版本
        // 检查必需的表是否存在
        // 检查索引是否创建
        
        await Task.CompletedTask;
    }
}

// 钩子 2: 外部服务检查
public class ExternalServiceHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("检查外部 API 可用性...");
        
        // 检查支付网关
        // 检查推送服务
        // 检查 CDN 连接
        
        await Task.CompletedTask;
    }
}

// 钩子 3: 许可证验证
public class LicenseValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("验证许可证...");
        
        // 读取许可证文件
        // 验证签名
        // 检查过期时间
        
        await Task.CompletedTask;
    }
}
```

### 示例 3: 进程类型特定逻辑

```csharp
public class GateServerHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // 只在 Gate 服务器执行
        if (ProgramDefine.ProcessType != "Gate")
        {
            return;
        }
        
        Log.Info("Gate 服务器初始化...");
        
        // Gate 专属初始化
        await InitializeConnectionPool();
        await LoadBlacklist();
        await InitializeRateLimiter();
    }
    
    private async Task InitializeConnectionPool()
    {
        Log.Info("初始化连接池...");
        // 设置连接池大小
        await Task.CompletedTask;
    }
    
    private async Task LoadBlacklist()
    {
        Log.Info("加载黑名单...");
        // 从数据库或文件加载黑名单
        await Task.CompletedTask;
    }
    
    private async Task InitializeRateLimiter()
    {
        Log.Info("初始化速率限制器...");
        // 配置速率限制规则
        await Task.CompletedTask;
    }
}
```

---

## 错误处理

### 关键验证失败应该抛出异常

如果初始化钩子检测到**关键错误**，应该抛出异常，阻止服务器启动：

```csharp
public class CriticalValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        try
        {
            await ValidateCriticalConfig();
        }
        catch (Exception e)
        {
            Log.Error($"关键配置验证失败: {e}");
            throw; // 让服务器停止启动
        }
    }
    
    private async Task ValidateCriticalConfig()
    {
        // 检查数据库配置
        var dbConfig = ConfigLoader.DatabaseConfig;
        if (string.IsNullOrEmpty(dbConfig?.ConnectionString))
        {
            throw new InvalidOperationException("数据库连接字符串未配置");
        }
        
        await Task.CompletedTask;
    }
}
```

### 非关键操作可以捕获异常

对于**可选功能**，可以捕获异常并继续启动：

```csharp
public class OptionalFeatureHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        try
        {
            await LoadOptionalConfig();
        }
        catch (Exception e)
        {
            // 可选配置加载失败不影响启动
            Log.Warning($"可选配置加载失败，将使用默认值: {e.Message}");
        }
    }
    
    private async Task LoadOptionalConfig()
    {
        // 尝试加载可选的增强配置
        await Task.CompletedTask;
    }
}
```

---

## 可访问的内容

### ✅ 可以使用

在 `IEntryInitializeHook.OnInitialize()` 中，以下内容已经可用：

| 可用内容 | 说明 | 示例 |
|---------|------|------|
| **命令行参数** | 已解析完成 | `ProgramDefine.ProcessType`<br>`ProgramDefine.ProcessId`<br>`ProgramDefine.RuntimeMode` |
| **日志系统** | 已初始化 | `Log.Debug("...")`<br>`Log.Info("...")`<br>`Log.Error("...")` |
| **配置数据** | 已加载 | `ConfigLoader.Instance`<br>`ProcessConfigData.Instance`<br>`SceneConfigData.Instance` |
| **程序集信息** | 已加载 | `AssemblyManifest` |

### ❌ 不能使用

以下内容**尚未初始化**，不能在钩子中访问：

| 不可用内容 | 原因 | 替代方案 |
|-----------|------|---------|
| **Scene 实例** | 尚未创建 | 使用 `OnCreateScene` 事件 |
| **EventComponent** | 依赖 Scene | 使用 `OnCreateScene` 事件 |
| **TimerComponent** | 依赖 Scene | 使用 `OnCreateScene` 事件 |
| **NetworkMessagingComponent** | 依赖 Scene | 使用 `OnCreateScene` 事件 |
| **序列化系统** | 尚未初始化 | 在钩子后初始化 |

---

## 调试技巧

### 1. 添加日志确认执行

```csharp
public class DebugHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("========================================");
        Log.Info("Entry 初始化钩子开始执行");
        Log.Info($"ProcessType: {ProgramDefine.ProcessType}");
        Log.Info($"ProcessId: {ProgramDefine.ProcessId}");
        Log.Info($"RuntimeMode: {ProgramDefine.RuntimeMode}");
        Log.Info("========================================");
        
        // 你的逻辑
        
        Log.Info("Entry 初始化钩子执行完成");
        await Task.CompletedTask;
    }
}
```

### 2. 检查生成的注册代码

编译项目后，检查生成的注册代码：

```bash
# 查看生成的代码
cat obj/Debug/net8.0/generated/Fantasy.SourceGenerator/*/CustomInterfaceRegistrar.g.cs
```

你应该看到类似这样的代码：

```csharp
private global::Fantasy.Assembly.CustomInterfaceInfo _gameStartupHook = 
    new global::Fantasy.Assembly.CustomInterfaceInfo(
        typeof(YourNamespace.GameStartupHook), 
        () => new YourNamespace.GameStartupHook());
```

### 3. 验证执行顺序

```csharp
public class OrderTestHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info($"[OrderTest] Hook 执行时间: {DateTime.Now:HH:mm:ss.fff}");
        Log.Info($"[OrderTest] 配置已加载: {ConfigLoader.Instance != null}");
        Log.Info($"[OrderTest] Scene 是否存在: 不应该存在");
        
        await Task.CompletedTask;
    }
}
```

---

## 常见问题

### Q: 钩子没有执行？

**A:** 检查以下几点：
1. 是否正确实现了 `IEntryInitializeHook` 接口
2. 类是否在 Hotfix 或 Entity 项目中（需要被加载的程序集）
3. 重新编译项目，确保源代码生成器运行
4. 检查 `obj/Debug/.../CustomInterfaceRegistrar.g.cs` 是否包含你的类

### Q: 可以访问 Scene 吗？

**A:** 不可以。在 `IEntryInitializeHook` 执行时，Scene 尚未创建。
- 如果需要初始化 Scene，使用 `OnCreateScene` 事件
- 如果需要在 Scene 创建前做全局验证，使用 `IEntryInitializeHook`

### Q: 多个钩子的执行顺序是什么？

**A:** 执行顺序**不保证**。设计钩子时确保它们相互独立，不依赖执行顺序。

### Q: 钩子抛出异常会怎样？

**A:** 服务器会停止启动。框架会记录错误日志：
```
EntryInitializeHook GameStartupHook failed: System.Exception: ...
```

### Q: 能在钩子中使用序列化吗？

**A:** 不能。`SerializerManager.Initialize()` 在钩子执行**之后**才初始化。

### Q: 可以创建多少个钩子？

**A:** 没有限制，但建议：
- 按职责拆分（数据库验证、服务检查、许可证验证等）
- 每个钩子保持单一职责
- 确保钩子之间相互独立

---

## 最佳实践

1. **单一职责** - 每个钩子只做一件事
2. **失败快速** - 关键验证失败立即抛出异常
3. **清晰日志** - 记录开始和结束，记录关键步骤
4. **进程过滤** - 使用 `ProgramDefine.ProcessType` 过滤不相关进程
5. **异步规范** - 正确使用 `async/await`
6. **独立设计** - 钩子之间不要有依赖关系

---

## 相关文档

- [编写启动代码](02-WritingStartupCode.md) - 了解服务器启动流程
- [OnCreateScene 事件](04-OnCreateScene.md) - Scene 创建后的初始化
- [命令行参数](03-CommandLineArguments.md) - 理解 ProgramDefine 的内容
- [Fantasy.config 配置](01-ServerConfiguration.md) - 配置文件结构

---

## 示例项目

完整的示例代码在 Fantasy 仓库的 `/Examples/Server/APP/Hotfix/GameStartupHook.cs`。

```bash
# 查看示例
cat Examples/Server/APP/Hotfix/GameStartupHook.cs
```
