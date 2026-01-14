# EventAwaiter 系统使用指南

## 概述

`EventAwaiterComponent` 是 Fantasy Framework 的**类型化异步等待组件**,提供了一种高性能、类型安全的事件等待和通知机制。它允许你在协程中等待特定类型的事件触发,并支持超时控制和取消令牌。

**EventAwaiter 系统的主要功能:**
- 类型化的异步等待机制 (Wait&lt;T&gt;)
- 超时控制 (Wait&lt;T&gt;(timeout))
- 取消令牌支持 (FCancellationToken)
- 事件通知 (Notify&lt;T&gt;)
- 零装箱、高性能
- 对象池优化

**源码位置:**
- EventAwaiterComponent: `/Fantasy.Packages/Fantasy.Net/Runtime/Core/Entitas/Component/EventAwaiterComponent/`

💡 **与 Event 系统的区别:**

| 特性 | **EventAwaiter** | **Event** |
|------|-----------------|-----------|
| **通信模式** | 一对多等待-通知 | 发布-订阅 |
| **使用方式** | `await Wait<T>()` + `Notify<T>()` | `Publish()` + 监听器 |
| **执行时机** | 等待者主动等待,通知者触发 | 发布时立即执行所有监听器 |
| **返回值** | ✅ 支持 (EventAwaiterResult&lt;T&gt;) | ❌ 无返回值 |
| **适用场景** | 需要等待特定条件满足的场景 | 模块解耦、事件驱动架构 |

**典型使用场景:**
```csharp
// EventAwaiter: 等待玩家确认对话框 (挂载到玩家实体)
var player = scene.GetEntity<Player>(playerId);
var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>();
if (result.ResultType == EventAwaiterResultType.Success)
{
    ProcessConfirm(result.Value);
}

// Event: 发布玩家升级事件给所有监听器 (Scene 级别)
scene.EventComponent.Publish(new PlayerLevelUpEvent { PlayerId = id });
```

---

## 核心概念

### 1. EventAwaiterComponent 结构

EventAwaiterComponent 是一个 Entity 组件,管理所有类型的事件等待队列:

```csharp
public sealed class EventAwaiterComponent : Entity
{
    // 存储不同类型事件的等待回调队列
    // Key: RuntimeTypeHandle (事件类型)
    // Value: List<IEventAwaiterCallback> (等待该类型事件的回调列表)
    private OneToManyList<RuntimeTypeHandle, IEventAwaiterCallback> WaitCallbacks { get; }
}
```

### 2. 事件结果类型

EventAwaiterResult&lt;T&gt; 包含四种结果状态:

```csharp
public enum EventAwaiterResultType : byte
{
    Success = 0,  // 成功: 事件正常触发并返回数据
    Cancel = 1,   // 取消: 通过 FCancellationToken 主动取消等待
    Timeout = 2,  // 超时: 等待时间超过指定的超时时间
    Destroy = 3,  // 销毁: EventAwaiterComponent 被销毁,等待被强制中断
}
```

**EventAwaiterResult&lt;T&gt; 结构:**
```csharp
public readonly struct EventAwaiterResult<T> where T : struct
{
    public EventAwaiterResultType ResultType { get; }  // 结果状态
    public T Value { get; }  // 事件数据 (仅在 Success 时有效)
}
```

### 3. 对象池优化

EventAwaiter 系统使用专用对象池管理内部对象,避免频繁的 GC 分配:

```csharp
// Scene 级别的对象池
Scene.EventAwaiterPool

// 对象池管理的对象:
// - EventAwaiterCallback<T>: 事件等待回调包装器
// - EventAwaiterCancelAction<T>: 取消动作
// - EventAwaiterTimeoutAction<T>: 超时处理器
```

### 4. 运行机制

**等待流程:**
1. 调用 `Wait<T>()` 创建等待回调
2. 回调被添加到对应类型的等待队列
3. 协程挂起,等待 FTask 完成
4. 其他代码调用 `Notify<T>(data)` 触发通知
5. 所有等待该类型的回调被唤醒,FTask 完成
6. 返回 EventAwaiterResult&lt;T&gt; 结果

**超时流程:**
1. Wait 时创建定时器
2. 定时器到期后调用 `SetTimeout()`
3. FTask 完成,返回 Timeout 结果
4. 定时器自动取消

**取消流程:**
1. Wait 时注册取消动作到 FCancellationToken
2. Token 触发时调用 `SetCancel()`
3. FTask 完成,返回 Cancel 结果

---

## 基础使用

### 1. 添加 EventAwaiterComponent

EventAwaiterComponent 应该根据业务需求挂载到具体实体上:

```csharp
// ✅ 推荐: 挂载到业务实体 (如玩家、交易会话等)
public class Player : Entity
{
    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// 在玩家创建时添加组件
public class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}

// 使用时通过实体访问
var player = scene.GetEntity<Player>(playerId);
var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>();
```

**挂载选择:**
- **业务实体级别** (推荐): 玩家确认、交易会话、组队邀请等 → 挂载到 Player/TradeSession/TeamInvite 实体
- **Scene 级别** (特殊情况): 全局事件等待、服务器间通信等 → 挂载到 Scene

```csharp
// ⚠️ 仅在需要 Scene 级别事件等待时使用
public class OnCreateSceneSystem : EventSystem<OnCreateScene>
{
    protected override void Handler(OnCreateScene self)
    {
        // 只有在确实需要 Scene 级别的事件等待时才添加
        if (self.Scene.SceneType == SceneType.Gate)
        {
            self.Scene.AddComponent<EventAwaiterComponent>();
        }
    }
}
```

