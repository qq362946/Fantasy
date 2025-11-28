# Address 消息 - 服务器间实体通信

## 概述

Address 消息是 Fantasy Framework 中用于**服务器间实体通信**的核心机制。每个 Entity 都有一个唯一的 Address（即 `RuntimeId`），通过这个地址可以跨服务器向目标实体发送消息。

**核心特性：**
- **基于实体地址**: 使用 `Entity.Address` 作为消息路由地址
- **跨服务器通信**: 自动发送到目标实体所在的服务器
- **透明转发**: 框架自动处理消息转发
- **内网协议**: 仅用于服务器之间的通信

**典型通信流程：**
1. 通过 `SceneConfigData` 查找目标服务器的 Scene 配置
2. 使用 `SceneConfig.Address` 向目标 Scene 发送消息
3. 目标 Scene 创建业务实体并返回实体的 Address
4. 后续直接使用实体 Address 进行通信

**源码位置：**
- `Fantasy.Packages/Fantasy.Net/Runtime/Core/Network/Message/IMessage.cs` - Address 消息接口定义
- `Fantasy.Packages/Fantasy.Net/Runtime/Core/Network/Message/Dispatcher/Interface/IAddressMessageHandler.cs` - Address 处理器基类
- `Fantasy.Packages/Fantasy.Net/Runtime/Core/Scene/Scene.cs` - Scene.Send/Call 扩展方法

---

## Entity.Address 属性

每个 Entity 都有一个 `Address` 属性，返回实体的 `RuntimeId`：

```csharp
/// <summary>
/// 获取当前实体的网络地址。
/// </summary>
public long Address => RuntimeId;
```

**Address 的特点：**
- 全局唯一，在整个分布式系统中唯一标识一个实体
- 包含服务器进程信息，框架可以根据 Address 自动找到目标服务器
- 实体销毁后，Address 失效（RuntimeId 重置为 0）

---

## Address 消息接口类型

Address 消息使用 `IAddressMessage` 和 `IAddressRequest` 接口，定义在内网协议中（Inner 文件夹）：

### 1. IAddressMessage - 单向消息

```csharp
/// <summary>
/// 内网消息接口，继承自 IRequest 接口。
/// </summary>
public interface IAddressMessage : IRequest { }
```

**使用场景：** 服务器之间发送单向通知消息，无需等待响应。

**协议定义示例：**
```protobuf
// Inner/InnerMessage.proto
message G2M_TestAddressMessage // IAddressMessage
{
    string Tag = 1;
}
```

### 2. IAddressRequest - RPC 请求

```csharp
/// <summary>
/// 内网消息请求接口，继承自 IAddressMessage 接口。
/// </summary>
public interface IAddressRequest : IAddressMessage { }

/// <summary>
/// 内网消息响应接口，继承自 IResponse 接口。
/// </summary>
public interface IAddressResponse : IResponse { }
```

**使用场景：** 服务器之间发送 RPC 请求，需要等待响应。

**协议定义示例：**
```protobuf
// Inner/InnerMessage.proto
message G2M_TestAddressRequest // IAddressRequest,M2G_TestAddressResponse
{
    string Tag = 1;
}

message M2G_TestAddressResponse // IAddressResponse
{
    string Result = 1;
}
```

---

## 如何获取目标服务器的 Address

**重要前提：** 在开始跨服务器通信时，你通常**不知道**目标服务器上具体实体的 Address。这时需要通过配置表获取目标服务器 Scene 的 Address。

### 获取流程

1. **通过配置表查找目标 Scene**：使用 `SceneConfigData` 根据 SceneType 查找目标服务器的 Scene 配置
2. **使用 SceneConfig.Address**：每个 SceneConfig 都有一个 `Address` 属性，这是该 Scene 的 Address
3. **发送消息到目标 Scene**：第一次通信通常是发送到目标服务器的 Scene
4. **Scene 创建实体并返回 Address**：目标 Scene 收到消息后创建实体，并将实体的 Address 返回
5. **后续直接使用实体 Address**：拿到实体 Address 后，后续就可以直接向该实体发送消息

### 代码示例

```csharp
public async FTask ConnectToMapServer(Scene gateScene)
{
    // 1. 通过配置表获取 Map 服务器的 Scene
    // GetSceneBySceneType() 返回指定类型的所有 Scene 配置
    var mapSceneConfigs = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map);

    // 通常选择第一个，实际项目中可能需要负载均衡算法选择
    var mapSceneConfig = mapSceneConfigs[0];

    // 2. 使用 SceneConfig.Address 发送消息到 Map Scene
    var response = (M2G_CreatePlayerResponse)await gateScene.Call(
        mapSceneConfig.Address,  // 目标 Scene 的 Address
        new G2M_CreatePlayerRequest
        {
            PlayerId = 123456,
            PlayerName = "Alice"
        }
    );

    // 3. 获取到玩家实体的 Address
    var playerAddress = response.PlayerEntityAddress;

    // 4. 后续直接使用玩家实体的 Address 进行通信
    gateScene.Send(playerAddress, new G2M_UpdatePlayerStatus
    {
        Status = PlayerStatus.Online
    });
}
```

