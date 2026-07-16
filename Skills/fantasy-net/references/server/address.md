# Address 消息 - 服务器间实体通信

每个 Entity 都有唯一的 `Address`（即 `RuntimeId`），框架根据这个地址自动将消息路由到目标实体所在的服务器。

> **与 Addressable 的区别：** Addressable 是客户端→特定实体的寻址（需要中央寻址服务）；Address 是服务器间直接通过 RuntimeId 路由。

---

## Workflow

收到"发送 Address 消息"需求时，按以下顺序逐步确认，不要跳步骤：

```
第 1 步：消息模式？
│
├─► 单向通知（不需要结果）
│       发送：scene.Send(address, msg)          协议：IAddressMessage
│       Handler：Address<TEntity, TMessage>
│
└─► RPC（需要等待响应或返回数据）
        发送：await scene.Call(address, req)     协议：IAddressRequest / IAddressResponse
        Handler：AddressRPC<TEntity, TReq, TRes>

第 2 步：目标 Address 从哪里来？
│
│  【原理】Entity.Address（RuntimeId）是运行时动态生成的，远程服务器上的业务实体
│  Address 本地无法预知。第一次通信要先取得目标 Scene 的入口 Address：
│  本地静态模式从 SceneConfig 获取；Control Center 模式从 ServiceDiscovery 获取。
│
├─► 已知 Address（之前已获取并保存在本地某个 Entity/Component 上）
│       → 直接使用，跳到第 3 步
│
└─► 不知道对方 Address（第一次通信）
        → 问清楚配置来源、目标 SceneType、范围和选择逻辑（见下方"选择目标 Scene"）
        → 取得目标 Scene Address 并向入口发 RPC
        → 目标 Scene 创建业务实体，将实体 Address 写入响应返回
        → 保存返回的 Address，后续直接使用，无需再走此流程

第 3 步：定义协议（Inner/InnerMessage.proto，见下方"协议与 Handler"）

第 4 步：在目标服务器 Hotfix 层创建 Handler，Run 方法首行检查实体状态

第 5 步：编译验证  dotnet build {解决方案}.sln
```

---

## 获取并选择目标 Scene

先按配置模式选择入口来源：

| 模式 | 获取方式 |
|---|---|
| 未启用 Control Center，拓扑静态 | 从 `SceneConfigData` 获取 `SceneConfig.Address` |
| 启用 Control Center，需要动态在线实例 | `ServiceDiscovery.DiscoverAddressAsync(...)` |
| 启用 Control Center，需要稳定 Key 路由 | `ServiceDiscovery.DiscoverAddressByHashAsync(...)` |
| 需要查看在线实例、自行选择或校验 SceneId | `ServiceDiscovery.DiscoverAsync(...)`；列表中的 Address 可以直接通信 |
| 查询指定 Root Scene 下的动态 SubScene | `ServiceDiscovery.DiscoverSubScenesAsync(parentAddress, ...)` 或两个 SubScene Address 选择 API |

不要无脑取 `[0]`，也不要对动态实例数量取模。先问清楚目标范围是 Namespace 全部、World 还是 WorldGroup，以及需要随机、稳定哈希还是严格业务绑定。服务发现细节见 `references/service-discovery/index.md`。

---

## 协议与 Handler

协议定义在 `Inner/InnerMessage.proto`，Handler 放在目标服务器的 Hotfix 层。

### RPC：第一次通信，通过目标 Scene 入口取得实体 Address

```protobuf
message G2M_CreatePlayerRequest // IAddressRequest,M2G_CreatePlayerResponse
{
    int64 PlayerId = 1;
    string PlayerName = 2;
}
message M2G_CreatePlayerResponse // IAddressResponse
{
    int64 PlayerEntityAddress = 1;
}
```

```csharp
// 发送方：Control Center 模式下先发现在线 Scene 入口
using NetServiceDiscovery = Fantasy.ServiceDiscovery;

var mapAddress = await NetServiceDiscovery.DiscoverAddressAsync(
    SceneType.Map,
    worldId: scene.SceneConfig.WorldConfigId);

if (mapAddress == 0)
{
    return;
}

var response = (M2G_CreatePlayerResponse)await scene.Call(
    mapAddress,
    new G2M_CreatePlayerRequest { PlayerId = playerId, PlayerName = "Alice" }
);
if (response.ErrorCode != 0) return;

// 将返回的实体 Address 保存到 Component 上，供后续消息使用
session.AddComponent<PlayerFlagComponent>().PlayerAddress = response.PlayerEntityAddress;

// 接收方 Handler（TEntity=Scene，因为消息发给 Scene 本身）
public sealed class G2M_CreatePlayerRequestHandler : AddressRPC<Scene, G2M_CreatePlayerRequest, M2G_CreatePlayerResponse>
{
    protected override async FTask Run(Scene scene, G2M_CreatePlayerRequest request,
        M2G_CreatePlayerResponse response, Action reply)
    {
        var player = Entity.Create<Player>(scene, request.PlayerId, true, true);
        player.Name = request.PlayerName;
        response.PlayerEntityAddress = player.Address; // 返回实体 Address
        await FTask.CompletedTask;
    }
}
```

未启用 Control Center 时，入口可以继续从静态配置取得：

```csharp
var mapConfig =
    SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];

var mapAddress = mapConfig.Address;
```

### 单向消息：已有 Address，直接发送

拿到实体 Address 后，后续消息直接从 Component 取出来用，不再需要经过 Scene 中转。

```protobuf
message G2M_NotifyPlayerMove // IAddressMessage
{
    int32 X = 1;
    int32 Y = 2;
}
```

```csharp
// 发送方：从 Component 取出之前保存的 Address
var playerAddress = session.GetComponent<PlayerFlagComponent>().PlayerAddress;
scene.Send(playerAddress, new G2M_NotifyPlayerMove { X = 100, Y = 200 });

// 接收方 Handler（TEntity=Player，框架自动根据 Address 找到对应实体注入）
public sealed class G2M_NotifyPlayerMoveHandler : Address<Player, G2M_NotifyPlayerMove>
{
    protected override async FTask Run(Player player, G2M_NotifyPlayerMove message)
    {
        if (player == null || player.IsDisposed) return;
        player.X = message.X;
        player.Y = message.Y;
        await FTask.CompletedTask;
    }
}
```

> **TEntity 选择：** 消息发给 Scene 本身用 `Address<Scene, ...>`；发给具体实体用对应类型（如 `Address<Player, ...>`）。
> Handler 框架自动根据 Address 查找并注入实体，无需手动 `GetEntity()`。

---

## 最佳实践

- Handler 首行检查 `entity == null || entity.IsDisposed`
- 错误用 `response.ErrorCode` 返回，不抛异常
- 避免 A→B RPC Handler 中再同步 RPC 回 A（死锁）
- 默认超时 30 秒，超时返回 `InnerErrorCode.ErrRouteTimeout`
- 服务发现 Address 查询返回 `0` 时不要继续发送
- 服务发现只用于找到入口；拿到业务 Entity Address 后缓存并复用
- 所有发现查询都会登记返回实例所属 Root Scene 的网络端点，列表中的 Address 可以直接发送
- 已知 SubScene Address 时直接 `Scene.Send` / `Scene.Call`；框架会在路由缺失时解析其父 Root Scene
