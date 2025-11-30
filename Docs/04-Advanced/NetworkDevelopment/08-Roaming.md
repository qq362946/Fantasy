# Roaming 漫游消息 - 分布式实体路由

## 什么是 Roaming？

Roaming（漫游）让客户端可以**通过 Gate 服务器自动路由到后端服务器**（如 Map、Chat、Battle），无需在 Gate 写转发代码。

**一句话总结：** 客户端发送消息 → Gate 自动转发 → 后端服务器处理 → 自动返回给客户端

**适用场景：** 客户端需要与多个后端服务器通信（Chat、Map、Battle 等）

---

## 快速开始

### 完整流程

```
1. 定义协议（带 RoamingType）
   ↓
2. 客户端登录时建立漫游路由（一次性）
   ↓
3. 客户端发送漫游消息
   ↓
4. Gate 自动转发到后端服务器
   ↓
5. 后端服务器处理并返回
```

下面按步骤详细说明。

---

## 步骤 1：定义协议

### 协议文件位置

Roaming 协议定义在网络协议目录的 `.proto` 文件中：

```
NetworkProtocol/
  ├── Outer/              # 客户端协议（客户端到服务器）
  │   └── OuterMessage.proto
  ├── Inner/              # 服务器间协议（服务器到服务器）
  │   └── InnerMessage.proto
  └── RoamingType.Config  # RoamingType 定义文件
```

### 配置 RoamingType

在定义 Roaming 协议之前，必须先在 `RoamingType.Config` 文件中定义 RoamingType 名称和数值。

**RoamingType.Config 示例：**

```
// Roaming协议定义(需要定义10000以上、因为10000以内的框架预留)
MapRoamingType = 10001
ChatRoamingType = 10002
BattleRoamingType = 10003
```

**配置规则：**

- RoamingType 的数值必须 >= 10000（10000 以下为框架预留）
- 每个 RoamingType 对应一个唯一的数值
- 定义格式：`RoamingType名称 = 数值`
- 命名规范：`XXXRoamingType`（如 `ChatRoamingType`、`MapRoamingType`）

**重要：** 协议定义中的 RoamingType 名称必须与配置文件中的名称一致。

---

### 单向漫游消息（IRoamingMessage）

单向消息只发送，不需要等待响应。

**定义格式：**

```protobuf
message 消息名称 // IRoamingMessage,RoamingType名称
{
    字段定义...
}
```

**参数说明：**

- `消息名称`：消息的名称（如 `C2Chat_TestMessage`）
- `// IRoamingMessage`：固定写法，标识这是单向漫游消息
- `,RoamingType名称`：指定路由到哪个服务器（如 `ChatRoamingType`）

**示例：**

```protobuf
// 客户端到 Chat 的单向消息
message C2Chat_TestMessage // IRoamingMessage,ChatRoamingType
{
    string Tag = 1;
}

// 客户端到 Map 的单向消息
message C2Map_TestMessage // IRoamingMessage,MapRoamingType
{
    string Tag = 1;
}

// Gate 到 Chat 的单向消息
message G2Chat_TestMessage // IRoamingMessage,ChatRoamingType
{
    string Content = 1;
}
```

---

### RPC 漫游消息（IRoamingRequest/IRoamingResponse）

RPC 消息需要发送请求并等待响应。

**定义格式：**

```protobuf
// 请求消息
message 请求名称 // IRoamingRequest,响应名称,RoamingType名称
{
    字段定义...
}

// 响应消息
message 响应名称 // IRoamingResponse
{
    字段定义...
}
```

**参数说明：**

- `请求名称`：请求消息的名称（如 `C2Chat_GetDataRequest`）
- `// IRoamingRequest`：固定写法，标识这是 RPC 请求
- `,响应名称`：对应的响应消息名称（如 `Chat2C_GetDataResponse`）
- `,RoamingType名称`：指定路由到哪个服务器（如 `ChatRoamingType`）
- 响应消息必须使用 `// IRoamingResponse`

**示例：**

```protobuf
// 客户端到 Chat 的 RPC 请求
message C2Chat_GetDataRequest // IRoamingRequest,Chat2C_GetDataResponse,ChatRoamingType
{
    int64 PlayerId = 1;
}

message Chat2C_GetDataResponse // IRoamingResponse
{
    string ChatData = 1;
}

// Gate 到 Map 的 RPC 请求
message G2Map_GetPlayerRequest // IRoamingRequest,Map2G_GetPlayerResponse,MapRoamingType
{
    int64 PlayerId = 1;
}

message Map2G_GetPlayerResponse // IRoamingResponse
{
    string PlayerName = 1;
    int32 Level = 2;
}
```

