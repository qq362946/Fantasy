# Roaming 路由建立

## 第 1 步：Gate 侧创建 Roaming 并 Link 到后端

Gate 侧在处理登录或重连请求时，使用 `session.TryCreateRoaming` 创建 Roaming 组件，再对每个需要通信的后端服务器各调用一次 `Link`。

**`TryCreateRoaming` 只需调用一次**，创建的 Roaming 组件可以同时 Link 到多个后端服务器（Chat、Battle、Map 等），每个 RoamingType 对应一条独立路由。需要支持哪些后端服务器，在 `RoamingType.Config` 中定义好对应类型后，依次 Link 即可。

**参数说明：**
- `roamingId`：漫游唯一标识，通常为玩家 ID；同一玩家每次登录/重连使用相同值
- `isAutoDispose`：Session 断开时是否自动断开漫游
- `delayRemove`：断开后延迟多少毫秒才真正移除（用于断线重连窗口）

**`Link` 行为：** 内部自动通过 `IsLinked(roamingType)` 判断走首次连接还是重连路径，无需手动区分。

`TryCreateRoaming` 返回 `CreateRoamingResult`，含 `Status` 字段：

| Status | 含义 |
|--------|------|
| `NewCreated` | 新创建（首次登录） |
| `AlreadyExists` | 已存在（断线重连） |
| `SessionAlreadyHasRoaming` | 错误：Session 已有不同 roamingId 的漫游 |

```csharp
var result = await session.TryCreateRoaming(
    roamingId: request.PlayerId,
    isAutoDispose: true,
    delayRemove: 180000
);

switch (result.Status)
{
    case CreateRoamingStatus.NewCreated:
    case CreateRoamingStatus.AlreadyExists:
    {
        // Link 到所有需要通信的后端服务器，每个 RoamingType 各调用一次
        // RoamingType.ChatRoamingType / BattleRoamingType 等常量来自 RoamingType.Config 导出的 RoamingType.cs
        var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
        var errorCode = await result.Roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
        if (errorCode != 0) { response.ErrorCode = errorCode; return; }

        var battleConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Battle)[0];
        errorCode = await result.Roaming.Link(session, battleConfig, RoamingType.BattleRoamingType);
        if (errorCode != 0) { response.ErrorCode = errorCode; return; }

        // 如需区分首次登录和断线重连的不同业务逻辑，在此判断 result.Status
        if (result.Status == CreateRoamingStatus.NewCreated)
        {
            // 仅首次登录执行：初始化账号数据等
        }
        break;
    }
    case CreateRoamingStatus.SessionAlreadyHasRoaming:
        // 当前 Session 已有不同 roamingId 的漫游，属于异常场景
        response.ErrorCode = ErrorCode.SessionAlreadyHasRoaming;
        return;
}
```

### 传递自定义参数到后端

Link 时可附带参数，后端在 `OnCreateTerminus` 事件中通过 `self.Args` 接收。

**⚠️ args 实体类必须标注 `[MemoryPackable]` 特性**，参数通过 MemoryPack 序列化传输：

```csharp
[MemoryPackable]
public sealed partial class PlayerLoginData : Entity
{
    public string PlayerName;
    public int Level;
}
```

```csharp
var loginData = Entity.Create<PlayerLoginData>(session.Scene);
loginData.PlayerName = request.PlayerName;
loginData.Level = request.Level;

var errorCode = await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType, loginData);

// ⚠️ Gate 侧无论成功失败都必须销毁原始对象
// 参数通过序列化传递，Gate 持有原始对象，后端收到的是反序列化副本，两端各自销毁
loginData.Dispose();

if (errorCode != 0) { response.ErrorCode = errorCode; return; }
```

---

## 第 2 步：后端侧监听 OnCreateTerminus

Gate 调用 `roaming.Link()` 时，目标服务器自动触发 `OnCreateTerminus` 事件，在此创建并关联业务实体。

详见 `references/server/roaming/on-create-terminus.md`。
