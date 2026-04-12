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
2. 客户端登录或重连时建立漫游路由
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

建立漫游路由后，客户端可以通过 Gate 自动与后端服务器（如 Chat、Map）通信。这个操作在客户端首次登录和断线重连时都可以直接调用 `Link`，框架会自动判断是首次连接还是重连。

### Gate 服务器：创建 Roaming 并链接到后端服务器

**核心 API：**

```csharp
// 1. 创建 Roaming 组件（简单版本，直接返回组件）
SessionRoamingComponent session.CreateRoaming(long roamingId, bool isAutoDispose, int delayRemove);

// 2. 创建 Roaming 组件（详细版本，返回状态信息）
CreateRoamingResult session.TryCreateRoaming(long roamingId, bool isAutoDispose, int delayRemove);

// 3. 链接到后端服务器（自动判断首次连接或断线重连）
uint roaming.Link(Session session, SceneConfig sceneConfig, int roamingType, Entity? args = null);

// 4. 判断指定 roamingType 是否已建立漫游关系
bool roaming.IsLinked(int roamingType);

// 5. 重新链接到后端服务器（已废弃，请使用 Link）
[Obsolete] uint roaming.ReLink(Session session, SceneConfig sceneConfig, int roamingType, Entity? args = null);
```

**CreateRoamingResult 结构体：**

```csharp
public readonly struct CreateRoamingResult
{
    // 创建状态
    public readonly CreateRoamingStatus Status;

    // 漫游组件实例（如果创建失败则为null）
    public readonly SessionRoamingComponent Roaming;
}

public enum CreateRoamingStatus
{
    NewCreated,              // 新创建的漫游组件
    AlreadyExists,           // 使用已存在的漫游组件（断线重连场景）
    SessionAlreadyHasRoaming // 错误：当前Session已经创建了不同roamingId的漫游组件
}
```

**完整示例（使用 CreateRoaming）：**

```csharp
// Gate 服务器：处理客户端的登录请求 - 简单版本
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
        var roaming = await session.CreateRoaming(
            roamingId: request.PlayerId,
            isAutoDispose: true,
            delayRemove: 1000
        );

        if (roaming == null)
        {
            response.ErrorCode = ErrorCode.RoamingCreateFailed;
            return;
        }

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

**传递自定义参数到后端服务器：**

```csharp
// Gate 服务器：传递初始化参数到 Chat 服务器
public class C2G_LoginWithDataRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(
        Session session,
        C2G_LoginRequest request,
        G2C_LoginResponse response,
        Action reply)
    {
        var roaming = await session.CreateRoaming(
            roamingId: request.PlayerId,
            isAutoDispose: true,
            delayRemove: 1000
        );

        if (roaming == null)
        {
            response.ErrorCode = ErrorCode.RoamingCreateFailed;
            return;
        }

        // 创建要传递的参数实体
        var loginData = Entity.Create<PlayerLoginData>(session.Scene);
        loginData.PlayerName = request.PlayerName;
        loginData.Level = request.Level;
        loginData.VipLevel = request.VipLevel;

        // 链接到 Chat 服务器并传递参数
        var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
        var errorCode = await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType, loginData);

        if (errorCode != 0)
        {
            response.ErrorCode = errorCode;
            // ⚠️ Link 失败时，参数未传递到后端，Gate 需要销毁
            loginData.Dispose();
            return;
        }

        // ⚠️ Link 成功后，参数已通过序列化传递到后端服务器
        // Gate 服务器上的原始对象需要立即销毁，销毁责任已转移到后端
        loginData.Dispose();

        Log.Info($"✅ 为玩家 {request.PlayerId} 建立到Chat的漫游路由，并传递了登录数据");
        await FTask.CompletedTask;
    }
}
```

**⚠️ 重要：args 参数的内存管理**

`Entity.Create<T>()` 创建的 Entity 使用了对象池，**必须在后端服务器的 OnCreateTerminus 事件中手动 Dispose() 销毁**，否则会导致内存泄露：

```csharp
// Chat 服务器：接收并销毁 args 参数
public sealed class OnCreateTerminusHandler : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        switch (self.Terminus.RoamingType)
        {
            case RoamingType.ChatRoamingType:
            {
                var chatPlayer = await self.Terminus.LinkTerminusEntity<ChatPlayer>(autoDispose: true);

                if (chatPlayer == null)
                {
                    Log.Error("创建 ChatPlayer 失败");

                    // ⚠️ 创建失败时也要销毁 args 参数
                    self.Args?.Dispose();
                    return;
                }

                // 使用传递的参数进行初始化
                if (self.Args is PlayerLoginData loginData)
                {
                    chatPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                    chatPlayer.PlayerName = loginData.PlayerName;
                    chatPlayer.Level = loginData.Level;
                    chatPlayer.VipLevel = loginData.VipLevel;

                    // ⚠️ 使用完毕后立即销毁，归还对象池，防止内存泄露
                    loginData.Dispose();

                    Log.Info($"✅ Chat 服务器使用传递的参数创建了 ChatPlayer，已销毁参数");
                }
                else
                {
                    // 没有传递参数或参数类型不匹配，使用默认初始化
                    chatPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                    chatPlayer.LoadData();

                    // ⚠️ 即使参数类型不匹配，也要销毁参数
                    self.Args?.Dispose();

                    Log.Info($"✅ Chat 服务器创建了 ChatPlayer，使用默认初始化");
                }

                break;
            }
            case RoamingType.MapRoamingType:
            {
                var mapPlayer = Entity.Create<MapPlayer>(self.Scene);
                mapPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                await self.Terminus.LinkTerminusEntity(mapPlayer, autoDispose: true);

                // ⚠️ Map 不需要参数，但仍需销毁传递过来的参数
                self.Args?.Dispose();

                Log.Info($"✅ Map 服务器创建了 MapPlayer，PlayerId={mapPlayer.PlayerId}");
                break;
            }
        }

        await FTask.CompletedTask;
    }

    private long GetPlayerIdFromRoamingId(Terminus terminus)
    {
        return terminus.RuntimeId;
    }
}
```

**⚠️ 内存管理核心要点：**

1. **Entity.Create<T>() 使用对象池**：所有通过 `Entity.Create<T>()` 创建的实体都使用对象池
2. **Gate 服务器必须销毁**：Gate 服务器在 Link 调用后（无论成功还是失败）都必须调用 `args.Dispose()` 销毁原始对象
3. **后端服务器必须销毁**：后端服务器在 `OnCreateTerminus` 中接收到反序列化的参数副本后，使用完毕必须调用 `Args?.Dispose()` 销毁
4. **双端都要销毁**：参数通过序列化传递，Gate 有原始对象，后端有反序列化的副本，**两端都需要各自销毁**

```csharp
// Gate 服务器：Link 后必须销毁原始对象
var loginData = Entity.Create<PlayerLoginData>(session.Scene);
loginData.PlayerName = request.PlayerName;
loginData.Level = request.Level;

