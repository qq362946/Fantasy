# SphereEvent 跨服域事件系统

## 什么是 SphereEvent？

SphereEvent（跨服域事件系统）是一种**分布式事件发布-订阅机制**，允许不同服务器之间通过事件进行解耦通信。

**一句话总结：** 服务器 A 订阅服务器 B 的事件 → 服务器 B 发布事件 → 服务器 A 自动接收并处理

**与本地 Event 系统的区别：**

| 特性 | Event（本地事件） | SphereEvent（跨服事件） |
|------|----------------|---------------------|
| 作用范围 | 单个 Scene 内部 | 跨 Scene、跨服务器 |
| 通信方式 | 进程内方法调用 | 网络通信（序列化传输） |
| 订阅方式 | 编译时自动注册 | 运行时动态订阅 |
| 适用场景 | 本地模块解耦 | 分布式服务器协作 |

**适用场景：**
- ✅ 跨服务器的业务事件通知（如玩家跨服聊天、公会战报、排行榜更新）
- ✅ 分布式系统的状态同步（如服务器状态变化、资源更新）
- ✅ 服务器间的松耦合通信（避免硬编码 RPC 调用）

---

## 快速开始

### 完整流程

```
1. 定义 SphereEvent 事件类
   ↓
2. 实现 SphereEventSystem 事件处理器
   ↓
3. 服务器 A 订阅服务器 B 的事件
   ↓
4. 服务器 B 发布事件
   ↓
5. 服务器 A 自动接收并处理
```

下面按步骤详细说明。

---

## 步骤 1：定义 SphereEvent 事件类

SphereEvent 事件类必须继承自 `SphereEventArgs` 基类。

### 基本定义

```csharp
using Fantasy.Sphere;

/// <summary>
/// 玩家等级变化事件
/// </summary>
public sealed class PlayerLevelChangedEvent : SphereEventArgs
{
    public long PlayerId { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
}
```

### 事件类特性

**SphereEventArgs 基类提供：**

- **序列化支持**：事件会自动序列化后通过网络传输
- **对象池管理**：使用 `Create()` 方法创建，减少 GC 压力
- **类型识别**：内置 `TypeHashCode` 用于快速类型匹配
- **自动释放**：支持自动回收到对象池

**定义规范：**

```csharp
// ✅ 正确：使用 sealed 修饰，继承 SphereEventArgs
public sealed class TestSphereEvent : SphereEventArgs
{
    public string Tag { get; set; }
    public int Value { get; set; }
}

// ❌ 错误：不要继承自 Entity 或其他类型
public class BadEvent : Entity // 错误！
{
}

// ❌ 错误：不要使用 struct
public struct BadEvent // 错误！
{
}
```

### 创建事件实例

```csharp
// 从对象池创建（推荐，减少 GC）
var event1 = SphereEventArgs.Create<PlayerLevelChangedEvent>(isFromPool: true);
event1.PlayerId = 10001;
event1.OldLevel = 10;
event1.NewLevel = 11;

// 不使用对象池创建
var event2 = SphereEventArgs.Create<PlayerLevelChangedEvent>(isFromPool: false);
```

---

## 步骤 2：实现事件处理器

### 定义事件处理器

事件处理器必须继承 `SphereEventSystem<T>` 并实现 `Handler` 方法：

```csharp
using Fantasy.Sphere;
using Fantasy.Async;

/// <summary>
/// 处理玩家等级变化事件
/// </summary>
public sealed class OnPlayerLevelChanged : SphereEventSystem<PlayerLevelChangedEvent>
{
    protected override async FTask Handler(Scene scene, PlayerLevelChangedEvent args)
    {
        Log.Info($"收到玩家等级变化事件: PlayerId={args.PlayerId}, {args.OldLevel} -> {args.NewLevel}");

        // 处理业务逻辑
        // 例如：更新排行榜、发送奖励、触发成就等

        await FTask.CompletedTask;
    }
}
```

