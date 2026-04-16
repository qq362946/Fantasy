# Timer 最佳实践与排错

## 最佳实践

1. 需要等待时优先用 `FTask.Wait`，不要为了等待再包一层 `OnceTimer`
2. 保存 timerId，尤其是重复定时器
3. 取消定时器时优先用 `FTask.RemoveTimer(scene, ref timerId)`
4. 需要热重载的业务逻辑优先用事件方式
5. 大量对象的周期更新，优先用一个共享定时器统一处理，不要给每个对象单独起短周期定时器

## 常见错误

### 错误 1：忘记取消重复定时器

重复定时器会一直执行，直到主动取消。

### 错误 2：定时器回调访问已销毁对象

在回调里访问 Entity 前，先确认对象还有效。

```csharp
if (!player.IsDisposed)
{
    player.Health += 100;
}
```

### 错误 3：创建大量短周期定时器

不要为每个对象各起一个 10ms / 100ms 定时器。优先用一个共享定时器批量处理。

### 错误 4：`OnceTillTimer` 传过去时间

如果目标时间早于当前时间，会立刻执行并记录错误日志。

## 常见问题

### 定时器回调抛异常会怎样

异常会被捕获并记录日志，不会影响其他定时器继续执行。

### `WaitAsync` 和 `Task.Delay` 的区别

优先用 Fantasy 的 Timer：

- 与 `Scene` 生命周期集成
- 使用 `FTask`
- 支持 `FCancellationToken`
- 性能和复用策略更适合框架内部

### 定时器精度是多少

Timer 精度取决于 `Update()` 频率。

- 服务器端：取决于 TimerComponentUpdateSystem 执行频率
- Unity 客户端：取决于帧率

因此会有一帧或一个 Update 间隔级别的误差。

### 重复定时器会不会累积误差

不会按简单固定累加方式漂移；框架会重新计算下一次触发时间。

### Scene 销毁后定时器会怎样

会自动清理。因为 `TimerComponent` 本身归属于 `Scene`。

## 排查顺序

1. 当前逻辑到底该用 `Wait` 还是 `OnceTimer` / `RepeatedTimer`
2. 是否忘记保存 timerId
3. 是否忘记取消重复定时器
4. 回调里访问的对象是否已经销毁
5. 是否误把热重载逻辑写成了 Action 回调
6. 是否对精度做了“绝对实时”假设
