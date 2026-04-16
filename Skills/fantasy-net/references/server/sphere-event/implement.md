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
var gateConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
await mapScene.SphereEventComponent.Subscribe<PlayerLevelChangedEvent>(gateConfig.Address);
```

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

### 服务器启动时订阅

适合在 `OnCreateScene` 或初始化路径建立订阅：

```csharp
public sealed class OnCreateMapSceneHandler : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var gateConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
        await self.Scene.SphereEventComponent.Subscribe<GuildBattleResultEvent>(gateConfig.Address);
    }
}
```

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
- `references/server/roaming/index.md` - 客户端经 Gate 访问后端服务
- `references/event/index.md` - 本地 Event 机制
