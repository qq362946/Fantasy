# Entry Initialize Hook 代码检查清单

## 检查项

### 1. 接口实现

**正确：**
```csharp
public class GameStartupHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // 实现逻辑
        await Task.CompletedTask;
    }
}
```

**错误：**
```csharp
// ❌ 缺少继承 IEntryInitializeHook
public class GameStartupHook
{
    public async Task OnInitialize() { }
}

// ❌ 方法签名不匹配
public class GameStartupHook : IEntryInitializeHook
{
    public void OnInitialize() { }  // 应该是 async Task
}

// ❌ 使用了 Task 而不是保持一致
public class GameStartupHook : IEntryInitializeHook
{
    public Task OnInitialize()  // 可以，但建议用 async Task
    {
        return Task.CompletedTask;
    }
}
```

### 2. 时机理解

**可以访问的内容（在 Hook 执行时）：**
- ✅ `ProgramDefine.ProcessType` / `ProcessId` / `RuntimeMode`
- ✅ `Log.Debug/Info/Error`
- ✅ `ConfigLoader` 和配置数据
- ✅ `AssemblyManifest`

**不能访问的内容：**
- ❌ Scene 实例（尚未创建）
- ❌ EventComponent（依赖 Scene）
- ❌ 序列化系统（尚未初始化）

**错误示例：**
```csharp
public class BadHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ❌ Scene 尚未创建
        var scene = Scene.GetScene(1);
        
        // ❌ EventComponent 依赖 Scene
        scene.EventComponent.Publish(new SomeEvent());
        
        // ❌ 序列化器尚未初始化
        var data = ProtoBufHelper.ToBytes(someObject);
    }
}
```

**正确示例：**
```csharp
public class GoodHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ✅ 检查配置
        if (ProgramDefine.ProcessType == "Game")
        {
            var config = ProcessConfigData.Instance.Get(ProgramDefine.ProcessId);
            Log.Info($"Process config loaded: {config.Id}");
        }
        
        // ✅ 验证外部服务
        await ValidateExternalService();
        
        await Task.CompletedTask;
    }
}
```

### 3. 职责边界

**正确用法：**
- ✅ 配置验证
- ✅ 数据库连接检查
- ✅ 外部服务可用性检查
- ✅ 环境变量加载
- ✅ 许可证验证
- ✅ 预加载静态数据

**错误用法（应该用其他机制）：**
- ❌ Scene 初始化逻辑 → 用 `OnCreateScene` 事件
- ❌ Entity/Component 创建 → 用 `AwakeSystem`
- ❌ 网络消息处理 → 用 Message Handler
- ❌ 定时任务 → 用 `TimerComponent`

**错误示例：**
```csharp
public class WrongHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ❌ 这应该在 OnCreateScene 事件里做
        var scene = Scene.Create(...);
        scene.AddComponent<SomeComponent>();
        
        // ❌ 这应该在 AwakeSystem 里做
        var entity = scene.Create<SomeEntity>();
        entity.AddComponent<SomeComponent>();
    }
}
```

### 4. 异常处理

**正确：关键验证失败时抛出异常**
```csharp
public class ValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        try
        {
            await ValidateCriticalConfig();
        }
        catch (Exception e)
        {
            Log.Error($"Critical validation failed: {e}");
            throw; // ✅ 让服务器停止启动
        }
    }
}
```

**错误：吞掉关键错误**
```csharp
public class BadHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        try
        {
            await ValidateCriticalConfig();
        }
        catch (Exception e)
        {
            // ❌ 吞掉异常，服务器继续启动（可能处于错误状态）
            Log.Error($"Validation failed: {e}");
        }
    }
}
```

**正确：非关键操作可以吞掉异常**
```csharp
public class OptionalHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        try
        {
            await LoadOptionalConfig();
        }
        catch (Exception e)
        {
            // ✅ 可选配置失败不影响启动
            Log.Warning($"Optional config load failed: {e}");
        }
    }
}
```

### 5. 多 Hook 独立性