### 自动注册机制

**SphereEventSystem 通过 Source Generator 自动注册**，无需手动调用注册方法：

```csharp
// ✅ 只需定义处理器类，编译时自动注册
public sealed class OnTestSphereEvent : SphereEventSystem<TestSphereEvent>
{
    protected override async FTask Handler(Scene scene, TestSphereEvent args)
    {
        Log.Debug($"OnTestSphereEvent {args.Tag} scene:{scene.SceneType}");
        await FTask.CompletedTask;
    }
}
```

**自动生成的代码：**

Source Generator 会生成类似以下的注册代码：

```csharp
// SphereEventRegistrar.g.cs (自动生成)
public class SphereEventRegistrar : ISphereEventRegistrar
{
    public RuntimeTypeHandle[] TypeHashCodes() => new[]
    {
        typeof(TestSphereEvent).TypeHandle,
        typeof(PlayerLevelChangedEvent).TypeHandle
    };

    public ISphereEvent[] SphereEvent() => new ISphereEvent[]
    {
        new OnTestSphereEvent(),
        new OnPlayerLevelChanged()
    };
}
```

---

## 步骤 3：订阅远程事件

### API 说明

```csharp
// 订阅远程服务器的事件
FTask SphereEventComponent.Subscribe<T>(long remoteAddress) where T : SphereEventArgs, new()

// 或使用 TypeHashCode 订阅
FTask SphereEventComponent.Subscribe(long remoteAddress, long typeHashCode)
```

**参数说明：**

- `remoteAddress`：远程服务器的 Address（Scene.Address）
- `T` 或 `typeHashCode`：要订阅的事件类型

### 订阅示例

**场景：Map 服务器订阅 Gate 服务器的事件**

```csharp
// Map 服务器代码
public class MapServerInit
{
    public async FTask Initialize(Scene mapScene)
    {
        // 获取 Gate 服务器的 Address
        var gateConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
        var gateAddress = gateConfig.Address;

        // 订阅 Gate 服务器的 PlayerLevelChangedEvent
        await mapScene.SphereEventComponent.Subscribe<PlayerLevelChangedEvent>(gateAddress);

        Log.Info("✅ Map 服务器已订阅 Gate 的 PlayerLevelChangedEvent");
    }
}
```

**通过 Address 消息订阅示例：**

```csharp
// Gate 服务器：接收 Map 的订阅请求
public sealed class G2Map_SubscribeSphereEventRequestHandler : AddressRPC<Scene, G2Map_SubscribeSphereEventRequest, G2Map_SubscribeSphereEventResponse>
{
    protected override async FTask Run(
        Scene scene,
        G2Map_SubscribeSphereEventRequest request,
        G2Map_SubscribeSphereEventResponse response,
        Action reply)
    {
        // Map 服务器订阅当前 Gate 服务器的事件
        await scene.SphereEventComponent.Subscribe<TestSphereEvent>(request.GateAddress);

        Log.Info($"✅ 订阅成功: GateAddress={request.GateAddress}");
    }
}
```

---

## 步骤 4：发布事件

### API 说明

```csharp
// 向所有订阅者发布事件
FTask SphereEventComponent.PublishToRemoteSubscribers(
    SphereEventArgs sphereEventArgs,
    bool isAutoDisposed
)
```

**参数说明：**

- `sphereEventArgs`：事件参数对象
- `isAutoDisposed`：发布完成后是否自动释放事件对象到对象池
  - `true`：自动释放（推荐，减少 GC）
  - `false`：手动管理生命周期

### 发布示例

