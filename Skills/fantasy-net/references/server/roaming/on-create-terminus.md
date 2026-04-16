# OnCreateTerminus 事件

Gate 调用 `roaming.Link()` 时，目标服务器自动触发 `OnCreateTerminus` 事件，在此创建并关联业务实体。

**事件参数：**

```csharp
public struct OnCreateTerminus
{
    public readonly Scene Scene;             // 所在场景
    public readonly Terminus Terminus;       // 漫游终端实体
    public readonly Entity? Args;            // Link 传入的参数（需标注 [MemoryPackable]）
    public readonly CreateTerminusType Type; // Link（首次）或 ReLink（重连）
}
```

**核心 API：**

```csharp
// 自动创建并关联业务实体（推荐）
FTask<T> terminus.LinkTerminusEntity<T>(bool autoDispose, bool startForwarding = true);

// 关联已有实体
FTask terminus.LinkTerminusEntity(Entity entity, bool autoDispose, bool startForwarding = true);

// 手动开关消息转发
void terminus.SetForwarding(bool isStartForwarding);
```

- `LinkTerminusEntity()` 是**可选的**；不调用时，漫游 Handler 中收到的实体就是 `Terminus` 本身
- `autoDispose=true`（推荐）：Terminus 销毁时自动销毁关联实体
- `startForwarding=false`：先不转发，数据准备好后调用 `SetForwarding(true)` 手动开启

**⚠️ Args 内存管理规则：**

| 场景 | Gate 侧 | 后端侧 |
|------|--------|--------|
| Link 成功 | `args.Dispose()` | `OnCreateTerminus` 中 `Args?.Dispose()` |
| Link 失败 | `args.Dispose()` | 不涉及（参数未传递） |
| 参数类型不匹配 | `args.Dispose()` | `Args?.Dispose()` |
| 无需参数的 RoamingType | `args.Dispose()` | `Args?.Dispose()` |

---

## 实现规范

每个后端服务器各自实现独立的 Handler，放在对应服务器的 Hotfix assembly 中，用 `if (RoamingType != ...) return;` 过滤无关类型，无需在一个 Handler 里 switch 所有类型。

**Chat 服务器示例（含首次创建 + 断线重连 + 参数）：**

```csharp
// Chat 服务器 Hotfix assembly
public sealed class ChatOnCreateTerminusHandler : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        if (self.Terminus.RoamingType != RoamingType.ChatRoamingType)
        {
            return;
        }

        switch (self.Type)
        {
            case CreateTerminusType.Link:
            {
                // 首次：创建新玩家实体
                var chatPlayer = await self.Terminus.LinkTerminusEntity<ChatPlayer>(autoDispose: true);
                if (chatPlayer == null)
                {
                    self.Args?.Dispose(); // ⚠️ 创建失败也要销毁
                    return;
                }

                chatPlayer.PlayerId = self.Terminus.RuntimeId;

                if (self.Args is PlayerLoginData loginData)
                {
                    chatPlayer.PlayerName = loginData.PlayerName;
                    chatPlayer.Level = loginData.Level;
                    loginData.Dispose(); // ⚠️ 使用完毕立即销毁
                }
                else
                {
                    chatPlayer.LoadData();
                    self.Args?.Dispose(); // ⚠️ 无论是否匹配都要销毁
                }
                break;
            }
            case CreateTerminusType.ReLink:
            {
                // 重连：复用或重建实体
                var chatPlayer = self.Terminus.TerminusEntity as ChatPlayer;
                if (chatPlayer != null)
                {
                    chatPlayer.IsOnline = true; // 实体仍在，直接恢复
                }
                else
                {
                    // 实体已超时销毁，重新创建并从数据库恢复
                    chatPlayer = await self.Terminus.LinkTerminusEntity<ChatPlayer>(autoDispose: true);
                    if (chatPlayer != null)
                    {
                        chatPlayer.PlayerId = self.Terminus.RuntimeId;
                        await chatPlayer.LoadFromDatabase();
                    }
                }
                self.Args?.Dispose(); // ⚠️ ReLink 也要销毁 Args
                break;
            }
        }

        await FTask.CompletedTask;
    }
}
```

**Map 服务器示例（先创建实体再关联）：**

```csharp
// Map 服务器 Hotfix assembly
public sealed class MapOnCreateTerminusHandler : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        if (self.Terminus.RoamingType != RoamingType.MapRoamingType)
        {
            return;
        }

        var mapPlayer = Entity.Create<MapPlayer>(self.Scene);
        mapPlayer.PlayerId = self.Terminus.RuntimeId;
        await self.Terminus.LinkTerminusEntity(mapPlayer, autoDispose: true);
        self.Args?.Dispose();

        await FTask.CompletedTask;
    }
}
```
