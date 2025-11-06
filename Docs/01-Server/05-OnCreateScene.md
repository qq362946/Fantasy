# OnCreateScene 事件使用指南

本指南将介绍如何使用 `OnCreateScene` 事件来初始化场景,为场景挂载组件以及执行初始化逻辑。

## 前置步骤

在开始使用 OnCreateScene 事件之前,请确保已完成以下步骤:

1. ✅ 已完成服务器启动代码的编写
2. ✅ 已配置好 `Fantasy.config` 文件

如果你还没有完成这些步骤,请先阅读:
- [编写启动代码](03-WritingStartupCode.md)
- [Fantasy.config 配置文件详解](01-ServerConfiguration.md)

---

## 什么是 OnCreateScene 事件?

`OnCreateScene` 是框架内置的场景创建事件,**在每个 Scene 启动完成后自动触发**。这是一个关键的生命周期事件,允许你在场景启动时执行自定义的初始化逻辑。

### 触发时机

```
场景启动流程:
┌─────────────────────────────────────────────────────┐
│ 1. Scene.Create()                                   │
│    ├─ 创建 Scene 实例                                │
│    ├─ 初始化核心组件 (EventComponent, Timer等)       │
│    ├─ 配置网络监听 (如果有配置)                       │
│    └─ 配置调度器 (MainThread/MultiThread/ThreadPool)│
├─────────────────────────────────────────────────────┤
│ 2. 发布 OnCreateScene 事件  ⬅️ 你的代码在这里执行     │
│    └─ EventComponent.PublishAsync(OnCreateScene)    │
│        └─ 触发你注册的 OnCreateSceneEvent Handler   │
├─────────────────────────────────────────────────────┤
│ 3. Scene 启动完成                                    │
└─────────────────────────────────────────────────────┘
```

**重要特性:**
- ✅ 在 Scene 核心组件初始化**之后**触发
- ✅ 在网络监听建立**之后**触发
- ✅ 支持异步操作 (`async/await`)
- ✅ 可以访问 Scene 的所有核心组件
- ✅ 支持为不同的 SceneType 执行不同的逻辑

---

## OnCreateScene 事件参数

`OnCreateScene` 是一个简单的结构体,定义在 `/Fantasy.Net/Fantasy.Net/Runtime/Core/Scene/Scene.cs:36`:

```csharp
/// <summary>
/// 当Scene创建完成后发送的事件参数
/// </summary>
public struct OnCreateScene
{
    /// <summary>
    /// 获取与事件关联的场景实体。
    /// </summary>
    public readonly Scene Scene;

    /// <summary>
    /// 初始化一个新的 OnCreateScene 实例。
    /// </summary>
    /// <param name="scene"></param>
    public OnCreateScene(Scene scene)
    {
        Scene = scene;
    }
}
```

**可用属性:**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Scene` | `Scene` | 当前创建的场景实例 |

通过 `Scene` 对象,你可以访问:
- `Scene.SceneType`: 场景类型 (对应 Fantasy.config 中的配置)
- `Scene.SceneConfigId`: 场景配置 ID
- `Scene.Process`: 所属的进程
- `Scene.World`: 所属的世界
- 所有核心组件: `EventComponent`, `TimerComponent`, `NetworkMessagingComponent` 等

---

## 创建 OnCreateScene 事件处理器

### 基础示例

在你的 Hotfix 或 Entity 项目中创建事件处理器:

```csharp
using Fantasy.Async;
using Fantasy.Event;

namespace Fantasy;

