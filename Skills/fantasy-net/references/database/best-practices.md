# MongoDB 最佳实践与排错

## 配置关联

### 配置写在哪

数据库配置写在 `Fantasy.config` 的 `<world>` 下面：

```xml
<world id="1" worldName="World1">
  <database dbType="MongoDB" dbName="game_data" dbConnection="mongodb://localhost:27017/" />
</world>
```

不要把 `<database>` 写到 `<scene>` 下面。

### 配置和运行时怎么对应

- `world` 持有数据库配置
- 归属于该 `world` 的 `scene` 通过 `scene.World` 访问数据库
- 所以“配置数据库”本质上是在给某个 `world` 配数据库能力

## 最佳实践

1. 需要持久化的 Entity 明确实现 `ISupportedSerialize`
2. 只查展示数据时优先用 `isDeserialize: false`
3. 大量数据查询优先用分页和指定字段
4. 高频查询字段提前建索引
5. 关键数据变更立即保存，非关键字段避免过度频繁保存
6. 修改同一玩家或同一记录时优先配合协程锁
7. 如果聚合实体子数据过大，再考虑 `SeparateTable`，不要一开始就过度拆分

## 查询优化

### 只查需要的字段

```csharp
var players = await database.Query<Player>(
    filter: p => p.Level > 10,
    cols: new[] { "Name", "Level" }
);
```

### 使用分页

```csharp
var (count, players) = await database.QueryCountAndDatesByPage<Player>(
    filter: p => p.Level > 0,
    pageIndex: 1,
    pageSize: 50
);
```

### 不要一次性查全表

```csharp
// 不推荐
var allPlayers = await database.Query<Player>(p => true);
```

## 保存策略

- 关键数据：立即保存
- 非关键刷新型数据：批量或延迟保存
- 日志型数据：可单独放到另一个 `<database>`

## 常见问题

### 查询返回 null

优先检查：

1. 数据是否真的存在
2. 查询的 ID 是否正确
3. 是否用了错误的集合名

### 保存后看起来没更新

优先检查：

1. 是否漏了 `await`
2. 是否把数据保存到了错误的集合
3. 是否读的是另一个数据库实例

### 查询性能慢

优先检查：

1. 是否缺少索引
2. 是否没做分页
3. 是否本来只读却用了 `isDeserialize: true`
4. 是否一次性加载了太多字段
5. 是否其实已经适合把重型子数据拆到 `SeparateTable`

### 如何在多个数据库之间切换

```csharp
var gameDb = scene.World[DatabaseName.game_data];
var logDb = scene.World[DatabaseName.log_data];
```

## 什么时候进一步看 SeparateTable

如果用户遇到的是“一个聚合实体包含大量子数据，保存和查询成本太高”的问题，再考虑 SeparateTable 方案，而不是一开始就上更复杂的存储拆分。

进一步看：`separate-table.md`
