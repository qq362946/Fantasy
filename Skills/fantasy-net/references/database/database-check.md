# 数据库审查清单

**本文件用于检查数据库访问代码。**

## 检查顺序

1. 数据库是否通过 `scene.World` 获取
2. 实体是否具备持久化能力
3. 查询方式和 `isDeserialize` 是否合适
4. 并发修改是否有协程锁
5. 是否已经适合 SeparateTable

## 常见问题

### 错误 1：不通过 `scene.World` 访问数据库

运行时应优先：

- `scene.World.Database`
- `scene.World.SelectDatabase(...)`
- `scene.World[...]`

### 错误 2：需要持久化的 Entity 没实现 `ISupportedSerialize`

### 错误 3：只读查询却用了 `isDeserialize: true`

如果只是展示数据，优先 `isDeserialize: false`。

### 错误 4：并发修改同一条数据却没加协程锁

特别注意这些“先查再建 / 先查再写”的路径：

- `LoadOrCreate`
- 账号注册
- 角色创建
- 任何按唯一字段先 `First/Exist` 再 `Save/Insert` 的逻辑

即使数据库唯一索引能兜底“最终不重复”，也不能替代业务层协程锁。

### 错误 5：主实体过重却还在硬撑单表

如果一个聚合实体子数据过大，应评估 SeparateTable。

### 错误 6：首次创建后过早保存，导致关键字段没有一起落盘

常见坏味道：

- 先创建实体并 `Save()`
- 再补 `LastLoginTime`、状态字段、初始运行数据

这会导致第一次落库的数据不完整。优先在首次保存前把本次需要持久化的关键字段填完整。

## 审查时重点问自己

1. 数据库配置和运行时访问是否匹配
2. 查询方式是否过重
3. 保存频率是否合理
4. 是否有并发写风险
5. 是否应该升级为 SeparateTable
6. 是否存在“先查后建”但没加锁的路径
7. 首次保存前关键字段是否已经准备完整

## 相关文档

- `index.md`
- `mongodb.md`
- `separate-table.md`
- `best-practices.md`