public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        // 在这里执行你的初始化逻辑
        Log.Info($"Scene created: SceneType={scene.SceneType}, SceneId={scene.Id}");

        await FTask.CompletedTask;
    }
}
```

**代码说明:**

1. **继承 `AsyncEventSystem<OnCreateScene>`**
   - 这是异步事件处理器的基类
   - 支持 `async/await` 操作

2. **重写 `Handler` 方法**
   - `self.Scene` 获取创建的场景实例
   - 返回 `FTask` (框架的异步任务类型)

3. **Source Generator 自动注册**
   - 编译时自动生成注册代码
   - 无需手动调用任何注册方法

---

## 常见使用场景

### 1. 根据 SceneType 执行不同逻辑

这是最常见的使用模式,为不同类型的场景执行不同的初始化逻辑:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
            {
                // Gate 场景初始化
                await InitializeGateScene(scene);
                break;
            }
            case SceneType.Map:
            {
                // Map 场景初始化
                await InitializeMapScene(scene);
                break;
            }
            case SceneType.Chat:
            {
                // Chat 场景初始化
                await InitializeChatScene(scene);
                break;
            }
            case SceneType.Addressable:
            {
                // Addressable 场景初始化
                await InitializeAddressableScene(scene);
                break;
            }
            default:
            {
                Log.Warning($"未处理的 SceneType: {scene.SceneType}");
                break;
            }
        }

        await FTask.CompletedTask;
    }

    private async FTask InitializeGateScene(Scene scene)
    {
        // Gate 场景特定的初始化逻辑
        Log.Info($"初始化 Gate 场景: {scene.Id}");
        await FTask.CompletedTask;
    }

    private async FTask InitializeMapScene(Scene scene)
    {
        // Map 场景特定的初始化逻辑
        Log.Info($"初始化 Map 场景: {scene.Id}");
        await FTask.CompletedTask;
    }

    private async FTask InitializeChatScene(Scene scene)
    {
        // Chat 场景特定的初始化逻辑
        Log.Info($"初始化 Chat 场景: {scene.Id}");
        await FTask.CompletedTask;
    }

    private async FTask InitializeAddressableScene(Scene scene)
    {
        // Addressable 场景特定的初始化逻辑
        Log.Info($"初始化 Addressable 场景: {scene.Id}");
        await FTask.CompletedTask;
    }
}
```

**SceneType 说明:**

- `SceneType` 是一个**枚举值**,由 `FantasyConfigGenerator` Source Generator 自动生成
- 生成规则基于 `Fantasy.config` 配置文件中的 `SceneType` 字段
- 生成位置: `obj/Debug/net8.0/generated/.../SceneType.g.cs`
- 使用时享有**编译时类型检查**和**智能提示**

---

### 2. 为场景挂载组件

为特定场景添加功能组件:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
            {
                // 为 Gate 场景添加连接管理组件
                scene.AddComponent<SessionManagerComponent>();
                scene.AddComponent<PlayerManagerComponent>();
                Log.Info("Gate 场景已挂载 SessionManager 和 PlayerManager 组件");
                break;
            }
            case SceneType.Map:
            {
                // 为 Map 场景添加地图管理组件
                scene.AddComponent<MapManagerComponent>();
                scene.AddComponent<MonsterManagerComponent>();
                scene.AddComponent<AOIComponent>();
                Log.Info("Map 场景已挂载地图相关组件");
                break;
            }
            case SceneType.Chat:
            {
                // 为 Chat 场景添加聊天管理组件
                scene.AddComponent<ChatManagerComponent>();
                scene.AddComponent<ChannelManagerComponent>();
                Log.Info("Chat 场景已挂载聊天相关组件");
                break;
            }
        }

        await FTask.CompletedTask;
    }
}
```

**组件挂载说明:**

- 使用 `scene.AddComponent<T>()` 添加组件
- 组件会自动触发其 `AwakeSystem` 生命周期
- 组件的生命周期与 Scene 绑定

---

### 3. 加载配置数据

在场景启动时加载必要的配置数据:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Map:
            {
                // 获取 Map 配置
                var mapConfig = GetMapConfig(scene.SceneConfigId);

                // 加载地图数据
                var mapData = await LoadMapData(mapConfig.MapId);

                // 初始化地图管理器
                var mapManager = scene.AddComponent<MapManagerComponent>();
                await mapManager.Initialize(mapData);

                Log.Info($"Map 场景已加载地图数据: MapId={mapConfig.MapId}");
                break;
            }
        }

        await FTask.CompletedTask;
    }

    private MapConfig GetMapConfig(uint sceneConfigId)
    {
        // 从配置表中获取地图配置
        // 这里是示例代码
        return new MapConfig { MapId = 1001 };
    }

    private async FTask<MapData> LoadMapData(int mapId)
    {
        // 从数据库或文件加载地图数据
        // 这里是示例代码
        await FTask.CompletedTask;
        return new MapData();
    }
}
```

---

### 4. 初始化数据库连接

