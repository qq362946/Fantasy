# 服务发现检查与排障

## 检查顺序

1. `controlCenter` 是否启用，URL 是否为根地址
2. `sceneTypes` 是否完整、稳定且与 Control Center 一致
3. Namespace / Machine / Process / WorldGroup / World / Scene 引用是否正确
4. 当前 OS 进程是否启动了预期 Process 和 Scene
5. 实例是否已注册、心跳是否续租、租约是否合理
6. 查询范围与路由策略是否符合业务语义
7. 无实例、首次查询失败、节点离线是否有处理
8. 公网部署是否补齐 TLS、认证和网络隔离

## 配置检查

- `enabled="true"` 时存在至少一个 `sceneType`。
- SceneType ID 大于 0、不重复，名称以大写字母开头且只含字母、数字、下划线。
- `url` 没有重复追加 `/api/v1`。
- `0 < heartbeatIntervalSeconds < leaseSeconds`，且 `5 <= leaseSeconds <= 300`。
- `fallbackToLocal="true"` 时本地 server 配置完整，Process 和 World 的 Namespace 一致，World 同时具有非零 NamespaceId 与 GroupId。
- Control Center 中 Process、WorldGroup、World、Scene 处于启用状态，引用对象没有被禁用或删除。

## API 检查

- 固定类型优先使用 `SceneType.Map`，而不是散落字符串。
- 不同时传 `worldId` 和 `worldGroupId`，也不传 `0`。
- 列表为空时不访问 `[0]`。
- Address 为 `0` 时停止发送并返回明确业务错误。
- 不依赖发现列表顺序。
- `DiscoverSubScenesAsync` 传父 Root Scene Address，而不是 SubScene Address、SceneId 或 Entity.Id。
- SubScene 的 SceneType 已声明在 `<controlCenter><sceneTypes>` 中。
- 可以直接使用发现列表中的 `endpoint.Address`；框架已经登记其所属 Root Scene 端点。
- 不在 `OnCreateScene` 中同步查询或等待服务发现初始化。
- 不用 `% instances.Count` 处理动态扩缩容。
- 需要严格归属时，不把 Rendezvous Hash 误当永久绑定。
- 获得业务 Entity Address 后缓存复用，不在每条消息前重复发现入口。

## 常见问题

### `Service discovery is not enabled.`

检查 `controlCenter.enabled`、配置是否被复制到输出目录，以及调用是否发生在 `OnCreateScene`。只有真正启用 Control Center，并且所有 Process / Scene 创建完成后，才初始化服务发现入口。

### `SceneType ... is not declared.`

把该类型加入 `<controlCenter><sceneTypes>`，保持名称与 Control Center 一致，然后重新编译。不要修改源生成的 `.g.cs`。

### 查询返回空列表或 Address 为 0

依次检查：

1. 目标 Scene 是否启动成功。
2. Control Center 实例页是否显示在线。
3. SceneType 是否完全一致，包括大小写。
4. 当前 Process 与目标 Scene 是否属于同一 Namespace。
5. `worldId` / `worldGroupId` 是否选错范围。
6. 实例是否因心跳中断超过租约而下线。

### SubScene 创建后查询不到

1. 确认 `CreateSubScene` 已经成功返回。
2. 确认 SubScene 的 SceneType 已声明。
3. 确认父 Root Scene 已注册且仍在线。
4. 确认查询使用父 Root Scene Address，并且类型一致。
5. 检查调用方是否仍在使用旧缓存快照；实时流程应直接传递创建后返回的 SubScene Address。

### 能发现但无法发送消息

检查目标 Machine 的 `innerBindIP` 是否能从调用方机器访问、InnerPort 是否开放、NAT 或容器是否发布正确端口。注册成功只说明 HTTP 控制面可达，不等于服务器之间的数据面网络可达。

Kubernetes 中检查 `innerBindIP` 是否为 StatefulSet Pod 专属 DNS，并确认 Headless Service 设置了 `publishNotReadyAddresses: true`。普通 ClusterIP Service DNS 不能绑定到 Pod 本地网卡；共享 Headless Service DNS 可能解析到其他 Pod，不能作为特定 Scene 的注册地址。完整检查见 `../server/kubernetes.md`。

SubScene 没有独立端口，它使用父 Root Scene 的 SceneId、Host 和 InnerPort。若 SubScene Address 来自 RPC 或事件而本地尚无路由，可直接调用 `Scene.Send` / `Scene.Call`，框架会解析父 Root Scene；不需要业务代码提前调用 `ResolveAddressAsync`。

### Control Center 短暂不可用

- 已有发现快照时，客户端会暂时复用旧快照。
- 注册和心跳会自动重试；被拒绝的实例会重新注册。
- 首次拉取配置失败且 `fallbackToLocal="false"` 时，服务器启动失败。
- 开启本地回退后，确认本地拓扑与 Control Center 的 Namespace / WorldGroup 隔离关系一致。

### 扩容后账号落到不同 Auth

Rendezvous Hash 在成员变化时只保证少量迁移，不保证零迁移。若断线重连和顶号要求永久归属，持久化 `AccountId -> Auth SceneId`，并在每次使用前检查该 Scene 是否仍在线。

## 生命周期检查

- Root Scene 全部启动成功后才注册，没有提前暴露未就绪实例。
- SubScene 在初始化回调和 `OnCreateScene` 完成后立即注册，`CreateSubScene` 返回时可以分发 Address。
- `SubScene.Close()` 先主动下线再销毁；Root Scene 下线时同时移除其子实例。
- 计划缩容先调用 `SetSceneOfflineAsync`，至少等待一个发现缓存周期，并在业务层拒绝新分配，再关闭 Scene。
- 正常关闭先停止心跳并下线，再销毁 Scene。
- 异常退出依赖租约过期；租约不要设得过长。
- 不在每个 Scene 内创建独立心跳任务；一个 OS 进程只应有一个 Worker。
- 不复制实现另一套注册、批量心跳、重注册或发现缓存。

## 公网部署检查

当前 Control Center 不应直接裸露到公网。至少使用：

- HTTPS 反向代理
- 认证与授权
- 防火墙、IP 白名单或 VPN / 私有网络
- 只向游戏服务器开放配置、注册、心跳和发现 API
- 数据库文件与备份目录的最小权限

## 验收标准

1. 两台服务器分别按 Process ID 启动不同 Scene。
2. Control Center 能看到在线实例、心跳时间和正确的 Namespace / WorldGroup / World。
3. 全量、World、WorldGroup 三种查询范围结果正确。
4. 随机和 Rendezvous Hash API 都能获得有效 Address 并完成一次 `Scene.Call`。
5. 停止一个实例后，正常关闭立即下线；强制结束则在租约到期后消失。
6. 重启 Control Center 或短暂断网后，实例可以自动恢复注册和心跳。
7. 创建一个 SubScene 后，能按父 Root Scene Address 查询、跨服务器向其 Address 完成一次 `Scene.Call`，关闭后主动下线。
8. 调用 `SetSceneOfflineAsync` 后，新发现结果不再返回该 Scene；Root Scene 的 SubScene 同时消失，排空后可正常关闭。
9. Control Center“服务实例”页面能按关键词、状态和拓扑范围组合查询实例。
