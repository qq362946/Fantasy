# OnDisposeTerminus 事件

Terminus 被销毁时框架自动触发 `OnDisposeTerminus` 事件。

**触发时机：**
- 客户端断开连接，且超过 `delayRemove` 设定的重连等待时间（`DisposeTerminusType.UnLink`）
- Terminus 传送完成后，原服务器侧清理旧 Terminus（`DisposeTerminusType.Transfer`）

**事件参数：**

```csharp
public struct OnDisposeTerminus
{
    public readonly Scene Scene;              // 所在场景
    public readonly Terminus Terminus;        // 被销毁的 Terminus 实例
    public readonly DisposeTerminusType Type; // 销毁原因：UnLink（断开）或 Transfer（传送）
}
```

**`DisposeTerminusType` 枚举：**

| 值 | 含义 | 典型处理 |
|----|------|---------|
| `UnLink` | 客户端真正断开（超时）| 保存数据、执行下线逻辑 |
| `Transfer` | Terminus 正在传送到其他服务器 | 仅做当前服务器的移除清理，**不执行下线逻辑** |

---

## 实现规范

每个后端服务器各自实现独立的 Handler，放在对应服务器的 Hotfix assembly 中。过滤条件推荐用 `scene.SceneType` 判断（与框架示例一致），而非 `RoamingType`。**必须区分 `UnLink` 和 `Transfer`**，否则玩家传送时会错误触发下线逻辑。

**Map 服务器示例：**

```csharp
// Map 服务器 Hotfix assembly
public sealed class OnDisposeTerminusEvent_Map : AsyncEventSystem<OnDisposeTerminus>
{
    protected override async FTask Handler(OnDisposeTerminus self)
    {
        if (self.Scene.SceneType != SceneType.Map)
        {
            return;
        }

        var unit = (Unit)self.Terminus.TerminusEntity;

        switch (self.Type)
        {
            case DisposeTerminusType.UnLink:
            {
                // 真正断线：执行完整下线流程（保存数据、离开地图等）
                await unit.ExitMapLine();
                await unit.Offline();
                return;
            }
            case DisposeTerminusType.Transfer:
            {
                // 传送中：Unit 正在迁移到其他地图，不执行下线逻辑
                // 只需从当前地图移除即可
                await self.Scene.GetComponent<MapUnitManageComponent>().Remove(
                    ClientBroadcastType.AllPlayersExceptSelf, unit.Id, true, false);
                return;
            }
        }
    }
}
```

**Chat 服务器示例：**

```csharp
// Chat 服务器 Hotfix assembly
public sealed class OnDisposeTerminusEvent_Chat : AsyncEventSystem<OnDisposeTerminus>
{
    protected override async FTask Handler(OnDisposeTerminus self)
    {
        if (self.Scene.SceneType != SceneType.Chat)
        {
            return;
        }

        var chatPlayer = self.Terminus.TerminusEntity as ChatPlayer;
        if (chatPlayer == null) return;

        switch (self.Type)
        {
            case DisposeTerminusType.UnLink:
            {
                await chatPlayer.SaveToDatabase();
                Log.Info($"ChatPlayer {chatPlayer.PlayerId} 离线，数据已保存");
                return;
            }
            case DisposeTerminusType.Transfer:
            {
                // Chat 通常不涉及传送，根据业务需要处理
                return;
            }
        }
    }
}
```

**注意：** `autoDispose=true` 时，`OnDisposeTerminus` 触发后框架会自动销毁 `TerminusEntity`，无需手动调用 `Dispose()`。在此事件中完成销毁前的最后处理即可。
