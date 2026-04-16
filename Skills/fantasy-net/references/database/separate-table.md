# SeparateTable 分表存储

SeparateTable 用于把复杂聚合实体的子组件拆到独立集合中存储，而不是全塞进主实体集合里。

## 什么时候用

优先在这些场景考虑 SeparateTable：

- 主实体包含大量子数据，导致主表文档过大
- 查询主实体基本信息时，不希望总是把背包、装备、好友、邮件等全部加载出来
- 不同子数据需要独立保存、独立索引、独立加载

如果主实体数据量并不大，优先先用普通 MongoDB 持久化，不要一开始就把结构做复杂。

## 核心思路

普通方式：

- `Player` 自己把背包、装备、好友等都嵌进去存一条记录

SeparateTable 方式：

- `Player` 只保存主信息
- `PlayerInventoryEntity` / `PlayerEquipmentEntity` 等子组件保存到独立集合
- 它们通常与主实体使用相同的 Id

## 第 1 步：定义主实体

主实体仍然实现 `ISupportedSerialize`：

```csharp
public sealed class Player : Entity, ISupportedSerialize
{
    public string Name { get; set; }
    public int Level { get; set; }
    public long Gold { get; set; }
}
```

## 第 2 步：定义分表组件

用 `[SeparateTable]` 标记子组件：

```csharp
[SeparateTable(typeof(Player), "PlayerInventory")]
public sealed class PlayerInventoryEntity : Entity
{
    public List<ItemData> Items { get; set; }
}

[SeparateTable(typeof(Player), "PlayerEquipment")]
public sealed class PlayerEquipmentEntity : Entity
{
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<int, int> Equipments { get; set; }
}
```

关键点：

- 第一个参数是聚合根类型
- 第二个参数是集合名称
- 加了 `[SeparateTable]` 后，不需要再实现 `ISupportedSerialize`

## 第 3 步：创建并保存聚合实体

```csharp
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
player.Name = "玩家001";
player.Level = 50;

var inventory = player.AddComponent<PlayerInventoryEntity>();
inventory.Items = new List<ItemData>();

var equipment = player.AddComponent<PlayerEquipmentEntity>();
equipment.Equipments = new Dictionary<int, int>();

await player.PersistAggregate(database);
```

## 第 4 步：加载聚合实体

### 先查主实体，再加载全部分表

```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);
await player.LoadWithSeparateTables(database);
```

### 一步到位加载

```csharp
var player = await database.LoadWithSeparateTables<Player>(playerId);
```

### 按需只加载单个分表

```csharp
bool loaded = await LoadWithSeparateTable<PlayerInventoryEntity>(player, database);
```

## 常用保存方式

### 保存聚合根和全部分表

```csharp
await player.PersistAggregate(database);
```

### 只保存分表，不保存主实体

```csharp
await player.PersistAggregate(isSaveSelf: false, database);
```

### 只保存某一个分表

```csharp
await player.PersistAggregate<Player, PlayerInventoryEntity>(database);
await player.PersistAggregate<Player, PlayerInventoryEntity>(isSaveSelf: false, database);
```

## 使用建议

### 推荐

- 主实体只保留高频查询的核心字段
- 大块低频数据拆到 SeparateTable 组件
- 修改哪个分表，就尽量只保存哪个分表
- 查询列表页、排行榜等轻量场景时，不要把所有分表一起加载

### 不推荐

- 数据量并不大却提前过度分表
- 每次保存都无脑保存整个聚合
- 主实体和子分表边界划分不清

## 什么时候该用 SeparateTable

可以用这个判断：

- “查玩家列表只要 Name/Level，但总是被背包和邮件拖慢” -> 适合
- “一个聚合实体下面挂了很多重型组件” -> 适合
- “只是普通玩家数据，字段并不多” -> 先不用

## 相关文档

- `mongodb.md` - 基础 MongoDB CRUD
- `best-practices.md` - 查询优化、保存策略、数据库排错
