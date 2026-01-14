# Session 使用指南

## 概述

`Session` 是 Fantasy Framework 中网络通信的核心类，代表客户端与服务器之间的网络会话。每个连接都会创建一个唯一的 Session 实例，用于发送和接收网络消息。

**Session 的主要功能：**
- 发送单向消息（Fire-and-Forget）
- 发送 RPC 请求并等待响应（Request-Response）
- 管理网络连接生命周期
- 心跳检测和连接状态管理

**源码位置：** `/Fantasy.Packages/Fantasy.Net/Runtime/Core/Network/Session/Session.cs`

---

## Session 的获取方式

### 客户端获取 Session

客户端通过 `Scene.Connect()` 方法连接到服务器并获取 Session：

```csharp
using Fantasy.Async;
using Fantasy.Network;

public class ClientExample
{
    private Scene _scene;
    private Session _session;

    public async FTask ConnectToServer()
    {
        // 1. 创建 Scene
        _scene = await Fantasy.Scene.Create(SceneRuntimeMode.MainThread);

        // 2. 连接到服务器，返回 Session
        _session = _scene.Connect(
            "127.0.0.1:20000",                    // 服务器地址和端口
            NetworkProtocolType.KCP,              // 网络协议类型（TCP/KCP/WebSocket）
            OnConnectComplete,                    // 连接成功回调
            OnConnectFail,                        // 连接失败回调
            OnConnectDisconnect,                  // 连接断开回调
            false,                                // 是否HTTPS请求（仅用于WebSocket）
            5000);                                // 连接超时时间（毫秒）
    }

    private void OnConnectComplete()
    {
        Log.Debug("连接成功");

        // 可选：添加心跳组件
        _session.AddComponent<SessionHeartbeatComponent>().Start(2000);

        // 开始通信
        SendMessage();
    }

    private void OnConnectFail()
    {
        Log.Debug("连接失败");
    }

    private void OnConnectDisconnect()
    {
        Log.Debug("连接断开");
    }

    private void SendMessage()
    {
        // 发送消息...
    }
}
```

### 服务器端获取 Session

服务器端在消息处理器（Handler）中自动接收 Session 参数：

```csharp
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

// 单向消息处理器
public class C2G_TestMessageHandler : Message<C2G_TestMessage>
{
    protected override async FTask Run(Session session, C2G_TestMessage message)
    {
        // session 参数由框架自动传入
        Log.Debug($"收到客户端消息，Session ID: {session.Id}");

        // 可以使用 session 回复消息
        session.Send(new G2C_TestMessage
        {
            Result = "处理成功"
        });

        await FTask.CompletedTask;
    }
}

// RPC 请求处理器
public class C2G_TestRequestHandler : MessageRPC<C2G_TestRequest, G2C_TestResponse>
{
    protected override async FTask Run(Session session, C2G_TestRequest request,
                                       G2C_TestResponse response, Action reply)
    {
        // session 参数由框架自动传入
        Log.Debug($"收到 RPC 请求，Session ID: {session.Id}");

        // 填充响应数据
        response.Message = $"处理了请求: {request.Tag}";

        await FTask.CompletedTask;
    }
}
```

---

## 发送单向消息 - Send()

单向消息适用于**不需要响应**的场景，例如：通知、状态更新、日志上报等。

### Send() 方法签名

```csharp
public virtual void Send<T>(T message, uint rpcId = 0, long address = 0) where T : IMessage
```

**参数说明：**
- `message`: 要发送的消息实例（必须实现 `IMessage` 接口）
- `rpcId`: RPC 标识符（通常为 0，框架内部使用）
- `address`: 路由地址（通常为 0，用于分布式路由场景）

### 使用示例

#### 客户端发送单向消息

```csharp
// 定义消息（通过 .proto 文件生成）
public partial class C2G_PlayerMove : IMessage
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

// 发送消息
// 方式1
_session.Send(new C2G_PlayerMove
{
    X = 100.5f,
    Y = 0.0f,
    Z = 200.3f
});
// 方式2(推荐)
_session.C2G_PlayerMove(100.5f, 0.0f, 200.3f);

Log.Debug("玩家移动消息已发送");
```