为需要数据库访问的场景初始化数据库:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
            case SceneType.Addressable:
            {
                // 初始化数据库组件
                var dbComponent = scene.AddComponent<DatabaseComponent>();
                await dbComponent.Initialize("mongodb://localhost:27017", "GameDB");

                Log.Info($"场景 {scene.SceneType} 已初始化数据库连接");
                break;
            }
        }

        await FTask.CompletedTask;
    }
}
```

---

### 5. 注册定时任务

在场景启动时注册定时任务:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Map:
            {
                // 每5秒执行一次怪物刷新检查
                scene.TimerComponent.Core.RepeatedTimer(5000, () =>
                {
                    RefreshMonsters(scene);
                });

                // 每分钟执行一次场景数据保存
                scene.TimerComponent.Core.RepeatedTimer(60000, async () =>
                {
                    await SaveMapData(scene);
                });

                Log.Info("Map 场景已注册定时任务");
                break;
            }
        }

        await FTask.CompletedTask;
    }

    private void RefreshMonsters(Scene scene)
    {
        // 怪物刷新逻辑
        Log.Debug("执行怪物刷新检查");
    }

    private async FTask SaveMapData(Scene scene)
    {
        // 保存地图数据
        Log.Debug("保存地图数据");
        await FTask.CompletedTask;
    }
}
```

---

### 6. 跨服务器连接初始化

为需要与其他服务器通信的场景建立连接:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    private static long _addressableSceneRuntimeId;

    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Addressable:
            {
                // 保存 Addressable 场景的 RuntimeId,供其他场景使用
                _addressableSceneRuntimeId = scene.RuntimeId;
                Log.Info($"Addressable 场景已启动: RuntimeId={scene.RuntimeId}");
                break;
            }
            case SceneType.Gate:
            {
                // Gate 场景需要连接到 Addressable 场景
                if (_addressableSceneRuntimeId != 0)
                {
                    var session = scene.GetSession(_addressableSceneRuntimeId);
                    Log.Info($"Gate 场景已建立到 Addressable 场景的连接: Session={session.Id}");
                }
                break;
            }
        }

        await FTask.CompletedTask;
    }
}
```

---

## 完整示例

以下是框架自带的完整示例 (`/Examples/Server/Hotfix/OnCreateSceneEvent.cs`):

```csharp
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;

namespace Fantasy;

// 示例组件:在 SubScene 下测试组件生命周期
public sealed class SubSceneTestComponent : Entity
{
    public override void Dispose()
    {
        Log.Debug("销毁SubScene下的SubSceneTestComponent");
        base.Dispose();
    }
}

// 示例组件的 Awake 系统
public sealed class SubSceneTestComponentAwakeSystem : AwakeSystem<SubSceneTestComponent>
{
    protected override void Awake(SubSceneTestComponent self)
    {
        Log.Debug("SubSceneTestComponentAwakeSystem");
    }
}

// OnCreateScene 事件处理器
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    private static long _addressableSceneRunTimeId;

    /// <summary>
    /// Handles the OnCreateScene event.
    /// </summary>
    /// <param name="self">The OnCreateScene object.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        await FTask.CompletedTask;

        switch (scene.SceneType)
        {
            case 6666:
            {
                // 使用自定义 SceneType 值
                break;
            }
            case SceneType.Addressable:
            {
                // 保存 Addressable 场景的 RuntimeId
                _addressableSceneRunTimeId = scene.RuntimeId;
                break;
            }
            case SceneType.Map:
            {
                // Map 场景初始化
                Log.Debug($"Map Scene  SceneRuntimeId:{scene.RuntimeId}");
                break;
            }
            case SceneType.Chat:
            {
                // Chat 场景初始化
                break;
            }
            case SceneType.Gate:
            {
                // Gate 场景初始化
                // 下面是压力测试代码示例(已注释)
                // var tasks = new List<FTask>(2000);
                // var session = scene.GetSession(_addressableSceneRunTimeId);
                // var sceneNetworkMessagingComponent = scene.NetworkMessagingComponent;
                // var g2ATestRequest = new G2A_TestRequest();
                //
                // async FTask Call()
                // {
                //     await sceneNetworkMessagingComponent.CallInnerRouteBySession(session,_addressableSceneRunTimeId,g2ATestRequest);
                // }
                //
                // for (int i = 0; i < 100000000000; i++)
                // {
                //     tasks.Clear();
                //     for (int j = 0; j < tasks.Capacity; ++j)
                //     {
                //         tasks.Add(Call());
                //     }
                //     await FTask.WaitAll(tasks);
                // }
                break;
            }
        }
    }
}
```

---

## 最佳实践

### 1. 按 SceneType 组织代码

**推荐做法:**

将不同 SceneType 的初始化逻辑拆分到不同的方法中:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
                await InitializeGateScene(scene);
                break;
            case SceneType.Map:
                await InitializeMapScene(scene);
                break;
            // ... 其他 SceneType
        }

        await FTask.CompletedTask;
    }

    // 每个 SceneType 独立的初始化方法
    private async FTask InitializeGateScene(Scene scene) { /* ... */ }
    private async FTask InitializeMapScene(Scene scene) { /* ... */ }
}
```

