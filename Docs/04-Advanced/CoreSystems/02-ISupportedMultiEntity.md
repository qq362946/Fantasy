# ISupportedMultiEntity - 多实例组件详解

`ISupportedMultiEntity` 是 Fantasy Framework 中用于支持在同一个父实体上添加**多个相同类型组件**的关键接口。

---

## 为什么需要 ISupportedMultiEntity？

### 默认行为：单一组件限制

在 Fantasy ECS 中，默认情况下，每个实体只能添加**一个**同类型的组件：

```csharp
public class HealthComponent : Entity { }

var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
player.AddComponent<HealthComponent>();  // ✅ 成功
player.AddComponent<HealthComponent>();  // ❌ 错误！会报错
```

**错误日志：**
```
type:HealthComponent If you want to add multiple components of the same type, please implement IMultiEntity
```

### 多实例组件：突破限制

实现 `ISupportedMultiEntity` 接口后，可以添加多个同类型的组件实例：

```csharp
public class BuffComponent : Entity, ISupportedMultiEntity { }

var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
var buff1 = player.AddComponent<BuffComponent>();  // ✅ 成功，ID: 自动生成
var buff2 = player.AddComponent<BuffComponent>();  // ✅ 成功，ID: 自动生成
var buff3 = player.AddComponent<BuffComponent>();  // ✅ 成功，ID: 自动生成
```

---

## 核心机制

### 1. ID 管理策略

**单一组件（不实现 ISupportedMultiEntity）：**
- 使用 **TypeHashCode** 作为 Key
- 存储在 `_tree` 字典中
- ID 等于父实体的 ID

**多实例组件（实现 ISupportedMultiEntity）：**
- 使用 **Entity.Id** 作为 Key
- 存储在 `_multi` 字典中
- ID 由 `Scene.EntityIdFactory.Create` 自动生成（唯一）

```csharp
// 源码逻辑（简化版）
public T AddComponent<T>(bool isPool = true) where T : Entity, new()
{
    if (EntitySupportedChecker<T>.IsMulti)
    {
        // 多实例组件：使用自动生成的 ID
        var id = Scene.EntityIdFactory.Create;
        var entity = Create<T>(Scene, id, isPool, false);
        _multi.Add(entity.Id, entity);  // 用 entity.Id 作为 Key
    }
    else
    {
        // 单一组件：使用父实体的 ID
        var id = Id;
        var entity = Create<T>(Scene, id, isPool, false);
        _tree.Add(entity.TypeHashCode, entity);  // 用 TypeHashCode 作为 Key
    }
}
```

### 2. 编译时优化

Framework 使用 `EntitySupportedChecker<T>` 在**编译时**缓存接口检查结果：

```csharp
public static class EntitySupportedChecker<T> where T : Entity
{
    public static bool IsMulti { get; }  // 编译时确定，JIT 内联为常量

    static EntitySupportedChecker()
    {
        var type = typeof(T);
        IsMulti = typeof(ISupportedMultiEntity).IsAssignableFrom(type);
    }
}
```

**性能优势：**
- ✅ 静态字段在每个具体类型实例化时仅初始化一次
- ✅ JIT 编译器将静态布尔值内联为常量，实现分支消除优化
- ✅ 避免重复的运行时类型检查开销
- ✅ 提高 CPU 缓存局部性

---

## 使用方式

### 1. 定义多实例组件

```csharp
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

// 实现 ISupportedMultiEntity 接口
public class BuffComponent : Entity, ISupportedMultiEntity
{
    public int BuffType { get; set; }
    public long ExpireTime { get; set; }
}
```

### 2. 添加多个组件实例

```csharp
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);

// 方式 1：自动生成 ID
var buff1 = player.AddComponent<BuffComponent>();
buff1.BuffType = 1001;
buff1.ExpireTime = TimeHelper.Now + 10000;

var buff2 = player.AddComponent<BuffComponent>();
buff2.BuffType = 1002;
buff2.ExpireTime = TimeHelper.Now + 20000;

Log.Info($"Buff1 ID: {buff1.Id}");  // 例如: 1001
Log.Info($"Buff2 ID: {buff2.Id}");  // 例如: 1002

// 方式 2：指定 ID（需要确保唯一性）
var buff3 = player.AddComponent<BuffComponent>(id: 9999);
Log.Info($"Buff3 ID: {buff3.Id}");  // 输出: 9999
```

