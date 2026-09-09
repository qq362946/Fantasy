# Roaming 审查清单

**本文件用于检查 Roaming 相关代码。**

## 检查顺序

1. 机制是否真的该用 Roaming
2. RoamingType 和协议是否匹配
3. 路由是否在正确阶段建立
4. Terminus 生命周期处理是否完整
5. 消息流转和传送逻辑是否混乱
6. Control Center 模式下目标 Address 是否由服务发现取得并登记
7. 动态 Gate 下是否保证单账号单登录，并保留 owner 隔离

## 常见问题

### 错误 1：本来不是客户端经 Gate 访问后端，却硬用 Roaming

如果不是这种场景，先考虑是否更适合 `SphereEvent`、`Address` 或本地 `Event`。

### 错误 2：RoamingType、协议和目标服务器不一致

重点检查：

- 协议标注的 RoamingType 是否正确
- Gate 建链时是否选对目标服务器
- Handler 是否落在真正接收该漫游消息的服务器上

### 错误 3：路由建立位置不对，或没有区分首次连接与重连

常见正确位置：

- 登录成功后
- 重连恢复时
- 明确的初始化流程里

不要在任意业务调用路径重复建链。

统一使用 `session.GetOrCreateRoaming(roamingId, delayRemove)`；它直接返回组件并自动处理本地 Session 重绑，但不会单独恢复目标 Terminus 的转发。审查它后面的每个 `Link` 时，必须确认首次连接与重连使用不同分支：

```csharp
uint errorCode;
if (roaming.IsLinked(roamingType))
{
    // 重连：沿用已保存的目标地址。
    errorCode = await roaming.Link(roamingType, args);
}
else
{
    // 首次连接：此时才选择目标服务器。
    var targetSceneAddress = GetTargetSceneAddress();
    errorCode = await roaming.Link(targetSceneAddress, roamingType, args);
}
```

每个需要恢复的 `roamingType` 都必须独立检查。以下两种写法都应作为审查问题提出：无条件调用带地址重载，会在重连时重复选择服务器；无条件调用无地址重载，会让首次连接返回 `ErrReLinkNotFoundRoaming`。

同时检查：

- 不要再向 `Link` 传 Session Address；框架从组件当前绑定的 Session 获取
- 同一 Gate 内已有本地路由时，必须调用无地址重载，避免重复选择服务器
- 新 Gate 没有原本的本地路由，即使目标端 Terminus 仍存在，也必须先取得原目标地址并调用带地址重载
- 带地址重载遇到已有本地路由时会沿用已保存的地址，不能用它直接替换目标服务器

### 错误 4：OnCreateTerminus / OnDisposeTerminus 清理不完整

重点检查：

- 创建时是否关联业务实体
- 断开时是否释放资源、保存数据
- 是否区分重连与真正销毁

### 错误 5：把普通消息流转和跨服传送逻辑混在一起

消息流转看 `messaging.md`，传送看 `transfer.md`。两者不要混着审。

### 错误 6：传送后继续使用旧实例

传送成功后旧实例会销毁。后续逻辑必须重新获取新实例，不要继续用旧引用。

### 错误 7：检测到异常状态却仍然按成功路径返回

常见坏味道：

- 日志已经明确记录“这是错误，一定要排查”
- 但方法最后仍然返回成功错误码或继续走成功逻辑

这种写法会把 Roaming 链路中的不一致状态隐藏起来，后续问题会更难排查。

### 错误 8：服务发现模式仍固定从 `SceneConfigData[0]` 取后端

Release 按 Process 拉取配置时，本进程不一定包含远程 SceneConfig。首次建链或新 Gate 接管时应使用 `DiscoverAddressAsync` / `DiscoverAddressByHashAsync`，处理 Address 为 `0`，再调用 `Link(long targetSceneAddress, int roamingType, Entity? args)`；同一 Gate 已有本地路由时直接调用 `Link(int roamingType, Entity? args)`，不要重复发现目标。

### 错误 9：把 Rendezvous Hash 当作永久 Roaming 归属

Rendezvous Hash 在节点集合变化时会迁移少量玩家。如果重连必须回到原后端，业务层需要持久化玩家到 SceneId 的绑定，并验证目标实例仍在线。

### 错误 10：动态 Gate 没有单登录约束

同一账号同时登录多个 Gate 会形成所有权争抢。业务层必须保证任一时刻只有一个有效登录；新 Gate 通过 Link 接管目标 Terminus，旧 Gate 依靠 Session 心跳、`delayRemove` 或 Scene 关闭流程清理本地上下文。不要增加可能发往已关闭 Gate 的释放协议。

### 错误 11：误解 delayRemove 的 0 值

`delayRemove <= 0` 表示立即移除，不是永久保留。自然断线使用 `GetOrCreateRoaming` 设定的窗口；`RemoveRoaming()` 默认立即移除。

## 审查时重点问自己

1. 这段代码真的该用 Roaming 吗
2. `GetOrCreateRoaming` 后的每个 `Link` 是否用 `IsLinked(roamingType)` 分开首次连接与重连
3. Handler 是否在正确服务器和正确实体类型上执行
4. 传送成功后是否仍然引用旧实体
5. 是否把异常状态吞掉并错误返回成功
6. 动态目标是否使用正确的发现范围、路由策略，并处理无在线实例
7. 同一账号是否保证单登录，旧 Gate 的延迟请求是否会被 owner 校验隔离

## 相关文档

- `index.md`
- `protocol.md`
- `setup.md`
- `on-create-terminus.md`
- `on-dispose-terminus.md`
- `handler.md`
- `messaging.md`
- `transfer.md`
- `references/service-discovery/index.md`
- `references/service-discovery/routing.md`
