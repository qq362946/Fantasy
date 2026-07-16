# SphereEvent 实现

## 第 1 步：定义事件类

事件类必须：

- 继承 `SphereEventArgs`
- 添加 `[MemoryPackable]`
- 使用 `partial`
- 推荐使用 `sealed`

```csharp
using Fantasy.Sphere;
using MemoryPack;

[MemoryPackable]
public sealed partial class PlayerLevelChangedEvent : SphereEventArgs
{
    public long PlayerId { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
}
```

## 第 2 步：实现处理器

处理器继承 `SphereEventSystem<T>`：

```csharp
public sealed class OnPlayerLevelChanged : SphereEventSystem<PlayerLevelChangedEvent>
{
    protected override async FTask Handler(Scene scene, PlayerLevelChangedEvent args)
    {
        Log.Info($"收到玩家等级变化事件: {args.PlayerId}, {args.OldLevel} -> {args.NewLevel}");
        await FTask.CompletedTask;
    }
}
```

Source Generator 会自动注册 `SphereEventSystem`，不需要手动注册。

## 第 3 步：订阅远程事件

通过远程 Scene 的 Address 建立订阅：

```csharp
// 文件顶部：
using NetServiceDiscovery = Fantasy.ServiceDiscovery;
```

```csharp
var gateAddress =
    await NetServiceDiscovery.DiscoverAddressAsync(
        SceneType.Gate,
        worldId: mapScene.SceneConfig.WorldConfigId);

if (gateAddress == 0)
{
    return;
}

await mapScene.SphereEventComponent
    .Subscribe<PlayerLevelChangedEvent>(gateAddress);
```

未启用 Control Center 时，也可以继续从 `SceneConfigData` 取得静态 `SceneConfig.Address`。启用服务发现后必须使用 Address 查询 API，确保动态网络端点已登记。

如果要按类型码订阅，也可以：

```csharp
await scene.SphereEventComponent.Subscribe(remoteAddress, typeHashCode);
```

## 第 4 步：发布事件

事件对象优先从对象池创建：

```csharp
var levelChangedEvent = SphereEventArgs.Create<PlayerLevelChangedEvent>(isFromPool: true);
levelChangedEvent.PlayerId = playerId;
levelChangedEvent.OldLevel = oldLevel;
levelChangedEvent.NewLevel = newLevel;

await scene.SphereEventComponent.PublishToRemoteSubscribers(levelChangedEvent, isAutoDisposed: true);
```

## 第 5 步：取消订阅 / 清理

### 主动取消自己的订阅

```csharp
await scene.SphereEventComponent.Unsubscribe<PlayerLevelChangedEvent>(remoteAddress);
```

### 发布方撤销某个远程订阅者

```csharp
await scene.SphereEventComponent.RevokeRemoteSubscriber<PlayerLevelChangedEvent>(subscriberAddress);
```

### Scene 销毁前清理全部订阅关系

```csharp
await scene.SphereEventComponent.Close();
```

## 常见使用模式

### 在服务发现初始化后订阅

未启用 Control Center 时，可以在 `OnCreateScene` 使用静态 SceneConfig 建立订阅。Control Center 模式下，服务发现是在所有 Process / Scene 创建完成后才初始化，不能在 `OnCreateScene` 中同步查询或等待。

把订阅放进项目明确的“服务启动完成”阶段，并复用一个小方法：

```csharp
private static async FTask SubscribeGateAsync(Scene scene)
{
    var gateAddress =
        await NetServiceDiscovery.DiscoverAddressAsync(
            SceneType.Gate,
            worldId: scene.SceneConfig.WorldConfigId);

    if (gateAddress == 0)
    {
        // 记录日志，并由启动编排按项目策略进行有限重试。
        return;
    }

    await scene.SphereEventComponent
        .Subscribe<GuildBattleResultEvent>(gateAddress);
}
```

不要做无间隔死循环。目标 Scene 可能比当前服务晚启动，查询为 `0` 时应记录状态，并在有界重试或服务就绪通知后重新订阅。

### 发布方只负责发布，不感知订阅方

```csharp
var eventArgs = SphereEventArgs.Create<GuildBattleResultEvent>(isFromPool: true);
eventArgs.GuildId = guildId;
eventArgs.GuildName = guildName;
eventArgs.IsWin = isWin;
eventArgs.Score = score;

await scene.SphereEventComponent.PublishToRemoteSubscribers(eventArgs, isAutoDisposed: true);
```

## 相关文档

- `best-practices.md` - 与 Event / Roaming / Address 的边界、热重载、排错
- `references/service-discovery/index.md` - 动态 Scene 入口与查询范围
- `references/server/roaming/index.md` - 客户端经 Gate 访问后端服务
- `references/event/index.md` - 本地 Event 机制
