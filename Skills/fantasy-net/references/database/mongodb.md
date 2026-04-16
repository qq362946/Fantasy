# MongoDB 使用

## 第 1 步：在 Fantasy.config 中配置数据库

MongoDB 配置放在 `<world>` 节点下面：

```xml
<world id="1" worldName="GameWorld">
  <database dbType="MongoDB"
            dbName="game_data"
            dbConnection="mongodb://localhost:27017/" />

  <database dbType="MongoDB"
            dbName="log_data"
            dbConnection="mongodb://localhost:27017/" />
</world>
```

关键点：

- `dbType` 当前常见用法为 `MongoDB`
- `dbName` 是代码里选择数据库的名字
- `dbConnection` 是 MongoDB 连接串
- 同一个 `world` 下可以配置多个 `<database>`

配置层关系详见：`references/config.md`。

## 第 2 步：获取数据库实例

### 默认数据库

```csharp
var database = scene.World.Database;
```

### 指定数据库

```csharp
var logDatabase = scene.World.SelectDatabase(DatabaseName.log_data);
var gameDb = scene.World[DatabaseName.game_data];
```

### 类型安全获取

```csharp
if (scene.World.TryGetDatabase<MongoDatabase>(DatabaseName.game_data, out var mongoDb))
{
    // 使用 mongoDb
}
```

### 获取原生 MongoDB 实例

```csharp
var mongoDatabase = (IMongoDatabase)database.GetDatabaseInstance;
```

## 第 3 步：让 Entity 支持持久化

需要持久化的 Entity 实现 `ISupportedSerialize`：

```csharp
public sealed class Player : Entity, ISupportedSerialize
{
    public string Name { get; set; }
    public int Level { get; set; }
    public long Gold { get; set; }
}
```

## 第 4 步：基础操作

### Save

```csharp
await database.Save(player);
await database.Save(player, collection: "players_backup");
```

### Insert

```csharp
await database.Insert(player);
await database.InsertBatch(players);
```

### Query by Id

```csharp
var player = await database.Query<Player>(playerId);
var player = await database.Query<Player>(playerId, isDeserialize: true);
var player = await database.QueryNotLock<Player>(playerId);
```

### 条件查询

```csharp
var players = await database.Query<Player>(p => p.Level > 10);
var player = await database.First<Player>(p => p.Name == "玩家001");
```

### 分页和排序

```csharp
var players = await database.QueryByPage<Player>(p => p.Level > 0, 2, 20);

var richPlayers = await database.QueryByPageOrderBy<Player>(
    filter: p => p.Gold > 0,
    pageIndex: 1,
    pageSize: 10,
    orderByExpression: p => p.Gold,
    isAsc: false
);
```

### 删除

```csharp
long deletedCount = await database.Remove<Player>(playerId);
```

### 统计和存在性检查

```csharp
long totalPlayers = await database.Count<Player>();
bool exists = await database.Exist<Player>(p => p.Name == "玩家001");
```

## 第 5 步：理解 `isDeserialize`

### 只读数据

```csharp
var player = await database.Query<Player>(playerId, isDeserialize: false);
```

适合：

- 展示数据
- 统计数据
- 不需要把对象注册回框架运行时

### 需要回到框架实体体系

```csharp
var player = await database.Query<Player>(playerId, isDeserialize: true);
player.AddComponent<InventoryComponent>();
```

适合：

- 需要继续操作 Entity / Component
- 需要把结果重新纳入 Scene 管理

## 第 6 步：并发修改同一条数据

修改同一玩家或同一记录时，优先结合协程锁：

```csharp
using (await scene.CoroutineLockComponent.Wait(playerId))
{
    var player = await database.QueryNotLock<Player>(playerId);
    player.Gold += 100;
    await database.Save(player);
}
```

## 第 7 步：索引

常用索引创建方式：

```csharp
await database.CreateIndex<Player>(
    Builders<Player>.IndexKeys.Ascending(p => p.Name)
);

await database.CreateIndex<Player>(
    Builders<Player>.IndexKeys
        .Ascending(p => p.Level)
        .Descending(p => p.Gold)
);
```

适合在 `OnCreateScene` 这类启动路径里做初始化索引。

## 相关文档

- `best-practices.md` - 查询优化、保存策略、排错
- `references/config.md` - `<world><database>` 配置关系
- `references/config-scenarios.md` - 新增 / 修改 world 和 database 的配置场景
