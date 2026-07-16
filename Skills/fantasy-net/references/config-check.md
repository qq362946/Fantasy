# Fantasy.config 审查清单

**本文件用于检查 Fantasy.config 是否正确。**

## 检查顺序

1. 当前使用本地 `<server>`，还是启用 Control Center
2. 是否仍在使用 `<configTable>`
3. machine / process / world / scene / database 引用关系是否正确
4. Namespace / WorldGroup 隔离关系是否正确
5. 端口和协议是否匹配
6. World 模式 ID 范围是否越界
7. 配置和运行时语义是否一致

## 常见问题

### 错误 1：仍在使用 `<configTable>`

当前统一使用 `<server>` 方式。

### 错误 2：ID 为 0 或重复

检查：

- `machine.id`
- `process.id`
- `world.id`
- `scene.id`
- `sceneType.id`

### 错误 3：引用关系断裂

重点检查：

- `process.machineId`
- `scene.processConfigId`
- `scene.worldConfigId`

### 错误 4：端口和协议不匹配

- `innerPort` 不能为 0
- `outerPort > 0` 时必须配置 `networkProtocol`
- 同一机器上端口不能冲突

### 错误 5：`<database>` 放错位置

数据库配置挂在 `<world>` 下，不是 `<scene>` 下。

### 错误 6：`idFactory type="World"` 时 ID 越界

此时 `worldId` 和 `sceneId` 范围要满足约束。

### 错误 7：存在 world / database / scene 的悬空配置

重点检查：

- 配了 `world` 但没有任何 `scene` 绑定到它
- 配了 `<database>` 但当前没有任何 Scene 会通过该 `world` 使用它
- 这类配置如果只是预留，应明确注释；否则容易误导后续维护者

### 错误 8：启用 Control Center 后仍把本地 Scene 当作生效配置

Control Center 加载成功后，运行时拓扑来自 Control Center，本地 `<server>` 不参与本次启动。只有远程加载失败且 `fallbackToLocal="true"` 时才使用本地配置。

### 错误 9：`sceneTypes` 缺失或不稳定

检查：

- `controlCenter.enabled="true"` 时存在至少一个 `sceneType`
- ID 大于 0、不能重复，发布后不修改或复用
- 名称与 Control Center 中的 SceneType 完全一致
- 名称满足 `[A-Z][A-Za-z0-9_]*`

### 错误 10：本地回退缺少隔离 ID

`fallbackToLocal="true"` 时检查：

- `process.namespaceId > 0`
- `world.namespaceId > 0`
- `world.groupId > 0`
- Scene 所属 Process 与 World 的 Namespace 一致
- World 的 NamespaceId 和 GroupId 要么都为 0，要么都大于 0；回退场景必须都大于 0

### 错误 11：心跳和租约无效

- `heartbeatIntervalSeconds > 0`
- `5 <= leaseSeconds <= 300`
- `heartbeatIntervalSeconds < leaseSeconds`
- 通常让租约至少为心跳间隔的 3 倍

### 错误 12：World 的数据库配置不完整

- 每个 World 至少配置一个 `<database>`
- `dbName` 在同一 World 内不重复
- 最多一个数据库设置 `isDefault="true"`
- `dbConnection` 可为空，但空字符串表示保留名称而不建立连接

## 审查时重点问自己

1. 配置是否能正确推导出运行时 Scene 和 World 结构
2. 端口和网络协议是否可实际启动
3. world 和 database 的关系是否清晰
4. 是否存在“看起来已启用、其实没有任何运行时场景使用”的悬空配置
5. 配置来源、Namespace 和 WorldGroup 隔离是否与实际部署一致
6. 新增的是已有 SceneType 的实例，还是需要重新生成常量的全新 SceneType

## 相关文档

- `config.md`
- `config-scenarios.md`
- `service-discovery/index.md`
- `service-discovery/service-discovery-check.md`
- `templates/Fantasy.config`
