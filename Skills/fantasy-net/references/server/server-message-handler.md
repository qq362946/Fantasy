# 服务器端消息 Handler — 接收客户端消息

**本文件仅适用于实现了 `IMessage` / `IRequest` / `IResponse` 接口的消息处理。Addressable、Roaming 等消息见各自文件。**

Handler 放在 **Hotfix assembly** 中，框架通过 Source Generator 编译时自动注册，无需手动注册，不要修改 `.g.cs`。

---

## Workflow

```
确认要处理的消息类型？
│
├─► 客户端单向消息（IMessage）──────────────────► Message<T>
└─► 客户端 RPC 请求（IRequest/IResponse）────────► MessageRPC<TReq, TRes>
```

### 第 1 步：确认生成的消息类已存在

Handler 依赖导出工具生成的 C# 消息类。先确认 `NetworkProtocolServerDirectory`（服务器协议输出目录）中已有对应的 `.cs` 文件。如果还没有，先参考 `references/protocol/export.md` 运行导出工具。

### 第 2 步：确定 Handler 文件位置

搜索项目中已有的 Handler 文件（通常以 `Handler.cs` 结尾），参考其所在目录放置新文件，保持项目约定一致。通常结构如下：

```
Examples/Server/APP/
└── Hotfix/
    └── Gate/               ← 按 Scene 类型组织
        └── Handler/
            ├── C2G_LoginGameRequestHandler.cs
            └── C2G_TestMessageHandler.cs
```

### 第 3 步：创建 Handler 类

根据消息类型选择对应基类（见下方"Handler 类型"），使用 `sealed class`。

### 第 4 步：编译验证

```bash
dotnet build {解决方案}.sln
```

---

## Handler 类型

### Message\<T\> — 单向消息（无响应）

客户端发送、服务器只处理、不回复：

```csharp
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

// 类名 = 消息名 + "Handler"，例如消息是 C2G_PlayerMove，类名就是 C2G_PlayerMoveHandler
public sealed class {消息名}Handler : Message<{消息名}>
{
    protected override async FTask Run(Session session, {消息名} message)
    {
        // 根据 message.{字段名} 处理业务逻辑
        await FTask.CompletedTask;
    }
}
```

**适用场景**：玩家移动同步、聊天发送、心跳、日志上报等不需要响应的操作。

---

### MessageRPC\<TReq, TRes\> — RPC 请求响应

客户端发送请求、服务器处理并返回响应：

```csharp
using System;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

// 类名 = 请求消息名 + "Handler"，例如请求是 C2G_LoginRequest，类名就是 C2G_LoginRequestHandler
public sealed class {请求消息名}Handler : MessageRPC<{请求消息名}, {响应消息名}>
{
    protected override async FTask Run(Session session, {请求消息名} request,
        {响应消息名} response, Action reply)
    {
        // 读取 request.{字段名}，处理业务逻辑
        // 将结果写入 response.{字段名}
        // ErrorCode 默认为 0（成功），失败时设置非零值后直接 return
        await FTask.CompletedTask;
    }
}
```

**关键点：**
- `response.ErrorCode = 0` 表示成功（默认值），非零表示失败
- `reply()` 提前发送响应（适用于需要先回复再做耗时操作的场景）；调用后对 `response` 的修改无效
- 不调用 `reply()` 时，框架在 `Run` 方法结束后（`finally` 块）自动发送响应
- 不要抛出异常表示业务错误，用 `response.ErrorCode` 代替

---

## 错误处理模式

```csharp
public sealed class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2G_LoginRequest request,
        G2C_LoginResponse response, Action reply)
    {
        // 参数校验，直接 return —— 框架自动回复带错误码的响应
        if (string.IsNullOrEmpty(request.AccountName))
        {
            response.ErrorCode = 1;
            return;
        }

        // 业务条件检查
        var accountManage = session.Scene.GetComponent<AccountManageComponent>();
        if (!accountManage.Add(request.AccountName, out var account))
        {
            response.ErrorCode = 2;  // 账号已存在（重复登录）
            return;
        }

        // 成功路径，ErrorCode 默认为 0
        account.Session = session;
        session.AddComponent<GateAccountFlagComponent>().Account = account;

        await FTask.CompletedTask;
    }
}
```

---

## Session 常用操作

```csharp
// 主动推送消息给客户端（单向）
session.Send(new G2C_PushMessage { Tag = "Push content" });

// 判断 Session 是否已断开（耗时操作前应检查）
if (session.IsDisposed) return;

// 获取 Session 所在的 Scene
var scene = session.Scene;

// 获取 Scene 上的组件
var component = session.Scene.GetComponent<SomeComponent>();

// Session 地址（用于跨服寻址）
var address  = session.Address;
var runtimeId = session.RuntimeId;

// 获取客户端 IP
var clientIp = session.RemoteEndPoint?.Address.ToString();
```

---

## 提前回复后再推送

先快速回复 RPC，再异步执行耗时操作，操作完成后推送通知：

```csharp
public sealed class C2G_BuyItemRequestHandler : MessageRPC<C2G_BuyItemRequest, G2C_BuyItemResponse>
{
    protected override async FTask Run(Session session, C2G_BuyItemRequest request,
        G2C_BuyItemResponse response, Action reply)
    {
        response.ErrorCode = 0;
        reply();  // 立即回复客户端，后续 response 修改无效

        // 耗时的异步操作（如写数据库）
        await DoSomethingAsync();

        // 操作完成后主动推送（确认 Session 未断开）
        if (!session.IsDisposed)
        {
            session.Send(new G2C_ItemUpdateNotify { ItemId = request.ItemId });
        }
    }
}
```

---

## 注意事项

- Handler 必须是 `sealed class`，不能是泛型类
- 一条消息只能有一个 Handler，重复定义会导致运行时只注册一个
- Handler 文件通常放在 Hotfix assembly，以支持热更新
- 不要手动注册 Handler，不要修改 `.g.cs` 生成文件