---

## 步骤 2：建立漫游路由

建立漫游路由后，客户端可以通过 Gate 自动与后端服务器（如 Chat、Map）通信。这个操作**在客户端登录后执行一次**。

### Gate 服务器：创建 Roaming 并链接到后端服务器

**核心 API：**

```csharp
// 1. 创建 Roaming 组件
RoamingComponent session.CreateRoaming(long roamingId, bool isAutoDispose, int delayRemove);

// 2. 链接到后端服务器
uint roaming.Link(Session session, SceneConfig sceneConfig, int roamingType);
```

**完整示例：**

```csharp
// Gate 服务器：处理客户端的登录请求
public class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(
        Session session,
        C2G_LoginRequest request,
        G2C_LoginResponse response,
        Action reply)
    {
        // 步骤 1：创建 Roaming 组件
        // roamingId: 漫游的唯一标识，通常使用玩家 ID
        // isAutoDispose: Session 断开时是否自动断开漫游功能
        // delayRemove: 延迟多久执行断开（毫秒）
        var roaming = session.CreateRoaming(
            roamingId: request.PlayerId,
            isAutoDispose: true,
            delayRemove: 1000
        );

        // 步骤 2：链接到 Chat 服务器
        var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
        var errorCode = await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);

        if (errorCode != 0)
        {
            response.ErrorCode = errorCode;
            return;
        }

        Log.Info($"✅ 为玩家 {request.PlayerId} 建立到Chat的漫游路由");
        await FTask.CompletedTask;
    }
}
```

---

### 后端服务器：监听 OnCreateTerminus 事件并创建业务实体

当 Gate 调用 `roaming.Link()` 时，框架会自动在后端服务器（如 Chat）上创建 `Terminus`，并触发 `OnCreateTerminus` 事件。

**核心 API：**

```csharp
// 创建并关联业务实体到 Terminus
FTask<T> terminus.LinkTerminusEntity<T>(bool autoDispose);

// 关联已有实体到 Terminus
FTask terminus.LinkTerminusEntity(Entity entity, bool autoDispose);
```

**重要说明：**

- `LinkTerminusEntity()` 是**可选的**，不调用也可以正常使用 Roaming
- 如果不调用 `LinkTerminusEntity()`，漫游消息处理器接收到的实体就是 `Terminus` 本身
- 如果调用了 `LinkTerminusEntity()`，漫游消息处理器接收到的实体就是关联的业务实体（如 `ChatPlayer`）

**完整示例：**

```csharp
// Chat 服务器：监听 OnCreateTerminus 事件
public sealed class OnCreateTerminusHandler : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        switch (self.Terminus.RoamingType)
        {
            case RoamingType.ChatRoamingType:
            {
                // 方式 1：创建新实体并关联
                var chatPlayer = await self.Terminus.LinkTerminusEntity<ChatPlayer>(autoDispose: true);

                if (chatPlayer == null)
                {
                    Log.Error("创建 ChatPlayer 失败");
                    return;
                }

                // 初始化 ChatPlayer
                chatPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                chatPlayer.LoadData();

                Log.Info($"✅ Chat 服务器创建了 ChatPlayer，PlayerId={chatPlayer.PlayerId}");
                break;
            }
            case RoamingType.MapRoamingType:
            {
                // 方式 2：先创建实体，再关联
                var mapPlayer = Entity.Create<MapPlayer>(self.Scene);
                mapPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                await self.Terminus.LinkTerminusEntity(mapPlayer, autoDispose: true);

                Log.Info($"✅ Map 服务器创建了 MapPlayer，PlayerId={mapPlayer.PlayerId}");
                break;
            }
        }

        await FTask.CompletedTask;
    }

    private long GetPlayerIdFromRoamingId(Terminus terminus)
    {
        // 假设 roamingId 就是 PlayerId
        return terminus.RuntimeId;
    }
}
```

**autoDispose 参数说明：**

- `autoDispose=true`：Terminus 销毁时**自动销毁**关联的实体（推荐）
- `autoDispose=false`：Terminus 销毁时**不销毁**关联的实体（需手动管理）

**✅ 路由建立完成！** 现在客户端可以发送 `C2Chat_xxx` 或 `C2Map_xxx` 消息，Gate 会自动转发到对应的后端服务器。

---

## 步骤 3：发送漫游消息

路由建立后，客户端可以直接发送消息，Gate 会**自动转发**。

### 客户端发送消息