### SceneConfigData 常用方法

```csharp
// 根据 SceneType 获取所有该类型的 Scene
List<SceneConfig> GetSceneBySceneType(int sceneType);

// 根据 SceneId 获取 Scene 配置
SceneConfig Get(uint sceneId);

// 根据进程 ID 获取该进程下的所有 Scene
IEnumerable<SceneConfig> GetSceneByProcess(uint processConfigId);
```

**关键点：**
- SceneConfig.Address 就是该 Scene 实例的 Address
- Scene 也是 Entity，所以也有 Address 属性
- 第一次通信通常发送到 Scene，由 Scene 创建业务实体
- 拿到业务实体的 Address 后，后续通信就直接使用实体 Address

---

## 发送 Address 消息

直接使用 `Scene` 的扩展方法发送 Address 消息。

### 1. 发送单向消息 - Send()

```csharp
public void Send<T>(long address, T message) where T : IAddressMessage
```

**示例：**
```csharp
// Gate 服务器发送单向消息给 Map 服务器上的某个实体
var playerEntity = GetPlayerEntity();
var message = new G2M_NotifyPlayerMove
{
    X = 100,
    Y = 200
};

// 使用 playerEntity.Address 发送消息
scene.Send(playerEntity.Address, message);
```

### 2. 发送 RPC 请求 - Call()

```csharp
public async FTask<IResponse> Call<T>(long address, T request) where T : IAddressMessage
```

**示例：**
```csharp
// Gate 服务器向 Map 服务器上的玩家实体发送 RPC 请求
var playerEntity = GetPlayerEntity();
var request = new G2M_GetPlayerDataRequest
{
    PlayerId = playerEntity.Id
};

// 使用 playerEntity.Address 发送 RPC 请求
var response = (M2G_GetPlayerDataResponse)await scene.Call(playerEntity.Address, request);

if (response.ErrorCode == 0)
{
    Log.Info($"获取玩家数据成功: {response.PlayerName}");
}
```

---

## 处理 Address 消息

在目标服务器上实现消息处理器来接收 Address 消息。

**Address 消息处理器基类：**
- `Address<TEntity, TMessage>` - 处理单向 Address 消息
- `AddressRPC<TEntity, TRequest, TResponse>` - 处理 RPC Address 请求

**重要特性：**
- 第一个泛型参数 `TEntity` 指定处理哪个实体类型（如 `Player`、`Scene`）
- Run 方法的第一个参数直接是目标实体，框架会自动根据 Address 查找并传入
- 无需手动调用 `scene.GetEntity(address)` 获取实体

### 处理单向消息

```csharp
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

// 处理 IAddressMessage，使用 Address<TEntity, TMessage> 基类
public class G2M_NotifyPlayerMoveHandler : Address<Player, G2M_NotifyPlayerMove>
{
    protected override async FTask Run(Player player, G2M_NotifyPlayerMove message)
    {
        // 直接使用 player 参数，框架会自动根据 Address 找到对应的实体
        if (player == null || player.IsDisposed)
        {
            Log.Warning("玩家实体不存在或已销毁");
            return;
        }

        // 处理移动逻辑
        player.X = message.X;
        player.Y = message.Y;
        Log.Info($"玩家移动到: ({message.X}, {message.Y})");

        await FTask.CompletedTask;
    }
}
```

### 处理 RPC 请求

```csharp
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

// 处理 IAddressRequest，使用 AddressRPC<TEntity, TRequest, TResponse> 基类
public class G2M_GetPlayerDataRequestHandler : AddressRPC<Player, G2M_GetPlayerDataRequest, M2G_GetPlayerDataResponse>
{
    protected override async FTask Run(
        Player player,
        G2M_GetPlayerDataRequest request,
        M2G_GetPlayerDataResponse response,
        Action reply)
    {
        // 直接使用 player 参数，框架会自动根据 Address 找到对应的实体
        if (player == null || player.IsDisposed)
        {
            response.ErrorCode = 1001;
            return;
        }

        // 填充响应数据
        response.PlayerName = player.Name;
        response.Level = player.Level;

        await FTask.CompletedTask;
    }
}
```

---

## 完整示例

### 场景：Gate 服务器通知 Map 服务器创建玩家实体

**1. 定义协议（Inner/InnerMessage.proto）：**
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