### 2. 等待事件 - Wait&lt;T&gt;()

等待特定类型的事件触发:

```csharp
// 定义事件数据类型 (必须是 struct)
public struct PlayerConfirmEvent
{
    public long PlayerId;
    public bool Confirmed;
    public string Message;
}

// 等待玩家确认
public async FTask ShowConfirmDialog(Player player)
{
    Log.Info("显示确认对话框,等待玩家响应...");

    // 等待玩家确认事件 (从玩家实体获取组件)
    var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>();

    // 检查结果状态
    if (result.ResultType == EventAwaiterResultType.Success)
    {
        Log.Info($"玩家确认: {result.Value.Confirmed}, 消息: {result.Value.Message}");

        if (result.Value.Confirmed)
        {
            ExecuteConfirmedAction();
        }
    }
    else
    {
        Log.Warning($"等待被中断,状态: {result.ResultType}");
    }
}
```

**方法签名:**
```csharp
public async FTask<EventAwaiterResult<T>> Wait<T>(FCancellationToken? cancellationToken = null)
    where T : struct
```

**参数说明:**
- `cancellationToken`: 可选的取消令牌
- **返回值**: EventAwaiterResult&lt;T&gt; 结果,包含状态和数据

⚠️ **注意:** 事件类型 `T` 必须是 `struct` (值类型),不能是 class。

### 3. 通知事件 - Notify&lt;T&gt;()

触发所有等待特定类型事件的回调:

```csharp
// 玩家点击确认按钮时
public void OnPlayerConfirm(Player player, bool confirmed, string message)
{
    // 创建事件数据
    var confirmEvent = new PlayerConfirmEvent
    {
        PlayerId = player.Id,
        Confirmed = confirmed,
        Message = message
    };

    // 通知所有等待 PlayerConfirmEvent 的回调 (从玩家实体获取组件)
    player.EventAwaiterComponent.Notify(confirmEvent);

    Log.Info($"已通知所有等待者: 玩家 {player.Id} 确认结果 {confirmed}");
}
```

**方法签名:**
```csharp
public void Notify<T>(T obj) where T : struct
```

**行为说明:**
- 通知**所有**等待该类型事件的回调
- 如果没有等待者,`Notify()` 不做任何操作
- 通知后自动清理该类型的等待队列

💡 **一对多通知:** Notify 会唤醒该实体上所有等待该类型事件的协程:

```csharp
// 假设挂载到玩家实体
var player = scene.GetEntity<Player>(playerId);

// 多个协程同时等待同一个玩家的事件
async FTask Waiter1()
{
    var result = await player.EventAwaiterComponent.Wait<TestEvent>();
    Log.Info("Waiter1 收到通知");
}

async FTask Waiter2()
{
    var result = await player.EventAwaiterComponent.Wait<TestEvent>();
    Log.Info("Waiter2 收到通知");
}

// 一次通知唤醒该玩家实体上的所有等待者
player.EventAwaiterComponent.Notify(new TestEvent { Value = 100 });
// 输出:
// Waiter1 收到通知
// Waiter2 收到通知
```

### 4. 带超时的等待 - Wait&lt;T&gt;(timeout)

设置等待的最长时间,避免无限等待:

```csharp
public async FTask WaitWithTimeout(Player player)
{
    Log.Info("等待玩家确认,超时时间 10 秒");

    // 等待最多 10 秒
    var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(10000);

    switch (result.ResultType)
    {
        case EventAwaiterResultType.Success:
            Log.Info($"玩家确认: {result.Value.Confirmed}");
            break;

        case EventAwaiterResultType.Timeout:
            Log.Warning("等待超时,玩家未响应");
            // 执行超时逻辑
            HandleTimeout();
            break;
    }
}
```

**方法签名:**
```csharp
public async FTask<EventAwaiterResult<T>> Wait<T>(int timeout, FCancellationToken? cancellationToken = null)
    where T : struct
```

**参数说明:**
- `timeout`: 超时时间 (毫秒),必须大于 0
- `cancellationToken`: 可选的取消令牌
- **返回值**: EventAwaiterResult&lt;T&gt;,可能返回 Success/Timeout/Cancel

⚠️ **注意:**
- timeout 必须大于 0,否则抛出 ArgumentException
- 超时后定时器会自动取消,不会泄漏

### 5. 使用取消令牌

允许外部主动取消等待:

```csharp
public async FTask CancellableWait(Player player)
{
    var cts = new FCancellationToken();

    // 5 秒后自动取消
    FTask.OnceTimer(player.Scene, 5000, () =>
    {
        Log.Info("触发取消令牌");
        cts.Cancel();
    });

    Log.Info("开始等待,可通过令牌取消");

    // 等待事件,支持取消
    var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(cts);

    switch (result.ResultType)
    {
        case EventAwaiterResultType.Success:
            Log.Info("正常完成");
            break;

        case EventAwaiterResultType.Cancel:
            Log.Warning("等待被取消");
            HandleCancellation();
            break;
    }
}
```

**带超时和取消的完整示例:**
```csharp
public async FTask FullFeaturedWait(Player player)
{
    var cts = new FCancellationToken();

    // 等待最多 30 秒,支持取消
    var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(
        timeout: 30000,
        cancellationToken: cts
    );

    switch (result.ResultType)
    {
        case EventAwaiterResultType.Success:
            Log.Info($"成功: {result.Value.Confirmed}");
            break;

        case EventAwaiterResultType.Timeout:
            Log.Warning("超时 (30 秒)");
            break;

        case EventAwaiterResultType.Cancel:
            Log.Warning("被取消");
            break;

        case EventAwaiterResultType.Destroy:
            Log.Error("EventAwaiterComponent 被销毁");
            break;
    }
}
```