```csharp
// Unity 客户端代码
public async FTask SendChatMessage(Session session, string content)
{
    // 直接发送消息，Gate 会自动转发到 Chat 服务器
    var response = (Chat2C_SendMessageResponse)await session.Call(
        new C2Chat_SendMessageRequest
        {
            Content = content
        }
    );

    if (response.ErrorCode == 0 && response.Success)
    {
        Log.Info("✅ 消息发送成功");
    }
}
```

**重点：**

- Gate 服务器**无需写任何转发代码**
- 框架根据协议定义中的 `ChatRoamingType` 自动转发到 Chat 服务器

---

### Gate 服务器主动发送漫游消息

```csharp
// Gate 服务器向 Chat 服务器发送消息
public async FTask SendToChatServer(Scene scene, Session session, int chatRoamingType)
{
    // 获取 Terminus
    var terminus = scene.TerminusComponent.GetTerminus(session, chatRoamingType);
    if (terminus == null)
    {
        Log.Error("❌ Terminus 不存在，请先建立路由");
        return;
    }

    // 发送单向消息
    terminus.Send(chatRoamingType, new G2Chat_TestRoamingMessage
    {
        Content = "Hello from Gate"
    });

    // 发送 RPC 请求
    var response = (Chat2G_GetDataResponse)await terminus.Call(
        chatRoamingType,
        new G2Chat_GetDataRequest
        {
            PlayerId = session.PlayerId
        }
    );

    if (response.ErrorCode == 0)
    {
        Log.Info($"✅ 从 Chat 获取数据: {response.ChatData}");
    }
}
```

---

## 步骤 4：处理漫游消息

在后端服务器（Chat）上实现消息处理器。

### 处理客户端的漫游消息

```csharp
// Chat 服务器：处理客户端的 RPC 消息
public class C2Chat_SendMessageRequestHandler : RoamingRPC<ChatPlayer, C2Chat_SendMessageRequest, Chat2C_SendMessageResponse>
{
    protected override async FTask Run(
        ChatPlayer chatPlayer,  // 框架自动找到 ChatPlayer 实体
        C2Chat_SendMessageRequest request,
        Chat2C_SendMessageResponse response,
        Action reply)
    {
        if (chatPlayer == null || chatPlayer.IsDisposed)
        {
            response.ErrorCode = 1001;
            return;
        }

        chatPlayer.SendMessage(request.Content);
        response.Success = true;

        Log.Info($"✅ ChatPlayer {chatPlayer.PlayerId} 发送消息: {request.Content}");
        await FTask.CompletedTask;
    }
}
```

---

### 处理 Gate 的漫游消息

```csharp
// Chat 服务器：处理 Gate 的单向消息
public class G2Chat_TestRoamingMessageHandler : Roaming<ChatPlayer, G2Chat_TestRoamingMessage>
{
    protected override async FTask Run(ChatPlayer chatPlayer, G2Chat_TestRoamingMessage message)
    {
        if (chatPlayer == null || chatPlayer.IsDisposed)
        {
            Log.Warning("❌ ChatPlayer 不存在");
            return;
        }

        Log.Info($"✅ 收到 Gate 消息: {message.Content}");
        chatPlayer.ProcessGateMessage(message.Content);

        await FTask.CompletedTask;
    }
}

// Chat 服务器：处理 Gate 的 RPC 请求
public class G2Chat_GetDataRequestHandler : RoamingRPC<ChatPlayer, G2Chat_GetDataRequest, Chat2G_GetDataResponse>
{
    protected override async FTask Run(
        ChatPlayer chatPlayer,
        G2Chat_GetDataRequest request,
        Chat2G_GetDataResponse response,
        Action reply)
    {
        if (chatPlayer == null || chatPlayer.IsDisposed)
        {
            response.ErrorCode = 1001;
            return;
        }

        response.ChatData = chatPlayer.GetChatHistory();
        Log.Info($"✅ 返回 ChatPlayer {chatPlayer.PlayerId} 的数据");

        await FTask.CompletedTask;
    }
}
```

**重点：**

- 使用 `Roaming<TEntity, TMessage>` 处理单向消息
- 使用 `RoamingRPC<TEntity, TRequest, TResponse>` 处理 RPC 请求
- 第一个泛型参数 `TEntity` 是 Terminus 关联的实体类型（如 `ChatPlayer`）
- 框架会自动找到对应的实体并传入 Run 方法

---

## 高级功能

### Terminus 向客户端发送消息

Chat 服务器可以通过 Terminus 主动向客户端发送消息：

