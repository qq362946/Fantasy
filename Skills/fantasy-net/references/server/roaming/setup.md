# Roaming 路由建立

## Gate 侧统一入口

首次登录和断线重连都调用 `GetOrCreateRoaming`。它直接返回 `SessionRoamingComponent`，不返回创建状态：

```csharp
var roaming = await session.GetOrCreateRoaming(
    roamingId: request.PlayerId,
    delayRemove: 180_000);
```

- `roamingId`：稳定的业务身份，通常是玩家 ID；重连前后必须一致
- `delayRemove`：Session 释放后的重连窗口，默认 3 分钟；小于等于 `0` 时立即销毁
- 同一 Gate 已有该 `roamingId`：取消延迟销毁并换绑新 Session；随后仍要为每条需要恢复的路由调用 `Link`
- 当前 Session 已绑定其他 `roamingId`：框架先按旧连接的延迟策略解除旧绑定

业务上的首次登录和重连应由账号状态或登录票据判断，不要从漫游 API 推断。

## Link 到目标 Scene

```csharp
FTask<uint> Link(long targetSceneAddress, int roamingType, Entity? args = null);
FTask<uint> Link(int roamingType, Entity? args = null);
```

首次建立某个 `roamingType` 时，调用带目标 Scene Address 的 `Link`：

```csharp
var errorCode = await roaming.Link(
    targetSceneAddress,
    RoamingType.ChatRoamingType,
    args: null);
```

重连时路由已经保存了目标地址，直接按 `roamingType` 恢复：

```csharp
var errorCode = await roaming.Link(
    RoamingType.ChatRoamingType,
    args: null);
```

两种重载都会从组件当前绑定的 Session 取得转发地址，不需要也不能由调用方传入 Session Address。

- `Link(targetSceneAddress, roamingType, args)`：首次连接时保存目标地址；本地路由已存在时自动改为重连，并使用路由中保存的原目标地址，传入的新地址不会替换它。
- `Link(roamingType, args)`：只恢复本地已经保存的路由；路由不存在时返回 `ErrReLinkNotFoundRoaming`。

目标 Scene 没有该 `roamingId` 时触发 `CreateTerminusType.Link`；已有 Terminus 时复用它并触发 `CreateTerminusType.ReLink`。

每个需要通信的后端 `RoamingType` 各调用一次 `Link`。一个 `SessionRoamingComponent` 可以同时维护 Chat、Map、Battle 等多条路由。

## Control Center 示例

```csharp
using NetServiceDiscovery = Fantasy.ServiceDiscovery;

var roaming = await session.GetOrCreateRoaming(
    request.PlayerId,
    delayRemove: 180_000);

uint errorCode;
if (roaming.IsLinked(RoamingType.ChatRoamingType))
{
    // 同一 Gate 内重连：沿用本地保存的目标，不再执行服务发现。
    errorCode = await roaming.Link(RoamingType.ChatRoamingType);
}
else
{
    // 首次连接或切换到了没有本地路由的新 Gate：先确定目标地址。
    var worldId = session.Scene.SceneConfig.WorldConfigId;
    var chatAddress = await NetServiceDiscovery.DiscoverAddressByHashAsync(
        SceneType.Chat,
        request.PlayerId,
        worldId: worldId);

    if (chatAddress == 0)
    {
        // 按项目约定设置“无在线 Chat”的业务错误码。
        return;
    }

    errorCode = await roaming.Link(
        chatAddress,
        RoamingType.ChatRoamingType);
}

if (errorCode != 0)
{
    response.ErrorCode = errorCode;
    return;
}
```

未启用 Control Center 时，首次连接从静态配置取得 `SceneConfig.Address`，再调用 `Link(address, roamingType, args)`。同一 Gate 内重连且本地路由仍存在时，不必再次服务发现或读取静态配置，调用 `Link(roamingType, args)` 即可。

Rendezvous Hash 只在在线实例集合不变时稳定。扩缩容后仍必须回到原后端时，由业务层持久化玩家到 SceneId 的绑定并验证实例仍在线；主动迁移使用 `StartTransfer`，不要把同一 `roamingType` 直接 Link 到另一个目标 Scene。

## 动态 Gate 与所有权

同一账号必须保证任一时刻只有一个有效登录。

- 新 Gate 没有旧 Gate 的本地路由，不能使用无地址重载；创建 `SessionRoamingComponent` 后，应取得原目标地址并调用 `Link(targetSceneAddress, roamingType, args)`
- Link 请求携带 `SessionRoamingComponent.RuntimeId` 作为 owner；目标 Terminus 已存在时由新 Gate 接管
- 旧 Gate 后续到达的暂停或断开请求因 owner 不匹配而被忽略，不会清理新 Gate 的连接
- 旧 Gate 的本地上下文由旧 Session 的心跳断开、`delayRemove` 或 Scene 关闭流程回收，不需要 Gate 间释放协议或全局注册表

## 传递自定义参数

参数 Entity 必须支持当前内部协议使用的序列化：

```csharp
[MemoryPackable]
public sealed partial class PlayerLoginData : Entity
{
    public string PlayerName;
    public int Level;
}
```

```csharp
var loginData = Entity.Create<PlayerLoginData>(session.Scene);
loginData.PlayerName = request.PlayerName;
loginData.Level = request.Level;

var errorCode = await roaming.Link(
    chatAddress,
    RoamingType.ChatRoamingType,
    loginData);

// Gate 始终销毁原始对象；后端在 OnCreateTerminus 中独立销毁反序列化副本。
loginData.Dispose();

if (errorCode != 0)
{
    response.ErrorCode = errorCode;
    return;
}
```

## 主动移除

```csharp
// 方式 1：移除整个漫游上下文；内部会先 UnLinkAll，默认立即执行
await session.RemoveRoaming();
```

```csharp
// 方式 2：只移除一条路由；整个上下文仍由 RemoveRoaming 统一回收
var isEmpty = await roaming.UnLink(
    RoamingType.ChatRoamingType,
    disposeIfEmpty: false);

if (isEmpty)
{
    await session.RemoveRoaming();
}
```

```csharp
// 方式 3：只断开全部后端路由，保留空的漫游上下文供后续重新 Link
await roaming.UnLinkAll();
```

Session 自然断开时通常不需要主动调用移除 API；框架会使用 `GetOrCreateRoaming` 设置的 `delayRemove`。

## 后端下一步

Gate 调用 `Link` 后，目标服务器通过 `OnCreateTerminus` 创建或恢复业务实体。继续读 `on-create-terminus.md`。
