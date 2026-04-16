# SubScene：动态子场景

SubScene 是在运行时从一个已有 Scene（父 Scene）中动态创建出来的**轻量级子场景**。它拥有独立的实体列表，但共享父 Scene 的核心基础设施（对象池、线程上下文、定时器、事件系统等），因此创建开销很小。

---

## 什么时候用 SubScene

SubScene 适用于需要**动态创建、独立管理、按需销毁**的隔离空间。典型场景：

| 场景 | 为什么用 SubScene |
|---|---|
| **副本/关卡** | 每个副本是一个 SubScene，副本结束时 `Close()` 即可清理所有实体，不影响主 Scene |
| **匹配房间** | 每局对战是一个 SubScene，对局结束直接销毁，玩家回到大厅（主 Scene） |
| **玩家私有空间** | 如个人庄园、个人商店，生命周期跟随玩家而非全局 Scene |
| **动态战场/区域** | 大世界中的临时区域，按需创建和回收 |

**什么时候不需要 SubScene：**

- 全局唯一的服务（如 Gate、Chat）→ 用配置文件定义的 Root Scene
- 只需要挂载组件的功能模块 → 直接 AddComponent 到现有 Entity 上

---

## 创建 SubScene

```csharp
var subScene = await Scene.CreateSubScene(parentScene, sceneType, onSubSceneSetup, onSubSceneCreated);
```

**参数说明：**

| 参数 | 类型 | 说明 |
|---|---|---|
| `parentScene` | `Scene` | 父 Scene 实例，SubScene 共享其核心组件 |
| `sceneType` | `int` | SceneType 枚举值，标识子场景类型（可自定义） |
| `onSubSceneSetup` | `Func<SubScene, Scene, FTask>` | 可选。OnCreateScene 事件发布**前**执行，用于挂载组件、做前置初始化 |
| `onSubSceneCreated` | `Func<SubScene, Scene, FTask>` | 可选。OnCreateScene 事件发布**后**执行，用于后续逻辑 |

**执行顺序：**
1. SubScene 初始化（共享父 Scene 资源）
2. `onSubSceneSetup` 回调执行
3. **`OnCreateScene` 事件发布** — 与 Root Scene 创建时触发的是同一个事件
4. `onSubSceneCreated` 回调执行

SubScene 创建时也会触发 `OnCreateScene` 事件，所以 `OnCreateSceneEvent` 的 Handler 中要通过 `SceneType` 区分当前是哪种 Scene，再执行对应的初始化逻辑：

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
                // Root Scene 初始化
                scene.AddComponent<AccountManageComponent>();
                break;
            case SceneType.Map:
            {
                // 通过scene.SceneRuntimeType 判断是否是SubScene
              	if (scene.SceneRuntimeType == SceneRuntimeType.SubScene)
                {
                  	//自定义 SubScene 类型的初始化
                }
               	// Root Scene 初始化
                scene.AddComponent<PlayerUnitManageComponent>();
                break;
            }
        }

        await FTask.CompletedTask;
    }
}
```

如果 SubScene 使用了和父 Scene 相同的 `SceneType`（如都是 `SceneType.Map`），通过 `SceneRuntimeType` 区分：

```csharp
if (scene.SceneRuntimeType == SceneRuntimeType.SubScene)
{
    // 这是 SubScene
}
```

### 基础用法

```csharp
// 在 Map Scene 上创建一个副本 SubScene
var dungeon = await Scene.CreateSubScene(mapScene, SceneType.Map);
```

### 带初始化回调

```csharp
var dungeon = await Scene.CreateSubScene(mapScene, SceneType.Map,
    onSubSceneSetup: async (sub, parent) =>
    {
        // OnCreateScene 事件发布前，先挂载必要组件
        sub.AddComponent<DungeonComponent>();
        sub.AddComponent<MonsterManagerComponent>();
        await FTask.CompletedTask;
    },
    onSubSceneCreated: async (sub, parent) =>
    {
        // OnCreateScene 事件发布后，执行后续逻辑
        Log.Info($"副本创建完成: {sub.Id}");
        await FTask.CompletedTask;
    });
```

---

## SubScene 与父 Scene 的关系

### 共享的资源

SubScene 不会重复创建这些组件，而是直接引用父 Scene 的：

- `EntityPool`（对象池）
- `EntityComponent`（ECS 生命周期管理）
- `TimerComponent`（定时器）
- `EventComponent`（事件系统）
- `CoroutineLockComponent`（协程锁）
- `MessageDispatcherComponent`（消息派发）
- `NetworkMessagingComponent`（内网消息，仅服务端）
- `ThreadSynchronizationContext`（线程上下文）

### 独立的部分

- **实体列表**：SubScene 维护自己的 `_entities` 字典，在 SubScene 中创建的 Entity 同时注册到 SubScene 和父 Scene（因此父 Scene 也能通过 RuntimeId 查到）
- **Address**：SubScene 有自己的 RuntimeId，可作为网络地址被其他服务器寻址
- **通过 `RootScene` 属性**可以访问父 Scene：`subScene.RootScene`

---

## 向 SubScene 发送消息

SubScene 拥有自己的 Address（RuntimeId），因此可以像普通 Scene 一样接收 Address 消息。

### 1. 获取 SubScene 的地址

创建 SubScene 后，通过 `subScene.Address` 拿到地址，通常需要将这个地址传回给需要通信的一方（如 Gate）：

```csharp
// Map 服务器创建 SubScene 后返回地址
var subScene = await Scene.CreateSubScene(scene, 6666);
response.SubSceneAddress = subScene.Address;
```

### 2. 通过地址发送消息

拿到地址后，使用 `NetworkMessagingComponent` 发送：

```csharp
// Gate 向 SubScene 发送单向消息
var subSceneAddress = session.GetComponent<GateSubSceneFlagComponent>().SubSceneAddressId;
session.Scene.NetworkMessagingComponent.Send(subSceneAddress, new G2SubScene_SentMessage
{
    Tag = "Hi SubScene",
});
```

### 3. SubScene 接收消息的 Handler

SubScene 收到消息后的 Handler 写法与普通 Address Handler 一致：

```csharp
// 泛型参数用 Scene 或 SubScene 都可以
public class G2SubScene_SentMessageHandler : Address<Scene, G2SubScene_SentMessage>
{
    protected override async FTask Run(Scene scene, G2SubScene_SentMessage message)
    {
        Log.Debug($"收到消息 SceneType:{scene.SceneType} Tag:{message.Tag}");
        await FTask.CompletedTask;
    }
}
```

---

## 销毁 SubScene

```csharp
await subScene.Close();
```

销毁时会：
1. 遍历并 Dispose SubScene 下管理的所有 Entity
2. 从父 Scene 的实体列表中移除这些 Entity
3. SubScene 自身被销毁

**销毁 SubScene 不影响父 Scene 和其他 SubScene。**

可以在收到客户端消息时销毁：
```csharp
public class C2SubScene_TestDisposeMessageHandler : Addressable<Unit, C2SubScene_TestDisposeMessage>
{
    protected override async FTask Run(Unit unit, C2SubScene_TestDisposeMessage message)
    {
        var unitScene = unit.Scene;
        await unitScene.Close(); // 销毁整个 SubScene
    }
}
```