### 3. 查找和访问组件

```csharp
// 通过 ID 获取
var buff = player.GetComponent<BuffComponent>(buff1.Id);

// 检查是否存在
if (player.HasComponent<BuffComponent>(buff1.Id))
{
    Log.Info("Buff exists!");
}

// 遍历所有多实例组件
foreach (var entity in player.ForEachMultiEntity)
{
    if (entity is BuffComponent buff)
    {
        Log.Info($"BuffType: {buff.BuffType}, ExpireTime: {buff.ExpireTime}");
    }
}
```

### 4. 删除组件

```csharp
// 删除指定 ID 的组件
player.RemoveComponent<BuffComponent>(buff1.Id);

// 删除但不销毁（只是解除父子关系）
player.RemoveComponent<BuffComponent>(buff2.Id, isDispose: false);
```

---

## 实际使用场景

### 场景 1：Buff 系统

```csharp
public class BuffComponent : Entity, ISupportedMultiEntity
{
    public int BuffId { get; set; }
    public int BuffType { get; set; }
    public float Value { get; set; }
    public long ExpireTime { get; set; }
}

public class BuffSystem : UpdateSystem<BuffComponent>
{
    protected override void Update(BuffComponent self)
    {
        // 检查 Buff 是否过期
        if (TimeHelper.Now >= self.ExpireTime)
        {
            Log.Info($"Buff {self.BuffId} expired!");
            self.Dispose();
        }
    }
}

// 使用示例
public void AddBuff(Player player, int buffId, int buffType, float value, long duration)
{
    var buff = player.AddComponent<BuffComponent>();
    buff.BuffId = buffId;
    buff.BuffType = buffType;
    buff.Value = value;
    buff.ExpireTime = TimeHelper.Now + duration;
}
```

### 场景 2：背包系统

```csharp
public class ItemComponent : Entity, ISupportedMultiEntity
{
    public int ItemId { get; set; }
    public int Count { get; set; }
    public int Quality { get; set; }
}

public class InventoryComponent : Entity
{
    public int MaxSlots { get; set; } = 100;
}

// 添加物品
public void AddItem(Player player, int itemId, int count)
{
    var inventory = player.GetOrAddComponent<InventoryComponent>();

    var item = player.AddComponent<ItemComponent>();
    item.ItemId = itemId;
    item.Count = count;

    Log.Info($"Added item {itemId} x{count}, ItemEntity ID: {item.Id}");
}

// 查找物品
public ItemComponent FindItemById(Player player, int itemId)
{
    foreach (var entity in player.ForEachMultiEntity)
    {
        if (entity is ItemComponent item && item.ItemId == itemId)
        {
            return item;
        }
    }
    return null;
}
```

### 场景 3：技能系统

```csharp
public class SkillComponent : Entity, ISupportedMultiEntity
{
    public int SkillId { get; set; }
    public int Level { get; set; }
    public long CooldownEndTime { get; set; }
}

// 学习技能
public void LearnSkill(Player player, int skillId, int level)
{
    var skill = player.AddComponent<SkillComponent>();
    skill.SkillId = skillId;
    skill.Level = level;
    skill.CooldownEndTime = 0;
}

// 使用技能
public bool UseSkill(Player player, long skillEntityId)
{
    var skill = player.GetComponent<SkillComponent>(skillEntityId);
    if (skill == null)
    {
        Log.Error("Skill not found!");
        return false;
    }

    if (TimeHelper.Now < skill.CooldownEndTime)
    {
        Log.Warning("Skill is on cooldown!");
        return false;
    }

    // 执行技能逻辑
    ExecuteSkill(skill);

    // 设置冷却时间
    skill.CooldownEndTime = TimeHelper.Now + GetSkillCooldown(skill.SkillId);
    return true;
}
```

### 场景 4：连接管理（Session）

Fantasy Framework 自身的 `Session` 就是一个典型的多实例组件应用：