---

## 实际使用场景

### 场景 1: 玩家确认对话框

```csharp
// 定义确认事件
public struct PlayerDialogConfirmEvent
{
    public long PlayerId;
    public int DialogId;
    public bool Confirmed;
}

// Player 实体定义
public class Player : Entity
{
    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// 在玩家创建时添加组件
public class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}

// 显示对话框并等待玩家确认
public class DialogSystem
{
    public async FTask<bool> ShowConfirmDialog(Player player, int dialogId, string message)
    {
        // 发送对话框到客户端
        player.Send(new S2C_ShowDialog
        {
            DialogId = dialogId,
            Message = message,
            Type = DialogType.Confirm
        });

        Log.Info($"显示对话框给玩家 {player.Id},等待确认...");

        // 等待玩家确认 (30 秒超时) - 从玩家实体获取组件
        var result = await player.EventAwaiterComponent.Wait<PlayerDialogConfirmEvent>(30000);

        if (result.ResultType == EventAwaiterResultType.Success)
        {
            Log.Info($"玩家 {player.Id} 确认结果: {result.Value.Confirmed}");
            return result.Value.Confirmed;
        }
        else if (result.ResultType == EventAwaiterResultType.Timeout)
        {
            Log.Warning($"玩家 {player.Id} 确认超时,默认取消");
            return false;
        }
        else
        {
            Log.Warning($"等待被中断: {result.ResultType}");
            return false;
        }
    }
}

// 客户端消息处理器
public class C2S_DialogConfirmHandler : Message<C2S_DialogConfirm>
{
    protected override async FTask Run(Session session, C2S_DialogConfirm message)
    {
        var player = session.GetEntity<Player>();

        // 通知等待者 (从玩家实体获取组件)
        player.EventAwaiterComponent.Notify(new PlayerDialogConfirmEvent
        {
            PlayerId = player.Id,
            DialogId = message.DialogId,
            Confirmed = message.Confirmed
        });
    }
}
```

### 场景 2: 交易系统

```csharp
// 定义交易事件
public struct TradeConfirmEvent
{
    public long PlayerId;
    public long TradeId;
    public bool Accepted;
}

// 交易会话实体
public class TradeSession : Entity
{
    public long Player1Id { get; set; }
    public long Player2Id { get; set; }
    public long TradeId { get; set; }
    public List<Item> Items { get; set; }

    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// 在交易会话创建时添加组件
public class TradeSessionAwakeSystem : AwakeSystem<TradeSession>
{
    protected override void Awake(TradeSession self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}

// 交易系统
public class TradeSystem
{
    public async FTask<bool> RequestTrade(Scene scene, long playerId1, long playerId2, List<Item> items)
    {
        // 创建交易会话实体
        var tradeSession = Entity.Create<TradeSession>(scene);
        tradeSession.Player1Id = playerId1;
        tradeSession.Player2Id = playerId2;
        tradeSession.TradeId = GenerateTradeId();
        tradeSession.Items = items;

        // 发送交易请求到双方客户端
        SendTradeRequestToPlayers(playerId1, playerId2, tradeSession.TradeId, items);

        Log.Info($"等待双方确认交易 {tradeSession.TradeId}...");

        // 等待双方确认 (60 秒超时)
        var cts = new FCancellationToken();

        // 并行等待两个玩家的确认 (使用交易会话实体的组件)
        var task1 = WaitPlayerConfirm(tradeSession, playerId1, cts);
        var task2 = WaitPlayerConfirm(tradeSession, playerId2, cts);

        await FTask.WhenAll(task1, task2);

        var result1 = await task1;
        var result2 = await task2;

        // 检查双方是否都确认
        if (result1 && result2)
        {
            Log.Info($"交易 {tradeSession.TradeId} 成功,双方都确认");
            ExecuteTrade(playerId1, playerId2, items);
            tradeSession.Dispose(); // 销毁交易会话
            return true;
        }
        else
        {
            Log.Warning($"交易 {tradeSession.TradeId} 失败");
            CancelTrade(tradeSession.TradeId);
            tradeSession.Dispose(); // 销毁交易会话
            return false;
        }
    }

    private async FTask<bool> WaitPlayerConfirm(TradeSession tradeSession, long playerId, FCancellationToken cts)
    {
        var result = await tradeSession.EventAwaiterComponent.Wait<TradeConfirmEvent>(60000, cts);

        if (result.ResultType == EventAwaiterResultType.Success && result.Value.PlayerId == playerId)
        {
            return result.Value.Accepted;
        }
        else
        {
            Log.Warning($"玩家 {playerId} 确认失败: {result.ResultType}");
            return false;
        }
    }

    private long GenerateTradeId() => TimeHelper.Now;
    private void SendTradeRequestToPlayers(long p1, long p2, long tradeId, List<Item> items) { }
    private void ExecuteTrade(long p1, long p2, List<Item> items) { }
    private void CancelTrade(long tradeId) { }
}

// 客户端确认处理器
public class C2S_TradeConfirmHandler : Message<C2S_TradeConfirm>
{
    protected override async FTask Run(Session session, C2S_TradeConfirm message)
    {
        // 根据 TradeId 获取交易会话实体
        var tradeSession = session.Scene.GetEntity<TradeSession>(message.TradeId);

        if (tradeSession != null)
        {
            tradeSession.EventAwaiterComponent.Notify(new TradeConfirmEvent
            {
                PlayerId = session.PlayerId,
                TradeId = message.TradeId,
                Accepted = message.Accepted
            });
        }
    }
}
```

