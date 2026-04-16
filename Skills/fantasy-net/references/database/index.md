# 数据库入口

Fantasy 通过 `IDatabase` 接口提供统一数据库访问能力，当前常见场景是内置的 MongoDB 集成。

数据库配置写在 `Fantasy.config` 的 `<world>` 节点下面；运行时通过 `scene.World.Database` 或 `scene.World.SelectDatabase(...)` 获取。

## 配置和运行时的关系

可以这样理解：

```text
Fantasy.config
  <world>
    <database ... />
    <database ... />

        ↓

该 World 启动后持有这些数据库配置
        ↓

属于这个 World 的 Scene 运行时通过 scene.World.Database / scene.World[...] 访问数据库
```

这意味着：

- `<database>` 不是全局随便挂的配置，它属于某个 `world`
- 绑定到该 `world` 的所有 `scene`，运行时都通过各自的 `scene.World` 访问这组数据库
- 如果一个 `world` 配了多个 `<database>`，代码里需要明确选择默认库还是指定库

## Workflow

```text
配置 Fantasy.config 里的 <world><database> -> config.md 和 config-scenarios.md
在代码里获取数据库并做 CRUD -> mongodb.md
聚合实体太大，需要把子组件拆到独立集合 -> separate-table.md
需要优化查询、索引、保存策略或排查问题 -> best-practices.md
```

## 必记规则

1. 数据库配置写在 `<world>` 下面，不是写在 `<scene>` 下面
2. 运行时从 `scene.World` 取数据库，不是从 `scene` 直接 new 数据库客户端
3. 需要持久化的 Entity 必须实现 `ISupportedSerialize`
4. 只读展示优先考虑 `isDeserialize: false`，需要回到框架实体体系再用 `isDeserialize: true`
5. 并发修改同一条数据时，优先结合协程锁处理

## 子文档

- `mongodb.md` - MongoDB 配置、获取实例、增删改查、索引、并发修改
- `separate-table.md` - 聚合实体分表存储、加载和保存
- `best-practices.md` - 最佳实践、配置关联、性能和排错