var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
var errorCode = await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType, loginData);

if (errorCode != 0)
{
    response.ErrorCode = errorCode;
    // ⚠️ Link 失败：Gate 销毁原始对象（参数未传递）
    loginData.Dispose();
    return;
}

// ⚠️ Link 成功：Gate 仍需销毁原始对象（参数已序列化传递，后端会收到副本）
loginData.Dispose();
```

**内存管理最佳实践：**

| 场景 | Gate 服务器 | 后端服务器 | 说明 |
|------|-----------|-----------|------|
| Link 成功 | 调用 `args.Dispose()` 销毁原始对象 | 在 `OnCreateTerminus` 中调用 `Args?.Dispose()` 销毁副本 | 双端都需要销毁 |
| Link 失败 | 调用 `args.Dispose()` 销毁原始对象 | 不涉及 | 参数未传递，只有 Gate 需要销毁 |
| 参数类型不匹配 | 调用 `args.Dispose()` 销毁原始对象 | 调用 `Args?.Dispose()` 销毁副本 | 即使不使用参数，也必须销毁 |
| 创建失败 | 调用 `args.Dispose()` 销毁原始对象 | 调用 `Args?.Dispose()` 销毁副本 | 即使 `LinkTerminusEntity()` 失败，也要销毁参数 |
| 不需要参数的 RoamingType | 调用 `args.Dispose()` 销毁原始对象 | 调用 `Args?.Dispose()` 销毁副本 | 防止误传参数导致内存泄露 |

**完整示例（使用 TryCreateRoaming 处理断线重连）：**

```csharp
// Gate 服务器：处理客户端的登录/重连请求 - 支持断线重连
public class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(
        Session session,
        C2G_LoginRequest request,
        G2C_LoginResponse response,
        Action reply)
    {
        // 步骤 1：创建 Roaming 组件，获取详细状态
        var result = await session.TryCreateRoaming(
            roamingId: request.PlayerId,
            isAutoDispose: true,
            delayRemove: 180000  // 3分钟延迟删除，支持断线重连
        );

        if (result.Status == CreateRoamingStatus.SessionAlreadyHasRoaming)
        {
            Log.Error($"❌ Session 已经创建了其他 roamingId 的漫游组件");
            response.ErrorCode = ErrorCode.SessionAlreadyHasRoaming;
            return;
        }

        var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];

        // 步骤 2：Link 自动判断首次连接或断线重连
        var errorCode = await result.Roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);

        if (errorCode != 0)
        {
            response.ErrorCode = errorCode;
            return;
        }

        await FTask.CompletedTask;
    }
}
```

如果需要根据首次登录和断线重连执行不同的业务逻辑，可以结合 `CreateRoamingStatus` 判断：

```csharp
switch (result.Status)
{
    case CreateRoamingStatus.NewCreated:
    {
        // 首次登录
        var errorCode = await result.Roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
        if (errorCode != 0) { response.ErrorCode = errorCode; return; }
        Log.Info($"✅ 玩家 {request.PlayerId} 首次登录，建立漫游路由");
        break;
    }
    case CreateRoamingStatus.AlreadyExists:
    {
        // 断线重连
        var errorCode = await result.Roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
        if (errorCode != 0) { response.ErrorCode = errorCode; return; }
        Log.Info($"✅ 玩家 {request.PlayerId} 断线重连成功");
        break;
    }
    case CreateRoamingStatus.SessionAlreadyHasRoaming:
    {
        Log.Error($"❌ Session 已经创建了其他 roamingId 的漫游组件");
        response.ErrorCode = ErrorCode.SessionAlreadyHasRoaming;
        return;
    }
}
```

**Link 的行为：**

| 场景 | 内部行为 | OnCreateTerminus.Type |
|------|---------|----------------------|
| 首次调用（roamingType 未建立） | 创建新的 Terminus | `CreateTerminusType.Link` |
| 再次调用（roamingType 已存在） | 复用或更新现有 Terminus | `CreateTerminusType.ReLink` |

`Link` 内部通过 `IsLinked(roamingType)` 自动判断走哪条路径，无需手动区分。

**`IsLinked` 的用途：**

如果需要精细控制，可以用 `IsLinked()` 在调用前主动判断状态：

```csharp
if (roaming.IsLinked(RoamingType.ChatRoamingType))
{
    // 已建立，可以直接发消息
}
else
{
    // 尚未建立，需要先 Link
    await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
}
```

**两种创建方法的选择：**

| 方法 | 适用场景 | 优点 | 缺点 |
|------|---------|------|------|
| `CreateRoaming()` | 简单场景，不需要详细状态 | 代码简洁，直接获取组件 | 无法区分新创建还是已存在 |
| `TryCreateRoaming()` | 需要详细状态判断的场景（如断线重连） | 可以根据不同状态做不同处理 | 代码稍复杂 |

---

### 后端服务器：监听 OnCreateTerminus 事件并创建业务实体

当 Gate 调用 `roaming.Link()` 时，框架会自动在后端服务器（如 Chat）上创建或更新 `Terminus`，并触发 `OnCreateTerminus` 事件。

**OnCreateTerminus 事件参数：**

```csharp
public struct OnCreateTerminus
{
    /// <summary>
    /// 获取与事件关联的场景实体。
    /// </summary>
    public readonly Scene Scene;

