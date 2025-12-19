# Entity-Component-System (ECS) 详解

Fantasy Framework 的 ECS 系统是一个创新的层级组件化架构，它结合了传统 ECS 的优势和面向对象的灵活性。

---

## 核心概念

### Entity（实体）

Entity 是框架中一切对象的基础，它既是游戏对象本身，也是组件的容器。与传统 ECS 不同，Fantasy 的 Entity：

- **支持层级关系**：Entity 可以包含子 Entity，形成树状结构
- **自带生命周期**：拥有唯一的 `Id` 和 `RuntimeId`
- **绑定 Scene**：每个 Entity 都归属于一个 `Scene`
- **支持对象池**：可以从对象池创建和回收，减少 GC 压力

```csharp
public abstract class Entity : IEntity
{
    public long Id { get; }                    // 持久化 ID
    public long RuntimeId { get; }             // 运行时 ID
    public Scene Scene { get; }                // 所属场景
    public Entity Parent { get; }              // 父实体
    public bool IsDisposed { get; }            // 是否已销毁
}
```

### Component（组件）

在 Fantasy 中，**Component 就是 Entity**。所有功能模块都继承自 `Entity`，通过父子关系组织：

- **单一组件**：默认情况下，每个类型只能添加一个实例
- **多实例组件**：实现 `ISupportedMultiEntity` 接口，可添加多个同类型组件（详见 [ISupportedMultiEntity 详解](02-ISupportedMultiEntity.md)）
- **数据库组件**：实现 `ISupportedDataBase` 接口，支持 MongoDB 持久化

---

## 创建 Entity

### 1. 创建独立 Entity

```csharp
// 从对象池创建，自动生成 ID
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);

// 指定 ID 创建
var monster = Entity.Create<Monster>(scene, id: 1001, isPool: true, isRunEvent: true);

// 使用 Type 创建
var npc = Entity.Create(scene, typeof(NPC), isPool: true, isRunEvent: true);
```

**参数说明：**
- `scene`: 实体所属的场景
- `isPool`: 是否从对象池创建（建议为 `true`）
- `isRunEvent`: 是否执行生命周期事件（`AwakeSystem`、`UpdateSystem` 等）

### 2. 添加组件（子 Entity）

```csharp
// 单一组件模式（每种类型只能添加一个）
var healthComponent = player.AddComponent<HealthComponent>();
var inventoryComponent = player.AddComponent<InventoryComponent>();

// 多实例组件模式（需实现 ISupportedMultiEntity）
public class BuffComponent : Entity, ISupportedMultiEntity { }

var buff1 = player.AddComponent<BuffComponent>();  // ID 自动生成
var buff2 = player.AddComponent<BuffComponent>();  // 可添加多个

// 指定 ID 添加组件
var item = inventory.AddComponent<ItemComponent>(id: 10001);
```

> **📖 延伸阅读**：关于多实例组件的详细用法、最佳实践和实际案例，请参考 [ISupportedMultiEntity 详解](02-ISupportedMultiEntity.md)

---

## 查找和访问组件

### 1. 获取单一组件

```csharp
// 获取组件，不存在返回 null
var health = player.GetComponent<HealthComponent>();

// 获取或添加组件（不存在则创建）
var inventory = player.GetOrAddComponent<InventoryComponent>();

// 检查组件是否存在
if (player.HasComponent<HealthComponent>())
{
    // 组件存在
}
```

### 2. 获取多实例组件

```csharp
// 通过 ID 获取
var buff = player.GetComponent<BuffComponent>(buffId);

// 检查是否存在
if (player.HasComponent<BuffComponent>(buffId))
{
    // 组件存在
}
```

### 3. 遍历组件

```csharp
// 遍历所有单一组件（不包括多实例组件）
foreach (var component in player.ForEachEntity)
{
    Log.Info($"Component: {component.Type.Name}");
}

// 遍历所有多实例组件
foreach (var buff in player.ForEachMultiEntity)
{
    Log.Info($"Buff ID: {buff.Id}");
}
```

---

## 删除组件

```csharp
// 删除单一组件
player.RemoveComponent<HealthComponent>();

// 删除多实例组件
player.RemoveComponent<BuffComponent>(buffId);

// 删除组件但不销毁（只是解除父子关系）
player.RemoveComponent<InventoryComponent>(isDispose: false);
```

---

## 层级关系

Fantasy 的 Entity 支持父子层级关系，类似 Unity 的 GameObject：

