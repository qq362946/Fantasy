# MongoDB 集成和使用

Fantasy Framework 提供了完整的 MongoDB 数据库集成方案，支持 Entity 的持久化存储、查询、更新和删除操作。框架通过统一的 `IDatabase` 接口提供数据库访问能力，并内置了高性能的 MongoDB 实现。

---

## 目录

- [核心概念](#核心概念)
- [配置数据库连接](#配置数据库连接)
- [获取数据库实例](#获取数据库实例)
- [Entity 持久化标记](#entity-持久化标记)
- [基础操作](#基础操作)
  - [保存 (Save)](#保存-save)
  - [插入 (Insert)](#插入-insert)
  - [查询 (Query)](#查询-query)
  - [删除 (Remove)](#删除-remove)
  - [统计和聚合](#统计和聚合)
- [高级特性](#高级特性)
  - [数据库锁机制](#数据库锁机制)
  - [反序列化参数](#反序列化参数)
  - [索引管理](#索引管理)
  - [自定义 MongoDB 客户端](#自定义-mongodb-客户端)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 核心概念

### IDatabase 接口

Fantasy 通过 `IDatabase` 接口定义了统一的数据库操作规范：

```csharp
public interface IDatabase
{
    DatabaseType DatabaseType { get; }      // 数据库类型
    string Name { get; }                    // 数据库名称
    object GetDatabaseInstance { get; }     // 原生数据库实例

    // 初始化、查询、保存、删除等方法...
}
```

### MongoDatabase 实现

`MongoDatabase` 是框架内置的 MongoDB 实现，提供：

- ✅ **自动协程锁**：确保数据库操作的线程安全
- ✅ **对象池优化**：减少序列化开销
- ✅ **BSON 序列化**：高性能的二进制序列化
- ✅ **可选反序列化**：通过 `isDeserialize` 决定是否把查询结果注册到框架系统
- ✅ **集合管理**：自动创建和管理数据库集合

---

## 配置数据库连接

### 1. 在 Fantasy.config 中配置数据库

数据库配置属于 `World`，必须放在 `<server><worlds><world>` 下。Scene 通过 `worldConfigId` 绑定 World，并共享该 World 的数据库配置：

```xml
<fantasy xmlns="http://fantasy.net/config">
  <server>
    <worlds>
      <world id="1" namespaceId="1" groupId="1" worldName="GameWorld">
        <database dbType="MongoDB"
                  dbName="game_data"
                  dbConnection="mongodb://127.0.0.1:27017/"
                  isDefault="true" />

        <database dbType="MongoDB"
                  dbName="log_data"
                  dbConnection="mongodb://127.0.0.1:27017/" />
      </world>
    </worlds>

    <!-- machines、processes、scenes 等其他必需配置省略 -->
  </server>
</fantasy>
```

**参数说明：**

| 参数 | 说明 | 示例 |
|------|------|------|
| `dbType` | 数据库类型；MongoDB 可写 `MongoDB` 或 `Mongo` | `MongoDB` |
| `dbName` | 数据库名称，同一 World 内不能重复；代码按此字符串取库 | `game_data` |
| `dbConnection` | MongoDB 连接字符串；为空时不会创建数据库连接 | `mongodb://127.0.0.1:27017/` |
| `isDefault` | 是否为当前 World 的默认数据库，默认 `false` | `true` |

纯本地模式可省略 `world` 的 `namespaceId` 和 `groupId`；允许从 Control Center 回退到本地时，两者必须配置为大于 `0`。要实际使用数据库，目标配置的 `dbConnection` 必须非空且可连接。

默认数据库的选择规则：

- 有一个或多个 `isDefault="true"` 时，使用第一个被标记的数据库
- 都未标记时，使用第一条 `<database>`
- `Scene.World.Database` 指向这个默认数据库

当前版本不再生成 `DatabaseName.g.cs`，非默认数据库直接使用 `dbName` 字符串获取。启用 Control Center 且远程配置加载成功时，World 和 Database 配置来自 Control Center；只有本地模式或回退本地配置时才读取这里的 `<server>`。

---

## 获取数据库实例

### 1. 获取默认数据库

```csharp
// 获取 isDefault 指定的数据库；未指定时获取第一条配置
var database = scene.World.Database;
```

### 2. 按名称获取数据库

```csharp
// 推荐：未找到或未初始化时返回 false
if (scene.World.TryGetDatabase("log_data", out var logDatabase))
{
    await logDatabase.Save(log);
}

// 也可以使用索引器；未找到时返回 null 并记录错误日志
var gameDatabase = scene.World["game_data"];
```

`TryGetDatabase` 返回 `IDatabase`，不再使用泛型参数；按名称取库也不会改变 `Scene.World.Database` 指向的默认库。

### 3. 获取原生 MongoDB 实例

```csharp
// 获取原生 IMongoDatabase 对象，用于高级操作
if (database is MongoDatabase)
{
    var mongoDatabase = (IMongoDatabase)database.GetDatabaseInstance;
}
```

---

## Entity 持久化标记

### ISupportedSerialize 接口

实体需要实现 `ISupportedSerialize` 接口才能被保存到数据库或支持序列化：

```csharp
// 定义可持久化的实体
public sealed class Player : Entity, ISupportedSerialize
{
    public string Name { get; set; }
    public int Level { get; set; }
    public long Gold { get; set; }
}

// 多实例 + 数据库持久化
public sealed class ItemComponent : Entity, ISupportedSerialize
{
    public int ItemId { get; set; }
    public int Count { get; set; }
}
```

**接口作用：**
- 标记实体支持数据库持久化
- Source Generator 会自动生成相关注册代码
- 运行时无反射开销，支持 Native AOT

---

## 基础操作

### 保存 (Save)

#### 1. 保存单个实体

```csharp
// 创建实体
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
player.Name = "玩家001";
player.Level = 10;
player.Gold = 1000;

// 保存到数据库（如果存在则更新，不存在则插入）
await database.Save(player);

// 保存到指定集合
await database.Save(player, "players_backup");
```

#### 2. 保存多个拥有相同 Id 实体

```csharp
var inventoryComponent = Entity.Create<InventoryComponent>(scene, isPool: true, isRunEvent: true);
var equipmentComponent = Entity.Create<EquipmentComponent>(scene, isPool: true, isRunEvent: true);

// 保存主实体及其所有组件
var entities = new List<(Entity, string)>
{
    // 元组中第一个为要保存实体的实体
  	// 第二个参数为要保存数据中的名字
    (inventoryComponent, "InventoryComponent"),
    (equipmentComponent, "EquipmentComponent")
};
await database.Save(player.Id, entities);
```

### 插入 (Insert)

#### 1. 插入单个实体

```csharp
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
player.Name = "新玩家";

// 插入到数据库（如果已存在会报错）
await database.Insert(player);
```

#### 2. 批量插入

```csharp
var players = new List<Player>();
for (int i = 0; i < 100; i++)
{
    var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
    player.Name = $"玩家{i:D3}";
    player.Level = i;
    players.Add(player);
}

// 批量插入（性能更高）
await database.InsertBatch(players);
```

### 查询 (Query)

#### 1. 按 ID 查询

```csharp
// 查询指定 ID 的实体
var player = await database.Query<Player>(playerId);

// 查询并自动反序列化（注册到框架系统）
var player = await database.Query<Player>(playerId, isDeserialize: true);

// 查询时不加锁（性能更高，但需确保线程安全）
var player = await database.QueryNotLock<Player>(playerId);
```

#### 2. 条件查询

```csharp
// 查询所有 10 级以上的玩家
var players = await database.Query<Player>(p => p.Level > 10);

// 查询指定等级范围的玩家
var players = await database.Query<Player>(
    p => p.Level >= 10 && p.Level <= 20
);

// 查询第一个符合条件的玩家
var player = await database.First<Player>(p => p.Name == "玩家001");
```

#### 3. 分页查询

```csharp
// 查询第 2 页，每页 20 条
int pageIndex = 2;
int pageSize = 20;
var players = await database.QueryByPage<Player>(
    filter: p => p.Level > 0,
    pageIndex: pageIndex,
    pageSize: pageSize
);

// 查询数量和数据
var (count, players) = await database.QueryCountAndDatesByPage<Player>(
    filter: p => p.Level > 0,
    pageIndex: 1,
    pageSize: 50
);
Log.Info($"总数: {count}, 当前页: {players.Count}");
```

#### 4. 排序查询

```csharp
// 按等级升序查询
var players = await database.QueryOrderBy<Player>(
    filter: p => p.Level > 0,
    orderByExpression: p => p.Level,
    isAsc: true
);

// 按金币降序分页查询
var richPlayers = await database.QueryByPageOrderBy<Player>(
    filter: p => p.Gold > 0,
    pageIndex: 1,
    pageSize: 10,
    orderByExpression: p => p.Gold,
    isAsc: false
);
```

#### 5. 指定字段查询

```csharp
// 只查询 Name 和 Level 字段（减少数据传输）
var players = await database.Query<Player>(
    filter: p => p.Level > 10,
    cols: new[] { "Name", "Level" }
);

// JSON 查询（高级用法）
string json = "{\"Level\": {\"$gt\": 10}}";
var players = await database.QueryJson<Player>(json);
```

### 删除 (Remove)

#### 1. 按 ID 删除

```csharp
// 删除指定 ID 的实体
long deletedCount = await database.Remove<Player>(playerId);
Log.Info($"删除了 {deletedCount} 条记录");
```

#### 2. 条件删除

```csharp
// 删除所有 1 级玩家
long coroutineLockKey = RandomHelper.RandInt64();
long deletedCount = await database.Remove<Player>(
    coroutineLockKey,
    filter: p => p.Level == 1
);
```

### 统计和聚合

#### 1. 统计数量

```csharp
// 统计所有玩家数量
long totalPlayers = await database.Count<Player>();

// 统计满足条件的玩家数量
long highLevelPlayers = await database.Count<Player>(p => p.Level >= 50);
```

#### 2. 检查是否存在

```csharp
// 检查是否存在指定名称的玩家
bool exists = await database.Exist<Player>(p => p.Name == "玩家001");
```

#### 3. 聚合计算

```csharp
// 计算所有玩家的总金币
long totalGold = await database.Sum<Player>(
    filter: p => p.Level > 0,
    sumExpression: p => p.Gold
);
Log.Info($"游戏内总金币: {totalGold}");
```

---

## 高级特性

### 数据库锁机制

按 ID 查询、保存、插入和删除等操作会在 `MongoDatabase` 内部自动加锁。`QueryNotLock` 只适用于调用方已经保证并发安全的场景。

“查询 → 修改 → 保存”是多个数据库操作，内部锁不会覆盖整个业务流程；并发修改同一实体时，应让所有写入路径使用相同的锁职责和实体 ID：

```csharp
var lockDuty = typeof(Player).TypeHandle.Value.ToInt64();
using (await scene.CoroutineLockComponent.Wait(lockDuty, playerId))
{
    var player = await database.QueryNotLock<Player>(playerId);
    if (player == null)
    {
        return;
    }

    player.Gold += 100;
    await database.Save(player);
}
```

### 反序列化参数

查询时可选择是否反序列化实体：

```csharp
// 不反序列化：仅数据对象，不注册到框架系统
var player = await database.Query<Player>(playerId, isDeserialize: false);
// player 此时只是一个数据容器，不能使用组件功能

// 反序列化：完整的框架实体
var player = await database.Query<Player>(playerId, isDeserialize: true);
// player 已注册到 Scene，可以使用 AddComponent、GetComponent 等功能
player.AddComponent<InventoryComponent>();
```

**使用建议：**
- 只读数据展示：`isDeserialize: false`（性能更高）
- 需要修改或使用组件：`isDeserialize: true`

### 索引管理

#### 1. 创建索引

```csharp
// 创建单字段索引
await database.CreateIndex<Player>(
    Builders<Player>.IndexKeys.Ascending(p => p.Name)
);

// 创建复合索引
await database.CreateIndex<Player>(
    Builders<Player>.IndexKeys
        .Ascending(p => p.Level)
        .Descending(p => p.Gold)
);

// 创建唯一索引（需要传入 options，与 keys 一一对应）
await database.CreateIndex<Player>(
    keys: new object[]
    {
        Builders<Player>.IndexKeys.Ascending(p => p.Account)
    },
    options: new object[]
    {
        new CreateIndexOptions { Unique = true }
    }
);

// 在指定集合上创建索引
await database.CreateIndex<Player>(
    collection: "players_backup",
    Builders<Player>.IndexKeys.Ascending(p => p.Name)
);
```

#### 2. 创建数据库集合

```csharp
// 手动创建集合（通常不需要，保存时会自动创建）
await database.CreateDB<Player>();
await database.CreateDB(typeof(ItemComponent));
```

### 自定义 MongoDB 客户端

如果需要自定义 MongoDB 连接参数：

```csharp
// 必须在 Scene/World 初始化数据库之前设置
Initializer.MongoDbCustomInitialize = config =>
{
    var settings = MongoClientSettings.FromConnectionString(config.ConnectionString);

    settings.MaxConnectionPoolSize = 500;
    settings.MinConnectionPoolSize = 10;
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
    settings.ConnectTimeout = TimeSpan.FromSeconds(10);

    return new MongoClient(settings);
};
```

---

## 最佳实践

### 1. 实体设计

```csharp
// ✅ 推荐：清晰的实体定义
public sealed class Player : Entity, ISupportedSerialize
{
    // 使用属性而非字段
    public string Name { get; set; }
    public int Level { get; set; }
    public long Gold { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

// ❌ 不推荐：在实体中包含大量嵌套数据
public sealed class Player : Entity, ISupportedSerialize
{
    public List<Item> Items { get; set; }  // 应该拆分为 ItemComponent
    public Dictionary<int, Buff> Buffs { get; set; }  // 应该拆分为 BuffComponent
}
```

### 2. 查询优化

```csharp
// ✅ 推荐：只查询需要的字段
var players = await database.Query<Player>(
    filter: p => p.Level > 10,
    cols: new[] { "Name", "Level" }  // 只加载姓名和等级
);

// ✅ 推荐：使用分页避免一次性加载大量数据
var (count, players) = await database.QueryCountAndDatesByPage<Player>(
    filter: p => p.Level > 0,
    pageIndex: 1,
    pageSize: 50
);

// ❌ 不推荐：一次性查询所有数据
var allPlayers = await database.Query<Player>(p => true);
```

### 3. 保存策略

```csharp
// ✅ 推荐：定期批量保存
var dirtyPlayers = new List<Player>();
// ... 收集需要保存的玩家

foreach (var player in dirtyPlayers)
{
    await database.Save(player);
}

// ✅ 推荐：关键数据立即保存
player.Gold += 100;
await database.Save(player);  // 金币变化立即保存

// ❌ 不推荐：频繁保存非关键数据
player.LastActivityTime = DateTime.UtcNow;
await database.Save(player);  // 每次活动都保存，性能开销大
```

### 4. 索引创建

```csharp
// 在 Scene 创建时创建索引
public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;
        var database = scene.World.Database;

        // 创建常用查询的索引
        await database.CreateIndex<Player>(
            Builders<Player>.IndexKeys.Ascending(p => p.Name)
        );

        await database.CreateIndex<Player>(
            Builders<Player>.IndexKeys
                .Ascending(p => p.Level)
                .Descending(p => p.Gold)
        );
    }
}
```

### 5. 异常处理

```csharp
// ✅ 推荐：捕获数据库异常
try
{
    var player = await database.Query<Player>(playerId, isDeserialize: true);
    if (player == null)
    {
        Log.Warning($"玩家不存在: {playerId}");
        return;
    }

    player.Gold += 100;
    await database.Save(player);
}
catch (Exception e)
{
    Log.Error($"保存玩家数据失败: {playerId}\n{e}");
    // 回滚操作或通知客户端
}
```

---

## 常见问题

### 1. 查询返回 null？

**原因：**
- 数据库中不存在该 ID 的实体
- 集合名称错误

**解决方法：**
```csharp
var player = await database.Query<Player>(playerId);
if (player == null)
{
    Log.Warning($"玩家不存在: {playerId}");
    return;
}
```

### 2. 保存后数据没有更新？

**原因：**
- 没有等待 `await` 完成
- 使用了错误的集合名称

**解决方法：**
```csharp
// ✅ 正确：等待保存完成
await database.Save(player);

// ❌ 错误：忘记 await
database.Save(player);  // 异步操作未完成
```

### 3. 如何处理并发修改？

**使用协程锁：**
```csharp
var lockDuty = typeof(Player).TypeHandle.Value.ToInt64();
using (await scene.CoroutineLockComponent.Wait(lockDuty, playerId))
{
    var player = await database.QueryNotLock<Player>(playerId);
    if (player == null)
    {
        return;
    }

    player.Gold += 100;
    await database.Save(player);
}
```

### 4. 查询性能慢？

**优化建议：**
- 创建索引
- 只查询需要的字段
- 使用分页查询
- 合理使用 `isDeserialize: false`

### 5. 如何在不同数据库之间切换？

```csharp
var gameDb = scene.World.Database;

if (!scene.World.TryGetDatabase("log_data", out var logDb))
{
    Log.Warning("log_data 数据库不存在或未初始化");
    return;
}
```

按名称获取数据库不会修改默认库；后续继续分别使用 `gameDb` 和 `logDb` 即可。

### 6. 如何进行数据迁移？

```csharp
// 查询所有旧数据
var oldPlayers = await database.Query<OldPlayer>(p => true);

// 转换为新格式
var newPlayers = new List<Player>();
foreach (var oldPlayer in oldPlayers)
{
    var newPlayer = Entity.Create<Player>(scene, oldPlayer.Id, isPool: true, isRunEvent: false);
    newPlayer.Name = oldPlayer.Name;
    newPlayer.Level = oldPlayer.Level;
    newPlayers.Add(newPlayer);
}

// 批量插入新数据
await database.InsertBatch(newPlayers);
```

---

## 相关文档

- [Fantasy.config 配置文件详解](../../01-Server/01-ServerConfiguration.md) - 数据库连接配置
- [Entity-Component-System 详解](../CoreSystems/01-ECS.md) - Entity 基础知识
- [ISupportedMultiEntity 详解](../CoreSystems/02-ISupportedMultiEntity.md) - 多实例组件
- [Scene 使用指南](../CoreSystems/03-Scene.md) - Scene 和数据库访问
- [SeparateTable 分表存储详解](16-SeparateTable.md) - 复杂聚合实体的优化存储方案

---

## 总结

Fantasy Framework 的 MongoDB 集成提供了：

- ✅ **简单易用**：统一的 `IDatabase` 接口
- ✅ **类型安全**：泛型方法和 LINQ 表达式
- ✅ **高性能**：自动协程锁、对象池、BSON 序列化
- ✅ **灵活扩展**：支持自定义 MongoDB 客户端配置
- ✅ **完整功能**：支持保存、查询、删除、聚合等所有常用操作

通过合理使用这些特性，可以轻松实现高性能、可扩展的游戏数据持久化方案。对于复杂的聚合实体，请参考 [SeparateTable 分表存储详解](16-SeparateTable.md)。
