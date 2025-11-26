# SeparateTable 分表存储详解

SeparateTable 是 Fantasy Framework 提供的一种高级数据库持久化方案，用于将复杂聚合实体的子组件存储到独立的数据库表中。这种设计可以有效优化查询性能、减小主表大小，并提供更灵活的数据管理能力。

---

## 目录

- [什么是 SeparateTable](#什么是-separatetable)
- [为什么需要分表存储](#为什么需要分表存储)
- [核心概念](#核心概念)
- [快速开始](#快速开始)
- [定义分表实体](#定义分表实体)
- [保存聚合实体](#保存聚合实体)
- [加载聚合实体](#加载聚合实体)
- [API 参考](#api-参考)
- [实际应用场景](#实际应用场景)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 什么是 SeparateTable

SeparateTable（分表存储）允许你将 Entity 的某些子组件单独存储到独立的数据库表（集合）中，而不是和父实体保存在同一个表中。

**传统方式（单表存储）：**
```
Player 集合:
{
  "_id": 1001,
  "Name": "玩家001",
  "Level": 50,
  "Inventory": { /* 背包数据 */ },      // 嵌套在 Player 中
  "Equipment": { /* 装备数据 */ },      // 嵌套在 Player 中
  "Friends": [ /* 好友列表 */ ]         // 嵌套在 Player 中
}
```

**分表存储方式：**
```
Player 集合:
{
  "_id": 1001,
  "Name": "玩家001",
  "Level": 50
}

PlayerInventory 集合:
{
  "_id": 1001,  // 与 Player ID 相同
  "Items": [ /* 背包数据 */ ]
}

PlayerEquipment 集合:
{
  "_id": 1001,
  "Equipments": { /* 装备数据 */ }
}

PlayerFriends 集合:
{
  "_id": 1001,
  "Friends": [ /* 好友列表 */ ]
}
```

---

## 为什么需要分表存储

### 1. 减小主表大小

```csharp
// ❌ 问题：Player 表过大，查询慢
public class Player : Entity, ISupportedDataBase
{
    public string Name { get; set; }
    public int Level { get; set; }
    public List<Item> Inventory { get; set; }          // 可能有数百个物品
    public Dictionary<int, Equip> Equipments { get; set; }
    public List<Friend> Friends { get; set; }          // 可能有数百个好友
    public List<MailData> Mails { get; set; }          // 可能有大量邮件
    // ... 更多嵌套数据
}
```

当查询玩家基本信息（姓名、等级）时，不需要加载背包、装备等大量数据。

### 2. 提升查询性能

```csharp
// ✅ 优化：只加载需要的数据
// 查询玩家列表（只需要基本信息）
var players = await database.Query<Player>(p => p.Level > 50);
// 只加载 Name、Level 等字段，不加载背包、装备等

// 需要背包时再单独加载
var inventory = await database.Query<PlayerInventoryEntity>(playerId);
```

### 3. 独立索引管理

```csharp
// 为不同的子表创建不同的索引
await database.CreateIndex<Player>(
    Builders<Player>.IndexKeys.Ascending(p => p.Level)
);

await database.CreateIndex<PlayerInventoryEntity>(
    Builders<PlayerInventoryEntity>.IndexKeys.Ascending(p => p.ItemId)
);
```

### 4. 灵活的数据管理

- 可以单独备份某个子表
- 可以单独清理过期数据
- 可以对不同子表应用不同的存储策略

---

## 核心概念

### ISupportedSeparateTable 接口

标记实体支持作为分表组件：

```csharp
public interface ISupportedSeparateTable { }
```

### SeparateTableAttribute 特性

定义分表实体的父实体类型和集合名称：

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class SeparateTableAttribute : Attribute
{
    public readonly Type RootType;           // 父实体类型
    public readonly string CollectionName;   // 数据库集合名称
}
```

### 扩展方法

Fantasy Framework 提供了一系列扩展方法，简化分表实体的加载和保存操作：

**加载方法：**

```csharp
// 1. 加载实体的所有分表组件
await entity.LoadWithSeparateTables(database);

// 2. 加载实体的单个分表组件（按需加载）
bool loaded = await LoadWithSeparateTable<PlayerInventoryEntity>(entity, database);

// 3. 通过 ID 查询实体并自动加载所有分表（一步到位）
var player = await database.LoadWithSeparateTables<Player>(playerId);

// 4. 通过条件查询实体并自动加载所有分表（一步到位）
var player = await database.LoadWithSeparateTables<Player>(p => p.Name == "张三");
```

**保存方法：**

```csharp
// 1. 保存聚合根及所有分表组件
await entity.PersistAggregate(database);

// 2. 控制是否保存聚合根本身
await entity.PersistAggregate(isSaveSelf: false, database);  // 只保存分表，不保存聚合根

// 3. 只保存指定的单个分表组件（不保存聚合根）
await player.PersistAggregate<Player, PlayerInventoryEntity>(database);

// 4. 保存指定分表组件，可选是否保存聚合根
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: true, database);
```

---

## 快速开始

### 完整示例

```csharp
// 1. 定义主实体
public class Player : Entity, ISupportedDataBase
{
    public string Name { get; set; }
    public int Level { get; set; }
    public long Gold { get; set; }
}

// 2. 定义分表组件
[SeparateTable(typeof(Player), "PlayerInventory")]
public class PlayerInventoryEntity : Entity, ISupportedDataBase
{
    public List<ItemData> Items { get; set; }
}

[SeparateTable(typeof(Player), "PlayerEquipment")]
public class PlayerEquipmentEntity : Entity, ISupportedDataBase
{
    public Dictionary<int, int> Equipments { get; set; }
}

// 3. 创建和保存
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
player.Name = "玩家001";
player.Level = 50;

var inventory = player.AddComponent<PlayerInventoryEntity>();
inventory.Items = new List<ItemData>();

var equipment = player.AddComponent<PlayerEquipmentEntity>();
equipment.Equipments = new Dictionary<int, int>();

// 一键保存主实体及所有分表组件
await player.PersistAggregate(database);

// 4. 查询和加载
var player = await database.Query<Player>(playerId, isDeserialize: true);
await player.LoadWithSeparateTables(database);

// 现在可以访问分表组件
var inventory = player.GetComponent<PlayerInventoryEntity>();
var equipment = player.GetComponent<PlayerEquipmentEntity>();
```

---

## 定义分表实体

### 1. 基本定义

```csharp
// 主实体（必须实现 ISupportedDataBase）
public class Player : Entity, ISupportedDataBase
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// 分表组件（使用 SeparateTable 特性标记）
[SeparateTable(typeof(Player), "PlayerInventory")]
public class PlayerInventoryEntity : Entity, ISupportedDataBase
{
    public List<ItemData> Items { get; set; }
}
```

**关键要素：**
- 分表实体必须实现 `ISupportedDataBase`
- 使用 `[SeparateTable]` 特性标记
- 第一个参数：父实体类型
- 第二个参数：数据库集合名称

### 2. 多个分表组件

```csharp
// 主实体
public class Player : Entity, ISupportedDataBase
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// 背包分表
[SeparateTable(typeof(Player), "PlayerInventory")]
public class PlayerInventoryEntity : Entity, ISupportedDataBase
{
    public List<ItemData> Items { get; set; }
}

// 装备分表
[SeparateTable(typeof(Player), "PlayerEquipment")]
public class PlayerEquipmentEntity : Entity, ISupportedDataBase
{
    public Dictionary<int, EquipmentData> Equipments { get; set; }
}

// 好友分表
[SeparateTable(typeof(Player), "PlayerFriends")]
public class PlayerFriendsEntity : Entity, ISupportedDataBase
{
    public List<long> FriendIds { get; set; }
}

// 任务分表
[SeparateTable(typeof(Player), "PlayerQuests")]
public class PlayerQuestsEntity : Entity, ISupportedDataBase
{
    public Dictionary<int, QuestProgress> ActiveQuests { get; set; }
}
```

### 3. 嵌套的分表组件

```csharp
// 主实体
public class Guild : Entity, ISupportedDataBase
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// 公会成员（Guild 的分表）
[SeparateTable(typeof(Guild), "GuildMembers")]
public class GuildMembersEntity : Entity, ISupportedDataBase
{
    public List<GuildMember> Members { get; set; }
}

// 公会仓库（Guild 的分表）
[SeparateTable(typeof(Guild), "GuildWarehouse")]
public class GuildWarehouseEntity : Entity, ISupportedDataBase
{
    public List<ItemData> Items { get; set; }
}
```

---

## 保存聚合实体

### 1. 保存所有分表组件

```csharp
// 创建主实体
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
player.Name = "玩家001";
player.Level = 50;

// 添加分表组件
var inventory = player.AddComponent<PlayerInventoryEntity>();
inventory.Items = new List<ItemData>
{
    new ItemData { ItemId = 1001, Count = 10 },
    new ItemData { ItemId = 1002, Count = 5 }
};

var equipment = player.AddComponent<PlayerEquipmentEntity>();
equipment.Equipments = new Dictionary<int, EquipmentData>
{
    { 1, new EquipmentData { EquipId = 2001 } },
    { 2, new EquipmentData { EquipId = 2002 } }
};

// 方式 1: 保存聚合根及所有分表组件（默认）
await player.PersistAggregate(database);

// 方式 2: 只保存分表组件，不保存聚合根本身
await player.PersistAggregate(isSaveSelf: false, database);
```

**执行结果：**
```
Player 集合:
{ "_id": 1001, "Name": "玩家001", "Level": 50 }

PlayerInventory 集合:
{ "_id": 1001, "Items": [ { "ItemId": 1001, "Count": 10 }, ... ] }

PlayerEquipment 集合:
{ "_id": 1001, "Equipments": { "1": { "EquipId": 2001 }, ... } }
```

### 2. 只保存指定的单个分表组件

```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);

// 加载并修改背包
var inventory = await database.Query<PlayerInventoryEntity>(playerId, isDeserialize: true);
player.AddComponent(inventory);
inventory.Items.Add(new ItemData { ItemId = 1003, Count = 1 });

// 方式 1: 使用新的 PersistAggregate 泛型方法（推荐）
// 只保存 PlayerInventoryEntity，不保存 Player 本身
await player.PersistAggregate<Player, PlayerInventoryEntity>(database);

// 方式 2: 控制是否同时保存聚合根
player.Level = 51;  // 修改了聚合根
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: true, database);
// 同时保存 Player 和 PlayerInventoryEntity

// 方式 3: 传统方式 - 手动保存
await database.Save(inventory, collection: "PlayerInventory");
```

### 3. 智能部分更新

```csharp
// 查询主实体
var player = await database.Query<Player>(playerId, isDeserialize: true);

// 修改主实体
player.Level = 51;
player.Gold += 1000;

// 加载并修改背包
var inventory = await database.Query<PlayerInventoryEntity>(playerId, isDeserialize: true);
player.AddComponent(inventory);
inventory.Items.Add(new ItemData { ItemId = 1003, Count = 1 });

// 场景 1: 只保存主实体（不影响分表）
await database.Save(player);

// 场景 2: 只保存背包分表（不影响主实体和其他分表）
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: false, database);

// 场景 3: 保存主实体和背包分表（不影响装备等其他分表）
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: true, database);

// 场景 4: 保存主实体及所有已加载的分表组件
await player.PersistAggregate(database);
```

### 4. 性能优化：按需保存

```csharp
// ✅ 推荐：使用脏标记机制，只保存修改过的组件
var player = await database.LoadWithSeparateTables<Player>(playerId);

// 只修改背包
var inventory = player.GetComponent<PlayerInventoryEntity>();
inventory.Items.Add(newItem);

// 只保存被修改的背包组件
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: false, database);

// ❌ 不推荐：总是保存所有组件（可能保存了未修改的数据）
await player.PersistAggregate(database);
```

---

## 加载聚合实体

### 1. 使用 LoadWithSeparateTables（加载所有分表）

```csharp
// 方式 1: 先查询主实体，再加载分表
var player = await database.Query<Player>(playerId, isDeserialize: true);
await player.LoadWithSeparateTables(database);

// 方式 2: 一步到位（推荐） - 直接通过 ID 查询并加载
var player = await database.LoadWithSeparateTables<Player>(playerId);

// 方式 3: 一步到位 - 通过条件查询并加载
var player = await database.LoadWithSeparateTables<Player>(p => p.Name == "张三");

// 现在可以访问所有组件
var inventory = player.GetComponent<PlayerInventoryEntity>();
var equipment = player.GetComponent<PlayerEquipmentEntity>();
var friends = player.GetComponent<PlayerFriendsEntity>();
```

### 2. 按需加载单个分表

```csharp
// 只加载主实体
var player = await database.Query<Player>(playerId, isDeserialize: true);

// 方式 1: 使用新的 LoadWithSeparateTable 方法（推荐）
bool loaded = await LoadWithSeparateTable<PlayerInventoryEntity>(player, database);
if (loaded)
{
    var inventory = player.GetComponent<PlayerInventoryEntity>();
    // 使用 inventory...
}

// 方式 2: 传统方式 - 手动查询并添加组件
if (needEquipment)
{
    var equipment = await database.Query<PlayerEquipmentEntity>(playerId, isDeserialize: true);
    player.AddComponent(equipment);
}
```

### 3. 批量加载

```csharp
// 加载多个玩家（使用新的一步到位方法）
var playerIds = new List<long> { 1001, 1002, 1003 };
var players = new List<Player>();

foreach (var playerId in playerIds)
{
    // 一步完成查询和分表加载
    var player = await database.LoadWithSeparateTables<Player>(playerId);
    if (player != null)
    {
        players.Add(player);
    }
}
```

### 4. 条件查询并加载分表

```csharp
// 查询高等级玩家并加载其分表数据
var highLevelPlayer = await database.LoadWithSeparateTables<Player>(p => p.Level >= 50);

if (highLevelPlayer != null)
{
    var inventory = highLevelPlayer.GetComponent<PlayerInventoryEntity>();
    var equipment = highLevelPlayer.GetComponent<PlayerEquipmentEntity>();
}
```

---

## API 参考

### 加载 API

#### 1. LoadWithSeparateTables (Entity 扩展方法)

加载实体的所有分表组件。

```csharp
public static FTask LoadWithSeparateTables<T>(this T entity, IDatabase database)
    where T : Entity, new()
```

**参数：**
- `entity`: 聚合根实体实例
- `database`: 数据库实例

**示例：**
```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);
await player.LoadWithSeparateTables(database);

var inventory = player.GetComponent<PlayerInventoryEntity>();
var equipment = player.GetComponent<PlayerEquipmentEntity>();
```

---

#### 2. LoadWithSeparateTable (泛型单表加载)

加载实体的指定单个分表组件。

```csharp
public static FTask<bool> LoadWithSeparateTable<T>(Entity entity, IDatabase database)
    where T : Entity, new()
```

**参数：**
- `entity`: 聚合根实体实例
- `database`: 数据库实例

**返回值：**
- `true`: 成功加载并添加分表组件
- `false`: 分表不存在或加载失败

**示例：**
```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);

// 按需加载背包
bool loaded = await LoadWithSeparateTable<PlayerInventoryEntity>(player, database);
if (loaded)
{
    var inventory = player.GetComponent<PlayerInventoryEntity>();
}
```

---

#### 3. LoadWithSeparateTables (Database 扩展 - 通过 ID)

从数据库查询指定 ID 的实体，并自动加载其所有分表数据。

```csharp
public static async FTask<T> LoadWithSeparateTables<T>(this IDatabase database, long id)
    where T : Entity, new()
```

**参数：**
- `database`: 数据库实例
- `id`: 实体的唯一标识 ID

**返回值：**
- 加载了分表数据的实体实例，如果实体不存在则返回 `null`

**示例：**
```csharp
// 一步完成查询和分表加载
var player = await database.LoadWithSeparateTables<Player>(playerId);
if (player != null)
{
    var inventory = player.GetComponent<PlayerInventoryEntity>();
    var equipment = player.GetComponent<PlayerEquipmentEntity>();
}
```

**等价于：**
```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);
if (player != null)
{
    await player.LoadWithSeparateTables(database);
}
```

---

#### 4. LoadWithSeparateTables (Database 扩展 - 通过条件)

从数据库查询满足条件的第一个实体，并自动加载其所有分表数据。

```csharp
public static async FTask<T> LoadWithSeparateTables<T>(
    this IDatabase database,
    Expression<Func<T, bool>> filter
) where T : Entity, new()
```

**参数：**
- `database`: 数据库实例
- `filter`: 查询过滤条件的 Lambda 表达式

**返回值：**
- 加载了分表数据的实体实例，如果没有满足条件的实体则返回 `null`

**示例：**
```csharp
// 根据条件查询并加载分表数据
var player = await database.LoadWithSeparateTables<Player>(p => p.Name == "张三");
if (player != null)
{
    var inventory = player.GetComponent<PlayerInventoryEntity>();
}