**正确：Hook 之间相互独立**
```csharp
// Hook 1
public class DatabaseHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        await ValidateDatabase();
    }
}

// Hook 2 - 独立运行，不依赖 Hook 1
public class LicenseHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        await ValidateLicense();
    }
}
```

**错误：Hook 之间有依赖**
```csharp
// ❌ Hook 之间的执行顺序不保证
public static class SharedState
{
    public static bool DatabaseValidated;
}

public class Hook1 : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        await ValidateDatabase();
        SharedState.DatabaseValidated = true;
    }
}

public class Hook2 : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ❌ 可能 Hook1 还没执行
        if (!SharedState.DatabaseValidated)
        {
            throw new Exception("Database not validated");
        }
    }
}
```

### 6. ProcessType 过滤

**正确：明确过滤不相关的进程类型**
```csharp
public class GameServerHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ✅ 只在 Game 进程执行
        if (ProgramDefine.ProcessType != "Game")
        {
            return;
        }
        
        Log.Info("Game server initialization...");
        await InitializeGameSpecificLogic();
    }
}
```

**可接受：所有进程都执行**
```csharp
public class CommonHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ✅ 所有进程类型都需要的逻辑
        Log.Info("Common initialization for all process types");
        await ValidateCommonConfig();
    }
}
```

### 7. 日志规范

**正确：清晰的日志**
```csharp
public class GoodLoggingHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("Starting database validation...");
        await ValidateDatabase();
        Log.Info("Database validation completed successfully");
    }
}
```

**错误：日志不清晰**
```csharp
public class BadLoggingHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ❌ 没有日志，出问题不知道在哪
        await ValidateDatabase();
        
        // ❌ 日志信息不明确
        Log.Info("Done");
    }
}
```

### 8. 异步规范

**正确：正确使用 await**
```csharp
public class AsyncHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ✅ 正确 await
        await ValidateAsync();
        await CheckExternalServiceAsync();
    }
}
```

**错误：忘记 await**
```csharp
public class BadAsyncHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // ❌ 忘记 await，不会等待完成
        ValidateAsync();
        
        // ❌ 使用 .Wait() 会阻塞线程
        CheckExternalServiceAsync().Wait();
    }
}
```

## 常见错误对比

| 错误代码 | 问题 | 正确做法 |
|---------|------|---------|
| `scene.EventComponent.Publish(...)` | Scene 尚未创建 | 在 `OnCreateScene` 事件中发布 |
| `Scene.Create(...)` | 不应该在 Hook 中创建 Scene | 让框架在 `StartProcess()` 中创建 |
| `entity.AddComponent<T>()` | Entity 逻辑不属于 Entry 初始化 | 在 `AwakeSystem` 中添加 |
| 多个 Hook 共享状态 | Hook 执行顺序不保证 | 设计独立的 Hook |
| 吞掉关键异常 | 服务器在错误状态下继续运行 | 关键错误应该 `throw` |
| 没有 ProcessType 过滤 | 在不相关进程执行无用逻辑 | 添加 `if (ProgramDefine.ProcessType == "...")` |

## 审查 Workflow

1. **检查接口实现** - 是否正确继承 `IEntryInitializeHook`
2. **检查时机理解** - 是否访问了尚未初始化的内容（Scene、EventComponent 等）
3. **检查职责边界** - 是否把 Scene 初始化、Entity 逻辑放在这里
4. **检查异常处理** - 关键验证失败是否正确抛出异常
5. **检查 Hook 独立性** - 多个 Hook 之间是否有隐含依赖
6. **检查日志规范** - 是否有清晰的日志输出
7. **检查 ProcessType 过滤** - 是否在不相关进程执行

## 审查输出顺序

1. **严重问题** - 访问未初始化内容、职责边界错误
2. **规范问题** - 接口实现错误、异步使用不当
3. **潜在风险** - 异常处理不当、Hook 之间有依赖
4. **可选优化** - 日志可以更清晰、可以添加 ProcessType 过滤
