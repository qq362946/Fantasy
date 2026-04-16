# EventAwaiter 实现步骤

**适用场景：** 已经确定要用 `EventAwaiter`，现在需要直接落代码。

## 第 1 步：给业务实体添加 `EventAwaiterComponent`

默认把 `EventAwaiterComponent` 挂到最贴近业务边界的实体上，不要默认挂到 `Scene`。

```csharp
public sealed class Player : Entity
{
    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

public sealed class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}
```

## 第 2 步：定义 `struct` 事件结果类型

事件类型 `T` 必须是 `struct`。

```csharp
public struct PlayerConfirmEvent
{
    public long PlayerId;
    public bool Confirmed;
}
```

## 第 3 步：在业务流程里调用 `Wait<T>()`

```csharp
public async FTask<EventAwaiterResult<T>> Wait<T>(FCancellationToken? cancellationToken = null)
    where T : struct;

public async FTask<EventAwaiterResult<T>> Wait<T>(int timeout, FCancellationToken? cancellationToken = null)
    where T : struct;
```

优先使用带超时版本，尤其是玩家交互、网络响应、跨服响应、资源加载。

```csharp
var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(30000);
```

## 第 4 步：在触发点调用 `Notify<T>()`

```csharp
public void Notify<T>(T obj) where T : struct;
```

```csharp
player.EventAwaiterComponent.Notify(new PlayerConfirmEvent
{
    PlayerId = player.Id,
    Confirmed = confirmed
});
```

## 第 5 步：处理结果状态

不要把 `Wait<T>()` 当成必定成功返回值的 API。必须检查结果状态。

```csharp
switch (result.ResultType)
{
    case EventAwaiterResultType.Success:
        ProcessConfirm(result.Value);
        break;
    case EventAwaiterResultType.Timeout:
        HandleTimeout();
        break;
    case EventAwaiterResultType.Cancel:
        HandleCancel();
        break;
    case EventAwaiterResultType.Destroy:
        HandleDestroy();
        break;
}
```

## 最小模板

```csharp
public struct XxxEvent
{
    public long Id;
    public bool Success;
}

var result = await entity.EventAwaiterComponent.Wait<XxxEvent>(30000);

if (result.ResultType == EventAwaiterResultType.Success)
{
    Handle(result.Value);
}

entity.EventAwaiterComponent.Notify(new XxxEvent
{
    Id = entity.Id,
    Success = true
});
```
