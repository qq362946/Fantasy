# 消息处理器实现指南

## 概述

消息处理器（Handler）是 Fantasy Framework 服务器端处理客户端消息的核心机制。当客户端通过 Session 发送消息到服务器时，框架会自动路由到对应的 Handler 进行处理。

**核心 Handler 类型：**
- `Message<T>`: 处理单向消息（客户端发送，服务器处理，无响应）
- `MessageRPC<TRequest, TResponse>`: 处理 RPC 请求（客户端请求，服务器处理并返回响应）

**源码位置：** `/Fantasy.Packages/Fantasy.Net/Runtime/Core/Network/Message/Dispatcher/Interface/IMessageHandler.cs`

---

## Handler 自动注册机制

Fantasy Framework 使用 **Roslyn Source Generator** 在编译时自动扫描和注册所有 Handler，无需手动注册。

### Source Generator 工作原理

1. **编译时扫描**: 编译器自动扫描所有继承 `Message<T>` 或 `MessageRPC<TRequest, TResponse>` 的类
2. **生成注册代码**: 自动生成 `MessageHandlerResolverRegistrar.g.cs` 文件
3. **运行时加载**: 框架启动时自动加载所有 Handler

### 生成的注册代码示例

编译后会在 `obj/` 目录下生成类似以下代码：

```csharp
// MessageHandlerResolverRegistrar.g.cs (自动生成，不要手动修改)
namespace Fantasy.Generated
{
    public static class MessageHandlerResolverRegistrar
    {
        public static void RegisterAll()
        {
            // 自动注册所有 Handler
            MessageDispatcherSystem.Register<C2G_TestMessage, C2G_TestMessageHandler>();
            MessageDispatcherSystem.Register<C2G_TestRequest, C2G_TestRequestHandler>();
            // ... 更多 Handler
        }
    }
}
```

**开发者无需关心注册细节，只需创建 Handler 类即可！**

---

## Message&lt;T&gt; - 单向消息处理器

### 适用场景

单向消息适用于**不需要响应**的场景，例如：

- 玩家移动同步
- 聊天消息发送
- 状态更新通知
- 日志上报
- 心跳消息

### 基本用法

#### 1. 定义协议消息

在 `.proto` 文件中定义消息（需要实现 `IMessage` 接口）：

```protobuf
// NetworkProtocol/Outer/C2G_TestMessage.proto
message C2G_TestMessage // IMessage
{
    string Tag = 1;
}
```

运行协议导出工具后，会生成：

```csharp
// 自动生成的 C# 代码
public partial class C2G_TestMessage : AMessage, IMessage
{
    public string Tag { get; set; }
}
```

#### 2. 创建 Handler 类

在服务器端项目中创建 Handler 类，继承 `Message<T>`：

```csharp
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy.Gate;

public sealed class C2G_TestMessageHandler : Message<C2G_TestMessage>
{
    protected override async FTask Run(Session session, C2G_TestMessage message)
    {
        // 业务逻辑处理
        Log.Debug($"收到客户端消息: Tag={message.Tag}");

        // 可以通过 session 回复消息
        session.Send(new G2C_Notification
        {
            Message = $"服务器收到了你的消息: {message.Tag}"
        });

        await FTask.CompletedTask;
    }
}
```

#### 3. 客户端发送消息

```csharp
// 客户端代码
_session.Send(new C2G_TestMessage
{
    Tag = "Hello Server"
});
```

### Message&lt;T&gt; 类详解

```csharp
public abstract class Message<T> : IMessageHandler
{
    // 获取处理的消息类型（框架内部使用）
    public Type Type() => typeof(T);

    // 框架调用的入口方法（框架内部使用）
    public async FTask Handle(Session session, uint rpcId, uint messageTypeCode, object message)
    {
        try
        {
            await Run(session, (T)message);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    // 开发者实现的业务逻辑方法
    protected abstract FTask Run(Session session, T message);
}
```

**开发者只需要实现 `Run()` 方法，其他由框架自动处理。**

### 完整示例：玩家移动同步

#### 协议定义

```protobuf
// NetworkProtocol/Outer/C2G_PlayerMove.proto
message C2G_PlayerMove // IMessage
{
    float X = 1;
    float Y = 2;
    float Z = 3;
}

message G2C_PlayerMoveNotify // IMessage
{
    int64 PlayerId = 1;
    float X = 2;
    float Y = 3;
    float Z = 4;
}
```

#### 服务器端 Handler

