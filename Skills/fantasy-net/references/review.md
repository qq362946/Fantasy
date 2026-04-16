# Fantasy 代码审查入口

**本文件用于检查已有的 Fantasy 代码。** 如果用户是在问“怎么实现”，优先走各主题实现文档；如果用户是在问“这段代码有没有问题、符不符合规范、有没有隐患”，先读本文件。

## Workflow

```text
先识别代码属于哪类主题
  ECS / Scene / SubScene -> ecs/index.md 和 ecs-check.md
  Event / EventAwaiter -> event/index.md 和 check-event.md
  Timer -> timer/index.md 和 best-practices.md
  Protocol / Handler -> protocol/index.md 和 protocol-check.md
  Address -> server/address.md 和 address-check.md
  SphereEvent -> server/sphere-event/index.md 和 best-practices.md
  Roaming -> server/roaming/index.md 和 roaming-check.md
  HTTP -> http.md 和 http-check.md
  Database -> database/index.md 和 database-check.md
  Fantasy.config -> config.md 和 config-check.md
  Unity -> unity/index.md 和 unity-check.md
  客户端消息 Handler -> server-message-handler.md 和 server-message-handler-check.md
  日志 -> logging.md 和 logging-check.md
  服务器项目接入 -> server/setup-server.md 和 setup-server-check.md
```

## 通用检查顺序

无论代码属于哪个主题，先按下面顺序过一遍：

1. 是否使用 `FTask`，而不是随手混用 `Task`
2. 类是否应为 `sealed class`
3. 是否误改或依赖手写注册，而本该由 Source Generator 自动注册
4. 是否把业务逻辑错误处理写成了异常抛出，而不是框架约定的返回模式
5. 是否把 Entity、Component、Handler、System 混在不合适的层里
6. 是否存在 Scene 生命周期、Dispose、Cancel、Close、RemoveTimer 之类清理遗漏
7. 是否误用了相似机制，例如 Event / EventAwaiter / SphereEvent / Roaming / Address 边界混乱

## 审查时的输出顺序

优先按下面顺序输出：

1. 严重问题
2. 规范问题
3. 潜在风险
4. 可选优化
5. 相关依据文档

如果没有明显问题，也要明确说明“未发现明显问题”，并指出仍未验证的风险点。

## 主题专项检查

### ECS / Scene

重点检查：

- Entity / Component 是否 `sealed class`
- 是否归属于正确的 `Scene`
- 是否该配套 `AwakeSystem` / `DestroySystem` / `UpdateSystem`
- 使用对象池时是否在 `DestroySystem` 里重置自定义字段
- 是否把 SubScene、普通 Scene、Component 职责混用

读：

- `ecs/index.md`
- `ecs/ecs-check.md`
- `ecs/entity-definition.md`
- `ecs/entity-operations.md`
- `ecs/object-pool.md`
- `ecs/lifecycle.md`

### Event / EventAwaiter

重点检查：

- 该场景到底该用 Event 还是 EventAwaiter
- Struct Event 是否真的用了 `struct`
- 监听器是否 `sealed`
- `PublishAsync` 是否忘记 `await`
- `EventAwaiter` 是否在同一个组件实例上等待和通知

读：

- `event/index.md`
- `event/check-event.md`
- `event/event-awaiter.md`

### Timer

重点检查：

- 该场景该用 `FTask.Wait` 还是 `OnceTimer` / `RepeatedTimer`
- 是否忘记保存和取消重复定时器
- 回调里是否访问已销毁对象
- 热重载逻辑是否误用了 Action 方式

读：

- `timer/index.md`
- `timer/best-practices.md`
- 需要时再看 `timer/event.md`

### Protocol / Message Handler

重点检查：

- 协议放在 Outer 还是 Inner
- 接口类型是否选对：`IMessage` / `IRequest` / `IResponse` / `IAddressRequest` 等
- Handler 基类是否匹配协议类型
- 协议改完后是否考虑导出
- Request / Response 注释与协议命名是否一致

读：

- `protocol/index.md`
- `protocol/protocol-check.md`
- `server/server-message-handler.md`
- `server/address.md`

### Roaming / SphereEvent / Address

重点检查：

- 机制边界是否选对
- Roaming 是否适合客户端经 Gate 访问后端
- SphereEvent 是否真的属于跨服发布订阅
- Address 是否明确知道目标地址并是一对一通信
- 路由建立、订阅建立、Address 缓存是否在正确生命周期里
- 订阅方/发布方取消订阅的语义是否混用
- 是否把异常状态吞掉并仍按成功路径返回

读：

- `server/roaming/index.md`
- `server/roaming/roaming-check.md`
- `server/sphere-event/index.md`
- `server/address.md`
- `server/address-check.md`

### HTTP

重点检查：

- 服务配置是否放在 `OnConfigureHttpServices` / `OnConfigureHttpApplication`
- 是否错误重复 `MapControllers()`
- 访问 Scene 运行时对象时是否用了 `SceneContextFilter`
- 是否误把 HTTP 返回写成消息 Handler 风格
- 中间件顺序是否正确

读：

- `http.md`
- `http-check.md`
- `http-server.md`
- `http-controller.md`

### Database / Config

重点检查：

- `<database>` 是否正确挂在 `<world>` 下
- 运行时是否通过 `scene.World` 访问数据库
- Entity 是否实现 `ISupportedSerialize`
- 是否误用 `isDeserialize`
- 并发修改是否缺少协程锁
- 是否其实已经需要 SeparateTable
- machine / process / world / scene / database 引用关系是否正确
- 是否存在“先查后建”却没加锁的路径
- 首次保存前关键字段是否已经填完整
- 是否存在悬空 world / database 配置

读：

- `database/index.md`
- `database/database-check.md`
- `database/best-practices.md`
- `config.md`
- `config-check.md`
- `config-scenarios.md`

### Unity

重点检查：

- `Fantasy.Unity` 版本是否与服务器端一致
- 编译宏是否完整
- 连接方式是否统一
- Session 生命周期是否清晰
- 主动推送 Handler 是否放在正确位置

读：

- `unity/index.md`
- `unity/unity-check.md`
- `unity/unity-connection.md`
- `unity/unity-session.md`
- `unity/unity-message-handler.md`

### 服务器消息 Handler

重点检查：

- `Message<T>` / `MessageRPC<TReq, TRes>` 基类是否匹配
- 是否正确使用 `response.ErrorCode`
- `reply()` 后是否还误修改 `response`
- 耗时逻辑前是否检查 `session.IsDisposed`

读：

- `server/server-message-handler.md`
- `server/server-message-handler-check.md`

### Logging / Setup

重点检查：

- 日志初始化方式是否完整
- NLog 配置和复制是否生效
- 服务器项目接入 Fantasy 的 NuGet、宏、AssemblyHelper、Program 入口是否完整

读：

- `logging.md`
- `logging-check.md`
- `server/setup-server.md`
- `server/setup-server-check.md`

## 一句话标准

如果一段代码“能跑”，但违反了 Fantasy 的线程模型、生命周期、生成注册机制或机制边界，这仍然算问题，审查时要明确指出。