### 场景 3: 组队邀请系统

```csharp
// 定义组队邀请事件
public struct TeamInviteResponseEvent
{
    public long InviterId;
    public long InviteeId;
    public bool Accepted;
    public string RejectReason;
}

// 组队邀请实体
public class TeamInvite : Entity
{
    public long InviterId { get; set; }
    public long InviteeId { get; set; }

    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// 在组队邀请创建时添加组件
public class TeamInviteAwakeSystem : AwakeSystem<TeamInvite>
{
    protected override void Awake(TeamInvite self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}

// 组队系统
public class TeamSystem
{
    public async FTask<bool> InviteToTeam(Scene scene, long inviterId, long inviteeId)
    {
        var inviter = scene.GetEntity<Player>(inviterId);
        var invitee = scene.GetEntity<Player>(inviteeId);

        // 创建组队邀请实体
        var teamInvite = Entity.Create<TeamInvite>(scene);
        teamInvite.InviterId = inviterId;
        teamInvite.InviteeId = inviteeId;

        // 发送邀请消息到被邀请者客户端
        invitee.Send(new S2C_TeamInvite
        {
            InviterId = inviterId,
            InviterName = inviter.Name,
            InviteId = teamInvite.Id  // 传递邀请实体 ID
        });

        Log.Info($"玩家 {inviterId} 邀请 {inviteeId} 加入队伍,等待响应...");

        // 等待被邀请者响应 (60 秒超时)
        var result = await teamInvite.EventAwaiterComponent.Wait<TeamInviteResponseEvent>(60000);

        bool success = false;

        if (result.ResultType == EventAwaiterResultType.Success)
        {
            if (result.Value.Accepted)
            {
                Log.Info($"玩家 {inviteeId} 接受邀请");
                AddPlayerToTeam(inviterId, inviteeId);
                success = true;
            }
            else
            {
                Log.Info($"玩家 {inviteeId} 拒绝邀请: {result.Value.RejectReason}");
                NotifyInviterRejected(inviterId, result.Value.RejectReason);
            }
        }
        else if (result.ResultType == EventAwaiterResultType.Timeout)
        {
            Log.Warning($"玩家 {inviteeId} 未响应邀请 (超时)");
            NotifyInviterTimeout(inviterId);
        }
        else
        {
            Log.Warning($"邀请等待被中断: {result.ResultType}");
        }

        // 销毁邀请实体
        teamInvite.Dispose();

        return success;
    }

    private void AddPlayerToTeam(long inviterId, long inviteeId) { }
    private void NotifyInviterRejected(long inviterId, string reason) { }
    private void NotifyInviterTimeout(long inviterId) { }
}

// 客户端响应处理器
public class C2S_TeamInviteResponseHandler : Message<C2S_TeamInviteResponse>
{
    protected override async FTask Run(Session session, C2S_TeamInviteResponse message)
    {
        // 根据 InviteId 获取邀请实体
        var teamInvite = session.Scene.GetEntity<TeamInvite>(message.InviteId);

        if (teamInvite != null)
        {
            teamInvite.EventAwaiterComponent.Notify(new TeamInviteResponseEvent
            {
                InviterId = teamInvite.InviterId,
                InviteeId = session.PlayerId,
                Accepted = message.Accepted,
                RejectReason = message.RejectReason
            });
        }
    }
}
```

### 场景 4: 异步资源加载

```csharp
// 定义资源加载完成事件
public struct ResourceLoadedEvent
{
    public string ResourcePath;
    public bool Success;
    public object Resource;
}

// 资源加载请求实体
public class ResourceLoadRequest : Entity
{
    public string ResourcePath { get; set; }

    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// 在资源加载请求创建时添加组件
public class ResourceLoadRequestAwakeSystem : AwakeSystem<ResourceLoadRequest>
{
    protected override void Awake(ResourceLoadRequest self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}

// 资源管理器
public class ResourceManager
{
    public async FTask<object> LoadResourceAsync(Scene scene, string path)
    {
        Log.Info($"开始加载资源: {path}");

        // 创建资源加载请求实体
        var loadRequest = Entity.Create<ResourceLoadRequest>(scene);
        loadRequest.ResourcePath = path;

        // 触发异步资源加载 (在后台线程或异步 IO)
        StartBackgroundLoad(loadRequest);

        // 等待资源加载完成 (30 秒超时)
        var result = await loadRequest.EventAwaiterComponent.Wait<ResourceLoadedEvent>(30000);

        object resource = null;

        if (result.ResultType == EventAwaiterResultType.Success)
        {
            if (result.Value.Success)
            {
                Log.Info($"资源加载成功: {path}");
                resource = result.Value.Resource;
            }
            else
            {
                Log.Error($"资源加载失败: {path}");
            }
        }
        else if (result.ResultType == EventAwaiterResultType.Timeout)
        {
            Log.Error($"资源加载超时: {path}");
        }
        else
        {
            Log.Error($"资源加载中断: {result.ResultType}");
        }

        // 销毁加载请求实体
        loadRequest.Dispose();

        return resource;
    }

    private void StartBackgroundLoad(ResourceLoadRequest loadRequest)
    {
        // 模拟异步加载
        Task.Run(async () =>
        {
            await Task.Delay(2000); // 模拟加载耗时

            // 加载完成后通知等待者
            var resource = LoadResourceFromDisk(loadRequest.ResourcePath);

            loadRequest.EventAwaiterComponent.Notify(new ResourceLoadedEvent
            {
                ResourcePath = loadRequest.ResourcePath,
                Success = resource != null,
                Resource = resource
            });
        });
    }

    private object LoadResourceFromDisk(string path)
    {
        // 实际的资源加载逻辑
        return new object();
    }
}
```

