# Scene 使用指南

本指南将介绍 Fantasy 框架中 Scene 的核心概念和使用方法，包括：
- 服务端 Scene 的配置和自动创建机制
- 客户端 Scene 的手动创建方式
- Scene 运行模式的选择
- 实体管理和网络通信

> **📌 重要提示:** 服务端的 Scene 通过配置文件自动创建，而客户端需要通过代码手动创建。这是两者的核心区别。

---

## 目录

- [核心概念](#核心概念)
- [服务端 Scene](#服务端-scene)
  - [配置文件自动创建](#配置文件自动创建)
  - [配置字段说明](#配置字段说明)
  - [启动流程](#启动流程)
  - [处理 OnCreateScene 事件](#处理-oncreatescene-事件)
  - [创建子 Scene](#创建子-scene-subscene)
- [客户端 Scene](#客户端-scene)
  - [手动创建](#手动创建)
  - [Unity 客户端示例](#unity-客户端示例)
  - [Console 客户端示例](#console-客户端示例)
- [SceneRuntimeMode 运行模式](#sceneruntimemode-运行模式)
- [Scene 核心组件](#scene-核心组件)
- [实体管理](#实体管理)
- [网络通信](#网络通信)
- [销毁 Scene](#销毁-scene)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 核心概念

Scene 是 Fantasy 框架的核心容器，负责管理实体（Entity）、组件（Component）和系统（System）的生命周期。

### 服务端 vs 客户端对比

| 特性 | 服务端 | 客户端 |
|------|--------|--------|
| 创建方式 | 📄 配置文件自动创建 | 💻 代码手动创建 |
| 生命周期 | 随服务器进程启动 | 应用程序控制 |
| 网络能力 | Inner/Outer 双网络 | 单一外部连接 |
| 配置来源 | Fantasy.config | 无需配置 |

---

## 服务端 Scene

### 配置文件自动创建

服务端的 Scene 在服务器启动时根据 `Fantasy.config` 配置文件**自动创建**。开发者无需手动调用创建方法。

> **📌 关键点:** 服务器启动时，框架会遍历配置文件中的所有 `<scene>` 节点，自动创建对应的 Scene 实例。

**Fantasy.config 配置示例：**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<fantasy xmlns="http://fantasy.net/config">
  <server>
    <!-- 场景配置 -->
    <scenes>
      <scene id="1001" processConfigId="1" worldConfigId="1"
             sceneRuntimeMode="MultiThread"
             sceneTypeString="Addressable"
             networkProtocol=""
             outerPort="0"
             innerPort="11001"/>
      <scene id="1002" processConfigId="1" worldConfigId="1"
             sceneRuntimeMode="MultiThread"
             sceneTypeString="Gate"
             networkProtocol="KCP"
             outerPort="20000"
             innerPort="11002"/>
      <scene id="1003" processConfigId="1" worldConfigId="1"
             sceneRuntimeMode="MultiThread"
             sceneTypeString="Map"
             networkProtocol=""
             outerPort="0"
             innerPort="11003"/>
    </scenes>
  </server>
</fantasy>
```

### 配置字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | uint | Scene 唯一标识符（必须唯一） |
| `processConfigId` | uint | 所属进程 ID |
| `worldConfigId` | uint | 所属世界 ID |
| `sceneRuntimeMode` | string | 运行模式：`MainThread` / `MultiThread` / `ThreadPool` |
| `sceneTypeString` | string | Scene 类型：`Gate` / `Map` / `Chat` / `Addressable` 等 |
| `networkProtocol` | string | 外网协议：`TCP` / `KCP` / `WebSocket`（空则不开启外网）|
| `outerPort` | int | 外网端口（0 表示不开启）|
| `innerPort` | int | 内网端口（服务器间通信）|

---

### 启动流程

服务端启动时，框架自动执行以下流程：

```
服务器启动流程:
┌─────────────────────────────────────────────────────────────┐
│ 1. Entry.Start()                                            │
│    └─ 加载 Fantasy.config 配置文件                            │
├─────────────────────────────────────────────────────────────┤
│ 2. 创建 Process 实例                                         │
│    └─ 遍历 ProcessConfig 配置                                 │
├─────────────────────────────────────────────────────────────┤
│ 3. 遍历 SceneConfig 创建 Scene                               │
│    ├─ Scene.Create(process, machineConfig, sceneConfig)     │
│    ├─ 初始化核心组件（Timer、Event、Entity 等）                │
│    ├─ 创建内网网络（如果 innerPort > 0）                       │
│    └─ 创建外网网络（如果 outerPort > 0）                       │
├─────────────────────────────────────────────────────────────┤
│ 4. 触发 OnCreateScene 事件                                   │
│    └─ 开发者在此处理 Scene 初始化逻辑                          │
└─────────────────────────────────────────────────────────────┘
```

**Program.cs 示例：**

```csharp
using Fantasy;

try
{
    // 初始化程序集
    AssemblyHelper.Initialize();
    // 启动 Fantasy 框架（自动创建所有配置的 Scene）
    await Fantasy.Platform.Net.Entry.Start();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"服务器启动失败：{ex}");
    Environment.Exit(1);
}
```

---

### 处理 OnCreateScene 事件

通过实现 `AsyncEventSystem<OnCreateScene>` 处理 Scene 创建完成后的初始化逻辑：

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        // 根据 SceneType 执行不同的初始化逻辑
        switch (scene.SceneType)
        {
            case SceneType.Gate:
                Log.Info($"Gate Scene 启动: {scene.RuntimeId}");
                // Gate 初始化逻辑
                break;

            case SceneType.Map:
                Log.Info($"Map Scene 启动: {scene.RuntimeId}");
                // 添加地图管理组件
                scene.AddComponent<MapManagerComponent>();
                break;

            case SceneType.Addressable:
                // Addressable Scene 会自动添加 AddressableManageComponent
                Log.Info($"Addressable Scene 启动: {scene.RuntimeId}");
                break;

            case SceneType.Chat:
                Log.Info($"Chat Scene 启动: {scene.RuntimeId}");
                break;
        }

        await FTask.CompletedTask;
    }
}
```

> **📌 提示:** `SceneType.Addressable` 类型的 Scene 会自动添加 `AddressableManageComponent`，无需手动添加。

---

### 创建子 Scene (SubScene)

服务端可在运行时动态创建子 Scene，适用于副本、动态地图等场景：

```csharp
// 创建子 Scene
var subScene = await Scene.CreateSubScene(
    parentScene,                    // 父 Scene
    SceneType.Map,                  // Scene 类型
    (subScene, parent) =>           // 创建完成回调（可选）
    {
        Log.Info($"SubScene 创建完成: {subScene.RuntimeId}");
        // 初始化副本逻辑
        subScene.AddComponent<DungeonComponent>();
    });
```

**SubScene 特性：**

| 特性 | 说明 |
|------|------|
| Id 生成器 | 共享父 Scene 的 EntityIdFactory 和 RuntimeIdFactory |
| 实体管理 | 独立管理，不与父 Scene 混淆 |
| 使用场景 | 副本、战斗房间、临时场景 |

---

## 客户端 Scene

### 手动创建

客户端必须通过代码手动创建 Scene：

```csharp
// 1. 初始化 Fantasy
await Fantasy.Platform.Unity.Entry.Initialize();

// 2. 创建 Scene
var scene = await Scene.Create(SceneRuntimeMode.MainThread);
```

> **📌 关键点:** 客户端的 `Scene.Create()` 方法接收运行模式参数，默认使用 `MainThread` 与 Unity 主线程同步。

---

### Unity 客户端示例

```csharp
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Scene _scene;
    private Session _session;

    private void Start()
    {
        InitializeAsync().Coroutine();
    }

    private void OnDestroy()
    {
        // 销毁 Scene，释放所有资源
        _scene?.Dispose();
    }

    private async FTask InitializeAsync()
    {
        // 1. 初始化 Fantasy 框架
        await Fantasy.Platform.Unity.Entry.Initialize();

        // 2. 创建客户端 Scene
        _scene = await Scene.Create(SceneRuntimeMode.MainThread);

        // 3. 连接服务器
        _session = _scene.Connect(
            "127.0.0.1:20000",           // 服务器地址
            NetworkProtocolType.KCP,      // 协议类型
            OnConnectComplete,            // 连接成功回调
            OnConnectFail,                // 连接失败回调
            OnConnectDisconnect,          // 断开连接回调
            false,                        // 是否 HTTPS (WebSocket)
            5000);                        // 连接超时 (毫秒)
    }

    private void OnConnectComplete()
    {
        Log.Debug("连接成功");
        // 添加心跳组件保持连接
        _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
    }

    private void OnConnectFail()
    {
        Log.Debug("连接失败");
    }

    private void OnConnectDisconnect()
    {
        Log.Debug("连接断开");
    }
}
```

**Connect 方法参数说明：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `remoteAddress` | string | 服务器地址，格式：`IP:Port` |
| `networkProtocolType` | NetworkProtocolType | 协议类型：`KCP` / `TCP` / `WebSocket` |
| `onConnectComplete` | Action | 连接成功回调 |
| `onConnectFail` | Action | 连接失败回调 |
| `onConnectDisconnect` | Action | 断开连接回调 |
| `isHttps` | bool | WebSocket 是否使用 HTTPS |
| `connectTimeout` | int | 连接超时时间（毫秒） |

---

### Console 客户端示例

```csharp
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

// 初始化框架
await Fantasy.Platform.Console.Entry.Initialize();

// 创建 Scene
var scene = await Scene.Create(SceneRuntimeMode.MainThread);

// 连接服务器
var session = scene.Connect(
    "127.0.0.1:20000",
    NetworkProtocolType.TCP,
    () => Log.Info("连接成功"),
    () => Log.Error("连接失败"),
    () => Log.Info("连接断开"),
    false);

// 添加心跳
session.AddComponent<SessionHeartbeatComponent>().Start(2000);

// 保持程序运行
Console.ReadLine();
```

---

## SceneRuntimeMode 运行模式

| 模式 | 说明 | 推荐使用场景 |
|------|------|-------------|
| `MainThread` | 在主线程中运行 | ✅ 客户端、Unity |
| `MultiThread` | 在独立线程中运行 | ✅ 服务端高并发 Scene |
| `ThreadPool` | 在线程池中运行 | 服务端轻量级 Scene |

**选择建议：**

- **服务端**：推荐 `MultiThread`，每个 Scene 独立线程，避免相互阻塞
- **客户端**：推荐 `MainThread`，与 Unity 主线程同步，方便 UI 操作

---

## Scene 核心组件

Scene 创建时自动初始化以下组件：

### 通用组件

| 组件 | 说明 |
|------|------|
| `TimerComponent` | 定时器系统 |
| `EventComponent` | 事件系统 |
| `EntityComponent` | 实体管理 |
| `MessagePoolComponent` | 消息对象池 |
| `CoroutineLockComponent` | 协程锁 |
| `MessageDispatcherComponent` | 消息分发 |
| `PoolGeneratorComponent` | 对象池生成器 |

### 服务端专属组件

| 组件 | 说明 |
|------|------|
| `NetworkMessagingComponent` | 内网消息发送 |
| `SeparateTableComponent` | 分表管理 |
| `TerminusComponent` | 漫游终端 |
| `RoamingComponent` | Session 漫游 |
| `SphereEventComponent` | 跨服事件 |

---

## 实体管理

### 创建实体

```csharp
// 在 Scene 下创建实体
var entity = Entity.Create<PlayerEntity>(scene, true, false);

// 添加组件
entity.AddComponent<MoveComponent>();
entity.AddComponent<BagComponent>();
```

### 查询实体

```csharp
// 通过 RuntimeId 查询（泛型）
var player = scene.GetEntity<PlayerEntity>(runtimeId);

// 通过 RuntimeId 查询（非泛型）
var entity = scene.GetEntity(runtimeId);

// 安全查询
if (scene.TryGetEntity<PlayerEntity>(runtimeId, out var player))
{
    // 使用 player
}
```

### 删除实体

```csharp
// 仅从 Scene 中移除（不调用 Dispose）
scene.RemoveEntity(runtimeId);

// 销毁实体（调用 Dispose）
entity.Dispose();
```

---

## 网络通信

### 服务端发送消息

```csharp
// 发送到指定地址
scene.Send(address, new PlayerEnterMessage { PlayerId = 123 });

// 发送到多个地址
scene.Send(addressList, new BroadcastMessage { Content = "Hello" });

// RPC 调用
var response = await scene.Call<GetPlayerResponse>(
    address,
    new GetPlayerRequest { PlayerId = 123 });
```

### 获取其他 Scene 的 Session

```csharp
// 服务端获取目标 Scene 的 Session
var session = scene.GetSession(targetRuntimeId);
await session.Call(request);
```

---

## 销毁 Scene

```csharp
// 异步关闭（推荐）
await scene.Close();

// 同步销毁
scene.Dispose();
```

**Close() 方法执行的操作：**

1. ✅ 关闭 SphereEventComponent（如有）
2. ✅ 清理所有子实体
3. ✅ 释放网络资源
4. ✅ 清理对象池

---

## 最佳实践

### 服务端

1. ✅ **通过配置文件规划 Scene 结构**，不要在代码中硬编码创建
2. ✅ **使用 OnCreateScene 事件初始化**，避免在配置加载阶段执行复杂逻辑
3. ✅ **选择合适的运行模式**，高并发 Scene 使用 `MultiThread`
4. ✅ **注意线程安全**，`MultiThread` 模式下跨 Scene 数据访问需要加锁

### 客户端

1. ✅ **保持单一 Scene 实例**，避免重复创建
2. ✅ **使用 MainThread 模式**，与 Unity 主线程同步
3. ✅ **在 OnDestroy 中销毁 Scene**，确保资源释放
4. ✅ **添加心跳组件**，保持与服务器的连接

---

## 常见问题

### Q1: 服务端 Scene 没有自动创建

**可能原因：**

1. `Fantasy.config` 配置文件不存在或路径错误
2. `<scene>` 节点配置格式错误
3. `processConfigId` 与当前进程不匹配

**解决方案：**

1. ✅ 检查 `Fantasy.config` 是否在正确位置（通常在 `AppContext.BaseDirectory`）
2. ✅ 检查 XML 格式是否正确，可以使用 `Fantasy.xsd` 验证
3. ✅ 确认启动参数中的 `ProcessId` 与配置匹配

---

### Q2: 客户端 Scene.Create 返回 null

**可能原因：**

1. 未调用 `Entry.Initialize()` 初始化框架
2. Scene ID 超出限制（最大 65535）

**解决方案：**

```csharp
// 确保先初始化框架
await Fantasy.Platform.Unity.Entry.Initialize();

// 然后创建 Scene
var scene = await Scene.Create(SceneRuntimeMode.MainThread);
```

---

### Q3: 客户端连接服务器失败

**可能原因：**

1. 服务器未启动或端口未开放
2. 协议类型不匹配（客户端 KCP，服务端 TCP）
3. 防火墙阻止连接

**解决方案：**

1. ✅ 确认服务器已启动并监听正确端口
2. ✅ 确认客户端和服务端使用相同的协议类型
3. ✅ 检查防火墙设置

---

### Q4: OnCreateScene 事件未触发

**可能原因：**

1. 事件处理类未正确继承 `AsyncEventSystem<OnCreateScene>`
2. Source Generator 未生成注册代码
3. 程序集未正确加载

**解决方案：**

1. ✅ 确认类继承自 `AsyncEventSystem<OnCreateScene>`
2. ✅ 重新编译项目，检查生成的代码
3. ✅ 确认调用了 `AssemblyHelper.Initialize()`

---

### Q5: MultiThread 模式下出现线程安全问题

**原因：**

`MultiThread` 模式下，每个 Scene 在独立线程运行，跨 Scene 访问数据会产生竞态条件。

**解决方案：**

```csharp
// 使用协程锁保护共享资源
using (await scene.CoroutineLockComponent.Wait(LockType.Custom, resourceId))
{
    // 安全访问共享资源
}
```

---

## 下一步

现在你已经掌握了 Scene 的使用方法，接下来可以：

1. 📖 阅读 [Entity 实体系统](./01-Entity.md) 学习实体和组件
2. 🌐 阅读 [网络协议](./07-NetworkProtocol.md) 学习消息定义
3. ⚙️ 阅读 [Fantasy.config 配置详解](./01-Server/01-ServerConfiguration.md) 深入了解配置
4. 🔧 阅读 [OnCreateScene 事件使用指南](./01-Server/04-OnCreateScene.md) 学习场景初始化
5. 📚 查看 `Examples/` 目录下的完整示例

---

## 获取帮助

- **GitHub**: https://github.com/qq362946/Fantasy
- **文档**: https://www.code-fantasy.com/
- **Issues**: https://github.com/qq362946/Fantasy/issues