```csharp
// Gate 服务器：发布玩家等级变化事件
public class PlayerService
{
    public async FTask OnPlayerLevelUp(Scene scene, long playerId, int oldLevel, int newLevel)
    {
        // 创建事件对象（使用对象池）
        var levelChangedEvent = SphereEventArgs.Create<PlayerLevelChangedEvent>(isFromPool: true);
        levelChangedEvent.PlayerId = playerId;
        levelChangedEvent.OldLevel = oldLevel;
        levelChangedEvent.NewLevel = newLevel;

        // 发布到所有订阅者（自动释放事件对象）
        await scene.SphereEventComponent.PublishToRemoteSubscribers(levelChangedEvent, isAutoDisposed: true);

        Log.Info($"✅ 已发布 PlayerLevelChangedEvent: PlayerId={playerId}");
    }
}
```

**客户端触发的发布示例：**

```csharp
// Gate 服务器：处理客户端的发布请求
public class C2G_PublishSphereEventRequestHandler : MessageRPC<C2G_PublishSphereEventRequest, G2C_PublishSphereEventResponse>
{
    protected override async FTask Run(
        Session session,
        C2G_PublishSphereEventRequest request,
        G2C_PublishSphereEventResponse response,
        Action reply)
    {
        // 创建事件
        var testSphereEvent = SphereEventArgs.Create<TestSphereEvent>(isFromPool: true);
        testSphereEvent.Tag = "Hi Sphere Event!";

        // 发布到所有远程订阅者
        await session.Scene.SphereEventComponent.PublishToRemoteSubscribers(testSphereEvent, isAutoDisposed: true);

        Log.Info("✅ 事件已发布");
    }
}
```

---

## 步骤 5：处理远程事件

当远程服务器发布事件时，本地的 `SphereEventSystem` 会自动被调用。

### 完整示例

```csharp
// Map 服务器：处理 Gate 发布的事件
public sealed class OnPlayerLevelChanged : SphereEventSystem<PlayerLevelChangedEvent>
{
    protected override async FTask Handler(Scene scene, PlayerLevelChangedEvent args)
    {
        Log.Info($"[Map] 收到玩家等级变化: PlayerId={args.PlayerId}, {args.OldLevel} -> {args.NewLevel}");

        // 业务逻辑 1: 更新地图内的玩家信息
        var player = scene.GetPlayerById(args.PlayerId);
        if (player != null)
        {
            player.Level = args.NewLevel;
            Log.Info($"[Map] 已更新玩家等级");
        }

        // 业务逻辑 2: 触发地图内的特效
        await ShowLevelUpEffect(scene, args.PlayerId);

        // 业务逻辑 3: 检查是否解锁新区域
        await CheckUnlockMapArea(scene, args.PlayerId, args.NewLevel);
    }

    private async FTask ShowLevelUpEffect(Scene scene, long playerId)
    {
        // 显示升级特效逻辑
        await FTask.CompletedTask;
    }

    private async FTask CheckUnlockMapArea(Scene scene, long playerId, int level)
    {
        // 检查解锁地图区域逻辑
        await FTask.CompletedTask;
    }
}
```

---

## 高级功能

### 1. 取消订阅

#### 主动取消订阅

```csharp
// 取消订阅远程服务器的事件
public async FTask UnsubscribeExample(Scene scene, long remoteAddress)
{
    await scene.SphereEventComponent.Unsubscribe<PlayerLevelChangedEvent>(remoteAddress);

    Log.Info("✅ 已取消订阅 PlayerLevelChangedEvent");
}
```

#### 撤销远程订阅者（服务器主动撤销）

```csharp
// 服务器主动撤销某个订阅者的订阅
public async FTask RevokeSubscriberExample(Scene scene, long subscriberAddress)
{
    // 撤销指定订阅者的订阅
    await scene.SphereEventComponent.RevokeRemoteSubscriber<PlayerLevelChangedEvent>(subscriberAddress);

    Log.Info($"✅ 已撤销订阅者 {subscriberAddress} 的订阅");
}
```

**Unsubscribe vs RevokeRemoteSubscriber：**

| 方法 | 调用方 | 作用 | 场景 |
|------|--------|------|------|
| **Unsubscribe** | 订阅方（本地） | 主动取消自己的订阅 | 不再需要监听某事件 |
| **RevokeRemoteSubscriber** | 发布方（远程） | 撤销某订阅者的订阅 | 主动断开某订阅者 |