// 查询高等级玩家
var highLevelPlayer = await database.LoadWithSeparateTables<Player>(p => p.Level >= 50);
```

---

### 保存 API

#### 1. PersistAggregate (保存所有分表 - 含聚合根)

保存聚合根实体及其所有分表组件到数据库。

```csharp
public static FTask PersistAggregate<T>(this T entity, IDatabase database)
    where T : Entity, new()
```

**参数：**
- `entity`: 聚合根实体实例
- `database`: 数据库实例

**示例：**
```csharp
var player = Entity.Create<Player>(scene);
player.Name = "玩家001";

var inventory = player.AddComponent<PlayerInventoryEntity>();
inventory.Items.Add(new ItemData { ItemId = 1001, Count = 10 });

// 保存主实体及所有分表组件
await player.PersistAggregate(database);
```

---

#### 2. PersistAggregate (保存所有分表 - 可选聚合根)

保存聚合根的所有分表组件，可选择是否保存聚合根本身。

```csharp
public static FTask PersistAggregate<T>(
    this T entity,
    bool isSaveSelf,
    IDatabase database
) where T : Entity, new()
```

**参数：**
- `entity`: 聚合根实体实例
- `isSaveSelf`: 是否保存聚合根本身
- `database`: 数据库实例

**示例：**
```csharp
// 只保存分表组件，不保存聚合根
await player.PersistAggregate(isSaveSelf: false, database);

