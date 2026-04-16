# Timer 与 Event 集成

Timer 可以直接执行 Action，也可以在定时器触发时发布 Event。

## 什么时候优先用事件方式

以下场景优先用“Timer 触发 Event”：

- 业务逻辑需要热重载
- 希望定时逻辑和处理逻辑解耦
- 未来可能会有多个监听器响应同一个定时事件

以下场景可以直接用 Action：

- 框架级、工具级、一次性简单逻辑
- 不依赖热重载

## 为什么事件方式更适合热重载

Action 定时器会捕获旧闭包；热重载后，旧定时器仍可能执行旧代码。

事件方式保存的是事件数据，触发时通过 `EventComponent` 找当前已注册的监听器，因此能跟随热重载后的新逻辑。

## Action 方式

```csharp
FTask.OnceTimer(scene, 5000, () =>
{
    RefreshShop();
});
```

## 事件方式

```csharp
public struct RefreshShopEvent { }

public sealed class OnRefreshShop : EventSystem<RefreshShopEvent>
{
    protected override void Handler(RefreshShopEvent self)
    {
        RefreshShop();
    }
}

FTask.OnceTimer(scene, 5000, new RefreshShopEvent());
```

## 事件定时器常用接口

```csharp
scene.TimerComponent.Net.OnceTimer(5000, new BattleStartEvent());
scene.TimerComponent.Net.OnceTillTimer(targetTime, new RefreshShopEvent());
scene.TimerComponent.Net.RepeatedTimer(60000, new ServerHeartbeatEvent());
```

## 选择建议

- 开发阶段要频繁热重载 -> 优先事件方式
- 生产环境简单逻辑 -> Action 和事件都可以，根据解耦需求决定