#### 服务器端发送单向消息

```csharp
public class SomeHandler : Message<C2G_SomeMessage>
{
    protected override async FTask Run(Session session, C2G_SomeMessage message)
    {
        // 处理完业务逻辑后，通知客户端
        session.Send(new G2C_Notification
        {
            Message = "服务器处理完成",
            Timestamp = TimeHelper.Now
        });

        await FTask.CompletedTask;
    }
}
```

### Send() 方法特点

✅ **优点：**
- 性能高，无需等待响应
- 适合高频率消息（如位置同步、状态更新）
- 不会阻塞后续代码执行

⚠️ **注意：**
- 无法确认消息是否被成功处理
- 不适合需要确认结果的业务逻辑
- 发送失败不会抛出异常（需要自行检查连接状态）

---

## 发送 RPC 请求 - Call()

RPC（Remote Procedure Call）请求适用于**需要等待响应**的场景，例如：登录验证、数据查询、事务操作等。

### Call() 方法签名

```csharp
public virtual FTask<IResponse> Call<T>(T request, long address = 0) where T : IRequest
```

**参数说明：**
- `request`: 请求消息实例（必须实现 `IRequest` 接口）
- `address`: 路由地址（通常为 0，用于分布式路由场景）

**返回值：**
- `FTask<IResponse>`: 异步任务，完成后返回响应消息

### 使用示例

#### 客户端发送 RPC 请求

```csharp
// 定义请求和响应消息（通过 .proto 文件生成）
public partial class C2G_LoginRequest : IRequest,G2C_LoginResponse
{
    public string Account { get; set; }
    public string Password { get; set; }
}

public partial class G2C_LoginResponse : IResponse
{
    public string Message { get; set; }
    public long PlayerId { get; set; }
}

// 发送 RPC 请求并等待响应
public async FTask Login(string account, string password)
{
    try
    {
        // 发送方式1
        // Call() 返回泛型响应，需要类型转换
        var response = (G2C_LoginResponse)await _session.Call(new C2G_LoginRequest
        {
            Account = account,
            Password = password
        });
        // 发送方式2（推荐）
        var response = await _session.C2G_LoginRequest(account, password);
        // 以上方式任选其一
        
        if (response.ErrorCode == 0)
        {
            Log.Debug($"登录成功，玩家 ID: {response.PlayerId}");
        }
        else
        {
            Log.Error($"登录失败: {response.Message}");
        }
    }
    catch (Exception e)
    {
        Log.Error($"RPC 调用异常: {e.Message}");
    }
}
```

#### 使用 NetworkProtocolHelper 简化调用

框架会自动生成 `NetworkProtocolHelper.cs` 扩展方法，简化 RPC 调用：

```csharp
// 自动生成的扩展方法（无需类型转换）
public static async FTask<G2C_LoginResponse> C2G_LoginRequest(
    this Session session, string account, string password)
{
    return (G2C_LoginResponse)await session.Call(new C2G_LoginRequest
    {
        Account = account,
        Password = password
    });
}

// 使用扩展方法（更简洁）
var response = await _session.C2G_LoginRequest(account, password);
if (response.ErrorCode == 0)
{
    Log.Debug($"登录成功，玩家 ID: {response.PlayerId}");
}
```

#### 服务器端处理 RPC 请求

```csharp
public class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2G_LoginRequest request,
                                       G2C_LoginResponse response, Action reply)
    {
        Log.Debug($"收到登录请求: {request.Account}");

        // 验证账号密码
        if (request.Account == "admin" && request.Password == "123456")
        {
            response.ErrorCode = 0;
            response.Message = "登录成功";
            response.PlayerId = IdFactory.NextRunTimeId(); // 生成玩家 ID
        }
        else
        {
            response.ErrorCode = 1001;
            response.Message = "账号或密码错误";
        }

        // reply() 会在 finally 块自动调用，通常不需要手动调用
        // 如果需要提前返回响应，可以调用 reply()

        await FTask.CompletedTask;
    }
}
```