### 场景 5: 服务器间请求-响应

```csharp
// 定义跨服请求事件
public struct CrossServerResponseEvent
{
    public long RequestId;
    public int ResponseCode;
    public byte[] ResponseData;
}

// 跨服请求实体
public class CrossServerRequest : Entity
{
    public long RequestId { get; set; }
    public int TargetServerId { get; set; }
    public byte[] RequestData { get; set; }

    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// 在跨服请求创建时添加组件
public class CrossServerRequestAwakeSystem : AwakeSystem<CrossServerRequest>
{
    protected override void Awake(CrossServerRequest self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}

// 跨服通信系统
public class CrossServerSystem
{
    private long _requestIdCounter = 0;

    public async FTask<byte[]> SendRequestToOtherServer(
        Scene scene,
        int targetServerId,
        byte[] requestData)
    {
        var requestId = ++_requestIdCounter;

        // 创建跨服请求实体
        var request = Entity.Create<CrossServerRequest>(scene);
        request.RequestId = requestId;
        request.TargetServerId = targetServerId;
        request.RequestData = requestData;

        // 发送请求到目标服务器
        SendNetworkMessage(targetServerId, requestId, requestData);

        Log.Info($"发送跨服请求 {requestId} 到服务器 {targetServerId},等待响应...");

        // 等待响应 (10 秒超时)
        var result = await request.EventAwaiterComponent.Wait<CrossServerResponseEvent>(10000);

        byte[] responseData = null;

        if (result.ResultType == EventAwaiterResultType.Success)
        {
            if (result.Value.ResponseCode == 200)
            {
                Log.Info($"收到服务器 {targetServerId} 的响应");
                responseData = result.Value.ResponseData;
            }
            else
            {
                Log.Error($"服务器 {targetServerId} 返回错误码: {result.Value.ResponseCode}");
            }
        }
        else if (result.ResultType == EventAwaiterResultType.Timeout)
        {
            Log.Error($"等待服务器 {targetServerId} 响应超时");
        }
        else
        {
            Log.Error($"请求被中断: {result.ResultType}");
        }

        // 销毁请求实体
        request.Dispose();

        return responseData;
    }

    private void SendNetworkMessage(int serverId, long requestId, byte[] data) { }
}

// 网络消息处理器
public class CrossServerResponseHandler : Message<CrossServerResponse>
{
    protected override async FTask Run(Session session, CrossServerResponse message)
    {
        // 根据 RequestId 获取请求实体
        var request = session.Scene.GetEntity<CrossServerRequest>(message.RequestId);

        if (request != null)
        {
            // 通知等待者
            request.EventAwaiterComponent.Notify(new CrossServerResponseEvent
            {
                RequestId = message.RequestId,
                ResponseCode = message.ResponseCode,
                ResponseData = message.ResponseData
            });
        }
    }
}
```

---

## 性能优化

### 1. 对象池复用

EventAwaiter 系统内部使用对象池管理所有辅助对象,避免 GC 压力:

```csharp
// 内部实现 (自动完成,无需手动操作)
var callback = Scene.EventAwaiterPool.Rent<EventAwaiterCallback<T>>().Initialize(Scene);
// ...使用完成后...
Scene.EventAwaiterPool.Return(typeof(EventAwaiterCallback<T>), callback);
```

**优点:**
- ✅ 零 GC 分配 (复用对象)
- ✅ 高性能 (避免频繁的对象创建和销毁)
- ✅ 自动管理 (框架内部处理)

### 2. 使用 Struct 事件类型

事件类型必须是 `struct`,在栈上分配,避免堆分配:

```csharp
// ✅ 推荐: Struct 事件,栈上分配
public struct PlayerConfirmEvent
{
    public long PlayerId;
    public bool Confirmed;
}

// ❌ 不支持: Class 类型
public class PlayerConfirmEvent  // 编译错误: where T : struct
{
    public long PlayerId;
    public bool Confirmed;
}
```

### 3. 避免大量并发等待

如果需要等待大量不同类型的事件,考虑合并为单个事件类型:

```csharp
// ❌ 不推荐: 为每个玩家创建不同的事件类型
public struct Player1ConfirmEvent { public bool Confirmed; }
public struct Player2ConfirmEvent { public bool Confirmed; }
// ...

// ✅ 推荐: 使用单个事件类型,通过字段区分
public struct PlayerConfirmEvent
{
    public long PlayerId;  // 用于区分不同玩家
    public bool Confirmed;
}

// 等待特定玩家的确认
var result = await scene.EventAwaiterComponent.Wait<PlayerConfirmEvent>();
if (result.Value.PlayerId == targetPlayerId)
{
    // 处理确认
}
```

### 4. 及时取消不需要的等待

使用 FCancellationToken 在不需要时主动取消等待:

```csharp
public async FTask CancellableOperation(Scene scene)
{
    var cts = new FCancellationToken();

    // 某个条件满足时取消等待
    if (SomeCondition())
    {
        cts.Cancel();  // 立即取消等待,释放资源
    }

    var result = await scene.EventAwaiterComponent.Wait<TestEvent>(cts);

    if (result.ResultType == EventAwaiterResultType.Cancel)
    {
        Log.Info("等待已取消");
    }
}
```

### 5. 合理设置超时时间

根据业务场景设置合适的超时时间:

