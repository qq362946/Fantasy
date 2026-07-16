# Fantasy Control Center

Fantasy Control Center 是独立运行的服务控制面，用来管理 `Namespace`、`Machine`、`Process`、`WorldGroup`、`World`、`Scene` 拓扑以及运行时服务实例。

## 本地运行

```bash
dotnet run --project Fantasy.Packages/Fantasy.ControlCenter/Fantasy.ControlCenter.csproj
```

默认管理地址为 `http://127.0.0.1:5277`。首次启动会在程序目录下创建 `data/fantasy-control.db`。

可以通过环境变量修改数据目录：

```bash
ControlCenter__DataDirectory=/var/lib/fantasy-control dotnet Fantasy.ControlCenter.dll
```

## 当前能力

- SqlSugar + SQLite 本地持久化及 WAL 模式
- 配置启动时一次加载，修改后增量构建并原子切换内存快照；不会重新查询数据库
- 服务注册、单实例/批量心跳和轻量发现使用内存索引，不访问数据库
- Namespace、Machine、Process、WorldGroup、World、World Database、Scene 拓扑管理
- 每个 World 支持多个数据库、默认库和连接字符串配置
- 配置 Revision 自动递增
- Scene 实例注册、心跳、摘除
- 按 `Namespace` 隔离服务，并支持按 `SceneType`、`WorldGroupId` 或 `WorldId` 查询健康实例
- 同一机器上的 Scene 端口冲突校验

拓扑配置会持久化。服务实例属于运行时租约状态，不持久化；控制中心重启后，各服务按正常流程重新注册。

当前开发版使用 SQLite Schema V4。检测到不兼容的旧 Schema 时会自动清空并重建数据库。

## API

- `GET /api/v1/health`
- `GET /api/v1/topology`
- `GET /api/v1/runtime/config`
- `GET /api/v1/namespaces`
- `PUT /api/v1/namespaces/{id}`
- `DELETE /api/v1/namespaces/{id}`
- `GET /api/v1/worlds/{worldId}/databases`
- `POST /api/v1/worlds/{worldId}/databases`
- `POST /api/v1/instances/register`
- `POST /api/v1/instances/heartbeat`
- `POST /api/v1/instances/heartbeat/batch`
- `POST /api/v1/instances/{instanceId}/offline`
- `GET /api/v1/discovery/scenes?sceneType=Map&namespaceId=1&worldId=1`

## 发布

先使用自包含目录方式按目标平台发布：

```bash
dotnet publish Fantasy.Packages/Fantasy.ControlCenter/Fantasy.ControlCenter.csproj \
  -c Release -r linux-x64 --self-contained true
```

在增加远程访问认证之前，控制中心应当只监听环回地址或可信内网。