```csharp
// 创建父实体
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);

// 添加子实体（自动设置父子关系）
var weapon = player.AddComponent<WeaponComponent>();
var armor = player.AddComponent<ArmorComponent>();

// 访问父实体
Entity parent = weapon.Parent;  // 返回 player
Player typedParent = weapon.GetParent<Player>();

// 销毁父实体会自动销毁所有子实体
player.Dispose();  // weapon 和 armor 也会被销毁
```

---

## 生命周期和事件

Entity 支持自动生命周期事件，通过 Source Generator 自动注册：

```csharp
// 实体定义
public class Player : Entity { }

// Awake 事件（实体创建时触发）
public class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        Log.Info("Player created!");
    }
}

// Update 事件（每帧触发）
public class PlayerUpdateSystem : UpdateSystem<Player>
{
    protected override void Update(Player self)
    {
        // 每帧逻辑
    }
}

// Destroy 事件（实体销毁时触发）
public class PlayerDestroySystem : DestroySystem<Player>
{
    protected override void Destroy(Player self)
    {
        Log.Info("Player destroyed!");
    }
}

// Deserialize 事件（从数据库加载时触发）
public class PlayerDeserializeSystem : DeserializeSystem<Player>
{
    protected override void Deserialize(Player self)
    {
        // 反序列化后的初始化逻辑
    }
}
```

**生命周期顺序：**
1. `AwakeSystem` - 实体创建时
2. `UpdateSystem` - 每帧更新（需注册）
3. `LateUpdateSystem` - 延迟更新（仅 Unity）
4. `DestroySystem` - 实体销毁时

---

## 对象池和内存管理

Fantasy 的 Entity 支持对象池，减少 GC 压力：

```csharp
// 从对象池创建（推荐）
var entity = Entity.Create<MyEntity>(scene, isPool: true, isRunEvent: true);

// 销毁时自动回收到对象池
entity.Dispose();

// 检查是否来自对象池
bool isFromPool = entity.IsPool();
```

**最佳实践：**
- ✅ 短生命周期的 Entity 使用对象池（`isPool: true`）
- ✅ 频繁创建销毁的组件使用对象池
- ⚠️ 长生命周期的全局对象可以不使用对象池
- ⚠️ 对象池对象销毁时会自动清理状态，需确保数据不会被意外重用

---

## 与传统 ECS 的区别

| 特性 | Fantasy ECS | 传统 ECS |
|------|-------------|----------|
| **组件定义** | Component 就是 Entity | Component 是纯数据结构 |
| **层级关系** | 支持父子树状结构 | 通常扁平化设计 |
| **生命周期** | 自动管理 `Awake/Destroy` | 手动管理 |
| **对象池** | 内置对象池支持 | 需自行实现 |
| **数据库** | 原生支持 MongoDB | 需额外集成 |
| **代码风格** | 面向对象 + ECS 混合 | 数据导向编程 |

---

## 最佳实践

### ✅ 推荐做法

```csharp
// 1. 使用对象池创建短生命周期对象
var entity = Entity.Create<Bullet>(scene, isPool: true, isRunEvent: true);

// 2. 使用 GetOrAddComponent 避免重复添加
var component = entity.GetOrAddComponent<MyComponent>();

// 3. 父实体销毁会自动销毁子实体
player.Dispose();  // 自动清理所有组件

```

### ⚠️ 注意事项

```csharp
// 1. 避免在 Entity 之外持有组件引用
var health = player.GetComponent<HealthComponent>();
player.Dispose();
// health 已被销毁，不要再使用

// 2. 单一组件不能重复添加
player.AddComponent<HealthComponent>();
player.AddComponent<HealthComponent>();  // ❌ 错误！会报错

```

---

## 总结

Fantasy 的 ECS 系统是一个**层级化的组件系统**，核心特点：

- **Entity 即 Component**：一切皆实体，组件也是实体
- **层级关系**：支持父子树状结构，销毁自动级联
- **生命周期自动化**：通过 System 自动管理 Awake/Update/Destroy
- **内存优化**：内置对象池，减少 GC 压力
- **数据库集成**：原生支持 MongoDB 持久化
- **Source Generator**：编译时自动注册，零反射开销

这种设计兼具**传统 ECS 的性能优势**和**面向对象的开发便利性**，非常适合复杂的游戏服务器开发。

---

## 相关文档

- [02-ISupportedMultiEntity.md](02-ISupportedMultiEntity.md) - 多实例组件详解