```csharp
// ✅ 推荐: 根据业务设置合理超时
await scene.EventAwaiterComponent.Wait<PlayerConfirmEvent>(30000);  // UI 操作 30 秒
await scene.EventAwaiterComponent.Wait<ResourceLoadedEvent>(10000);  // 资源加载 10 秒
await scene.EventAwaiterComponent.Wait<NetworkResponseEvent>(5000);  // 网络请求 5 秒

// ❌ 不推荐: 超时时间过长或不设置超时
await scene.EventAwaiterComponent.Wait<PlayerConfirmEvent>();  // 无超时,可能无限等待
await scene.EventAwaiterComponent.Wait<NetworkResponseEvent>(300000);  // 5 分钟太长
```

---

## EventAwaiter vs Event 对比

| 特性 | **EventAwaiter** | **Event** |
|------|-----------------|-----------|
| **通信模式** | 一对多等待-通知 | 发布-订阅 |
| **使用方式** | `await Wait<T>()` + `Notify<T>()` | `Publish()` + EventSystem 监听器 |
| **执行时机** | 等待者主动等待,通知者触发 | 发布时立即执行所有监听器 |
| **返回值** | ✅ 支持 (EventAwaiterResult&lt;T&gt;) | ❌ 无返回值 |
| **监听器注册** | ❌ 无监听器概念 | ✅ Source Generator 自动注册 |
| **一对多** | ✅ 支持 (多个等待者) | ✅ 支持 (多个监听器) |
| **热重载** | ✅ 支持 (组件级别) | ✅ 支持 (监听器自动重新注册) |
| **超时控制** | ✅ 原生支持 | ❌ 需要手动结合 Timer |
| **取消支持** | ✅ 原生支持 FCancellationToken | ❌ 需要自行实现 |
| **性能** | 极高 (对象池优化) | 极高 (零装箱) |
| **适用场景** | 需要等待特定条件满足 | 模块解耦、事件驱动架构 |

**使用建议:**

**使用 EventAwaiter (等待-通知模式):**
- ✅ 等待玩家操作 (确认对话框、交易请求)
- ✅ 异步资源加载
- ✅ 跨服请求-响应
- ✅ 需要返回值的场景
- ✅ 需要超时控制的场景

**使用 Event (发布-订阅模式):**
- ✅ 模块解耦 (登录事件触发多个系统初始化)
- ✅ 事件驱动架构 (战斗伤害、成就系统)
- ✅ 不需要返回值的场景
- ✅ 多个监听器处理同一事件

**示例对比:**

```csharp
// EventAwaiter: 等待玩家确认对话框 (挂载到玩家实体)
var player = scene.GetEntity<Player>(playerId);
var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(30000);
if (result.ResultType == EventAwaiterResultType.Success && result.Value.Confirmed)
{
    ExecuteConfirmedAction();
}

// Event: 发布玩家升级事件 (Scene 级别)
scene.EventComponent.Publish(new PlayerLevelUpEvent
{
    PlayerId = id,
    NewLevel = 10
});
// 所有监听器自动执行 (UI 更新、成就检查、奖励发放等)
```

---

## 最佳实践

### ✅ 推荐做法

```csharp
// 1. 事件命名清晰,使用 Event 后缀
public struct PlayerConfirmEvent { }  // ✅ 好
public struct Confirm { }  // ❌ 不清晰

// 2. 根据业务挂载到合适的实体
// ✅ 推荐: 挂载到业务实体
public class Player : Entity
{
    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// ⚠️ 仅在必要时挂载到 Scene (如全局请求-响应)
public class OnCreateSceneSystem : EventSystem<OnCreateScene>
{
    protected override void Handler(OnCreateScene self)
    {
        // 只在特定 Scene 类型添加
        if (self.Scene.SceneType == SceneType.Gate)
        {
            self.Scene.AddComponent<EventAwaiterComponent>();
        }
    }
}

// 3. 始终检查结果状态
var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>();

switch (result.ResultType)
{
    case EventAwaiterResultType.Success:
        ProcessSuccess(result.Value);  // ✅ 处理成功
        break;
    case EventAwaiterResultType.Timeout:
        HandleTimeout();  // ✅ 处理超时
        break;
    case EventAwaiterResultType.Cancel:
        HandleCancellation();  // ✅ 处理取消
        break;
    case EventAwaiterResultType.Destroy:
        HandleDestroy();  // ✅ 处理销毁
        break;
}

// 4. 为交互操作设置合理的超时时间
await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(30000);  // ✅ 30 秒超时

// 5. 使用取消令牌管理长时间等待
var cts = new FCancellationToken();
var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(60000, cts);

// 6. Notify 前检查是否有等待者 (可选优化)
// 直接 Notify (框架内部已优化,无等待者时快速返回)
player.EventAwaiterComponent.Notify(confirmEvent);

// 7. 事件数据包含必要的上下文信息
public struct PlayerConfirmEvent
{
    public long PlayerId;  // ✅ 标识是哪个玩家
    public int DialogId;   // ✅ 标识是哪个对话框
    public bool Confirmed;
    public string Message;
}

// 8. 创建专用的业务实体管理复杂等待
public class TradeSession : Entity
{
    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}

// 9. 等待完成后及时销毁实体
var tradeSession = Entity.Create<TradeSession>(scene);
var result = await tradeSession.EventAwaiterComponent.Wait<TradeConfirmEvent>(60000);
tradeSession.Dispose();  // ✅ 及时释放资源

// 10. 多个并发等待使用 FTask.WhenAll
var task1 = entity.EventAwaiterComponent.Wait<Event1>();
var task2 = entity.EventAwaiterComponent.Wait<Event2>();
await FTask.WhenAll(task1, task2);

var result1 = await task1;
var result2 = await task2;
```