### Call() 方法特点

✅ **优点：**
- 确保消息被处理并返回结果
- 支持错误处理和异常捕获
- 适合业务逻辑验证和数据查询

⚠️ **注意：**
- 会阻塞当前异步流程直到收到响应
- 性能略低于单向消息（有往返延迟）
- 需要设置合理的超时机制
- 响应消息会在 `finally` 块自动发送（由框架保证）

---

## Session 生命周期管理

### 检查 Session 是否有效

在发送消息前，建议检查 Session 是否已断开：

```csharp
if (_session.IsDisposed)
{
    Log.Warning("Session 已断开，无法发送消息");
    return;
}

// 安全地发送消息
_session.Send(new C2G_TestMessage());
```

### 主动断开连接

```csharp
// 客户端主动断开连接
_session.Dispose();

// 断开连接后会触发 OnConnectDisconnect 回调
```

### 心跳检测

框架提供 `SessionHeartbeatComponent` 组件，用于维持连接活性：

```csharp
// 客户端添加心跳组件（建议在连接成功后添加）
_session.AddComponent<SessionHeartbeatComponent>().Start(2000);  // 每 2 秒发送一次心跳

// 服务器端会自动添加心跳检测组件（通过 Fantasy.config 配置）
```

### Session 空闲检测（服务器端）

服务器端可以配置空闲超时检测，自动断开不活跃的连接：

```csharp
// 在 Fantasy.config 中配置 SessionIdleCheckerInterval 和 SessionIdleCheckerTimeout
// 或在代码中动态调整
session.RestartIdleChecker(interval: 10000, timeOut: 30000);
```

---

## 最佳实践

### 1. **检查 Session 状态**

发送消息前检查 `IsDisposed` 属性：

```csharp
if (_session.IsDisposed)
{
    Log.Warning("Session 已断开");
    return;
}
_session.Send(message);
```

### 2. **合理使用 Send 和 Call**

- **高频消息（如位置同步）**: 使用 `Send()`
- **重要业务（如登录、交易）**: 使用 `Call()`

```csharp
// 高频位置更新 - 使用 Send
_session.Send(new C2G_PlayerMove { X = x, Y = y, Z = z });

// 购买道具 - 使用 Call
var response = await _session.Call(new C2G_BuyItemRequest { ItemId = 1001 });
```

### 4. **心跳和超时配置**

客户端添加心跳组件，服务器端配置超时检测：

```csharp
// 客户端
_session.AddComponent<SessionHeartbeatComponent>().Start(2000);

// 服务器端（在 OnCreateScene 或 Handler 中）
session.RestartIdleChecker(interval: 10000, timeOut: 30000);
```

### 5. **避免内存泄漏**

使用完 Session 后及时释放：

```csharp
// 客户端断开连接
_session.Dispose();

// 服务器端通常由框架自动管理，无需手动释放
```

---

## 相关文档

- [02-MessageHandler.md](02-MessageHandler.md) - 消息处理器实现指南
- [01-Server/07-NetworkProtocol.md](../01-Server/07-NetworkProtocol.md) - 网络协议定义
- [01-Server/08-NetworkProtocolExporter.md](../01-Server/08-NetworkProtocolExporter.md) - 协议导出工具

---

## 总结

`Session` 是 Fantasy Framework 网络通信的核心：

1. **客户端**: 通过 `Scene.Connect()` 获取 Session
2. **服务器端**: 在 Handler 中自动接收 Session 参数
3. **单向消息**: 使用 `Send()` 发送，高性能但无响应
4. **RPC 请求**: 使用 `Call()` 发送，等待响应结果
5. **生命周期**: 及时检查 `IsDisposed`，使用完后调用 `Dispose()`

掌握 Session 的使用是开发 Fantasy 网络应用的基础，建议结合 [02-MessageHandler.md](02-MessageHandler.md) 学习完整的通信流程。
