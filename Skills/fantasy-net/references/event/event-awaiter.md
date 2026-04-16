# EventAwaiter 入口

**本文件只做分流。** 需要实现代码、设计挂载位置、或排查 bug 时，继续读对应子文档。

## 快速判断

- 等一个结果回来 -> `EventAwaiter`
- 广播给多个监听器 -> `Event`

## Workflow

```text
实现 EventAwaiter 代码 -> event-awaiter-implement.md
设计挂载和业务模型 -> event-awaiter-design.md
排查现有 EventAwaiter 代码 -> event-awaiter-troubleshoot.md
```

## 必记规则

1. `Wait<T>()` / `Notify<T>()` 的 `T` 必须是 `struct`
2. 等待和通知必须发生在同一个 `EventAwaiterComponent` 实例上
3. 默认挂到最贴近业务边界的实体，不要默认挂到 `Scene`
4. 玩家交互、网络响应、跨服响应通常应设置 timeout
5. `Wait<T>()` 返回的是 `EventAwaiterResult<T>`，必须检查结果状态

## 子文档

- `event-awaiter-implement.md` - 实现步骤和最小模板
- `event-awaiter-design.md` - 挂载位置和业务建模规则
- `event-awaiter-troubleshoot.md` - 检查清单和常见错误
