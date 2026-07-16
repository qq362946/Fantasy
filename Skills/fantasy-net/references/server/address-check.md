# Address 审查清单

**本文件用于检查 Address 消息相关代码。**

## 检查顺序

1. 这是单向消息还是 RPC
2. 当前是本地静态配置还是 Control Center，Address 获取方式是否匹配
3. 第一次通信是否正确走了 Scene 入口地址
4. SubScene 是否按父 Root Scene Address 发现，并把自身 Address 作为消息目标
5. Handler 泛型和消息接口是否匹配
6. 是否正确缓存并复用返回的实体 Address

## 常见问题

### 错误 1：第一次通信就假设自己知道业务实体 Address

远程业务实体 Address 往往本地无法预知。第一次通信通常应该：

- 本地静态模式从 `SceneConfig.Address` 获取入口；Control Center 模式从 `ServiceDiscovery` 获取在线入口
- 由目标 Scene 创建或定位业务实体
- 再把实体 Address 返回给调用方缓存

### 错误 2：单向消息和 RPC 模式混淆

- 单向通知 -> `IAddressMessage` + `Address<TEntity, TMessage>`
- RPC -> `IAddressRequest` / `IAddressResponse` + `AddressRPC<TEntity, TReq, TRes>`

### 错误 3：Handler 的 `TEntity` 选错

重点检查：

- 发给 Scene 本身 -> `Address<Scene, ...>` / `AddressRPC<Scene, ...>`
- 发给具体业务实体 -> 对应实体类型，如 `Address<Player, ...>`

### 错误 4：收到 Address 后没有缓存，导致每次都重复走入口流程

第一次拿到远程业务实体 Address 后，应保存到本地 Entity / Component 上，后续直接复用。

### 错误 5：Handler 里不检查实体状态

常见首行检查：

```csharp
if (player == null || player.IsDisposed) return;
```

### 错误 6：在 A→B RPC Handler 里再同步 RPC 回 A

这类相互等待容易引入死锁风险。

### 错误 7：`GetSceneBySceneType(...)[0]` 默认假设目标 Scene 永远只有一个

这不是一定错误，但属于需要明确业务前提的写法。

审查时要问：

- 当前项目是否真的只有一个该类型 Scene
- 是否应该按 Namespace、WorldGroup、World、负载均衡或配置记录来选择目标 Scene
- 启用 Control Center 后是否应该改用 `ServiceDiscovery`

### 错误 8：服务发现返回 0 后仍然发送

`DiscoverAddressAsync` 和 `DiscoverAddressByHashAsync` 在没有在线实例时返回 `0`。发送前必须判断并走明确的业务失败或重试路径。

### 错误 9：把 Rendezvous Hash 当作永久业务绑定

扩缩容时 Rendezvous Hash 仍会迁移少量 Key。账号断线重连必须回到原 Auth 等严格归属，应持久化 `AccountId -> SceneId` 并检查实例在线状态。

### 错误 10：把 SubScene 当成独立网络端点

SubScene 使用自身 Address 作为消息目标，但通过父 Root Scene 的 SceneId、Host 和 InnerPort 建立连接。应按父 Root Scene Address 查询子实例；如果业务已经拿到 SubScene Address，直接 `Scene.Send` / `Scene.Call`，不要为它单独拼接 Host/Port，也不需要提前手动解析路由。

## 审查时重点问自己

1. 这是第一次通信还是后续直接发给实体
2. Address 来源是否与本地配置 / Control Center 模式匹配
3. SubScene 的父子范围和实际消息 Address 是否正确
4. 单向 / RPC 模式是否匹配
5. Handler 实体类型是否正确
6. 是否存在重复走入口、没缓存地址、死锁式双向 RPC 的风险
7. 是否无依据地把目标 Scene 固定成 `[0]`
8. 是否处理发现空结果、动态扩缩容和严格归属边界

## 相关文档

- `address.md`
- `../service-discovery/index.md`
- `../service-discovery/routing.md`
- `protocol/index.md`
- `server/server-message-handler.md`