// 保存聚合根及所有分表组件
await player.PersistAggregate(isSaveSelf: true, database);
```

---

#### 3. PersistAggregate (保存指定单表 - 不含聚合根)

只保存指定的单个分表组件，不保存聚合根。

```csharp
public static FTask PersistAggregate<T, TEntity>(this T entity, IDatabase database)
    where T : Entity, new()
    where TEntity : Entity, new()
```

**参数：**
- `entity`: 聚合根实体实例
- `database`: 数据库实例

**泛型参数：**
- `T`: 聚合根实体类型
- `TEntity`: 要保存的分表实体类型

**示例：**
```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);

// 加载并修改背包
var inventory = await database.Query<PlayerInventoryEntity>(playerId, isDeserialize: true);
player.AddComponent(inventory);
inventory.Items.Add(new ItemData { ItemId = 1003, Count = 1 });

// 只保存 PlayerInventoryEntity，不保存 Player 本身
await player.PersistAggregate<Player, PlayerInventoryEntity>(database);
```

---

#### 4. PersistAggregate (保存指定单表 - 可选聚合根)

保存指定的单个分表组件，可选择是否保存聚合根。

```csharp
public static FTask PersistAggregate<T, TEntity>(
    this T entity,
    bool isSaveSelf,
    IDatabase database
)
    where T : Entity, new()
    where TEntity : Entity, new()
