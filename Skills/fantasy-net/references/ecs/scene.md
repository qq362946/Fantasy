# Scene：ECS 的顶层容器

Scene 是 Fantasy ECS 的核心概念——它是所有 Entity/Component 的**归属容器**和**生命周期边界**。

---

## 每个 Entity/Component 都属于一个 Scene

每个 Entity 都有一个 `Scene` 属性，指向它所属的 Scene。这个属性在创建时自动设置：

- 通过 `Entity.Create<T>(scene, ...)` 创建时，`scene` 参数决定了归属
- 通过 `AddComponent<T>()` 添加组件时，组件自动继承父 Entity 的 Scene
- 反序列化（`entity.Deserialize(scene)`）时，需要显式传入 Scene 来重新建立归属关系

```csharp
var player = Entity.Create<Player>(scene);
// player.Scene == scene

var health = player.AddComponent<HealthComponent>();
// health.Scene == scene（自动继承父实体的 Scene）
```

---

## Scene 管理的核心资源

Scene 本身也是 Entity（`Scene : Entity`），但它是特殊的根实体。每个 Scene 持有一套完整的运行时基础设施：

| 组件 | 用途 |
|---|---|
| `EntityComponent` | ECS 系统组件，管理 Awake/Update/Destroy 生命周期 |
| `TimerComponent` | 定时器系统 |
| `EventComponent` | 事件发布/订阅系统 |
| `CoroutineLockComponent` | 协程锁（异步同步控制） |
| `NetworkMessagingComponent` | 内网消息发送（仅服务端） |
| `SeparateTableComponent` | 数据库分表（仅服务端） |

这些组件在 Scene 创建时自动初始化，不需要手动添加。

---

## Scene 销毁 = 级联清理一切

**当 Scene 销毁时，它会自动销毁下面注册的所有 Entity。** 这是 Scene 作为生命周期边界的核心含义——你不需要逐个清理 Entity，只需销毁 Scene：

```csharp
// scene 下有 player、npc、monster 等大量实体
scene.Dispose(); // 或 await scene.Close();
// 所有属于这个 Scene 的 Entity 都被销毁、回收
```

这意味着：
- 在需要的 Entity 上正确设置 Scene（通过 `Entity.Create(scene, ...)` 的第一个参数）
- 不要在 Scene 销毁后继续使用它下面的 Entity 引用——它们已经被销毁了
- 如果某个 Entity 需要在 Scene 销毁后存活，它应该属于另一个 Scene

---

## Scene 类型与 SceneRuntimeType

通过 `scene.SceneRuntimeType` 可以判断当前 Scene 是 Root Scene 还是 SubScene：

```csharp
public enum SceneRuntimeType
{
    None = 0,       // 默认（已销毁后会被重置为 None）
    Root = 1,       // 主 Scene
    SubScene = 2,   // 子场景
}
```

```csharp
if (scene.SceneRuntimeType == SceneRuntimeType.SubScene)
{
    // 这是一个 SubScene
}
```

这在 `OnCreateScene` 事件中特别有用——Root Scene 和 SubScene 创建时都会触发同一个 `OnCreateScene` 事件（详见 subscene.md），当它们使用了相同的 `SceneType` 时，可以通过 `SceneRuntimeType` 区分：

```csharp
protected override async FTask Handler(OnCreateScene self)
{
    var scene = self.Scene;

    if (scene.SceneRuntimeType == SceneRuntimeType.SubScene)
    {
        // SubScene 专属初始化
        return;
    }

    // Root Scene 初始化
    switch (scene.SceneType)
    {
        case SceneType.Map:
            scene.AddComponent<PlayerUnitManageComponent>();
            break;
    }

    await FTask.CompletedTask;
}
```

| 类型 | 说明 |
|---|---|
| **Root Scene** | 主 Scene，拥有独立的对象池、ID 工厂、线程上下文。由配置文件驱动创建 |
| **SubScene** | 运行时动态创建的子场景，共享父 Scene 的核心组件但有独立的实体列表。适用于副本、匹配房间等需要按需创建和销毁的场景 → **详见 subscene.md**  |

---

## OnCreateScene 事件

Scene 创建完成后会发布 `OnCreateScene` 事件，这是初始化 Scene 级组件的标准入口：

```csharp
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
                scene.AddComponent<AccountManageComponent>();
                break;
            case SceneType.Map:
                scene.AddComponent<PlayerUnitManageComponent>();
                break;
        }

        await FTask.CompletedTask;
    }
}
```

---

## 通过 Scene 访问系统组件

在任意 Entity 中，通过 `.Scene` 属性访问当前 Scene 的系统组件：

```csharp
// 在任意 Entity 中使用定时器
self.Scene.TimerComponent.Net.WaitAsync(5000);

// 发布事件
await self.Scene.EventComponent.PublishAsync(new SomeEvent());

// 发送内网消息（服务端）
self.Scene.NetworkMessagingComponent.Send(address, message);

// 通过 Scene 查找实体
var entity = self.Scene.GetEntity<Player>(runtimeId);
```