**2. Gate 服务器发送请求：**
```csharp
public class GateServerLogic
{
    public async FTask CreatePlayerOnMap(Scene scene, long playerId, string playerName)
    {
        // 步骤1: 通过配置表获取目标服务器的 Scene
        // 这是第一次通信时的关键步骤，因为此时还不知道目标实体的 Address
        var mapSceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
        var mapSceneAddress = mapSceneConfig.Address;

        // 步骤2: 使用 Scene 的 Address 向目标服务器发送 RPC 请求
        var response = (M2G_CreatePlayerResponse)await scene.Call(
            mapSceneAddress,  // 目标 Scene 的 Address
            new G2M_CreatePlayerRequest
            {
                PlayerId = playerId,
                PlayerName = playerName
            }
        );

        if (response.ErrorCode == 0)
        {
            // 步骤3: 获取到玩家实体的 Address
            Log.Info($"Map 服务器创建玩家实体成功，Address: {response.PlayerEntityAddress}");

            // 步骤4: 保存 PlayerEntityAddress，后续可以直接向这个 Address 发送消息
            // 不再需要通过 SceneConfig 查找
            return response.PlayerEntityAddress;
        }

        return 0;
    }
}
```

**3. Map 服务器处理请求：**
```csharp
// 使用 AddressRPC<Scene, ...> 因为消息是发送到 Map Scene 的
public class G2M_CreatePlayerRequestHandler : AddressRPC<Scene, G2M_CreatePlayerRequest, M2G_CreatePlayerResponse>
{
    protected override async FTask Run(
        Scene scene,
        G2M_CreatePlayerRequest request,
        M2G_CreatePlayerResponse response,
        Action reply)
    {
        // 直接使用 scene 参数，框架会自动找到对应的 Scene

        // 创建玩家实体
        var playerEntity = Entity.Create<Player>(scene, request.PlayerId, true, true);
        playerEntity.Name = request.PlayerName;

        // 返回玩家实体的 Address
        response.PlayerEntityAddress = playerEntity.Address;

        Log.Info($"创建玩家实体成功: {playerEntity.Address}");

        await FTask.CompletedTask;
    }
}
```

**4. 后续直接向玩家实体发送消息：**
```csharp
// Gate 服务器向 Map 服务器上的玩家实体发送消息
scene.Send(playerEntityAddress, new G2M_UpdatePlayerStatus
{
    Status = PlayerStatus.Online
});
```

---

## 最佳实践

### 1. **在处理器中检查实体状态**

```csharp
// 在 Address 处理器中，框架会自动传入实体，只需检查状态
public class MyAddressHandler : Address<Player, MyMessage>
{
    protected override async FTask Run(Player player, MyMessage message)
    {
        if (player == null || player.IsDisposed)
        {
            Log.Warning("玩家实体不存在或已销毁");
            return;
        }

        // 处理业务逻辑...
        await FTask.CompletedTask;
    }
}
```

### 2. **错误处理**

```csharp
var response = (M2G_TestResponse)await scene.Call(address, request);
if (response.ErrorCode != 0)
{
    Log.Error($"调用失败: {response.ErrorCode}");
    return;
}
```

### 3. **避免循环依赖**

服务器 A 向服务器 B 发送 RPC 请求，服务器 B 的处理器中不要再向服务器 A 发送 RPC 请求，避免死锁。

### 4. **超时处理**

框架会自动处理超时（默认 30 秒），超时后会返回 `InnerErrorCode.ErrRouteTimeout` 错误。

---

## 相关文档

- [01-Session.md](../../03-Networking/01-Session.md) - Session 使用指南
- [02-MessageHandler.md](../../03-Networking/02-MessageHandler.md) - 消息处理器实现指南
- [08-Roaming.md](08-Roaming.md) - Roaming 漫游消息（分布式路由）

---

## 总结

Address 消息是 Fantasy Framework 中服务器间通信的基础：

**核心概念：**
1. **基于实体地址**: 使用 `Entity.Address` 作为地址
2. **获取初始 Address**: 通过 `SceneConfigData` 查找目标服务器 Scene 的 Address
3. **透明发送**: 框架自动将消息发送到目标服务器并找到目标实体

**使用方式：**
4. **两种发送模式**: `scene.Send()` 单向消息、`scene.Call()` RPC 请求
5. **两种处理器基类**: `Address<TEntity, TMessage>` 和 `AddressRPC<TEntity, TRequest, TResponse>`
6. **自动实体注入**: 处理器的 Run 方法直接接收目标实体参数，无需手动查找

**技术特性：**
7. **内网协议**: 使用 `IAddressMessage` 和 `IAddressRequest` 接口
8. **高性能**: 直接发送，无需中心化查询

**典型流程**: SceneConfig.Address → 发送到目标 Scene → Scene 创建实体并返回 Address → 后续直接使用实体 Address