```csharp
// Framework 源码
public class Session : Entity, ISupportedMultiEntity
{
    public long LastReceiveTime { get; set; }
    public ANetwork Network { get; set; }
    // ...
}

// 可以在 Scene 上添加多个 Session
var session1 = scene.AddComponent<Session>();
var session2 = scene.AddComponent<Session>();
var session3 = scene.AddComponent<Session>();
```

---

## 数据库持久化

多实例组件同样支持数据库持久化，只需实现 `ISupportedDataBase` 接口：

```csharp
public class ItemComponent : Entity, ISupportedMultiEntity, ISupportedDataBase
{
    public int ItemId { get; set; }
    public int Count { get; set; }
}

var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
var item1 = player.AddComponent<ItemComponent>();
var item2 = player.AddComponent<ItemComponent>();

// 保存到数据库（会保存所有子组件）
await player.Save();

// 从数据库加载
var loadedPlayer = await scene.GetDataBase<Player>().Query(playerId);
loadedPlayer.Deserialize(scene);

// 遍历加载的物品
foreach (var entity in loadedPlayer.ForEachMultiEntity)
{
    if (entity is ItemComponent item)
    {
        Log.Info($"Loaded item: {item.ItemId} x{item.Count}");
    }
}
```

**数据库存储结构：**
- 多实例组件存储在 `_multiDb` 字段（仅 .NET，BSON 序列化）
- 每个组件的 `Id` 会被持久化，确保唯一性
- 反序列化时自动重建 `_multi` 字典和父子关系

---

## 单一组件 vs 多实例组件对比

| 特性 | 单一组件 | 多实例组件 (ISupportedMultiEntity) |
|------|---------|-----------------------------------|
| **接口** | 无需特殊接口 | 必须实现 `ISupportedMultiEntity` |
| **数量限制** | 每个类型只能添加 1 个 | 每个类型可以添加**无限个** |
| **ID 生成** | 使用父实体的 ID | 自动生成唯一 ID |
| **存储位置** | `_tree` 字典 (Key: TypeHashCode) | `_multi` 字典 (Key: Entity.Id) |
| **查找方式** | `GetComponent<T>()` | `GetComponent<T>(id)` |
| **检查存在** | `HasComponent<T>()` | `HasComponent<T>(id)` |
| **删除方式** | `RemoveComponent<T>()` | `RemoveComponent<T>(id)` |
| **遍历方式** | `ForEachEntity` | `ForEachMultiEntity` |
| **适用场景** | 单例功能组件 (Health, Inventory) | 集合数据 (Buff, Item, Skill) |

---

## 最佳实践

### ✅ 推荐做法

```csharp
// 1. 明确语义：类名体现"多实例"特性
public class BuffComponent : Entity, ISupportedMultiEntity { }  // ✅ 好
public class PlayerBuff : Entity { }  // ❌ 容易误解

// 2. 保存组件 ID，方便后续访问
var buffId = player.AddComponent<BuffComponent>().Id;
playerData.ActiveBuffIds.Add(buffId);

// 3. 使用 ForEachMultiEntity 遍历
foreach (var entity in player.ForEachMultiEntity)
{
    if (entity is BuffComponent buff)
    {
        // 处理 Buff
    }
}

// 4. 删除前检查存在性
if (player.HasComponent<BuffComponent>(buffId))
{
    player.RemoveComponent<BuffComponent>(buffId);
}

// 5. 组合使用多实例和数据库持久化
public class EquipmentComponent : Entity, ISupportedMultiEntity, ISupportedDataBase
{
    // 装备会跟随角色一起保存到数据库
}
```

### ⚠️ 注意事项