---

### 2. 注销远程订阅者

```csharp
// 注销远程订阅者（仅本地移除，不通知远程）
public async FTask UnregisterSubscriberExample(Scene scene, long subscriberAddress)
{
    await scene.SphereEventComponent.UnregisterRemoteSubscriber<PlayerLevelChangedEvent>(subscriberAddress);

    Log.Info("✅ 已注销远程订阅者");
}
```

**UnregisterRemoteSubscriber vs RevokeRemoteSubscriber：**

| 方法 | 通知远程 | 使用场景 |
|------|---------|---------|
| **UnregisterRemoteSubscriber** | ❌ 否 | 远程服务器已断开或无需通知 |
| **RevokeRemoteSubscriber** | ✅ 是 | 主动撤销订阅，通知远程服务器 |

---

### 3. 清理所有订阅

```csharp
// 关闭并清理所有订阅关系
public async FTask CloseExample(Scene scene)
{
    await scene.SphereEventComponent.Close();

    Log.Info("✅ 已清理所有 SphereEvent 订阅");
}
```

**Close() 方法会：**

1. 取消所有本地订阅（通知远程服务器）
2. 撤销所有远程订阅者（通知订阅方）
3. 释放相关资源（CoroutineLock 等）

**使用场景：**
- 服务器关闭前清理
- Scene 销毁前清理
- 重置订阅关系

---

### 4. 多个订阅者场景

一个事件可以被多个服务器订阅：

```csharp
// Gate 服务器发布事件
var event1 = SphereEventArgs.Create<PlayerLevelChangedEvent>(isFromPool: true);
event1.PlayerId = 10001;
event1.NewLevel = 50;

// 发布到所有订阅者（Map1、Map2、Map3 等）
await gateScene.SphereEventComponent.PublishToRemoteSubscribers(event1, isAutoDisposed: true);

// 所有订阅了该事件的服务器都会收到通知
```

**订阅关系示例：**

```
Gate 服务器（发布方）
    ↓ 发布 PlayerLevelChangedEvent
    ├─→ Map1 服务器（订阅方 1）
    ├─→ Map2 服务器（订阅方 2）
    ├─→ Chat 服务器（订阅方 3）
    └─→ Rank 服务器（订阅方 4）
```

---

## 完整使用示例

### 示例场景：跨服公会战报通知

**需求：** Gate 服务器上的公会战斗结束后，通知 Map 和 Chat 服务器更新公会信息。

#### 1. 定义事件

```csharp
using Fantasy.Sphere;

/// <summary>
/// 公会战报事件
/// </summary>
public sealed class GuildBattleResultEvent : SphereEventArgs
{
    public long GuildId { get; set; }
    public string GuildName { get; set; }
    public bool IsWin { get; set; }
    public int Score { get; set; }
}
```

#### 2. 实现事件处理器

**Map 服务器处理器：**

```csharp
public sealed class OnGuildBattleResult_Map : SphereEventSystem<GuildBattleResultEvent>
{
    protected override async FTask Handler(Scene scene, GuildBattleResultEvent args)
    {
        Log.Info($"[Map] 公会战报: {args.GuildName} {(args.IsWin ? "胜利" : "失败")}, 积分: {args.Score}");

        // 更新地图内的公会信息
        var guildComponent = scene.GetComponent<GuildComponent>();
        await guildComponent.UpdateGuildScore(args.GuildId, args.Score);

        // 显示战报特效
        await ShowBattleResultEffect(scene, args);
    }

    private async FTask ShowBattleResultEffect(Scene scene, GuildBattleResultEvent args)
    {
        // 显示特效逻辑
        await FTask.CompletedTask;
    }
}
```

**Chat 服务器处理器：**

