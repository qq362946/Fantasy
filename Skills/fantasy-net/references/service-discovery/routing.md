# 服务发现范围与路由策略

## 查询范围

所有查询都会自动限定为当前 Process 的 Namespace：

| 参数 | 查询范围 |
|---|---|
| 不传 `worldId` / `worldGroupId` | 当前 Namespace 中全部匹配实例 |
| 只传 `worldId` | 当前 Namespace 的指定 World |
| 只传 `worldGroupId` | 当前 Namespace 的指定 WorldGroup |

`worldId` 与 `worldGroupId` 互斥，且不能为 `0`。如果需要跨 Namespace 通信，应重新审视隔离边界，不要绕过服务发现限制拼接远程端点。

SubScene 不使用 World / WorldGroup 参数查询。调用 `DiscoverSubScenesAsync(parentAddress, sceneType)`，范围固定为指定父 Root Scene 下的直接子实例；`parentAddress` 必须是 Root Scene Address。

## 如何选择实例

| 业务需求 | 使用方式 |
|---|---|
| 查看全部在线实例、检查特定 Scene 是否在线 | `DiscoverAsync` |
| 无状态请求、任意实例均可处理 | `DiscoverAddressAsync` |
| 相同 Key 尽量稳定落到同一实例 | `DiscoverAddressByHashAsync` |
| 查看指定 Root Scene 下的全部子实例 | `DiscoverSubScenesAsync` |
| 在指定 Root Scene 下随机或稳定选择子实例 | `DiscoverSubSceneAddressAsync` / `DiscoverSubSceneAddressByHashAsync` |
| 扩缩容后仍必须回到之前的实例 | 业务持久化绑定 + 服务发现校验在线状态 |

### 随机选择

随机选择适合无状态服务，例如通用查询、可重试任务或任意 Map 都能处理的请求。它不保证连续两次调用返回同一个 Scene。

### Rendezvous Hash

`DiscoverAddressByHashAsync` 和 `DiscoverSubSceneAddressByHashAsync` 使用 Rendezvous Hash，并以端点 `Address` 作为节点标识：

- 节点集合不变时，相同 `routingKey` 会选择相同 Scene。
- 新增或删除实例时，只重新分配部分 Key，不会像 `% instanceCount` 那样大范围漂移。
- 它提供稳定选择，不提供永久归属。
- SubScene 销毁后重新创建会获得新 Address，因此属于一个新节点。

不要使用下面的动态服务路由：

```csharp
var index = accountId % authScenes.Count;
var auth = authScenes[index];
```

实例数量从 3 变成 5 时，取模会让大量账号迁移。

## 严格账号归属

断线重连、顶号等逻辑如果要求账号必须回到上一次 Auth，绑定属于业务状态，应保存到 Redis 或数据库：

```text
AccountId -> Auth SceneId
```

推荐流程：

1. 读取已有 SceneId 绑定。
2. 用 `DiscoverAsync(SceneType.Auth)` 的在线快照确认该 Scene 仍在线。
3. 在线时使用列表中对应端点的 Address；也可以调用 `ResolveAddressAsync(sceneId)` 精确解析 Root Scene。
4. 没有绑定或原 Scene 已离线时，使用 Rendezvous Hash 选择新节点。
5. 更新业务绑定。

`DiscoverAsync` 会登记返回列表中全部实例所属 Root Scene 的网络端点，因此列表中的 `endpoint.Address` 可以直接用于 `Scene.Send` / `Scene.Call`。`ResolveAddressAsync` 只用于已经保存 SceneId、需要精确解析 Root Scene 的场景。

不要把永久账号绑定写进 Control Center 的服务注册表。注册表描述“谁在线”，业务库描述“账号归谁”。

## 本地配置与服务发现

- 未启用 Control Center：可从 `SceneConfigData` 选择静态 Scene，再使用 `SceneConfig.Address`。
- 已启用 Control Center：动态目标优先使用 `ServiceDiscovery` 获取 Address；不要假设本地配置列表顺序或远程端点永远不变。
- 已获得 SubScene Address：直接发送即可；路由缺失时框架会解析其所属 Root Scene，不要把 SubScene 当成独立 TCP 端点。
- 已获取远程业务 Entity 的 Address，且当前进程仍保有对应连接端点：缓存并复用该 Address，不必每条消息重新发现 Scene。
- Scene 下线后，旧业务 Entity Address 也应视为失效；按业务恢复流程重新发现入口并建立状态。

## 缓存与性能

服务发现客户端不会在每次查询时访问 Control Center：

- Root Scene 缓存键由 SceneType 和 World / WorldGroup 范围组成。
- SubScene 缓存键由 SceneType 和父 Root Scene Address 组成。
- 缓存有效期复用心跳间隔。
- 同一个缓存键只有一个请求负责刷新，其余调用复用结果。
- 快照有效时走内存只读路径，不创建临时实例列表。
- Control Center 短暂不可用且已有旧快照时，继续返回旧快照并在后续周期重试。
- 首次查询没有缓存且请求失败时，会报告异常。

不要在业务层再包一套无失效规则的长期缓存。严格业务绑定可以缓存，但必须独立处理实例离线。

## 与 Address 消息的关系

服务发现解决“第一次怎样找到目标 Scene 入口”，Address 消息解决“怎样向已知 RuntimeId 发送一对一消息”。SubScene Address 的 SceneId 指向所属 Root Scene 网络入口：`Scene.Call` 在路由缺失时等待解析，`Scene.Send` 把消息加入该 SceneId 的短暂等待队列并异步解析。业务层不需要先手动调用 `ResolveAddressAsync`。获得 Address 后，继续按 `references/server/address.md` 定义协议和 Handler。