```

**参数：**
- `entity`: 聚合根实体实例
- `isSaveSelf`: 是否保存聚合根本身
- `database`: 数据库实例

**泛型参数：**
- `T`: 聚合根实体类型
- `TEntity`: 要保存的分表实体类型

**示例：**
```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);
player.Level = 51;  // 修改聚合根

var inventory = await database.Query<PlayerInventoryEntity>(playerId, isDeserialize: true);
player.AddComponent(inventory);
inventory.Items.Add(newItem);  // 修改分表

// 同时保存 Player 和 PlayerInventoryEntity
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: true, database);

// 只保存 PlayerInventoryEntity
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: false, database);
```

---

### API 对比表

| API | 加载/保存 | 聚合根 | 分表范围 | 使用场景 |
|-----|---------|-------|---------|---------|
| `entity.LoadWithSeparateTables(db)` | 加载 | 需已查询 | 所有分表 | 已有实体，加载所有分表 |
| `LoadWithSeparateTable<T>(entity, db)` | 加载 | 需已查询 | 指定单表 | 已有实体，按需加载单个分表 |
| `db.LoadWithSeparateTables<T>(id)` | 加载 | 自动查询 | 所有分表 | 一步到位查询并加载（推荐） |
| `db.LoadWithSeparateTables<T>(filter)` | 加载 | 自动查询 | 所有分表 | 条件查询并加载（推荐） |
| `entity.PersistAggregate(db)` | 保存 | 保存 | 所有分表 | 保存聚合根及所有分表 |
| `entity.PersistAggregate(false, db)` | 保存 | 不保存 | 所有分表 | 只保存分表，不保存聚合根 |
| `entity.PersistAggregate<T, TEntity>(db)` | 保存 | 不保存 | 指定单表 | 只保存指定分表（性能优化） |
| `entity.PersistAggregate<T, TEntity>(true, db)` | 保存 | 保存 | 指定单表 | 保存聚合根和指定分表 |

---

## 实际应用场景

### 场景 1: 玩家数据管理

```csharp
// 主实体：玩家基本信息（经常查询）
public class Player : Entity, ISupportedDataBase
{
    public string Name { get; set; }
    public int Level { get; set; }
    public long Gold { get; set; }
    public DateTime LastLoginTime { get; set; }
}