    /// <summary>
    /// 获取与事件关联的Terminus。
    /// </summary>
    public readonly Terminus Terminus;

    /// <summary>
    /// 获取传递过来的参数（来自 Link 方法的 args 参数）
    /// </summary>
    public readonly Entity? Args;

    /// <summary>
    /// 获取漫游终端的创建类型。
    /// 用于区分是首次创建（Link）还是重新连接（ReLink）。
    /// </summary>
    public readonly CreateTerminusType Type;
}
```

**CreateTerminusType 枚举说明：**

| 枚举值 | 说明 |
|--------|------|
| `None` | 未指定类型 |
| `Link` | 首次创建漫游终端（客户端首次登录时） |
| `ReLink` | 重新连接漫游终端（断线重连或目标服务器重启后） |

**核心 API：**

```csharp
// 创建并关联业务实体到 Terminus
FTask<T> terminus.LinkTerminusEntity<T>(bool autoDispose, bool startForwarding = true);

// 关联已有实体到 Terminus
FTask terminus.LinkTerminusEntity(Entity entity, bool autoDispose, bool startForwarding = true);

// 开启或关闭消息转发（关联后可随时调用）
void terminus.SetForwarding(bool isStartForwarding);
```

**参数说明：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `autoDispose` | `bool` | — | Terminus 销毁时是否自动销毁关联实体 |
| `startForwarding` | `bool` | `true` | 关联实体后是否立即开启消息转发到客户端 |

**重要说明：**

- `LinkTerminusEntity()` 是**可选的**，不调用也可以正常使用 Roaming
- 如果不调用 `LinkTerminusEntity()`，漫游消息处理器接收到的实体就是 `Terminus` 本身
- 如果调用了 `LinkTerminusEntity()`，漫游消息处理器接收到的实体就是关联的业务实体（如 `ChatPlayer`）
- `OnCreateTerminus.Args` 可以接收 Gate 服务器 `Link()` 方法传递的自定义参数
- **`OnCreateTerminus.Type` 可用于区分是首次创建还是断线重连**，业务层可根据此参数执行不同的初始化逻辑
- `startForwarding=false` 时，关联后消息不会转发到客户端，可在准备完成后调用 `SetForwarding(true)` 手动开启

**完整示例（区分 Link 和 ReLink）：**

```csharp
// Chat 服务器：监听 OnCreateTerminus 事件，区分首次创建和断线重连
public sealed class OnCreateTerminusHandler : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        switch (self.Terminus.RoamingType)
        {
            case RoamingType.ChatRoamingType:
            {
                // 根据 Type 区分首次创建和断线重连
                switch (self.Type)
                {
                    case CreateTerminusType.Link:
                    {
                        // 首次连接：创建新的玩家实体
                        var chatPlayer = await self.Terminus.LinkTerminusEntity<ChatPlayer>(autoDispose: true);
                        
                        if (chatPlayer == null)
                        {
                            Log.Error("创建 ChatPlayer 失败");
                            self.Args?.Dispose();
                            return;
                        }
                        
                        // 初始化玩家数据
                        chatPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                        chatPlayer.LoadData();
                        
                        Log.Info($"✅ Chat 服务器首次创建 ChatPlayer，PlayerId={chatPlayer.PlayerId}");
                        break;
                    }
                    case CreateTerminusType.ReLink:
                    {
                        // 断线重连：恢复玩家状态
                        // 注意：如果之前已经 LinkTerminusEntity，重连时 TerminusEntity 仍然存在
                        var chatPlayer = self.Terminus.TerminusEntity as ChatPlayer;
                        
                        if (chatPlayer != null)
                        {
                            // 玩家实体仍在，直接恢复在线状态
                            chatPlayer.IsOnline = true;
                            Log.Info($"✅ ChatPlayer {chatPlayer.PlayerId} 断线重连成功，恢复在线状态");
                        }
                        else
                        {
                            // 玩家实体已销毁（可能超时被清理），需要重新创建并从数据库加载
                            chatPlayer = await self.Terminus.LinkTerminusEntity<ChatPlayer>(autoDispose: true);
                            
                            if (chatPlayer == null)
                            {
                                Log.Error("ReLink 时创建 ChatPlayer 失败");
                                self.Args?.Dispose();
                                return;
                            }
                            
                            chatPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                            await chatPlayer.LoadFromDatabase();
                            
                            Log.Info($"✅ ChatPlayer {chatPlayer.PlayerId} 断线重连成功，从数据库恢复数据");
                        }
                        break;
                    }
                }
                
                // ⚠️ 无论 Link 还是 ReLink，都要销毁 Args 参数
                self.Args?.Dispose();
                break;
            }
            case RoamingType.MapRoamingType:
            {
                // 方式 2：先创建实体，再关联
                var mapPlayer = Entity.Create<MapPlayer>(self.Scene);
                mapPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                await self.Terminus.LinkTerminusEntity(mapPlayer, autoDispose: true);

                Log.Info($"✅ Map 服务器创建了 MapPlayer，PlayerId={mapPlayer.PlayerId}");
                
                // ⚠️ 销毁 Args 参数
                self.Args?.Dispose();
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

**使用传递的参数：**

```csharp
// Chat 服务器：使用 Link 传递的参数初始化实体
public sealed class OnCreateTerminusHandler : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        switch (self.Terminus.RoamingType)
        {
            case RoamingType.ChatRoamingType:
            {
                var chatPlayer = await self.Terminus.LinkTerminusEntity<ChatPlayer>(autoDispose: true);

                if (chatPlayer == null)
                {
                    Log.Error("创建 ChatPlayer 失败");
                    // ⚠️ 创建失败时也要销毁 args 参数
                    self.Args?.Dispose();
                    return;
                }

                // 使用传递的参数进行初始化
                if (self.Args is PlayerLoginData loginData)
                {
                    chatPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                    chatPlayer.PlayerName = loginData.PlayerName;
                    chatPlayer.Level = loginData.Level;
                    chatPlayer.VipLevel = loginData.VipLevel;

                    Log.Info($"✅ Chat 服务器使用传递的参数创建了 ChatPlayer，" +
                            $"Name={chatPlayer.PlayerName}, Level={chatPlayer.Level}");

                    // ⚠️ 使用完毕后立即销毁，归还对象池
                    loginData.Dispose();
                }
                else
                {
                    // 没有传递参数，使用默认初始化
                    chatPlayer.PlayerId = GetPlayerIdFromRoamingId(self.Terminus);
                    chatPlayer.LoadData();

                    Log.Info($"✅ Chat 服务器创建了 ChatPlayer，使用默认初始化");

                    // ⚠️ 即使没有匹配的参数类型，也要销毁 Args
                    self.Args?.Dispose();
                }

                break;
            }
        }

        await FTask.CompletedTask;
    }

    private long GetPlayerIdFromRoamingId(Terminus terminus)
    {
        return terminus.RuntimeId;
    }
}
```

**autoDispose 与 startForwarding 参数说明：**

- `autoDispose=true`：Terminus 销毁时**自动销毁**关联的实体（推荐）
- `autoDispose=false`：Terminus 销毁时**不销毁**关联的实体（需手动管理）
- `startForwarding=true`（默认）：关联实体后立即开启消息转发到客户端
- `startForwarding=false`：关联后暂不转发，可在准备完成后调用 `terminus.SetForwarding(true)` 开启

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
    // 获取 roaming
    // 注意 Gate 一般都是为消息转发端，所以 Gate 端需要获取roaming来进行发送
    // 如果是非 Gate 端不要使用
    if (!session.TryGetRoaming(out var roaming))
    {
        Log.Error("❌ roaming 不存在，请先建立Roaming");
        return;
    }

    // 发送单向消息
    roaming.Send(chatRoamingType, new G2Chat_TestRoamingMessage
    {
        Content = "Hello from Gate"
    });

    // 发送 RPC 请求
    var response = (Chat2G_GetDataResponse)await roaming.Call(
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

Chat 服务器可以通过 Terminus 主动向客户端发送消息。有两种方式：

#### 方式 1：使用 TerminusHelper 扩展方法（推荐）

```csharp
// Chat 服务器代码
public class ChatPlayerLogic
{
    public void NotifyClient(ChatPlayer chatPlayer, string notification)
    {
        // 使用扩展方法直接从实体发送消息
        chatPlayer.Send(new Chat2C_Notification
        {
            Content = notification
        });

        Log.Info("✅ 向客户端发送通知");
    }

    // 发送漫游消息到其他服务器
    public void SendToMapServer(ChatPlayer chatPlayer, int mapRoamingType)
    {
        // 发送单向消息
        chatPlayer.Send(mapRoamingType, new Chat2Map_TestMessage
        {
            Data = "Hello Map"
        });
    }

    // 调用其他服务器的 RPC
    public async FTask CallMapServer(ChatPlayer chatPlayer, int mapRoamingType)
    {
        var response = await chatPlayer.Call(mapRoamingType, new Chat2Map_GetDataRequest
        {
            PlayerId = chatPlayer.PlayerId
        });

        if (response.ErrorCode == 0)
        {
            Log.Info("✅ 从 Map 服务器获取数据成功");
        }
    }
}
```

**TerminusHelper 提供的扩展方法：**

| 方法 | 说明 |
|------|------|
| `entity.Send<T>(message)` | 向客户端发送单向消息 |
| `entity.Send<T>(roamingType, message)` | 向指定漫游类型的服务器发送单向消息 |
| `entity.Call<T>(roamingType, request)` | 向指定漫游类型的服务器发送 RPC 请求 |
| `entity.StartTransfer(targetSceneAddress)` | 传送实体到目标场景 |
| `entity.GetLinkTerminus()` | 获取实体关联的 Terminus |
| `entity.TryGetLinkTerminus(out terminus)` | 安全地获取实体关联的 Terminus |

#### 方式 2：通过 Terminus 发送（高性能场景）

```csharp
// Chat 服务器代码 - 性能优化版本
public class ChatPlayerLogic
{
    public void NotifyClientOptimized(ChatPlayer chatPlayer, string notification)
    {
        // 先获取 Terminus，避免重复查找
        if (!chatPlayer.TryGetLinkTerminus(out var terminus))
        {
            Log.Error("❌ Terminus 不存在");
            return;
        }

        // 直接使用 Terminus 发送
        terminus.Send(new Chat2C_Notification
        {
            Content = notification
        });

        Log.Info("✅ 向客户端发送通知");
    }

    // 频繁发送消息时的最佳实践
    public void SendMultipleMessages(ChatPlayer chatPlayer)
    {
        // 一次获取，多次使用，避免重复查找组件
        if (!chatPlayer.TryGetLinkTerminus(out var terminus))
        {
            return;
        }

        terminus.Send(new Chat2C_Message1 { });
        terminus.Send(new Chat2C_Message2 { });
        terminus.Send(new Chat2C_Message3 { });
    }
}
```

**性能对比：**

| 场景 | 推荐方式 | 原因 |
|------|---------|------|
| 单次发送 | `entity.Send()` | 代码简洁，性能差异可忽略 |
| 频繁发送（如每帧） | 先获取 `Terminus`，再调用 `terminus.Send()` | 避免重复查找组件，性能更优 |
| 发送多条消息 | 先获取 `Terminus`，再多次调用 | 一次查找，多次使用 |

---

### Terminus 传送

将玩家实体从一个服务器传送到另一个服务器。

#### 发起传送

有两种方式发起传送：

**方式 1：使用实体扩展方法（推荐）**

```csharp
// 将 MapPlayer 从 Map1 传送到 Map2
public async FTask TransferPlayer(MapPlayer mapPlayer, long targetSceneAddress)
{
    var errorCode = await mapPlayer.StartTransfer(targetSceneAddress);

    if (errorCode == 0)
    {
        Log.Info("✅ 玩家传送成功");
        // 注意：传送成功后，当前 mapPlayer 实例已被销毁
    }
    else
    {
        Log.Error($"❌ 玩家传送失败: {errorCode}");
    }
}
```

**方式 2：通过 Terminus 传送**

```csharp
// 将玩家从 Map1 传送到 Map2
public async FTask TransferPlayer(MapPlayer mapPlayer, long targetSceneAddress)
{
    // 获取 Terminus
    if (!mapPlayer.TryGetLinkTerminus(out var terminus))
    {
        Log.Error("❌ Terminus 不存在");
        return;
    }

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

#### 监听传送生命周期

传送过程分为两个阶段，分别通过 `TransferOutSystem` 和 `TransferInSystem` 组件事件处理：

| 组件事件 | 触发时机 | 所在服务器 |
|----------|----------|------------|
| `TransferOutSystem` | 传送发起前 | 原服务器 |
| `TransferInSystem` | 传送到达后 | 目标服务器 |

```csharp
// 原 Map 服务器：传送前执行（保存数据、清理资源等）
public sealed class MapPlayerTransferOutSystem : TransferOutSystem<MapPlayer>
{
    protected override async FTask Out(MapPlayer self)
    {
        // 传送前保存玩家数据
        await self.SaveToDatabase();
        Log.Debug($"MapPlayer {self.PlayerId} 正在离开当前场景");
    }
}

// 目标 Map 服务器：传送到达后执行（加载数据、初始化等）
public sealed class MapPlayerTransferInSystem : TransferInSystem<MapPlayer>
{
    protected override async FTask In(MapPlayer self)
    {
        // 传送到达后加载新地图数据，通知客户端
        await self.LoadMapData();
        self.Send(new Map2C_TransferCompleteNotification
        {
            MapId = self.Scene.SceneConfig.Id,
            Position = self.SpawnPosition
        });
        Log.Debug($"MapPlayer {self.PlayerId} 已到达新场景");
    }
}
```

> **注意：** `TransferOutSystem` 和 `TransferInSystem` 均为纯异步接口，由 Source Generator 编译期自动注册，无需手动注册。

#### 传送机制详解

**传送流程：**

1. **执行 TransferOutSystem**：在原服务器调用 `StartTransfer()` 前，框架触发 `TransferOutSystem`，供业务层保存数据、清理资源
2. **锁定 Terminus**：锁定 Terminus，暂停所有消息发送
3. **序列化**：序列化 Terminus 和关联的实体数据
4. **发送到目标**：通过 `I_TransferTerminusRequest` 将数据发送到目标服务器
5. **目标服务器接收**：目标服务器调用 `TransferComplete()` 恢复数据
6. **反序列化**：恢复 Terminus 和关联实体
7. **解锁**：解锁 Terminus，恢复消息发送
8. **执行 TransferInSystem**：框架触发 `TransferInSystem`，供业务层加载数据、通知客户端
9. **原服务器清理**：原服务器销毁 Terminus 和关联实体

**传送注意事项：**

- ⚠️ 传送完成后，原服务器上的实体会被销毁，不要继续使用原实例
- ⚠️ 如果有其他组件引用了传送的实体，需要提前记录 ID，传送后重新查找
- ⚠️ 传送过程中会锁定消息发送，如果传送失败会自动解锁
- ✅ 客户端的连接不会断开，框架会自动更新路由
- ✅ 传送后客户端发送的消息会自动路由到新服务器

**完整示例：**

```csharp
// 玩家切换地图的完整流程

// 原 Map 服务器：传送前保存数据（TransferOutSystem）
public sealed class MapPlayerTransferOutSystem : TransferOutSystem<MapPlayer>
{
    protected override async FTask Out(MapPlayer self)
    {
        // 1. 保存玩家数据
        await self.SaveToDatabase();

        // 2. 通知客户端开始传送
        self.Send(new Map2C_TransferStartNotification
        {
            TargetMapId = self.TargetMapId
        });

        Log.Info($"✅ 玩家 {self.PlayerId} 准备离开当前地图");
    }
}

// 原 Map 服务器：发起传送
public async FTask<uint> TransferToNewMap(MapPlayer mapPlayer, int targetMapId)
{
    var targetMapConfig = SceneConfigData.Instance.GetSceneBySceneType(targetMapId)[0];
    var errorCode = await mapPlayer.StartTransfer(targetMapConfig.Address);

    if (errorCode != 0)
    {
        Log.Error($"❌ 传送失败: {errorCode}");
        mapPlayer.Send(new Map2C_TransferFailedNotification { ErrorCode = errorCode });
    }

    return errorCode;
}

// 目标 Map 服务器：传送到达后初始化（TransferInSystem）
public sealed class MapPlayerTransferInSystem : TransferInSystem<MapPlayer>
{
    protected override async FTask In(MapPlayer self)
    {
        // 1. 加载新地图数据
        await self.LoadMapData(self.Scene);

        // 2. 设置玩家在新地图的初始位置
        self.SetSpawnPosition();

        // 3. 通知客户端传送完成
        self.Send(new Map2C_TransferCompleteNotification
        {
            MapId = self.Scene.SceneConfig.Id,
            Position = self.Position
        });

        Log.Info($"✅ 玩家 {self.PlayerId} 传送到地图 {self.Scene.SceneConfig.Id} 完成");
    }
}
```

---

### OnDisposeTerminus 事件

当 Terminus 被销毁时，框架会触发 `OnDisposeTerminus` 事件，供业务层执行清理逻辑。

**事件参数：**

```csharp
public struct OnDisposeTerminus
{
    public readonly Scene Scene;      // 所属场景
    public readonly Terminus Terminus; // 被销毁的 Terminus 实例
}
```

**触发时机：**

- 客户端断开连接，Gate 销毁 Terminus 时
- 手动调用 `terminus.Dispose()` 时
- 传送完成后，原服务器清理旧 Terminus 时

**使用示例：**

```csharp
// 监听 Terminus 销毁事件，执行清理逻辑
public sealed class OnDisposeTerminusHandler : AsyncEventSystem<OnDisposeTerminus>
{
    protected override async FTask Handler(OnDisposeTerminus self)
    {
        // 根据 RoamingType 区分处理
        switch (self.Terminus.RoamingType)
        {
            case RoamingType.MapRoamingType:
            {
                var mapPlayer = self.Terminus.TerminusEntity as MapPlayer;
                if (mapPlayer != null)
                {
                    // 玩家离线：保存数据、通知其他玩家等
                    await mapPlayer.SaveToDatabase();
                    Log.Info($"✅ MapPlayer {mapPlayer.PlayerId} 已离线，数据已保存");
                }
                break;
            }
        }

        await FTask.CompletedTask;
    }
}
```

---

## 错误码参考

漫游模块使用精细化的错误码，帮助快速定位问题。所有错误码定义在 `InnerErrorCode.cs` 中。

### 漫游相关错误码

| 错误码常量名 | 值 | 含义 | 常见原因 |
|-------------|-----|------|---------|
| `ErrNotFoundRoaming` | `100000011` | 未找到漫游连接 | 指定的 roamingType 从未 Link 过，或已被 UnLink |
| `ErrRoamingNotReady` | `100000030` | 漫游正在连接中，尚未就绪 | Link 尚未完成时就并发发送了消息 |
| `ErrRoamingDisposed` | `100000032` | 漫游组件已销毁 | Session 断开或漫游被主动移除后仍在发送消息 |
| `ErrRoamingTimeout` | `100000012` | 漫游消息超时 | 消息发送过程中组件被销毁或网络超时 |
| `ErrRoamingRetryExhausted` | `100000031` | 漫游消息重试次数超限 | 连续重试超过上限仍无法送达 |
| `ErrReLinkNotFoundRoaming` | `100000033` | ReLink 时找不到已有连接 | 调用了已废弃的 ReLink 方法，但该 roamingType 从未 Link 过 |
| `ErrTerminusNotLinked` | `100000034` | Entity 未关联 Terminus | 通过 TerminusHelper 扩展方法发送消息，但 Entity 没有调用过 LinkTerminusEntity |

### Terminus 相关错误码

| 错误码常量名 | 值 | 含义 | 常见原因 |
|-------------|-----|------|---------|
| `ErrAddRoamingTerminalAlreadyExists` | `100000010` | 漫游终端已存在 | 同一个 roamingId 重复创建 Terminus |
| `ErrCreateTerminusInvalidRoamingId` | `100000028` | 无效的 RoamingId | 传入的 roamingId 为 0 |
| `ErrSetForwardSessionAddressNotFoundTerminus` | `100000027` | 未找到漫游终端 | 更新转发地址时找不到对应的 Terminus |
| `ErrTerminusStartTransfer` | `100000017` | 传送过程发生错误 | StartTransfer 执行中抛出异常 |
| `ErrTransfer` | `100000029` | 传送通用错误 | 传送过程中的通用错误 |

### 锁定相关错误码

| 错误码常量名 | 值 | 含义 | 常见原因 |
|-------------|-----|------|---------|
| `ErrLockTerminusIdNotFoundRoamingType` | `100000014` | 锁定时找不到漫游类型 | Lock/Unlock/GetTerminusId 请求到达 Gate 时，对应的 Roaming 已不存在 |

### 常见排查场景

**场景 1：收到 `ErrRoamingNotReady`（100000030）**

Link 尚未完成时就并发发送了消息，这是最容易踩的坑。

```csharp
// ❌ 错误写法：Link 和发送并发执行
roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);  // 没有 await
session.Call(new C2Chat_SendMessageRequest { });  // Link 还没完成就发了，返回 ErrRoamingNotReady

// ✅ 正确写法：等待 Link 完成后再发送
var errorCode = await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
if (errorCode == 0)
{
    var response = await session.Call(new C2Chat_SendMessageRequest { });
}
```

**场景 2：收到 `ErrNotFoundRoaming`（100000011）**

指定的 roamingType 从未建立过漫游连接，或已被断开。检查是否遗漏了 `Link` 调用，或漫游是否已被 `UnLink`/`Remove`。

**场景 3：收到 `ErrRoamingDisposed`（100000032）**

漫游组件已被销毁（Session 断开、主动 `Remove` 等）。检查 Session 是否仍然有效，或是否有其他逻辑提前销毁了漫游。

**场景 4：收到 `ErrTerminusNotLinked`（100000034）**

通过 Entity 扩展方法（如 `entity.Send()`）发送消息，但该 Entity 没有通过 `LinkTerminusEntity` 关联到 Terminus。确保在 `OnCreateTerminus` 事件中调用了 `LinkTerminusEntity`。

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
- Terminus 销毁时会触发 `OnDisposeTerminus` 事件，供业务层执行清理逻辑

---

### Q4: autoDispose 和 startForwarding 参数应该怎么设置？

```csharp
// autoDispose=true（推荐）
// Terminus 销毁时自动销毁关联实体，适用于大部分场景
var player = await terminus.LinkTerminusEntity<Player>(autoDispose: true);

// autoDispose=false
// Terminus 销毁时不销毁关联实体，需手动管理实体生命周期
var player = await terminus.LinkTerminusEntity<Player>(autoDispose: false);

// startForwarding=false
// 关联后先不转发消息，待准备完成后再手动开启（如需要先从数据库加载数据）
var player = await terminus.LinkTerminusEntity<Player>(autoDispose: true, startForwarding: false);
await player.LoadFromDatabase();
terminus.SetForwarding(true);  // 准备完成后开启转发
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

### Q7: 什么时候使用 TerminusHelper 扩展方法？什么时候直接使用 Terminus？

**使用 TerminusHelper 扩展方法（`entity.Send()`）：**

- ✅ 单次发送消息
- ✅ 代码简洁性优先
- ✅ 非性能敏感场景

**直接使用 Terminus（`terminus.Send()`）：**

- ✅ 频繁发送消息（如每帧、循环中）
- ✅ 一次性发送多条消息
- ✅ 性能敏感场景

**示例对比：**

```csharp
// 场景 1：单次发送 - 推荐使用扩展方法
public void SendNotification(ChatPlayer player)
{
    player.Send(new Chat2C_Notification { Content = "Hello" });
}

// 场景 2：频繁发送 - 推荐先获取 Terminus
public void UpdateLoop(ChatPlayer player)
{
    // 获取一次，多次使用
    if (!player.TryGetLinkTerminus(out var terminus))
    {
        return;
    }

    for (int i = 0; i < 100; i++)
    {
        terminus.Send(new Chat2C_Update { Frame = i });
    }
}

// 场景 3：每帧更新 - 推荐缓存 Terminus
public class ChatPlayerComponent : Entity
{
    private Terminus _terminus;

    public void OnAwake()
    {
        // 初始化时获取并缓存
        Parent.TryGetLinkTerminus(out _terminus);
    }

    public void OnUpdate()
    {
        // 每帧使用缓存的 Terminus
        _terminus?.Send(new Chat2C_FrameUpdate { });
    }
}
```

---

### Q8: TransferOutSystem 和 TransferInSystem 什么时候触发？

```
原服务器                          目标服务器
   |                                  |
   | TransferOutSystem ⭐             |
   | (传送前：保存数据、通知客户端)    |
   |                                  |
   | StartTransfer()                  |
   |--------------------------------->|
   |                                  | TransferComplete()
   |                                  | 1. 反序列化 Terminus 和实体
   |                                  | 2. 解锁 Terminus
   |                                  | 3. TransferInSystem ⭐
   |                                  |    (到达后：加载数据、通知客户端)
   |<---------------------------------|
   | 销毁 Terminus 和实体              |
   | 触发 OnDisposeTerminus ⭐         |
```

**使用场景：**

- `TransferOutSystem`：保存玩家数据到数据库、注销当前场景订阅、通知客户端传送开始
- `TransferInSystem`：加载新场景数据、设置出生点位置、通知客户端传送完成

**注意：** 两个 System 均仅在对应服务器触发，`TransferOutSystem` 在原服务器，`TransferInSystem` 在目标服务器。

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
5. **便捷接口**：TerminusHelper 提供扩展方法，简化消息发送和传送操作

**使用步骤回顾：**

1. 定义协议（带 RoamingType）
2. 客户端登录时建立路由（一次性）
3. 发送消息（自动转发）
4. 处理消息（使用 Roaming 处理器）

**核心 API 速查：**

| API | 返回值 | 说明 | 使用场景 |
|-----|--------|------|---------|
| `session.CreateRoaming()` | `SessionRoamingComponent` | Gate 创建 Roaming 组件（简单版本） | 不需要详细状态时 |
| `session.TryCreateRoaming()` | `CreateRoamingResult` | Gate 创建 Roaming 组件（详细版本，包含状态） | 需要判断创建状态时 |
| `roaming.Link(session, config, type, args)` | `uint` | 建立到后端服务器的路由，自动判断首次连接或重连 | 客户端登录和断线重连 |
| `roaming.IsLinked(roamingType)` | `bool` | 判断指定 roamingType 是否已建立漫游关系 | 精细控制场景 |
| `terminus.LinkTerminusEntity()` | `FTask<T>` | 关联业务实体到 Terminus | OnCreateTerminus 事件中 |
| `entity.Send(message)` | `void` | 向客户端发送消息 | 服务器主动推送 |
| `entity.Send(roamingType, message)` | `void` | 向其他服务器发送消息 | 服务器间通信 |
| `entity.Call(roamingType, request)` | `FTask<IResponse>` | 向其他服务器发送 RPC | 服务器间 RPC |
| `entity.StartTransfer(address)` | `FTask<uint>` | 传送实体到目标服务器 | 跨服传送 |
| `entity.GetLinkTerminus()` | `Terminus` | 获取关联的 Terminus | 性能优化场景 |
| `entity.TryGetLinkTerminus(out t)` | `bool` | 安全获取关联的 Terminus | 性能优化场景 |

**性能优化建议：**

- 📌 单次发送：使用 `entity.Send()` 扩展方法，代码简洁
- 📌 频繁发送：先获取 Terminus，避免重复查找组件
- 📌 每帧更新：初始化时缓存 Terminus，每帧直接使用
- 📌 传送操作：使用 `TransferOutSystem` 处理传送前逻辑，`TransferInSystem` 处理到达后逻辑
- 📌 延迟转发：关联实体时设置 `startForwarding=false`，数据准备完成后调用 `SetForwarding(true)`
