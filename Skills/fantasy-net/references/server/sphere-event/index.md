# SphereEvent 入口

SphereEvent 是 Fantasy 的跨服域事件系统，用于在不同 Scene、不同服务器之间做发布-订阅式解耦通信。

一句话理解：

- 订阅方订阅远程服务器的事件
- 发布方发布事件
- 所有订阅方自动收到并处理

## 什么时候用

优先在这些场景使用 SphereEvent：

- 跨服务器业务事件通知
- 一个发布方，需要通知多个订阅方
- 想避免硬编码点对点 RPC 调用
- 排行榜更新、跨服公告、公会战报、状态同步等松耦合事件

不要在这些场景优先用 SphereEvent：

- 单 Scene 内部模块通信 -> 用本地 `Event`
- 客户端经 Gate 定向访问后端服务 -> 用 `Roaming`
- 明确知道目标实体地址的一对一点对点通信 -> 用 `Address`

## Workflow

```text
直接实现 SphereEvent 事件、处理器、订阅和发布 -> implement.md
对 SphereEvent / Event / Roaming / Address 的边界拿不准，或需要排错和优化 -> best-practices.md
```

## 必记规则

1. SphereEvent 事件参数必须继承 `SphereEventArgs`
2. 2025.2.1410+ 版本里，事件类必须加 `[MemoryPackable]` 且使用 `partial`
3. 事件对象优先通过 `SphereEventArgs.Create<T>(isFromPool: true)` 创建，不要直接 `new`
4. 订阅是运行时动态建立的，不是像本地 Event 一样只靠编译期自动注册
5. 发布时优先使用 `isAutoDisposed: true`，减少对象池管理出错

## 子文档

- `implement.md` - 事件类、处理器、订阅、发布、取消订阅
- `best-practices.md` - 热重载、断线清理、性能建议、机制选择
