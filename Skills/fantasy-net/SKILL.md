---
name: fantasy-net
description: 用于 Fantasy / Fantasy.Net / Fantasy.Unity 分布式游戏服务器框架（C#）的开发与代码审查指南。只要用户在游戏服务器、Unity 客户端、ECS、分布式场景、热更新或网络架构上下文中提到 Fantasy，或提到 FTask、Scene、SubScene、Entity、Component、System、OnCreateScene、Timer、Event、EventAwaiter、SphereEvent、跨服域事件、SphereEventComponent、SphereEventArgs、SphereEventSystem、远程订阅、发布订阅、PublishToRemoteSubscribers、Subscribe、网络消息 Handler、proto/Outer/Inner 协议、协议导出、Address、Addressable、Roaming、Terminus、跨服传送、HTTP 服务器、HTTP Controller、OnConfigureHttpServices、OnConfigureHttpApplication、SceneContextFilter、Fantasy.config、machine/process/world/scene/idFactory/network/session/database、MongoDB、scene.World.Database、SeparateTable、日志、服务器项目创建/集成、Unity 连接与 Session 等主题时，都应优先使用此 skill。只要用户要求 review、代码审查、检查代码、看看有没有问题、是否符合 Fantasy 规范、有没有隐患、最佳实践、排查原因、哪里写得不对，也应主动使用此 skill。即使用户没有明确说“Fantasy”，但需求明显是在编写、审查或排查 Fantasy 框架相关实体、消息、跨服事件、配置、数据库、HTTP 服务、客户端连接、服务端架构或运行时系统代码，也要主动触发此 skill。
---

# Fantasy-net

Fantasy 是高性能 C# 分布式游戏服务器框架，基于 ECS 架构，使用 `FTask` 进行异步操作。

## 核心原则

- 所有异步操作用 `FTask`，不用 `Task`
- Entity 数据与逻辑（Handler/System）分开存放；多 assembly 项目必须分离以支持热更新
- 所有注册由源码生成器编译时完成，不要手动注册，不要修改 `.g.cs`
- 实体、组件、Handler 均用 `sealed class`；除结构体外所有 class 必须用 Entity 创建
- 使用文件范围命名空间（`namespace Fantasy;`）
- 日志用 `Log.Debug/Info/Error()`，错误码通过 `response.ErrorCode` 返回，业务逻辑不抛异常
- Event 系统用于模块解耦：发布事件而非直接调用；Struct 事件优先（零 GC），Entity 事件用于复杂逻辑；同步用 EventSystem，异步用 AsyncEventSystem
- 严格遵循 SOLID 原则

## 参考文件导航

根据需求读取对应文件，复杂任务可读多个。