```csharp
public sealed class OnGuildBattleResult_Chat : SphereEventSystem<GuildBattleResultEvent>
{
    protected override async FTask Handler(Scene scene, GuildBattleResultEvent args)
    {
        Log.Info($"[Chat] 公会战报: {args.GuildName} {(args.IsWin ? "胜利" : "失败")}");

        // 发送公会频道消息
        var chatComponent = scene.GetComponent<ChatComponent>();
        var message = $"恭喜 {args.GuildName} 在公会战中{(args.IsWin ? "获得胜利" : "奋勇作战")}, 获得 {args.Score} 积分!";
        await chatComponent.SendGuildChannelMessage(args.GuildId, message);
    }
}
```

#### 3. 服务器启动时建立订阅

**Map 服务器启动代码：**

```csharp
public class OnCreateMapSceneHandler : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        // 订阅 Gate 服务器的公会战报事件
        var gateConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
        await scene.SphereEventComponent.Subscribe<GuildBattleResultEvent>(gateConfig.Address);

        Log.Info("✅ Map 服务器已订阅 Gate 的 GuildBattleResultEvent");
    }
}
```

**Chat 服务器启动代码：**

```csharp
public class OnCreateChatSceneHandler : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        // 订阅 Gate 服务器的公会战报事件
        var gateConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
        await scene.SphereEventComponent.Subscribe<GuildBattleResultEvent>(gateConfig.Address);

        Log.Info("✅ Chat 服务器已订阅 Gate 的 GuildBattleResultEvent");
    }
}
```

#### 4. Gate 服务器发布战报

```csharp
public class GuildBattleService
{
    public async FTask OnBattleEnd(Scene scene, long guildId, string guildName, bool isWin, int score)
    {
        // 创建战报事件
        var battleResultEvent = SphereEventArgs.Create<GuildBattleResultEvent>(isFromPool: true);
        battleResultEvent.GuildId = guildId;
        battleResultEvent.GuildName = guildName;
        battleResultEvent.IsWin = isWin;
        battleResultEvent.Score = score;

        // 发布到所有订阅者（Map、Chat）
        await scene.SphereEventComponent.PublishToRemoteSubscribers(battleResultEvent, isAutoDisposed: true);

        Log.Info($"✅ 已发布公会战报: {guildName}");
    }
}
```

---

## 常见问题

### Q1: SphereEvent 和 Roaming 有什么区别？

| 特性 | SphereEvent | Roaming |
|------|-------------|---------|
| **通信模式** | 发布-订阅（一对多） | 点对点路由（一对一） |
| **主动方** | 发布方主动推送 | 客户端发起请求 |
| **订阅关系** | 服务器间动态订阅 | 客户端登录时建立路由 |
| **适用场景** | 服务器间事件通知 | 客户端经 Gate 访问后端 |
| **解耦程度** | 高（发布方不知道订阅方） | 中（需要建立路由关系） |

**选择建议：**

- ✅ 使用 **SphereEvent**：服务器间松耦合通知（如战报、排行榜更新、公告推送）
- ✅ 使用 **Roaming**：客户端访问后端服务（如进入地图、发送聊天）

---

### Q2: SphereEvent 和本地 Event 有什么区别？

| 特性 | Event（本地） | SphereEvent（跨服） |
|------|--------------|-------------------|
| **作用范围** | Scene 内部 | 跨 Scene、跨服务器 |
| **性能** | 高（进程内调用） | 中（网络传输） |
| **序列化** | 不需要 | 需要（网络传输） |
| **订阅方式** | 编译时自动注册 | 运行时动态订阅 |
| **使用场景** | 本地模块解耦 | 分布式通信 |

**选择建议：**

- ✅ 使用 **Event**：单服务器内的模块通信
- ✅ 使用 **SphereEvent**：跨服务器的事件通知

---

### Q3: 如何避免事件重复订阅？

框架内部使用 `_localSubscribers` 存储订阅关系，自动避免重复订阅：

