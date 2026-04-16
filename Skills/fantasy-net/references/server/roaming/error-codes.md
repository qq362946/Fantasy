# Roaming 错误码

出错时根据错误码值查找对应条目，定位原因并按建议修复。

## 错误码表

| 错误码 | 值 | 含义 | 常见原因 |
|--------|-----|------|---------|
| `ErrNotFoundRoaming` | 100000011 | 未找到漫游连接 | roamingType 未 Link 或已 UnLink |
| `ErrRoamingNotReady` | 100000030 | 漫游尚未就绪 | Link 未完成时并发发送了消息（忘记 await Link） |
| `ErrRoamingDisposed` | 100000032 | 漫游组件已销毁 | Session 断开后仍在发送 |
| `ErrRoamingTimeout` | 100000012 | 漫游消息超时 | 组件销毁或网络超时 |
| `ErrRoamingRetryExhausted` | 100000031 | 重试超限 | 连续重试超过上限仍未送达 |
| `ErrReLinkNotFoundRoaming` | 100000033 | ReLink 时找不到已有连接 | 该 roamingType 从未 Link 过 |
| `ErrTerminusNotLinked` | 100000034 | Entity 未关联 Terminus | 未调用 `LinkTerminusEntity` 就使用扩展方法发送 |
| `ErrAddRoamingTerminalAlreadyExists` | 100000010 | 漫游终端已存在 | 同一 roamingId 重复创建 |
| `ErrCreateTerminusInvalidRoamingId` | 100000028 | 无效的 RoamingId | 传入的 roamingId 为 0 |
| `ErrSetForwardSessionAddressNotFoundTerminus` | 100000027 | 未找到漫游终端 | 更新转发地址时找不到对应 Terminus |
| `ErrTerminusStartTransfer` | 100000017 | 传送过程错误 | StartTransfer 执行中异常 |
| `ErrTransfer` | 100000029 | 传送通用错误 | 传送过程中的通用错误 |
| `ErrLockTerminusIdNotFoundRoamingType` | 100000014 | 锁定时找不到漫游类型 | Lock/Unlock 请求到达 Gate 时对应 Roaming 已不存在 |

---

## 常见排查

### `ErrRoamingNotReady`（100000030）— 最常踩的坑

Link 未完成时就并发发送了消息，通常是忘记 `await`：

```csharp
// ❌ 错误：Link 和发送并发执行
roaming.Link(session, chatConfig, RoamingType.ChatRoamingType); // 没有 await
session.Call(new C2Chat_SendMessageRequest { });                // Link 还没完成

// ✅ 正确：等待 Link 完成后再发送
var errorCode = await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
if (errorCode == 0)
    await session.Call(new C2Chat_SendMessageRequest { });
```

### `ErrNotFoundRoaming`（100000011）

roamingType 从未建立过漫游，或已被断开。检查：
- 登录流程中是否遗漏了对应 roamingType 的 `Link` 调用
- 漫游是否已被 `UnLink` 或因超时被移除

### `ErrRoamingDisposed`（100000032）

漫游组件已销毁（Session 断开、主动移除等）。检查：
- Session 是否仍然有效（`session.IsDisposed`）
- 是否有其他逻辑提前销毁了漫游

### `ErrTerminusNotLinked`（100000034）

通过 Entity 扩展方法（如 `entity.Send()`）发送消息，但该 Entity 没有通过 `LinkTerminusEntity` 关联到 Terminus。检查 `OnCreateTerminus` 中是否调用了 `LinkTerminusEntity`。