// 背包：物品数据（只在打开背包时加载）
[SeparateTable(typeof(Player), "PlayerInventory")]
public class PlayerInventoryEntity : Entity, ISupportedDataBase
{
    public List<ItemData> Items { get; set; }
    public int Capacity { get; set; }
}

// 装备：装备数据（只在查看角色时加载）
[SeparateTable(typeof(Player), "PlayerEquipment")]
public class PlayerEquipmentEntity : Entity, ISupportedDataBase
{
    public Dictionary<int, EquipmentData> Equipments { get; set; }
}

// 任务：任务进度（只在打开任务界面时加载）
[SeparateTable(typeof(Player), "PlayerQuests")]
public class PlayerQuestsEntity : Entity, ISupportedDataBase
{
    public List<QuestProgress> ActiveQuests { get; set; }
    public List<int> CompletedQuestIds { get; set; }
}

// 邮件：邮件数据（只在打开邮箱时加载）
[SeparateTable(typeof(Player), "PlayerMails")]
public class PlayerMailsEntity : Entity, ISupportedDataBase
{
    public List<MailData> Mails { get; set; }
}
```

**使用示例：**

```csharp
// 玩家登录：只加载基本信息
var player = await database.Query<Player>(playerId, isDeserialize: true);
Log.Info($"玩家 {player.Name} 登录，等级 {player.Level}");

