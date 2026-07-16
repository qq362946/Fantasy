# 服务发现入口

Fantasy 服务发现由 `Fantasy.Net` 与 `Fantasy.ControlCenter` 协作完成：Control Center 持久化服务器拓扑，Fantasy.Net 拉取当前进程配置、自动注册 Scene、批量续租，并向业务代码提供在线实例查询。

## 先判断是否需要服务发现

```text
目标 Scene 已有固定 Address，且拓扑不会动态变化
  -> 可以继续使用本地 SceneConfig

需要多机部署、动态扩缩容、在线状态或范围过滤
  -> 使用 Control Center + ServiceDiscovery

需要发布副本、房间等动态 SubScene
  -> 创建后自动注册；按父 Root Scene Address 查询或直接传递 SubScene Address

需要严格保证账号下次仍落到原节点
  -> 服务发现只负责找在线节点；业务数据库或 Redis 持久化绑定
```

## Workflow

```text
启用 Control Center、配置 sceneTypes、启动服务器
  -> implement.md

按 SceneType 查询、获得 Address、发送 Address 消息
  -> implement.md

创建 SubScene、查询父节点下的子实例、跨服发送
  -> implement.md + ../ecs/subscene.md

计划缩容时先摘流、排空业务、再关闭 Scene
  -> implement.md

选择在线快照 / 随机 / Rendezvous Hash / 严格持久绑定
  -> routing.md

检查配置、空结果、心跳、缓存、部署安全
  -> service-discovery-check.md
```

## 机制边界

- Control Center 管理并持久化 `Namespace / Machine / Process / WorldGroup / World / Database / Scene` 拓扑。
- Fantasy.Net 在 Root Scene 启动成功后自动注册实例；SubScene 创建完成后立即注册到父 Root Scene 下，`Close()` 前主动下线。
- `ServiceDiscovery` 只返回当前 Namespace 中在线且匹配范围的 Scene，不承载账号归属、玩家状态或业务锁。
- Address 是 Fantasy 的 `RuntimeId`；Host 和 InnerPort 是建立内部网络连接所需的端点信息。
- SubScene 使用自身 Address 作为消息目标，但复用父 Root Scene 的 SceneId、Host 和 InnerPort 作为网络入口。
- 不要再通过“实例数量取模”或固定列表下标选择动态服务。

## 必记规则

1. `controlCenter.enabled="true"` 时必须声明 `<sceneTypes>`。
2. `sceneType.id` 必须大于 0、不能重复，发布后不要修改或复用。
3. `sceneType.name` 必须与 Control Center 中的 SceneType 完全一致。
4. 固定类型优先传源生成的 `SceneType.Map`，不要手写字符串。
5. `worldId` 与 `worldGroupId` 不能同时传，也不能传 `0`。
6. Namespace 由当前 Process 自动确定，业务查询不能跨 Namespace。
7. Address 查询无实例时返回 `0`；列表查询无实例时返回空列表。
8. 不要手动实现 Root Scene/SubScene 注册、心跳或下线循环，框架已经自动处理。
9. 所有发现查询都会登记返回实例所属 Root Scene 的网络端点，列表中的 `endpoint.Address` 可以直接用于 `Scene.Send` / `Scene.Call`。
10. `DiscoverSubScenesAsync` 必须传父 Root Scene Address；不能用 SubScene Address 作为父节点继续嵌套查询。
11. 不要在 Root Scene 的 `OnCreateScene` 中同步查询服务；服务发现会在所有 Process 和 Root Scene 创建完成后初始化。
12. 计划缩容调用 `SetSceneOfflineAsync` 后，拒绝新业务分配并等待一个发现缓存周期，再排空和关闭 Scene；该操作不会自动恢复注册。

## 相关文档

- `implement.md`
- `routing.md`
- `service-discovery-check.md`
- `../ecs/subscene.md`
- `../config.md`
- `../server/address.md`
