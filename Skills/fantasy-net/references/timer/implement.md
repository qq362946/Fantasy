# Timer 实现

## 获取 Timer

`TimerComponent` 是 Scene 的核心组件，Scene 创建时自动初始化。

服务器端常用：

```csharp
var timerNet = scene.TimerComponent.Net;
```

推荐优先使用 `FTask` 简化封装：

```csharp
await FTask.Wait(scene, 1000);
```

## 异步等待

### 等待一段时间

```csharp
bool completed = await FTask.Wait(scene, 1000, cts);
```

返回值：

- `true` - 正常完成
- `false` - 被取消

### 等到指定时间

```csharp
await FTask.WaitTill(scene, targetTime);
```

如果 `targetTime` 小于当前时间，会立即返回。

### 等一帧

```csharp
await FTask.WaitFrame(scene);
```

## 一次性定时器

### 延迟执行一次

```csharp
long timerId = FTask.OnceTimer(scene, 5000, () =>
{
    StartBattle();
});
```

### 到指定时间执行一次

```csharp
long timerId = FTask.OnceTillTimer(scene, targetTime, RefreshShop);
```

## 重复定时器

```csharp
long timerId = FTask.RepeatedTimer(scene, 1000, UpdateAI);
```

重复定时器会持续执行，直到主动取消。

## 取消定时器

推荐使用带 `ref` 的方式，取消后自动把 timerId 置为 0。

```csharp
private long _timerId;

_timerId = FTask.OnceTimer(scene, 5000, Callback);
FTask.RemoveTimer(scene, ref _timerId);
```

## Unity 客户端

Unity 侧有独立的 Unity Timer 封装。只有在明确是 Unity 客户端定时逻辑时，才使用对应 Unity 方法：

```csharp
long timerId = FTask.UnityOnceTimer(scene, 5000, Callback);
```

## 何时用 Wait，何时用回调

- 当前就在异步流程里，要等待后续继续执行 -> 用 `FTask.Wait`
- 需要独立安排一个未来回调 -> 用 `OnceTimer` / `RepeatedTimer`
- 需要热重载友好 -> 看 `event.md`
