# Roaming 消息处理与推送

## 第 1 步：客户端发送漫游消息

路由建立后，客户端直接发送，Gate 无需写任何转发代码，框架根据协议注释中的 `RoamingType` 自动路由。

```csharp
var response = (Chat2C_SendMessageResponse)await session.Call(
    new C2Chat_SendMessageRequest { Content = content }
);
```

## 第 2 步：Gate 主动向后端发送

```csharp
if (!session.TryGetRoaming(out var roaming))
{
    Log.Error("roaming 不存在，请先建立漫游");
    return;
}

roaming.Send(RoamingType.ChatRoamingType, new G2Chat_TestMessage { Content = "Hello" });

var resp = (Chat2G_GetDataResponse)await roaming.Call(
    RoamingType.ChatRoamingType,
    new G2Chat_GetDataRequest { PlayerId = playerId }
);
```

## 第 3 步：实现漫游 Handler

在后端服务器实现 Handler，框架自动找到对应实体传入 `Run`，由 Source Generator 自动注册，无需手动注册。

第一个泛型参数 `TEntity`：

- 调用了 `LinkTerminusEntity<ChatPlayer>()` -> 传入 `ChatPlayer`
- 未调用 `LinkTerminusEntity()` -> 传入 `Terminus`

### 单向消息

```csharp
public sealed class C2Chat_TestMessageHandler : Roaming<ChatPlayer, C2Chat_TestMessage>
{
    protected override async FTask Run(ChatPlayer chatPlayer, C2Chat_TestMessage message)
    {
        if (chatPlayer == null || chatPlayer.IsDisposed) return;

        chatPlayer.ProcessMessage(message.Tag);
        await FTask.CompletedTask;
    }
}
```

### RPC 消息

```csharp
public sealed class C2Chat_SendMessageRequestHandler
    : RoamingRPC<ChatPlayer, C2Chat_SendMessageRequest, Chat2C_SendMessageResponse>
{
    protected override async FTask Run(
        ChatPlayer chatPlayer,
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
        await FTask.CompletedTask;
    }
}
```

## 第 4 步：后端向客户端推送 / 跨服发送

后端服务器通过 TerminusHelper 扩展方法直接发送，无需手动获取 Session。

```csharp
chatPlayer.Send(new Chat2C_Notification { Content = "Hello" });

chatPlayer.Send(RoamingType.MapRoamingType, new Chat2Map_TestMessage { Data = "Hi" });

var resp = await chatPlayer.Call(
    RoamingType.MapRoamingType,
    new Chat2Map_GetDataRequest { PlayerId = chatPlayer.PlayerId }
);
```

## 高频发送优化

高频发送时先获取 Terminus，减少重复查找开销。

```csharp
if (!chatPlayer.TryGetLinkTerminus(out var terminus)) return;

for (int i = 0; i < 100; i++)
{
    terminus.Send(new Chat2C_FrameUpdate { Frame = i });
}
```