```csharp
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy.Gate;

public sealed class C2G_PlayerMoveHandler : Message<C2G_PlayerMove>
{
    protected override async FTask Run(Session session, C2G_PlayerMove message)
    {
        // 1. 更新玩家位置（假设 Session 上挂载了 Player 组件）
        var player = session.GetComponent<PlayerComponent>();
        if (player == null)
        {
            Log.Warning($"Session {session.Id} 没有 PlayerComponent");
            return;
        }

        player.Position = new Vector3(message.X, message.Y, message.Z);
        Log.Debug($"玩家 {player.PlayerId} 移动到: ({message.X}, {message.Y}, {message.Z})");

        // 2. 广播给同场景的其他玩家
        var scene = session.Scene;
        var allSessions = scene.GetAllEntity<Session>();

        foreach (var otherSession in allSessions)
        {
            if (otherSession.Id == session.Id)
                continue; // 跳过自己

            otherSession.Send(new G2C_PlayerMoveNotify
            {
                PlayerId = player.PlayerId,
                X = message.X,
                Y = message.Y,
                Z = message.Z
            });
        }

        await FTask.CompletedTask;
    }
}
```

#### 客户端发送移动消息

```csharp
// 客户端代码
void UpdatePlayerPosition(float x, float y, float z)
{
    _session.Send(new C2G_PlayerMove
    {
        X = x,
        Y = y,
        Z = z
    });
}
```

---

## MessageRPC&lt;TRequest, TResponse&gt; - RPC 请求处理器

### 适用场景

RPC 请求适用于**需要等待响应**的场景，例如：

- 用户登录验证
- 数据查询（背包、角色信息）
- 交易和购买操作
- 任务提交和完成
- 创建房间或副本

### 基本用法

#### 1. 定义协议消息

在 `.proto` 文件中定义请求和响应消息：

```protobuf
// NetworkProtocol/Outer/C2G_TestRequest.proto
message C2G_TestRequest // IRequest,G2C_TestResponse
{
    string Tag = 1;
}

message G2C_TestResponse // IResponse
{
    string Tag = 1;
}
```

运行协议导出工具后，会生成：

```csharp
// 自动生成的 C# 代码
public partial class C2G_TestRequest : AMessage, IRequest
{
    public string Tag { get; set; }
}

public partial class G2C_TestResponse : AMessage, IResponse
{
    public int ErrorCode { get; set; }
    public string Tag { get; set; }
}
```

#### 2. 创建 Handler 类

在服务器端项目中创建 Handler 类，继承 `MessageRPC<TRequest, TResponse>`：

```csharp
using System;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy.Gate;

public sealed class C2G_TestRequestHandler : MessageRPC<C2G_TestRequest, G2C_TestResponse>
{
    protected override async FTask Run(Session session, C2G_TestRequest request,
                                       G2C_TestResponse response, Action reply)
    {
        // 业务逻辑处理
        Log.Debug($"收到 RPC 请求: Tag={request.Tag}");

        // 填充响应数据
        response.ErrorCode = 0;  // 0 表示成功
        response.Tag = $"服务器响应: {request.Tag}";

        // reply() 会在 finally 块自动调用，通常不需要手动调用
        // 如果需要提前返回响应，可以调用 reply()

        await FTask.CompletedTask;
    }
}
```

#### 3. 客户端发送请求

```csharp
// 客户端代码
var response = (G2C_TestResponse)await _session.Call(new C2G_TestRequest
{
    Tag = "Hello Server"
});

if (response.ErrorCode == 0)
{
    Log.Debug($"请求成功: {response.Tag}");
}
else
{
    Log.Error($"请求失败，错误码: {response.ErrorCode}");
}
```

### MessageRPC&lt;TRequest, TResponse&gt; 类详解

```csharp
public abstract class MessageRPC<TRequest, TResponse> : IMessageHandler
    where TRequest : IRequest
    where TResponse : AMessage, IResponse, new()
{
    // 获取处理的消息类型（框架内部使用）
    public Type Type() => typeof(TRequest);

    // 框架调用的入口方法（框架内部使用）
    public async FTask Handle(Session session, uint rpcId, uint messageTypeCode, object message)
    {
        if (message is not TRequest request)
        {
            Log.Error($"消息类型转换错误: {message.GetType().Name} to {typeof(TRequest).Name}");
            return;
        }

        var response = new TResponse();
        var isReply = false;

        void Reply()
        {
            if (isReply) return;
            isReply = true;

            if (session.IsDisposed) return;

            session.Send(response, rpcId);  // 自动回复响应
        }

        try
        {
            await Run(session, request, response, Reply);
        }
        catch (Exception e)
        {
            Log.Error(e);
            response.ErrorCode = InnerErrorCode.ErrRpcFail;  // 设置错误码
        }
        finally
        {
            Reply();  // 确保响应被发送
        }
    }

    // 开发者实现的业务逻辑方法
    protected abstract FTask Run(Session session, TRequest request, TResponse response, Action reply);
}
```