```csharp
// Chat 服务器代码
public class ChatPlayerLogic
{
    public void NotifyClient(ChatPlayer chatPlayer, string notification)
    {
        // 获取 ChatPlayer 关联的 Terminus
        var terminus = chatPlayer.GetComponent<TerminusFlagComponent>()?.Terminus;
        if (terminus == null)
        {
            Log.Error("❌ Terminus 不存在");
            return;
        }

        // 直接发送消息给客户端
        terminus.Send(new Chat2C_Notification
        {
            Content = notification
        });

        Log.Info("✅ 向客户端发送通知");
    }
}
```

---

### Terminus 传送

将玩家实体从一个服务器传送到另一个服务器：

```csharp
// 将玩家从 Map1 传送到 Map2
public async FTask TransferPlayer(Terminus terminus, long targetSceneAddress)
{
    var errorCode = await terminus.StartTransfer(targetSceneAddress);

    if (errorCode == 0)
    {
        Log.Info("✅ Terminus 传送成功");
    }
    else
    {
        Log.Error($"❌ Terminus 传送失败: {errorCode}");
    }
}
```

**传送机制：**

1. 锁定 Terminus，暂停消息发送
2. 序列化 Terminus 和关联实体
3. 发送到目标服务器
4. 目标服务器恢复实体并解锁
5. 原服务器销毁实体

---

## 常见问题

### Q1: 什么时候使用 Roaming？什么时候使用 Address？

**使用 Roaming：**

- ✅ 客户端通过 Gate 与后端服务器通信
- ✅ 需要减少 Gate 转发代码
- ✅ 玩家实体需要在多个服务器间传送

**使用 Address：**

- ✅ 服务器间直接通信（不经过 Gate）
- ✅ 一次性的 RPC 调用
- ✅ 不需要维护路由关系

---

### Q2: 一个客户端可以建立多个 Roaming 路由吗？

可以！一个客户端可以同时建立到多个服务器的路由：

```csharp
// 建立到 Chat 的路由
await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);

// 建立到 Map 的路由
await roaming.Link(session, mapConfig, RoamingType.MapRoamingType);

// 建立到 Battle 的路由
await roaming.Link(session, battleConfig, RoamingType.BattleRoamingType);
```

---

### Q3: Terminus 什么时候销毁？

- 客户端断开连接时，Gate 上的 Terminus 自动销毁
- Terminus 销毁时，如果 `autoDispose=true`，关联的实体也会销毁
- 可以手动调用 `terminus.Dispose()` 销毁

---

### Q4: autoDispose 参数应该设置为 true 还是 false？

```csharp
// autoDispose=true（推荐）
// Terminus 销毁时自动销毁关联实体，适用于大部分场景
var player = await terminus.LinkTerminusEntity<Player>(autoDispose: true);

// autoDispose=false
// Terminus 销毁时不销毁关联实体，需要手动管理实体生命周期
var player = await terminus.LinkTerminusEntity<Player>(autoDispose: false);
```

---

### Q5: 为什么需要 RoamingType？

因为一个客户端可能需要与多个后端服务器通信：

```
客户端 --ChatRoamingType--> Gate --自动路由--> Chat 服务器
       --MapRoamingType--> Gate --自动路由--> Map 服务器
       --BattleRoamingType--> Gate --自动路由--> Battle 服务器
```

RoamingType 告诉框架应该路由到哪个服务器。

---

### Q6: Terminus 可以替换关联的实体吗？

**不可以！** 一个 Terminus 只能关联一次实体：

```csharp
// ❌ 错误
var player1 = await terminus.LinkTerminusEntity<Player>(autoDispose: true);
var player2 = await terminus.LinkTerminusEntity<Player>(autoDispose: true); // 报错！

// 原因：TerminusId 已在分布式系统中使用，替换会导致路由失效
```

---

## 相关文档

- [06-Address消息.md](06-Address消息.md) - Address 消息 - 服务器间实体通信
- [01-Session.md](../../03-Networking/01-Session.md) - Session 使用指南
- [02-MessageHandler.md](../../03-Networking/02-MessageHandler.md) - 消息处理器实现指南

---

## 总结

Roaming 漫游系统的核心优势：

1. **减少代码量**：Gate 无需写转发代码，协议数量减少 50%
2. **自动路由**：框架根据 RoamingType 自动转发消息
3. **支持传送**：实体可以在服务器间传送，路由自动更新
4. **简化架构**：客户端无需知道后端服务器地址

**使用步骤回顾：**

1. 定义协议（带 RoamingType）
2. 客户端登录时建立路由（一次性）
3. 发送消息（自动转发）
4. 处理消息（使用 Roaming 处理器）
