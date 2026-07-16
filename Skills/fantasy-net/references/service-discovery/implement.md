# 服务发现接入与 API

## 第 1 步：启用 Control Center

在 `Fantasy.config` 根节点中、`<server>` 之前增加：

```xml
<controlCenter enabled="true"
               url="http://127.0.0.1:5000"
               fallbackToLocal="false"
               heartbeatIntervalSeconds="5"
               leaseSeconds="15">
  <sceneTypes>
    <sceneType id="1" name="Gate" />
    <sceneType id="2" name="Map" />
    <sceneType id="3" name="Auth" />
  </sceneTypes>
</controlCenter>

<!-- XSD 要求 server 节点始终存在 -->
<server />
```

配置规则：

- `url` 只填 Control Center 根地址，不要追加 `/api/v1`。
- `heartbeatIntervalSeconds` 必须大于 0 且小于 `leaseSeconds`。
- `leaseSeconds` 范围是 5～300 秒；通常至少设为心跳间隔的 3 倍。
- `fallbackToLocal="false"` 时可以使用空 `<server />`。
- `fallbackToLocal="true"` 时，`<server>` 必须保留完整可启动配置；`process.namespaceId`、`world.namespaceId` 和 `world.groupId` 必须大于 0。
- 新增已有类型的 Scene 实例不需要重新编译；增加全新 SceneType 时才增加声明并重新编译。

源生成器根据 `sceneTypes` 生成 `SceneType` 常量。不要修改 `.g.cs`，也不要把运行时新增实例误当成新增类型。

## 第 2 步：建立 Control Center 拓扑

按依赖顺序创建：

1. Namespace
2. Machine
3. Process
4. WorldGroup
5. World 和 Database
6. Scene

确认 Scene 的 Process、World、SceneType、内外网端口都正确。多台机器可以只启动分配给自己的 Process，无需在每台机器保留同一份 Scene 列表。

## 第 3 步：启动服务器

生产环境通常一个 OS 进程启动一个 Fantasy Process：

```bash
dotnet YourServer.dll -m Release --pid 1
```

本地开发可启动当前配置内全部 Process：

```bash
dotnet YourServer.dll -m Develop
```

Develop 模式下，同一个 OS 进程中的所有 Process 必须属于同一 Namespace。

服务器完成 Process 和 Root Scene 创建后，会自动完成注册、心跳和下线；运行时创建的 SubScene 也由框架管理。业务代码不要手动调用 Control Center 注册接口。

服务发现入口也是在所有 Process 和 Scene 创建完成后初始化。不要在 `OnCreateScene` Handler 中同步调用 `ServiceDiscovery`，也不要在该 Handler 里等待它就绪，否则会形成启动阶段错误或等待环。普通消息 Handler 可直接使用；需要启动即建立跨服订阅时，应放到项目明确的“服务启动完成”阶段。

## 第 4 步：查询在线实例

公共入口是 `Fantasy.Platform.Net.ServiceDiscovery`。建议使用别名避免名称冲突：

```csharp
using NetServiceDiscovery =
    Fantasy.Platform.Net.ServiceDiscovery;
```

获取当前 Namespace 中全部 Map：

```csharp
var endpoints =
    await NetServiceDiscovery.DiscoverAsync(SceneType.Map);
```

限定 World：

```csharp
var endpoints =
    await NetServiceDiscovery.DiscoverAsync(
        SceneType.Map,
        worldId: session.Scene.SceneConfig.WorldConfigId);
```

限定 WorldGroup：

```csharp
var endpoints =
    await NetServiceDiscovery.DiscoverAsync(
        SceneType.Map,
        worldGroupId: 1);
```

`DiscoverAsync` 提供 `int` 和 `string` 重载。固定 SceneType 使用源生成常量；只有类型来自外部数据时才使用字符串重载。

所有发现查询都会在发布快照前登记返回实例所属 Root Scene 的 Host / InnerPort。可以从 `DiscoverAsync` 列表中选择 `endpoint.Address`，再直接调用 `Scene.Send` 或 `Scene.Call`。

## 第 5 步：选择 Address 并通信

无状态服务随机选择一个在线实例：

```csharp
var address =
    await NetServiceDiscovery.DiscoverAddressAsync(
        SceneType.Map,
        worldId: session.Scene.SceneConfig.WorldConfigId);

if (address == 0)
{
    // 按项目约定设置“无在线服务”的业务错误码。
    return;
}

var result = await session.Scene.Call(
    address,
    new G2M_Request());
```

相同业务 Key 尽量稳定落到相同在线节点：

```csharp
var address =
    await NetServiceDiscovery.DiscoverAddressByHashAsync(
        SceneType.Auth,
        routingKey: accountId);
```

调用 Address 查询后，框架会登记该 Scene 的网络端点；后续 `Scene.Send` 和 `Scene.Call` 可按返回的 Address 建立内部连接。

## SubScene：创建后立即注册

SubScene 不需要单独配置到 Control Center 拓扑中。它在运行时创建，并自动挂到所属 Root Scene 实例下面：

```csharp
var dungeon = await Scene.CreateSubScene(
    mapScene,
    SceneType.Map,
    onSubSceneSetup: async (subScene, parentScene) =>
    {
        subScene.AddComponent<DungeonComponent>();
        await FTask.CompletedTask;
    });

var dungeonAddress = dungeon.Address;
```

顺序固定为：