**关键点：**
1. `response` 对象会在 `finally` 块自动发送给客户端
2. 即使发生异常，也会返回响应（ErrorCode 设置为错误码）
3. 开发者只需填充 `response` 对象，无需手动调用 `reply()`（除非需要提前返回）

---

## Reply() 方法的使用

### 默认行为（推荐）

**大多数情况下，不需要手动调用 `reply()`**，框架会在 `finally` 块自动发送响应：

```csharp
protected override async FTask Run(Session session, C2G_TestRequest request,
                                   G2C_TestResponse response, Action reply)
{
    // 填充响应数据
    response.ErrorCode = 0;
    response.Tag = "处理成功";

    // 不需要调用 reply()，框架会自动发送响应

    await FTask.CompletedTask;
}
```

### 提前返回响应

如果需要**提前返回响应**，可以手动调用 `reply()`：

```csharp
protected override async FTask Run(Session session, C2G_LoginRequest request,
                                   G2C_LoginResponse response, Action reply)
{
    // 快速验证失败，提前返回
    if (string.IsNullOrEmpty(request.Account))
    {
        response.ErrorCode = 1000;
        response.Message = "账号不能为空";
        reply();  // 提前返回响应
        return;
    }

    // 正常业务逻辑...
    await ProcessLogin(request, response);

    // reply() 会在 finally 块自动调用
}
```

### Reply() 防重入机制

框架内部实现了防重入机制，多次调用 `reply()` 只会发送一次响应：

```csharp
void Reply()
{
    if (isReply) return;  // 防止重复发送
    isReply = true;

    if (session.IsDisposed) return;  // 检查 Session 是否已断开

    session.Send(response, rpcId);
}
```

---

## 常见问题

### Q1: Handler 需要手动注册吗？

**A:** 不需要！框架使用 **Source Generator** 在编译时自动扫描和注册所有 Handler。只要继承了 `Message<T>` 或 `MessageRPC<TRequest, TResponse>`，就会自动注册。

### Q2: Handler 可以是泛型类吗？

**A:** 不可以。Handler 必须是**具体类型**，不能是泛型类。Source Generator 只扫描具体类型的 Handler。

```csharp
// ❌ 错误：不支持泛型 Handler
public class GenericHandler<T> : Message<T> { }

// ✅ 正确：具体类型 Handler
public class C2G_TestMessageHandler : Message<C2G_TestMessage> { }
```

### Q3: 一个消息可以有多个 Handler 吗？

**A:** 不可以。一个消息类型只能有**一个 Handler**。如果定义多个 Handler 处理同一个消息，编译时会报错或运行时只会注册一个。

### Q4: MessageRPC 中不调用 reply() 会怎样？

**A:** 不会有问题。框架在 `finally` 块会自动调用 `reply()`，确保响应被发送。只有在需要**提前返回响应**时才需要手动调用 `reply()`。

### Q5: 如何在 Handler 中获取客户端 IP？

**A:** 通过 `Session.RemoteEndPoint` 获取：

```csharp
protected override async FTask Run(Session session, C2G_LoginRequest request,
                                   G2C_LoginResponse response, Action reply)
{
    var clientIp = session.RemoteEndPoint?.Address.ToString();
    Log.Debug($"客户端 IP: {clientIp}");

    // 业务逻辑...
    await FTask.CompletedTask;
}
```
---

## 相关文档

- [01-Session.md](01-Session.md) - Session 使用指南
- [01-Server/07-NetworkProtocol.md](../01-Server/07-NetworkProtocol.md) - 网络协议定义
- [01-Server/08-NetworkProtocolExporter.md](../01-Server/08-NetworkProtocolExporter.md) - 协议导出工具

---

## 总结

消息处理器（Handler）是服务器端处理客户端消息的核心：

1. **Message&lt;T&gt;**: 处理单向消息，无响应，性能高
2. **MessageRPC&lt;TRequest, TResponse&gt;**: 处理 RPC 请求，有响应，可靠性高
3. **自动注册**: Source Generator 自动扫描和注册 Handler，无需手动注册
4. **异常处理**: 框架自动捕获异常并设置错误码
5. **Reply 机制**: 响应在 `finally` 块自动发送，确保可靠性

掌握 Handler 的使用是开发 Fantasy 服务器端应用的核心技能，建议结合 [01-Session.md](01-Session.md) 学习完整的客户端-服务器通信流程。