```csharp
// 多次订阅同一事件，只有第一次生效
await scene.SphereEventComponent.Subscribe<TestEvent>(remoteAddress);
await scene.SphereEventComponent.Subscribe<TestEvent>(remoteAddress); // 自动忽略
```

---

### Q4: 事件对象池何时释放？

**自动释放（推荐）：**

```csharp
// isAutoDisposed = true，发布完成后自动释放
await scene.SphereEventComponent.PublishToRemoteSubscribers(event1, isAutoDisposed: true);
```

**手动释放：**

```csharp
// isAutoDisposed = false，需要手动释放
var event1 = SphereEventArgs.Create<TestEvent>(isFromPool: true);
await scene.SphereEventComponent.PublishToRemoteSubscribers(event1, isAutoDisposed: false);

// 手动释放
event1.Dispose();
```

---

### Q5: 订阅者如何知道远程服务器的 Address？

通过配置系统获取：

```csharp
// 方式 1: 通过 SceneType 获取
var gateConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
var gateAddress = gateConfig.Address;

// 方式 2: 通过 SceneId 获取
var sceneConfig = SceneConfigData.Instance.Get(sceneId);
var address = sceneConfig.Address;

// 方式 3: 通过消息传递
// 远程服务器在消息中携带自己的 Address
```

---

### Q6: 如何处理订阅者断线？

框架会自动处理断线情况：

1. **订阅方断线**：框架会记录订阅关系，重连后需要重新订阅
2. **发布方断线**：发布失败会返回错误码，可在业务逻辑中处理
3. **清理订阅**：使用 `Close()` 方法在 Scene 销毁前清理订阅关系

**最佳实践：**

```csharp
// Scene 销毁时清理订阅
public class OnDestroySceneHandler : EventSystem<OnDestroyScene>
{
    protected override void Handler(OnDestroyScene self)
    {
        // 清理所有 SphereEvent 订阅
        self.Scene.SphereEventComponent.Close().Coroutine();
    }
}
```

---

### Q7: 可以订阅多个服务器的同一事件吗？

可以！一个服务器可以订阅多个远程服务器的同一事件：

```csharp
// Rank 服务器订阅多个 Map 服务器的事件
var map1Address = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0].Address;
var map2Address = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[1].Address;

await scene.SphereEventComponent.Subscribe<PlayerLevelChangedEvent>(map1Address);
await scene.SphereEventComponent.Subscribe<PlayerLevelChangedEvent>(map2Address);
```

**订阅关系：**

```
Rank 服务器
    ├─→ 订阅 Map1 的 PlayerLevelChangedEvent
    └─→ 订阅 Map2 的 PlayerLevelChangedEvent
```

---

### Q8: SphereEvent 是否支持热更新？

支持！SphereEventComponent 实现了 `IAssemblyLifecycle` 接口，支持程序集热重载：

```csharp
public sealed class SphereEventComponent : Entity, IAssemblyLifecycle
{
    public async FTask OnLoad(AssemblyManifest assemblyManifest)
    {
        // 加载新程序集中的事件处理器
    }

    public async FTask OnUnload(AssemblyManifest assemblyManifest)
    {
        // 卸载旧程序集中的事件处理器
    }
}
```

**热更新流程：**

1. 修改 `SphereEventSystem` 处理器代码
2. 重新编译 Hotfix 程序集
3. 框架自动卸载旧处理器并加载新处理器
4. 订阅关系保持不变

---

## 线程安全与并发

### 协程锁保护

SphereEventComponent 使用 `CoroutineLock` 保证订阅和发布的线程安全：

```csharp
// 本地订阅锁
private CoroutineLock _localSphereEventLock;

// 远程订阅锁
private CoroutineLock _remoteSphereEventLock;
```

**锁的使用场景：**

```csharp
// 订阅时加锁
using (await _localSphereEventLock.Wait(typeHashCode))
{
    // 订阅操作（线程安全）
}

// 发布时加锁
using (await _remoteSphereEventLock.Wait(typeHashCode))
{
    // 发布操作（线程安全）
}
```