// 打开背包界面
var inventory = await database.Query<PlayerInventoryEntity>(playerId, isDeserialize: true);
player.AddComponent(inventory);
SendInventoryToClient(inventory.Items);

// 打开任务界面
var quests = await database.Query<PlayerQuestsEntity>(playerId, isDeserialize: true);
player.AddComponent(quests);
SendQuestsToClient(quests.ActiveQuests);
```

### 场景 2: 公会系统

```csharp
// 主实体：公会基本信息
public class Guild : Entity, ISupportedDataBase
{
    public string Name { get; set; }
    public int Level { get; set; }
    public long Gold { get; set; }
    public long LeaderId { get; set; }
}

// 成员列表（可能有数百人）
[SeparateTable(typeof(Guild), "GuildMembers")]
public class GuildMembersEntity : Entity, ISupportedDataBase
{
    public List<GuildMember> Members { get; set; }
}

// 申请列表（临时数据，经常变化）
[SeparateTable(typeof(Guild), "GuildApplications")]
public class GuildApplicationsEntity : Entity, ISupportedDataBase
{
    public List<GuildApplication> Applications { get; set; }
}

// 公会仓库（大量物品数据）
[SeparateTable(typeof(Guild), "GuildWarehouse")]
public class GuildWarehouseEntity : Entity, ISupportedDataBase
{
    public List<ItemData> Items { get; set; }
}

