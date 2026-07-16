# Fantasy.config 常见场景

## 场景 1：新建项目的最小配置

最少需要这些部分：

- 一个 `machine`
- 一个 `process`
- 一个 `world`
- 至少一个 `scene`

直接从 `templates/Fantasy.config` 复制最小可运行示例，再按项目实际端口和 SceneType 修改。

## 场景 2：新增机器或进程

规则：

- 新 `machine.id` 不能重复
- `process.machineId` 必须引用已存在的 `machine.id`
- `startupGroup` 决定多进程启动顺序

## 场景 3：新增 World 或修改数据库

`<database>` 是挂在 `<world>` 下面的配置，不是挂在 `<scene>` 下面。

这意味着：

- 你是在给某个 `world` 配置数据库能力
- 绑定到该 `world` 的 Scene，运行时通过 `scene.World.Database` 或 `scene.World[...]` 使用这些数据库
- 如果一个 `world` 配了多个 `<database>`，代码里可以切默认库，也可以按名字常量切指定库

规则：

- `world.id` 不能重复
- 一个 `world` 下可配置多个 `<database>`
- `dbName` 在同一 `world` 内不能重复
- 未显式设置默认库时，默认使用第一条数据库配置

代码使用方式见 `references/database/index.md` 和 `references/database/mongodb.md`。

## 场景 4：新增或删除 Scene

这里的 `<scene>` 不是纯静态配置项。每一条 `<scene>` 配置在服务器启动后都会创建一个对应的运行时 Root Scene。

这意味着：

- 新增一个 `<scene>`，本质上是在新增一个要运行的服务器 Scene
- 删除一个 `<scene>`，本质上是在移除一个启动时不会再创建的 Scene
- `sceneTypeString` 不只是名字，它会影响 `OnCreateScene` 等运行时初始化逻辑

规则：

- `scene.processConfigId` 必须引用已存在的 `process.id`
- `scene.worldConfigId` 必须引用已存在的 `world.id`
- `scene.innerPort` 不能为 0
- `outerPort > 0` 时必须填写 `networkProtocol`

常见 `sceneTypeString` 例子：

- `Gate`
- `Map`
- `Chat`
- `Addressable`
- `HttpGift`

如果用户同时在问“配置怎么写”和“这个 Scene 运行起来后该加什么组件”，要把 `Fantasy.config` 和 `references/ecs/scene.md` 一起看：

- `Fantasy.config` 决定会启动哪些 Root Scene
- `OnCreateScene` / Scene 代码决定这些 Root Scene 启动后各自加载什么运行时能力

## 场景 5：修改端口或网络协议

规则：

- 同一机器上 `innerPort` / `outerPort` 不能互相冲突
- 客户端接入 Scene 常见 `networkProtocol`：`KCP` / `TCP` / `WebSocket` / `HTTP`
- 纯内网 Scene 可让 `outerPort="0"`，同时 `networkProtocol` 留空或省略

## 场景 6：配置多区服

做法：

- 新增多个 `world`
- 每个 `scene` 绑定到对应的 `worldConfigId`
- 如需避免合区时 ID 冲突，考虑把 `<idFactory type="World" />` 打开

## 场景 7：启用 Control Center 和服务发现

在根节点增加 `<controlCenter>`，并把所有编译期 Scene 类型放进 `<sceneTypes>`。

规则：

- `url` 不包含 `/api/v1`
- `sceneType.id` 稳定且唯一
- `sceneType.name` 与 Control Center 完全一致
- `heartbeatIntervalSeconds` 小于 `leaseSeconds`
- 不允许本地回退时保留空 `<server />` 即可

完整流程和 API 见 `service-discovery/index.md`。

## 场景 8：动态增加 Scene

先区分“实例”和“类型”：

- 新增一个已有 Map 类型的 Scene 实例：只在 Control Center 新增 Scene，不改 `sceneTypes`，不重新编译。
- 新增全新的 Match SceneType：在 Control Center 建立类型，同时向 `sceneTypes` 增加稳定 ID 和名称，然后重新编译源生成常量。

## 场景 9：多台服务器分别启动不同 Scene

在 Control Center 中让不同 Process 绑定不同 Machine 和 Scene。每台服务器使用 Release 模式指定自己的 Process：

```bash
dotnet YourServer.dll -m Release --pid 2
```

服务器只创建该 Process 所需的 Scene，启动完成后自动注册当前进程实例。

## 场景 10：允许 Control Center 失败后本地回退

设置 `fallbackToLocal="true"`，并保留完整 `<server>`。额外要求：

- Process 配置非零 `namespaceId`
- World 同时配置非零 `namespaceId` 和 `groupId`
- Scene 所属 Process 和 World 位于同一 Namespace
- 本地 SceneType 与 `<sceneTypes>` 一致

本地回退不是另一套隔离规则；它必须与 Control Center 拓扑保持相同语义。

## 场景 11：检查配置为什么启动失败

优先检查：

1. 是否还在使用 `<configTable>`
2. `id` 是否有 0 或重复
3. 引用关系是否正确：`machineId` / `processConfigId` / `worldConfigId`
4. `innerPort` 是否为 0 或端口冲突
5. `outerPort > 0` 时是否漏填 `networkProtocol`
6. `idFactory type="World"` 时 ID 范围是否越界
7. Control Center URL、`sceneTypes`、心跳和租约是否合法
8. 本地回退是否缺少 Namespace / WorldGroup ID
9. 当前 Process ID 是否存在并绑定了 Scene
