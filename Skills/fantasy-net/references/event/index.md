# 事件相关入口

本文件用于先判断应该使用 `EventAwaiter` 还是 `Event`。

- `EventAwaiter`：等待-通知，适合“先发起动作，再等待结果”
- `Event`：发布-订阅，适合模块解耦和广播事件

跨服事件见对应跨服事件文档。

`Event` 监听器应与 Entity 数据定义分离：多 assembly 项目放在逻辑层 assembly，单 assembly 项目按文件夹分离即可。框架通过 Source Generator 编译时自动注册，无需手动注册，不要修改 `.g.cs`。

---

## Workflow

```
确认要做什么？
│
├─► 需要等待某个条件成立、等待玩家响应、等待异步结果
│   └─► 读 event-awaiter.md
│       ├─► 实现代码 ───────────────► 读 event-awaiter-implement.md
│       ├─► 设计挂载和业务模型 ─────► 读 event-awaiter-design.md
│       └─► 排查现有代码 ───────────► 读 event-awaiter-troubleshoot.md
│
├─► 创建新的发布-订阅事件（模块解耦、广播通知）
│   │
│   ├─► 创建新的 Struct 事件（推荐，适合大多数场景）
│   └─► 读 struct-event.md
│       ├─► 第 1 步：定义 Struct 事件
│       ├─► 第 2 步：创建监听器（同步/异步）
│       └─► 第 3 步：发布事件
│
│   ├─► 创建新的 Entity 事件（仅当需要传递现有 Entity 时）
│   │   └─► 读 entity-event.md
│   │       ├─► 第 1 步：创建监听器（同步/异步）
│   │       └─► 第 2 步：发布现有 Entity（注意 isDisposed 参数）
│
│   └─► 检查已有的 Event 代码 ─────────────► 读 check-event.md
│
└─► 不确定该用哪套机制
    └─► 先看下面的快速选择指南
```

---

## 快速选择指南

| 场景 | 推荐类型 | 原因 |
|---|---|---|
| 等待玩家确认、交易响应、资源加载完成、跨流程结果返回 | EventAwaiter | 本质是等待一个结果回来，适合 `Wait<T>()` + `Notify<T>()` |
| 大多数模块解耦场景（伤害、移动、登录、任务等） | Struct Event | 零 GC，简单直接，适合发布-订阅 |
| 需要在发布-订阅里传递现有 Entity（玩家、怪物、物品等） | Entity Event | 直接传递 Entity 引用 |

**重要：**

- 不要为了使用 Entity Event 而专门创建新的 Entity，直接使用 Struct Event 即可。
- 如果你的真实需求是“等待结果”，不要强行用 Event 监听器回调链模拟，优先考虑 `EventAwaiter`。

---

## 相关文档

- `event-awaiter.md` - EventAwaiter 入口和分流（实现 / 建模 / 排错）
- `event-awaiter-implement.md` - EventAwaiter 实现步骤和最小模板
- `event-awaiter-design.md` - EventAwaiter 挂载位置和业务建模规则
- `event-awaiter-troubleshoot.md` - EventAwaiter 检查清单和常见错误
- `struct-event.md` - Struct 事件完整流程（定义 → 监听器 → 发布）
- `entity-event.md` - Entity 事件完整流程（定义 → 监听器 → 发布 → 对象池）
- `check-event.md` - Event 系统代码检查清单
