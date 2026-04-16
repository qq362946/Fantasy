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
│  【原理】Entity.Address（RuntimeId）是运行时动态生成的，远程服务器上的实体
│  Address 本地无法预知。唯一的"已知入口"是 SceneConfig.Address——
│  Scene 本身也是 Entity，其 Address 可从配置表静态获得。
│
├─► 已知 Address（之前已获取并保存在本地某个 Entity/Component 上）
│       → 直接使用，跳到第 3 步
│
└─► 不知道对方 Address（第一次通信）
        → 问清楚目标 SceneType 及选择逻辑（见下方"选择目标 Scene"）
        → 用 sceneConfig.Address 作入口向目标 Scene 发 RPC
        → 目标 Scene 创建业务实体，将实体 Address 写入响应返回
        → 保存返回的 Address，后续直接使用，无需再走此流程

第 3 步：定义协议（Inner/InnerMessage.proto，见下方"协议与 Handler"）

第 4 步：在目标服务器 Hotfix 层创建 Handler，Run 方法首行检查实体状态

第 5 步：编译验证  dotnet build {解决方案}.sln
```

---

## 选择目标 Scene

`GetSceneBySceneType` 返回同类型所有 Scene 的列表，**不要无脑取 `[0]`**，根据业务决定：

| 场景 | 写法 |
|------|------|
| 只有一个目标 | `GetSceneBySceneType(SceneType.Map)[0]` |
| 按玩家存档记录 | `SceneConfigData.Instance.Get(player.MapSceneConfigId)` |
| 随机负载均衡 | `list[Random.Shared.Next(list.Count)]` |

> 先问清楚项目有几个同类 Scene、选择逻辑是什么，再决定写法。

---

## 协议与 Handler

协议定义在 `Inner/InnerMessage.proto`，Handler 放在目标服务器的 Hotfix 层。

### RPC：第一次通信，通过 SceneConfig.Address 作入口取得实体 Address

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
// 发送方：用 SceneConfig.Address 作入口（唯一已知的远程地址）
var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0]; // 按业务选择
var response = (M2G_CreatePlayerResponse)await scene.Call(
    mapConfig.Address,
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