### ⚠️ 注意事项

```csharp
// 1. 不要忘记添加 EventAwaiterComponent
// ❌ 错误: 未添加组件
var player = scene.GetEntity<Player>(playerId);
await player.EventAwaiterComponent.Wait<TestEvent>();  // NullReferenceException

// ✅ 正确: 在 AwakeSystem 中添加组件
public class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        self.EventAwaiterComponent = self.AddComponent<EventAwaiterComponent>();
    }
}

// 2. 不要在事件类型中存储大量数据
public struct BadEvent
{
    public int[] LargeArray;  // ❌ Struct 在栈上分配,可能栈溢出
}

// ✅ 正确: 传递引用或 ID
public struct GoodEvent
{
    public long DataId;  // 通过 ID 获取数据
}

// 3. 超时时间必须大于 0
await scene.EventAwaiterComponent.Wait<TestEvent>(0);  // ❌ ArgumentException
await scene.EventAwaiterComponent.Wait<TestEvent>(-100);  // ❌ ArgumentException
await scene.EventAwaiterComponent.Wait<TestEvent>(1000);  // ✅ 正确

// 4. Notify 的事件类型必须与 Wait 的类型匹配
await player.EventAwaiterComponent.Wait<Event1>();
player.EventAwaiterComponent.Notify(new Event2());  // ❌ 类型不匹配,永远等不到

// 5. 不要在销毁的实体上使用 EventAwaiter
await player.Dispose();
await player.EventAwaiterComponent.Wait<TestEvent>();  // ❌ 组件已销毁
// 等待会立即返回 Destroy 状态

// 6. 不要在 Notify 中再次 Wait 同类型事件 (避免死锁)
public void BadNotify(Player player)
{
    player.EventAwaiterComponent.Notify(new TestEvent());

    // ❌ 错误: Notify 后立即 Wait 同类型事件
    var result = await player.EventAwaiterComponent.Wait<TestEvent>();
    // 永远等不到 (已经 Notify 过了,队列已清空)
}

// 7. 不要忘记 await
player.EventAwaiterComponent.Wait<TestEvent>();  // ❌ 忘记 await,等待不会生效
await player.EventAwaiterComponent.Wait<TestEvent>();  // ✅ 正确

// 8. 不要在不同实体间通知 (挂载在不同实体上)
var player1 = scene.GetEntity<Player>(playerId1);
var player2 = scene.GetEntity<Player>(playerId2);

await player1.EventAwaiterComponent.Wait<TestEvent>();
player2.EventAwaiterComponent.Notify(new TestEvent());  // ❌ 不同实体,等不到

// ✅ 正确: 在同一实体上等待和通知
await player1.EventAwaiterComponent.Wait<TestEvent>();
player1.EventAwaiterComponent.Notify(new TestEvent());  // ✅ 同一实体
```

---

## 常见问题

### Q1: EventAwaiter 和 Event 有什么区别？

**A:**

**EventAwaiter (等待-通知模式):**
- 等待者主动等待特定事件
- 通知者触发时唤醒所有等待者
- 支持返回值 (EventAwaiterResult&lt;T&gt;)
- 支持超时和取消
- 适合需要等待结果的场景 (如确认对话框、交易请求)

**Event (发布-订阅模式):**
- 监听器在编译时注册
- 发布事件时立即执行所有监听器
- 无返回值
- 适合模块解耦和事件驱动架构 (如玩家升级、伤害事件)

**类比:**
- EventAwaiter 像 **RPC 调用** (等待响应)
- Event 像 **广播通知** (发布给所有订阅者)

### Q2: 为什么事件类型必须是 struct？

**A:**

使用 `struct` 有以下优势:
1. **零 GC 分配**: Struct 在栈上分配,不会产生 GC 压力
2. **性能优化**: 值类型复制开销小,适合频繁触发的事件
3. **类型安全**: 编译时类型检查,避免运行时错误

如果需要传递大量数据,建议:
- 传递数据的 ID 或引用
- 或使用 Entity 作为数据容器 (通过 Scene 访问)

```csharp
// ✅ 推荐: 传递 ID
public struct PlayerDataEvent
{
    public long PlayerId;  // 通过 ID 从 Scene 获取数据
}

// ❌ 不推荐: 传递大量数据
public struct PlayerDataEvent
{
    public int[] Items;  // 栈空间有限
    public Dictionary<int, int> Stats;  // Struct 不适合复杂类型
}
```

### Q3: 如果没有调用 Notify,等待会怎样？

**A:**

等待会一直挂起,直到:
1. **超时** (如果设置了 timeout)
2. **取消** (如果使用了 FCancellationToken 并触发)
3. **销毁** (如果 EventAwaiterComponent 被销毁)

**示例:**
```csharp
// 没有超时和取消令牌
var result = await scene.EventAwaiterComponent.Wait<TestEvent>();
// 如果没有调用 Notify<TestEvent>(),会永远等待 (除非组件销毁)

// 推荐: 始终设置超时
var result = await scene.EventAwaiterComponent.Wait<TestEvent>(30000);
// 30 秒后超时,返回 Timeout 状态
```

### Q4: 可以在不同实体之间通知吗？

**A:**

不可以。EventAwaiterComponent 是实体级别的组件,只能在同一个实体内使用:

```csharp
// ❌ 错误: 跨实体通知
var player1 = scene.GetEntity<Player>(playerId1);
var player2 = scene.GetEntity<Player>(playerId2);

await player1.EventAwaiterComponent.Wait<TestEvent>();
player2.EventAwaiterComponent.Notify(new TestEvent());  // 无效,不同实体

// ✅ 正确: 同一实体内
await player1.EventAwaiterComponent.Wait<TestEvent>();
player1.EventAwaiterComponent.Notify(new TestEvent());  // 有效
```

**跨实体通信建议:**
- 使用 **Event 系统** (Scene.EventComponent)
- 使用 **共享 Entity** (如创建专用的会话实体)
- 使用 **消息传递**

**示例: 使用专用会话实体**
```csharp
// 创建共享的交易会话实体
var tradeSession = Entity.Create<TradeSession>(scene);

// 两个玩家都等待同一个会话实体的事件
async FTask Player1Wait()
{
    await tradeSession.EventAwaiterComponent.Wait<TradeConfirmEvent>();
}

async FTask Player2Wait()
{
    await tradeSession.EventAwaiterComponent.Wait<TradeConfirmEvent>();
}

// 任何一方确认时,通知会话实体
tradeSession.EventAwaiterComponent.Notify(new TradeConfirmEvent());
```

### Q5: 多个等待者收到的事件数据相同吗？

**A:**

**是的**,所有等待者收到的是**同一个事件数据的副本** (因为是 struct):

```csharp
// 等待者 1
var player = scene.GetEntity<Player>(playerId);
var task1 = player.EventAwaiterComponent.Wait<TestEvent>();

// 等待者 2
var task2 = player.EventAwaiterComponent.Wait<TestEvent>();

// 通知
player.EventAwaiterComponent.Notify(new TestEvent { Value = 100 });

var result1 = await task1;  // result1.Value.Value = 100
var result2 = await task2;  // result2.Value.Value = 100
```

⚠️ **注意:** 由于是 struct,每个等待者收到的是**副本**,修改不会影响其他等待者:

```csharp
public class Waiter1
{
    var player = scene.GetEntity<Player>(playerId);
    var result = await player.EventAwaiterComponent.Wait<TestEvent>();
    result.Value.Value = 200;  // 修改副本,不影响其他等待者
}

public class Waiter2
{
    var player = scene.GetEntity<Player>(playerId);
    var result = await player.EventAwaiterComponent.Wait<TestEvent>();
    Log.Info(result.Value.Value);  // 仍然是 100,不受 Waiter1 影响
}
```

### Q6: EventAwaiterComponent 销毁时会发生什么？

**A:**

所有等待中的回调会收到 **Destroy** 状态:

```csharp
// 启动等待
var player = scene.GetEntity<Player>(playerId);
var task = player.EventAwaiterComponent.Wait<TestEvent>();

// 销毁实体 (或 EventAwaiterComponent)
await player.Dispose();

// 等待立即返回 Destroy 状态
var result = await task;
if (result.ResultType == EventAwaiterResultType.Destroy)
{
    Log.Warning("EventAwaiterComponent 已销毁");
}
```

**内部实现:**
```csharp
// EventAwaiterComponent.Dispose()
public override void Dispose()
{
    // 通知所有等待中的回调
    foreach (var (_, waitCallbackList) in WaitCallbacks)
    {
        foreach (var waitCallback in waitCallbackList)
        {
            waitCallback.SetDestroyResult();  // 返回 Destroy 状态
        }
    }

    WaitCallbacks.Clear();
    base.Dispose();
}
```

### Q7: 如何调试 EventAwaiter？

**A:**

使用以下方法调试:

```csharp
// 方法 1: 在 Wait 和 Notify 前后打印日志
Log.Debug("开始等待 PlayerConfirmEvent");
var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(30000);
Log.Debug($"等待结束,结果: {result.ResultType}");

// 方法 2: 使用 try-catch 捕获异常
try
{
    var result = await player.EventAwaiterComponent.Wait<PlayerConfirmEvent>(30000);
    Log.Info($"成功: {result.ResultType}");
}
catch (Exception ex)
{
    Log.Error($"等待异常: {ex}");
}

// 方法 3: 检查组件是否存在
if (player.EventAwaiterComponent == null)
{
    Log.Error("EventAwaiterComponent 未添加到实体");
}

// 方法 4: 在 Notify 时打印日志
Log.Debug($"通知 PlayerConfirmEvent,数据: {confirmEvent.Confirmed}");
player.EventAwaiterComponent.Notify(confirmEvent);
```

---

## 总结

EventAwaiter 系统是 Fantasy Framework 的**类型化异步等待组件**,提供了:

- **类型安全**: 编译时类型检查,避免运行时错误
- **高性能**: 零装箱调用,对象池优化
- **灵活控制**: 支持超时、取消、多等待者
- **易用性**: 简洁的 API 设计,符合 async/await 习惯
- **可靠性**: 异常保护、自动资源清理

**设计理念:**
通过类型化的等待-通知机制,实现高性能的异步协作,特别适合需要等待外部条件满足的场景 (如玩家交互、跨服请求、资源加载)。

**核心优势:**
- ✅ 支持返回值 (EventAwaiterResult&lt;T&gt;)
- ✅ 原生支持超时和取消
- ✅ 一对多通知
- ✅ 对象池优化,零 GC

---

## 相关文档

- [11-Timer.md](11-Timer.md) - Timer 系统使用指南
- [04-Event.md](04-Event.md) - Event 系统使用指南
- [01-ECS.md](01-ECS.md) - Entity-Component-System 详解
- [03-Scene.md](03-Scene.md) - Scene 和 SubScene 使用