### 并发处理

发布事件时，框架会**并发调用所有订阅者**：

```csharp
// 并发调用所有事件处理器
using var tasks = ListPool<FTask>.Create();

foreach (var @event in sphereEvents)
{
    tasks.Add(@event(Scene, eventArgs));
}

// 等待所有处理器执行完成
await FTask.WaitAll(tasks);
```

**注意事项：**

- 多个 SphereEventSystem 会并发执行，确保处理器内部的线程安全
- 如需顺序执行，可在处理器内部使用 `CoroutineLock`

---

## 性能优化建议

### 1. 使用对象池

```csharp
// ✅ 推荐：使用对象池
var event1 = SphereEventArgs.Create<TestEvent>(isFromPool: true);
await scene.SphereEventComponent.PublishToRemoteSubscribers(event1, isAutoDisposed: true);

// ❌ 不推荐：频繁创建新对象
var event2 = SphereEventArgs.Create<TestEvent>(isFromPool: false);
```

### 2. 自动释放事件对象

```csharp
// ✅ 推荐：自动释放
await scene.SphereEventComponent.PublishToRemoteSubscribers(event1, isAutoDisposed: true);

// ❌ 不推荐：手动管理（容易忘记释放）
await scene.SphereEventComponent.PublishToRemoteSubscribers(event1, isAutoDisposed: false);
event1.Dispose(); // 容易遗漏
```

### 3. 避免过大的事件对象

```csharp
// ❌ 不推荐：事件对象包含大量数据
public sealed class BadEvent : SphereEventArgs
{
    public byte[] LargeData { get; set; } // 网络传输开销大
    public List<int> LargeList { get; set; } // 序列化开销大
}

// ✅ 推荐：事件对象只包含必要数据
public sealed class GoodEvent : SphereEventArgs
{
    public long EntityId { get; set; }      // 通过 ID 查询详细数据
    public int EventType { get; set; }
}
```

### 4. 合理使用订阅

```csharp
// ❌ 不推荐：订阅不需要的事件
await scene.SphereEventComponent.Subscribe<UnusedEvent>(remoteAddress);

// ✅ 推荐：只订阅需要的事件
await scene.SphereEventComponent.Subscribe<PlayerLevelChangedEvent>(remoteAddress);
```

---

## 相关文档

- [08-Roaming.md](08-Roaming.md) - Roaming 漫游消息 - 分布式实体路由
- [06-Address消息.md](06-Address消息.md) - Address 消息 - 服务器间实体通信
- [04-Event.md](../../04-Advanced/CoreSystems/04-Event.md) - Event 系统使用指南（本地事件）
- [01-Session.md](../../03-Networking/01-Session.md) - Session 使用指南
- [02-MessageHandler.md](../../03-Networking/02-MessageHandler.md) - 消息处理器实现指南

---

## 总结

SphereEvent 跨服域事件系统的核心优势：

1. **松耦合架构**：发布方无需知道订阅方的存在，订阅方动态订阅
2. **一对多通知**：一次发布，所有订阅者自动接收
3. **自动序列化**：事件对象自动序列化通过网络传输
4. **对象池优化**：减少 GC 压力，提升性能
5. **线程安全**：使用 CoroutineLock 保证并发安全
6. **支持热更新**：事件处理器可热重载，订阅关系保持不变

**使用步骤回顾：**

1. 定义事件类（继承 `SphereEventArgs`）
2. 实现事件处理器（继承 `SphereEventSystem<T>`）
3. 订阅远程事件（`Subscribe<T>(remoteAddress)`）
4. 发布事件（`PublishToRemoteSubscribers(event, isAutoDisposed)`）
5. 自动接收并处理（`Handler(scene, args)`）

**适用场景：**

- ✅ 跨服务器的业务事件通知
- ✅ 分布式系统的状态同步
- ✅ 服务器间的松耦合通信