**好处:**
- ✅ 代码结构清晰
- ✅ 易于维护和测试
- ✅ 减少单个方法的复杂度

---

### 2. 使用静态变量共享 RuntimeId

在某些场景中,你可能需要在不同场景之间共享信息:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    // 使用静态变量保存关键场景的 RuntimeId
    private static long _addressableSceneRuntimeId;
    private static long _gateSceneRuntimeId;

    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Addressable:
                _addressableSceneRuntimeId = scene.RuntimeId;
                break;
            case SceneType.Gate:
                _gateSceneRuntimeId = scene.RuntimeId;
                break;
        }

        await FTask.CompletedTask;
    }
}
```

**注意事项:**
- ⚠️ 静态变量在多进程环境下不共享
- ⚠️ 考虑线程安全性
- ✅ 适用于进程内场景引用

---

### 3. 异常处理

虽然框架会捕获异常,但建议在关键逻辑中添加异常处理:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        try
        {
            switch (scene.SceneType)
            {
                case SceneType.Gate:
                {
                    await InitializeGateScene(scene);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"场景初始化失败: SceneType={scene.SceneType}, Error={ex}");
            // 根据需要决定是否重新抛出异常
            throw;
        }

        await FTask.CompletedTask;
    }
}
```

---

### 4. 日志记录

添加适当的日志记录,便于调试和监控:

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        Log.Info($"[OnCreateScene] 场景创建: SceneType={scene.SceneType}, SceneId={scene.Id}, RuntimeId={scene.RuntimeId}");

        switch (scene.SceneType)
        {
            case SceneType.Gate:
            {
                Log.Info($"[OnCreateScene] 开始初始化 Gate 场景");
                await InitializeGateScene(scene);
                Log.Info($"[OnCreateScene] Gate 场景初始化完成");
                break;
            }
        }

        await FTask.CompletedTask;
    }
}
```

---

### 5. 避免阻塞操作

OnCreateScene 事件处理器应该**快速完成**,避免长时间阻塞:

**❌ 不推荐:**

```csharp
protected override async FTask Handler(OnCreateScene self)
{
    // 阻塞 5 秒 - 会延迟场景启动
    Thread.Sleep(5000);

    await FTask.CompletedTask;
}
```

**✅ 推荐:**

```csharp
protected override async FTask Handler(OnCreateScene self)
{
    var scene = self.Scene;

    // 快速初始化
    scene.AddComponent<MyComponent>();

    // 如果有耗时操作,使用异步或延迟执行
    scene.TimerComponent.Core.OnceTimer(0, async () =>
    {
        await LongRunningInitialization(scene);
    });

    await FTask.CompletedTask;
}
```

---

## SubScene 的 OnCreateScene

SubScene (子场景) 也会触发 `OnCreateScene` 事件:

```csharp
// 创建 SubScene
var subScene = Scene.CreateSubScene(parentScene, SceneType.MapInstance, (sub, parent) =>
{
    Log.Info($"SubScene 创建完成: {sub.Id}");
});

// OnCreateScene 事件处理器会自动触发
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        // 判断是否为 SubScene
        if (scene.SceneRuntimeType == SceneRuntimeType.SubScene)
        {
            Log.Info($"这是一个 SubScene: SceneType={scene.SceneType}");

            // SubScene 特定的初始化逻辑
            if (scene is SubScene subScene)
            {
                Log.Info($"SubScene 的父场景: {subScene.RootScene.Id}");
            }
        }

        await FTask.CompletedTask;
    }
}
```

**SubScene 特性:**

- ✅ 共享父场景的 `EntityIdFactory` 和 `RuntimeIdFactory`
- ✅ 可以访问 `RootScene` 属性获取父场景
- ✅ 独立的实体管理
- ✅ 适用于副本、战斗场景等

---

## 常见问题

### Q1: OnCreateScene 事件没有被触发?

**可能原因:**

1. **Source Generator 没有生成注册代码**
   - 检查项目是否正确引用了 `Fantasy.SourceGenerator`
   - 确保定义了 `FANTASY_NET` 或 `FANTASY_UNITY` 宏

2. **程序集未正确加载**
   - 确保在 `Entry.Start()` 之前调用了 `AssemblyHelper.Initialize()`
   - 参考 [编写启动代码](04-WritingStartupCode.md)

3. **事件处理器定义错误**
   - 确保继承自 `AsyncEventSystem<OnCreateScene>`
   - 确保重写了 `Handler` 方法

**解决:**

```bash
# 清理并重新构建
dotnet clean
dotnet build