| 文件 | 适用场景 |
|---|---|
| `references/ecs/index.md` | ECS 入口：先分流到 Scene / SubScene / Entity 定义 / 组件操作 / 对象池 / 生命周期；服务器/Unity 通用；**凡涉及 Entity 定义、组件管理或 ECS 机制选择时先读此文件** |
| `references/review.md` | Fantasy 代码审查入口：按 ECS / Event / Timer / Protocol / Roaming / SphereEvent / HTTP / Database / Config 分流检查；**当用户要 review、检查代码、看是否符合 Fantasy 规范时先读此文件** |
| `references/ecs/scene.md` | **Scene 是所有 Entity/Component 的归属容器和生命周期边界**：Scene 销毁时级联销毁其下所有实体、OnCreateScene 事件、通过 `self.Scene` 访问系统组件（TimerComponent/EventComponent/NetworkMessagingComponent 等）；**凡涉及 Scene 概念、Scene 初始化、OnCreateScene 事件、或需要理解 Entity 归属关系时读此文件** |
| `references/ecs/ecs-check.md` | ECS 审查清单：Entity / Component / System / Scene / 对象池 / 生命周期常见问题；**当用户要检查 ECS 代码是否符合 Fantasy 规范时读此文件** |
| `references/timer/index.md` | Timer 入口：分流到异步等待 / 回调定时器 / 事件集成 / 最佳实践；**当用户要做延时执行、重复任务、倒计时、Wait、OnceTimer、RepeatedTimer 时先读此文件** |
| `references/timer/implement.md` | Timer 实现：`FTask.Wait`、`WaitTill`、`WaitFrame`、`OnceTimer`、`RepeatedTimer`、取消定时器；**仅在直接写 Timer 代码时读此文件** |
| `references/timer/event.md` | Timer 与 Event 集成：事件方式定时器、热重载差异、何时用事件替代 Action；**仅在需要热重载友好的 Timer 逻辑时读此文件** |
| `references/timer/best-practices.md` | Timer 最佳实践与排错：性能建议、常见错误、Scene 销毁、精度、取消策略；**仅在优化或排查 Timer 代码时读此文件** |
| `references/ecs/subscene.md` | **SubScene 动态子场景**：在运行时从父 Scene 动态创建的轻量级隔离空间，共享父 Scene 核心组件但有独立实体列表；`Scene.CreateSubScene()` API 详解（参数、回调执行顺序）、向 SubScene 发送 Address 消息、SubScene 上使用 Addressable、销毁 SubScene；**凡需要创建副本、匹配房间、分线地图、动态战场、玩家私有空间，或需要按需创建和销毁独立场景时读此文件** |
| `references/ecs/lifecycle.md` | ECS 生命周期 System：AwakeSystem、UpdateSystem、DestroySystem、DeserializeSystem、TransferOutSystem/TransferInSystem（跨服传送专用）及触发顺序；**凡需要响应 Entity 生命周期事件时读此文件** |
| `references/event/index.md` | 事件相关入口：先判断该用 EventAwaiter 还是 Event，再按 Workflow 进入对应文档；**当需求里出现“等待结果”或“发布事件”这类事件机制选择问题时先读此文件** |
| `references/event/event-awaiter.md` | EventAwaiter 入口：先分流到实现 / 建模 / 排错；**当用户要等待某个条件成立、等待玩家操作、做请求-响应式异步流程，或明确提到 EventAwaiter/EventAwaiterComponent 时先读此文件** |
| `references/event/struct-event.md` | Struct 事件完整流程：第 1 步定义事件 → 第 2 步创建监听器 → 第 3 步发布事件；**推荐大多数场景使用，需要创建 Struct 事件时读此文件** |
| `references/event/entity-event.md` | Entity 事件完整流程：第 1 步创建监听器 → 第 2 步发布现有 Entity（注意 isDisposed 参数）；**仅当需要传递现有 Entity 时读此文件** |
| `references/event/check-event.md` | Event 系统代码检查：Struct 事件检查清单、Entity 事件检查清单、监听器检查清单、常见错误对比；**需要检查已有事件代码时读此文件** |
| `references/server/setup-server.md` | 新建 Fantasy 服务器项目、现有项目集成 Fantasy（.NET/NuGet）、三层结构搭建、日志系统快速配置 |
| `references/unity/index.md` | Unity 客户端入口：安装、连接、Session、接收推送的分流；**凡涉及 Fantasy Unity 客户端时先读此文件** |
| `references/unity/unity-check.md` | Unity 审查清单：版本一致性、编译宏、连接方式、Session 使用、推送 Handler 常见问题；**当用户要检查 Unity 客户端代码时读此文件** |
| `references/unity/setup-unity.md` | Unity 客户端安装 Fantasy.Unity、配置编译符号、导入协议；**仅在安装或初始接入时读此文件** |
| `references/unity/unity-connection.md` | Unity 客户端连接服务器：FantasyRuntime 组件、`scene.Connect`、`Runtime.Connect`、协议选择；**仅在连接初始化时读此文件** |
| `references/unity/unity-session.md` | Unity 客户端 Session 使用：发送消息、RPC、连接持有、断开连接；**仅在连接成功后如何发消息时读此文件** |
| `references/logging.md` | 日志系统详解：Fantasy.NLog 完整配置、Fantasy配置或增加日志，NLog.config 说明、自定义 ILog 实现（Serilog/文件日志等） |
| `references/logging-check.md` | 日志审查清单：日志初始化、NLog 规则、配置复制、模式切换、自定义 ILog 常见问题；**当用户要检查日志接入是否正确时读此文件** |
| `references/protocol/index.md` | 协议入口：定义 `.proto`、导出 C#、安装导出工具的分流；**凡涉及 `.proto`、Outer/Inner、协议导出时先读此文件** |
| `references/protocol/protocol-check.md` | 协议审查清单：Outer/Inner 选择、接口匹配、命名、导出、Handler 对齐；**当用户要检查协议或 Handler 定义是否正确时读此文件** |
| `references/protocol/define.md` | 协议定义入口：定位协议根目录并分流到 Outer/Inner；**当需要新建协议文件或确定协议放哪时读此文件** |
| `references/protocol/define-outer.md` | 外网协议：客户端↔服务器消息、`IMessage` / `IRequest` / `IResponse`；**仅在需要定义 Outer 协议时读此文件** |
| `references/protocol/define-inner.md` | 内网协议：服务器↔服务器消息、`IAddressMessage` / `IAddressRequest` / `IAddressResponse`；**仅在需要定义 Inner 协议时读此文件** |
| `references/protocol/define-common.md` | 协议通用特性：字段、集合、Map、枚举、序列化、代码注入；**仅在字段或序列化细节需要时读此文件** |
| `references/protocol/export.md` | 协议导出：检查工具、运行导出、验证结果；**协议定义完成后或用户要求重新导出时读此文件** |
| `references/protocol/export-install.md` | 导出工具安装与 `ExporterSettings.json` 配置；**工具未安装或路径未配置时读此文件** |
| `references/server/server-message-handler.md` | 服务器端接收客户端消息 Handler，**仅限实现了 IMessage/IRequest/IResponse 接口的消息**；Message\<T\>/MessageRPC\<TReq,TRes\> 模板、reply() 用法、错误码模式、Session 推送；Addressable/Roaming 见各自文件 |
| `references/server/server-message-handler-check.md` | 服务器消息 Handler 审查清单：基类选择、错误码、reply()、Session 生命周期、重复 Handler 常见问题；**当用户要检查客户端消息 Handler 时读此文件** |
| `references/unity/unity-message-handler.md` | Unity 客户端接收服务器主动推送 Handler：`Message<Session,T>`、文件位置约定、编译校验；**当用户需要在 Unity 创建接收服务器消息的 Handler 时读此文件** |
| `references/server/address.md` | 服务器间基于 Entity.Address（RuntimeId）的消息：**仅限实现了IAddressMessage/IAddressRequest/IAddressResponse 接口的消息,定义Address消息的Handler时读此文件** |
| `references/server/address-check.md` | Address 审查清单：消息模式、入口地址获取、Handler 类型、首次通信与缓存地址常见错误；**当用户要检查 Address 代码时读此文件** |
| `references/server/sphere-event/index.md` | SphereEvent 入口：跨服域事件、订阅、发布、取消订阅、与 Event/Roaming 的选择；**凡涉及跨服事件通知、`SphereEventComponent`、`SphereEventArgs`、`SphereEventSystem` 时先读此文件** |
| `references/server/sphere-event/implement.md` | SphereEvent 实现：定义事件类、实现处理器、订阅远程事件、发布事件、取消订阅；**仅在直接写 SphereEvent 代码时读此文件** |
| `references/server/sphere-event/best-practices.md` | SphereEvent 最佳实践与排错：对象池、热重载、事件大小、断线清理、与 Event/Roaming 的区别；**仅在优化或排查 SphereEvent 逻辑时读此文件** |
| `references/server/roaming/index.md` | Roaming 概念入口：核心概念（SessionRoamingComponent/Terminus/RoamingType）与 Workflow 决策树；**先读此文件，再按需读子文件** |
| `references/server/roaming/roaming-check.md` | Roaming 审查清单：协议、建链、Terminus 生命周期、消息流转、传送常见问题；**当用户要检查 Roaming 代码时读此文件** |
| `references/server/roaming/protocol.md` | 定义漫游协议：RoamingType.Config 配置、IRoamingMessage/IRoamingRequest/IRoamingResponse 格式；**仅需定义协议和增加漫游类型或服务器时读此文件** |
| `references/server/roaming/setup.md` | 建立漫游路由：Gate 侧 TryCreateRoaming/Link、传递参数；**仅需建立路由时读此文件** |
| `references/server/roaming/on-create-terminus.md` | OnCreateTerminus 事件：事件参数、LinkTerminusEntity API、Args 内存管理规则、各服务器独立 Handler 实现；**仅需处理 Terminus 创建/重连时读此文件** |
| `references/server/roaming/on-dispose-terminus.md` | OnDisposeTerminus 事件：触发时机、DisposeTerminusType 区分、各服务器独立 Handler 实现；**仅需处理 Terminus 销毁时读此文件** |
| `references/server/roaming/handler.md` | 漫游 Handler 入口：分流到消息处理 / 推送 / 跨服发送 / 传送；**当用户要写 Roaming Handler 或做传送时先读此文件** |
| `references/server/roaming/messaging.md` | 漫游消息处理：客户端发送、Gate 主动发后端、Roaming Handler、后端推送客户端、跨服发送；**仅需实现消息流转时读此文件** |
| `references/server/roaming/transfer.md` | Terminus 传送：`StartTransfer`、`TransferOutSystem`、`TransferInSystem`、生命周期与注意事项；**仅需实现跨服传送时读此文件** |
| `references/server/roaming/error-codes.md` | Roaming 所有错误码含义与排查方法；**仅在遇到 Roaming 相关错误时读此文件** |
| `references/http.md` | HTTP 入口：分流到服务器配置事件和 Controller 编写；**凡涉及 HTTP 服务器、Controller、OnConfigureHttpServices、OnConfigureHttpApplication 时先读此文件** |
| `references/http-check.md` | HTTP 审查清单：服务配置阶段、中间件顺序、SceneContextFilter、返回模式、路由映射常见问题；**当用户要检查 HTTP 代码时读此文件** |
| `references/http-server.md` | HTTP 服务器配置：`OnConfigureHttpServices`、`OnConfigureHttpApplication`、认证、授权、CORS、中间件；**仅在配置 HTTP 服务和中间件时读此文件** |
| `references/http-controller.md` | HTTP Controller 编写：`SceneContextFilter`、`Scene` 注入、Action 返回值、线程切换、Controller 示例；**仅在编写或排查 Controller 时读此文件** |
| `references/database/index.md` | 数据库入口：MongoDB 配置、获取数据库实例、持久化、查询、索引、并发修改的分流；**凡涉及 MongoDB、`IDatabase`、`scene.World.Database`、数据持久化时先读此文件** |
| `references/database/database-check.md` | 数据库审查清单：`scene.World` 访问、`ISupportedSerialize`、`isDeserialize`、协程锁、SeparateTable 适用性；**当用户要检查数据库代码时读此文件** |
| `references/database/mongodb.md` | MongoDB 使用：`ISupportedSerialize`、`Save`、`Insert`、`Query`、`Remove`、索引、`isDeserialize`、并发修改；**仅在直接写数据库代码时读此文件** |
| `references/database/separate-table.md` | SeparateTable 分表存储：聚合实体拆分存储、`[SeparateTable]`、`PersistAggregate`、`LoadWithSeparateTables`；**仅在聚合实体子数据过大、需要分表优化时读此文件** |
| `references/database/best-practices.md` | MongoDB 最佳实践与排错：配置关联、查询优化、保存策略、常见问题；**仅在优化或排查数据库逻辑时读此文件** |
| `references/config.md` | Fantasy.config 入口：按“新增机器 / 进程 / World / Scene / 数据库 / 端口”分流；**凡涉及 Fantasy.config 时先读此文件** |
| `references/config-check.md` | Fantasy.config 审查清单：machine/process/world/scene/database 引用关系、端口、World 模式 ID 范围常见问题；**当用户要检查配置是否正确时读此文件** |
| `references/config-scenarios.md` | Fantasy.config 常见场景：新建项目、增删 Scene、改数据库、改端口、多区服；**需要按具体场景改配置时读此文件** |
| `references/server/setup-server-check.md` | 服务器项目接入审查清单：三层结构、目标框架、Fantasy-Net 引用、编译宏、AssemblyHelper、Program 入口常见问题；**当用户要检查服务器项目接入 Fantasy 是否正确时读此文件** |
| `templates/Fantasy.config` | 完整注释模板：所有节点、属性、可选值与示例；**需要落具体 XML 时再读此文件** |