// 公会日志（历史记录）
[SeparateTable(typeof(Guild), "GuildLogs")]
public class GuildLogsEntity : Entity, ISupportedDataBase
{
    public List<GuildLog> Logs { get; set; }
}
```

### 场景 3: 排行榜系统

```csharp
// 主实体：排行榜元数据
public class Leaderboard : Entity, ISupportedDataBase
{
    public int SeasonId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

// 排行数据（可能有大量玩家）
[SeparateTable(typeof(Leaderboard), "LeaderboardRankings")]
public class LeaderboardRankingsEntity : Entity, ISupportedDataBase
{
    public List<RankEntry> Rankings { get; set; }
}

// 奖励配置
[SeparateTable(typeof(Leaderboard), "LeaderboardRewards")]
public class LeaderboardRewardsEntity : Entity, ISupportedDataBase
{
    public Dictionary<int, RewardData> Rewards { get; set; }
}
```

---

## 最佳实践

### 1. 何时使用分表

**✅ 适合使用分表的场景：**
- 子数据量大（如背包有数百个物品）
- 不经常一起查询（如基本信息和邮件数据）
- 需要独立管理（如独立清理过期数据）
- 需要独立索引（如按物品 ID 查询）

**❌ 不适合使用分表的场景：**
- 数据量小（如几个字段）
- 总是一起查询（如玩家名称和等级）
- 强关联关系（如订单和订单项）

### 2. 集合命名规范

```csharp
// ✅ 推荐：使用前缀区分父实体
[SeparateTable(typeof(Player), "PlayerInventory")]
[SeparateTable(typeof(Player), "PlayerEquipment")]
[SeparateTable(typeof(Guild), "GuildMembers")]
[SeparateTable(typeof(Guild), "GuildWarehouse")]

// ❌ 不推荐：名称不清晰
[SeparateTable(typeof(Player), "Inventory")]  // 不知道是谁的 Inventory
[SeparateTable(typeof(Guild), "Members")]     // 不知道是谁的 Members
```

### 3. 按需加载

```csharp
// ✅ 推荐：按需加载
var player = await database.Query<Player>(playerId, isDeserialize: true);

// 只在需要时加载背包
if (needInventory)
{
    var inventory = await database.Query<PlayerInventoryEntity>(playerId, isDeserialize: true);
    player.AddComponent(inventory);
}

// ❌ 不推荐：总是加载所有分表
await player.LoadWithSeparateTables(database);  // 加载了可能用不到的数据
```

### 4. 保存策略

```csharp
// ✅ 推荐：只保存修改过的组件
if (inventory.IsDirty)
{
    await database.Save(inventory, collection: "PlayerInventory");
}

if (equipment.IsDirty)
{
    await database.Save(equipment, collection: "PlayerEquipment");
}

// ❌ 不推荐：总是保存所有组件
await player.PersistAggregate(database);  // 可能保存了未修改的数据
```

### 5. 索引优化

```csharp
// 为分表创建合适的索引
await database.CreateIndex<PlayerInventoryEntity>(
    Builders<PlayerInventoryEntity>.IndexKeys.Ascending("Items.ItemId")
);

await database.CreateIndex<GuildMembersEntity>(
    Builders<GuildMembersEntity>.IndexKeys.Ascending("Members.PlayerId")
);
```

---

## 常见问题

### 1. PersistAggregate 保存了哪些数据？

**答：** `PersistAggregate` 会保存：
- 主实体本身
- 所有标记了 `[SeparateTable]` 的子组件

```csharp
await player.PersistAggregate(database);

// 等价于：
await database.Save(player);
await database.Save(player.GetComponent<PlayerInventoryEntity>(), "PlayerInventory");
await database.Save(player.GetComponent<PlayerEquipmentEntity>(), "PlayerEquipment");
// ... 其他分表组件
```

### 2. LoadWithSeparateTables 加载了哪些数据？

**答：** `LoadWithSeparateTables` 会加载所有注册为该实体分表的组件。

```csharp
await player.LoadWithSeparateTables(database);

// 等价于：
var inventory = await database.Query<PlayerInventoryEntity>(player.Id, isDeserialize: true);
player.AddComponent(inventory);

var equipment = await database.Query<PlayerEquipmentEntity>(player.Id, isDeserialize: true);
player.AddComponent(equipment);
// ... 其他分表组件
```

### 3. 如何只加载部分分表？

**手动查询需要的分表：**

```csharp
// 只加载背包和装备
var player = await database.Query<Player>(playerId, isDeserialize: true);

var inventory = await database.Query<PlayerInventoryEntity>(playerId, isDeserialize: true);
player.AddComponent(inventory);

var equipment = await database.Query<PlayerEquipmentEntity>(playerId, isDeserialize: true);
player.AddComponent(equipment);
```

### 4. 分表组件的 ID 必须和主实体相同吗？

**答：** 是的。分表组件使用与主实体相同的 ID，这样可以通过 ID 关联数据。

```csharp
// Player._id = 1001
// PlayerInventory._id = 1001  （相同 ID）
// PlayerEquipment._id = 1001  （相同 ID）
```

### 5. 如何删除分表数据？

```csharp
// 删除主实体
await database.Remove<Player>(playerId);

// 删除分表组件
await database.Remove<PlayerInventoryEntity>(playerId);
await database.Remove<PlayerEquipmentEntity>(playerId);
```

### 6. 分表组件可以是多实例组件吗？

**答：** 不建议。分表组件通常和主实体 ID 一一对应。如果需要多实例，建议在分表组件内部使用列表或字典。

```csharp
// ❌ 不推荐
[SeparateTable(typeof(Player), "PlayerItems")]
public class PlayerItemEntity : Entity, ISupportedMultiEntity, ISupportedDataBase { }

// ✅ 推荐
[SeparateTable(typeof(Player), "PlayerInventory")]
public class PlayerInventoryEntity : Entity, ISupportedDataBase
{
    public List<ItemData> Items { get; set; }  // 在组件内部使用列表
}
```

---

## 相关文档

- [MongoDB 集成和使用](14-Database.md) - 数据库基础操作
- [Entity-Component-System 详解](../CoreSystems/01-ECS.md) - Entity 基础知识
- [ISupportedMultiEntity 详解](../CoreSystems/02-ISupportedMultiEntity.md) - 多实例组件

---

## 总结

SeparateTable 分表存储提供了：

- ✅ **性能优化**：减小主表大小，提升查询速度
- ✅ **按需加载**：只加载需要的数据，减少内存占用
- ✅ **独立管理**：为不同子表创建独立索引和管理策略
- ✅ **灵活扩展**：方便添加新的分表组件
- ✅ **自动化**：Source Generator 自动生成注册代码

合理使用分表存储，可以有效优化复杂聚合实体的数据库性能，是大型游戏项目的重要优化手段。