1. 初始化 SubScene。
2. 执行 `onSubSceneSetup`。
3. 发布 `OnCreateScene`。
4. 执行 `onSubSceneCreated`。
5. 向 Control Center 立即注册 SubScene。
6. 完成 `CreateSubScene` 并返回。

SubScene 的 SceneType 必须存在于 `<controlCenter><sceneTypes>`。注册信息继承父 Root Scene 的 Namespace、WorldGroup、World、Process、Host 和 InnerPort；SubScene 只提供自己的 SceneType 与 Address。

### 查询父 Root Scene 下的子实例

```csharp
var endpoints =
    await NetServiceDiscovery.DiscoverSubScenesAsync(
        parentAddress: mapRootAddress,
        sceneType: SceneType.Map);
```

`parentAddress` 必须是父 Root Scene Address。查询只返回该父节点下、SceneType 匹配且在线的 SubScene。

随机选择：

```csharp
var address =
    await NetServiceDiscovery.DiscoverSubSceneAddressAsync(
        mapRootAddress,
        SceneType.Map);
```

Rendezvous Hash 选择：

```csharp
var address =
    await NetServiceDiscovery.DiscoverSubSceneAddressByHashAsync(
        mapRootAddress,
        SceneType.Map,
        routingKey: teamId);
```

三个 SubScene API 都有 `int` 和 `string` 重载；无实例时，列表查询返回空列表，两个 Address 查询返回 `0`。

### 拿到 SubScene Address 后直接通信

```csharp
scene.Send(dungeonAddress, new G2Dungeon_Notify());

var response = await scene.Call(
    dungeonAddress,
    new G2Dungeon_EnterRequest());
```

SubScene 自己没有独立监听端口。框架从 Address 提取 SceneId，并连接到它所属的 Root Scene。如果本地尚无该路由，`Scene.Send` / `Scene.Call` 会通过服务发现精确解析 Root Scene；业务代码通常不需要手动 `await ServiceDiscovery.ResolveAddressAsync(address)`。

实时创建流程优先由创建方直接把新 Address 返回或发布给使用方。`DiscoverSubScenesAsync` 适合列表、选择和恢复流程，但它复用本地快照，其他服务器已有旧快照时可能要到下一个缓存周期才看到新实例。

### 主动下线

```csharp
await dungeon.Close();
```

`Close()` 会先从 Control Center 主动下线，再销毁 SubScene；Root Scene 下线时，其全部 SubScene 也会一起下线。远端缓存仍可能在一个缓存周期内保留旧端点，调用方必须把发送失败视为正常的节点离线结果。

## 计划缩容：提前摘流

Scene 需要继续运行一段时间来迁移玩家或排空任务时，先停止对外发现：

```csharp
await NetServiceDiscovery.SetSceneOfflineAsync(scene);

await FTask.Wait(
    scene,
    ProgramDefine.ControlCenterHeartbeatIntervalSeconds * 1000L);

// 迁移现有状态后再关闭。
await scene.Close();
```

该调用会把 Scene 从本机心跳集合移除，并等待 Control Center 确认下线；Root Scene 的 SubScene 会一起摘除。旧发现快照最多保留一个心跳间隔，且已知 Address 仍然可达，所以业务代码必须同时拒绝新的玩家或任务分配。摘流后不会自动恢复注册。

### Control Center 实例查询

“服务实例”页面支持按状态、Namespace、WorldGroup、World 分组，并可用关键词匹配 SceneType、SceneId、Address、ParentAddress、InstanceId、Host 和拓扑名称。查询只过滤已加载的内存注册表，不访问数据库，也不影响实例状态。

## 返回端点字段

`DiscoverAsync` 返回 `IReadOnlyList<ServiceEndpointContract>`。常用字段：

| 字段 | 含义 |
|---|---|
| `InstanceId` | 本次启动实例 ID；重启后可变化 |
| `SceneId` | 用于连接的 Root Scene ID；SubScene 返回父 Root Scene ID |
| `Address` | 实际消息目标的 RuntimeId；SubScene 返回自身 Address |
| `IsSubScene` | 是否为动态 SubScene |
| `ParentInstanceId` | 父 Root Scene 实例 ID；Root Scene 为空 |
| `ParentAddress` | 父 Root Scene Address；Root Scene 为 `0` |
| `NamespaceId` | Namespace 范围 |
| `WorldGroupId` | WorldGroup 范围 |
| `WorldId` | World 范围 |
| `ProcessId` | 所属 Process |
| `Host` / `InnerPort` | 内部网络连接端点 |
| `OuterPort` | 对外端口，`0` 表示未开放 |

不要依赖返回列表的顺序。列表适合监控、在线校验和业务决策；直接消息路由使用下面的 Address 查询 API。

## 自动生命周期

| 事件 | 框架行为 |
|---|---|
| 所有 Root Scene 启动成功 | 注册当前 OS 进程中的 Root Scene |
| `CreateSubScene` 完成初始化 | 立即注册到父 Root Scene 下 |
| 注册完成 | 立即发送一次批量心跳 |
| 正常运行 | 按配置批量续租，单批最多 4096 个实例 |
| 心跳返回实例不存在 | 自动重新注册对应实例 |
| 网络短暂失败 | 自动重试，并控制重复警告日志 |
| `SubScene.Close()` | 先主动下线，再销毁 SubScene |
| Root Scene 下线 | 同时下线其全部 SubScene |
| `SetSceneOfflineAsync` | 提前摘流，Scene 保持运行，等待业务排空后再关闭 |
| 正常关闭 | 停止心跳并主动下线 |
| 异常退出 | 等待租约到期后自动失效 |
