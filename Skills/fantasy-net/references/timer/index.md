# Timer 入口

Fantasy 的 `TimerComponent` 是 Scene 的核心调度组件，用于延时执行、周期任务、异步等待和倒计时。

**推荐优先使用 `FTask` 简化方法**，只有在需要底层接口或特殊重载时再直接用 `scene.TimerComponent.Net`。

## Workflow

```text
异步等待一段时间 / 等到某个时间 / 等一帧 -> implement.md
延迟执行一次回调 / 周期执行回调 / 取消定时器 -> implement.md
需要热重载友好的定时逻辑 -> event.md
需要优化、排错、理解精度和清理行为 -> best-practices.md
```

## 必记规则

1. 服务器端优先通过 `FTask.Wait` / `FTask.OnceTimer` / `FTask.RepeatedTimer` 使用 Timer
2. Timer 归属于 `Scene`，`Scene` 销毁后其下定时器会自动清理
3. 重复定时器需要主动取消，不要忘记保存 timerId
4. 需要热重载的业务逻辑，优先用“Timer 触发 Event”而不是 Action 回调
5. 定时器精度取决于 `Update()` 频率，不是绝对实时

## 子文档

- `implement.md` - 异步等待、回调定时器、取消方法
- `event.md` - 事件方式定时器与热重载
- `best-practices.md` - 性能建议、常见问题、排错清单