# 检查生成的代码
cat obj/Debug/net8.0/generated/Fantasy.SourceGenerator/Fantasy.SourceGenerator.EventSystemGenerator/EventSystemRegistrar.g.cs
```

---

### Q2: 如何在 OnCreateScene 中访问场景配置?

通过 `scene.SceneConfig` 属性:

```csharp
protected override async FTask Handler(OnCreateScene self)
{
    var scene = self.Scene;
    var config = scene.SceneConfig;

    Log.Info($"场景配置: SceneType={config.SceneType}, InnerPort={config.InnerPort}, OuterPort={config.OuterPort}");

    await FTask.CompletedTask;
}
```

---

### Q3: 可以注册多个 OnCreateScene 事件处理器吗?

**可以,但不推荐。**

框架支持为同一事件注册多个处理器,但这会使初始化逻辑分散,难以维护。

**推荐做法:**

```csharp
// ✅ 推荐:单一事件处理器,内部按需分发
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        await SceneInitializer.Initialize(self.Scene);
    }
}

// 在单独的类中组织初始化逻辑
public static class SceneInitializer
{
    public static async FTask Initialize(Scene scene)
    {
        switch (scene.SceneType)
        {
            case SceneType.Gate:
                await InitializeGate(scene);
                break;
            // ... 其他类型
        }
    }

    private static async FTask InitializeGate(Scene scene) { /* ... */ }
}
```

---

### Q4: OnCreateScene 中的异步操作会阻塞场景启动吗?

**会的。**

`OnCreateScene` 使用 `PublishAsync()` 发布,会等待所有处理器完成:

```csharp
// Scene.cs:443
scene.EventComponent.PublishAsync(new OnCreateScene(scene)).Coroutine();
```

因此:
- ✅ 适合执行必要的初始化逻辑
- ⚠️ 避免长时间阻塞操作
- ✅ 耗时操作应使用定时器延迟执行

---

### Q5: 如何在 Unity 客户端使用 OnCreateScene?

Unity 客户端也支持 `OnCreateScene` 事件:

```csharp
// Unity 客户端代码
using Fantasy;
using Fantasy.Async;
using Fantasy.Event;

public sealed class ClientOnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        Log.Info($"Unity 客户端场景创建: {scene.Id}");

        // 客户端特定的初始化逻辑
        // 例如:初始化 UI 管理器、资源加载器等

        await FTask.CompletedTask;
    }
}
```

---

## 相关生命周期事件

除了 `OnCreateScene`,框架还提供了其他场景相关的事件:

| 事件 | 触发时机 | 用途 |
|------|---------|------|
| `OnCreateScene` | 场景创建完成后 | 场景初始化 |
| `OnDisposeScene` | 场景销毁之前 | 场景清理 (需自行定义) |

**自定义场景销毁事件示例:**

```csharp
// 定义事件参数
public struct OnDisposeScene
{
    public readonly Scene Scene;
    public OnDisposeScene(Scene scene) => Scene = scene;
}

// 在 Scene.Dispose() 中发布事件 (需修改框架代码或通过其他机制)

// 事件处理器
public sealed class OnDisposeSceneEvent : EventSystem<OnDisposeScene>
{
    protected override void Handler(OnDisposeScene self)
    {
        var scene = self.Scene;
        Log.Info($"场景销毁: {scene.SceneType}");

        // 清理逻辑
    }
}
```

---

## 下一步

现在你已经掌握了如何使用 `OnCreateScene` 事件,接下来可以:

1. 📖 阅读 [ECS 系统详解](06-ECS.md) 学习实体组件系统 (待完善)
2. 🌐 阅读 [网络消息处理](07-Network.md) 学习消息处理器 (待完善)
3. 🔧 阅读 [协议定义指南](08-Protocol.md) 学习 .proto 文件 (待完善)
4. 📚 查看 `Examples/Server/Hotfix` 目录下的完整示例

## 获取帮助

- **GitHub**: https://github.com/qq362946/Fantasy
- **文档**: https://www.code-fantasy.com/
- **Issues**: https://github.com/qq362946/Fantasy/issues

---