```csharp
// 1. 不要忘记指定 ID 参数
var buff = player.GetComponent<BuffComponent>();  // ❌ 编译错误！必须传 ID
var buff = player.GetComponent<BuffComponent>(buffId);  // ✅ 正确

// 2. 不要混淆单一组件和多实例组件的 API
player.RemoveComponent<BuffComponent>();  // ❌ 编译错误！必须传 ID
player.RemoveComponent<BuffComponent>(buffId);  // ✅ 正确

// 3. 指定 ID 时注意唯一性
var item1 = player.AddComponent<ItemComponent>(id: 1001);
var item2 = player.AddComponent<ItemComponent>(id: 1001);  // ⚠️ ID 冲突！

// 4. 不要在组件销毁后继续使用
var buff = player.GetComponent<BuffComponent>(buffId);
player.RemoveComponent<BuffComponent>(buffId);
buff.BuffType = 1001;  // ❌ 错误！buff 已被销毁

// 5. 遍历时不要修改集合
foreach (var entity in player.ForEachMultiEntity)
{
    player.RemoveComponent<BuffComponent>(entity.Id);  // ❌ 运行时错误！迭代中修改集合
}

// 正确做法：先收集 ID，再删除
var idsToRemove = new List<long>();
foreach (var entity in player.ForEachMultiEntity)
{
    if (entity is BuffComponent buff && buff.ExpireTime <= TimeHelper.Now)
    {
        idsToRemove.Add(buff.Id);
    }
}
foreach (var id in idsToRemove)
{
    player.RemoveComponent<BuffComponent>(id);
}
```

---

## 性能考虑

### 内存开销

- **单一组件**: `EntitySortedDictionary<long, Entity>` (Key: TypeHashCode)
- **多实例组件**: `EntitySortedDictionary<long, Entity>` (Key: Entity.Id)

两者内存结构相同，区别仅在 Key 的语义。

### 查找性能

```csharp
// 单一组件：O(log n)，n = 父实体的组件总数
var health = player.GetComponent<HealthComponent>();

// 多实例组件：O(log m)，m = 多实例组件的数量
var buff = player.GetComponent<BuffComponent>(buffId);
```

两者都使用 `SortedDictionary`，查找复杂度相同。

### 遍历性能

```csharp
// 遍历所有单一组件
foreach (var entity in player.ForEachEntity) { }  // 遍历 _tree

// 遍历所有多实例组件
foreach (var entity in player.ForEachMultiEntity) { }  // 遍历 _multi
```

两者性能相近，取决于集合大小。

---

## 常见问题

### Q1: 什么时候使用 ISupportedMultiEntity？

**A:** 当你需要在同一个父实体上添加**多个相同类型**的组件时使用。典型场景：
- Buff 系统（一个玩家可以有多个 Buff）
- 背包系统（一个背包可以有多个物品）
- 技能系统（一个玩家可以学习多个技能）
- 连接管理（一个服务器可以有多个客户端连接）

### Q2: 如何选择是否实现 ISupportedMultiEntity？

**决策树：**
```
是否需要在同一个父实体上添加多个相同类型的实例？
├─ 是 → 实现 ISupportedMultiEntity
└─ 否 → 不需要实现
```

### Q3: 可以动态切换单一/多实例模式吗？

**A:** 不可以。`ISupportedMultiEntity` 是编译时决定的，无法在运行时切换。

### Q4: 多实例组件会影响性能吗？

**A:** 几乎无影响。Framework 使用编译时检查（`EntitySupportedChecker<T>`），JIT 会内联为常量，无运行时开销。

### Q5: 可以同时实现 ISupportedMultiEntity 和 ISupportedDataBase 吗？

**A:** 可以！完全兼容：

```csharp
public class ItemComponent : Entity, ISupportedMultiEntity, ISupportedDataBase
{
    // 既支持多实例，也支持数据库持久化
}
```

---

## 总结

`ISupportedMultiEntity` 是 Fantasy Framework 中实现**同类型多组件**的关键机制：

- **核心功能**: 允许在同一父实体上添加多个相同类型的组件
- **实现方式**: 通过接口标记，编译时检查，运行时使用 `_multi` 字典管理
- **性能优化**: 编译时缓存接口检查，JIT 内联优化，零运行时开销
- **典型场景**: Buff、Item、Skill、Session 等集合类数据
- **兼容性**: 可与 `ISupportedDataBase` 配合使用，支持数据库持久化

**设计理念**: 通过简单的接口标记，实现灵活的组件管理，兼顾开发便利性和运行时性能。

---

## 相关文档

- [01-ECS.md](01-ECS.md) - Entity-Component-System 详解
- 03-ISupportedDataBase.md - 数据库持久化详解（规划中）
- 04-ISupportedSeparateTable.md - 分表存储详解（规划中）
