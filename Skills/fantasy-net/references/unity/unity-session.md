# Unity 客户端：Session 使用

`Session` 是客户端与服务器通讯的核心对象，由 `scene.Connect()` 或 `Runtime.Connect()` 返回。  
协议导出工具（`Fantasy.ProtocolExportTool`）会为每条消息生成 `Session` 扩展方法，直接调用即可，无需手动构造消息对象。

## Workflow

```
确认用户需要哪种操作？
│
├─► 获取 Session──────────────────────────► 根据连接方式（scene.Connect / Runtime.Connect）生成代码
├─► 发送单向消息（IMessage）─────────────► _session.C2X_YourMessage(...)
├─► 发送 RPC 请求并等待响应（IRequest）──► var resp = await _session.C2X_YourRequest(...)
├─► 接收服务器主动推送────────────────────► 见 unity-message-handler.md
└─► 断开连接──────────────────────────────► _session.Dispose() 或随 Scene 销毁
```

---

## 获取 Session

连接方式不同，Session 的持有和访问方式也不同。提示用户选择，根据用户使用的连接方式生成对应代码：

### scene.Connect 方式

`_scene` 和 `_session` 必须声明为类的**私有成员变量**，不能用局部变量：

- `Entry.Initialize()` 全局只需调用一次，放在游戏启动最早的 MonoBehaviour 中
- `_scene.Connect()` 返回 `Session`，赋值给 `_session` 成员变量，后续发消息通过它访问
- `OnDestroy()` 中必须调用 `_scene?.Dispose()`，Session 随 Scene 一起释放，无需单独释放
- 连接三个回调（onConnectComplete / onConnectFail / onConnectDisconnect）根据用户需求决定是否实现
- 协议类型使用 `NetworkProtocolType` 枚举（`TCP` / `KCP` / `WebSocket`）

### Runtime.Connect 方式（推荐或者默认方式）

`Runtime.Connect` 内部已自动处理初始化和 Scene 创建，调用方不需要提前初始化：

- 连接成功后，`Runtime.Session` 和 `Runtime.Scene` 在项目任意处静态访问，无需保存成员变量
- 调用方若是 MonoBehaviour，`OnDestroy` 中调用 `Runtime.OnDestroy()` 清理资源
- 重连时再次调用 `Runtime.Connect()` 即可，框架自动断开旧连接并创建新连接
- `enableHeartbeat` 及心跳参数根据用户需求决定；不启用时 `enableHeartbeat` 传 `false`，其余心跳参数传 `0`
- 连接三个回调根据用户需求决定是否实现
- 协议类型使用 `FantasyRuntime.NetworkProtocolType` 枚举（与 `scene.Connect` 的枚举类型不同）

---

## 发送单向消息（IMessage）

协议导出工具为每条 `IMessage` 生成两个重载，根据用户实际协议和字段生成对应调用：

```csharp
// 重载一：直接传字段值（推荐）
_session.C2X_YourMessage(field1, field2, ...);

// 重载二：传入消息对象
_session.C2X_YourMessage(new C2X_YourMessage { Field1 = ..., Field2 = ... });
```

---

## 发送 RPC 请求（IRequest / IResponse）

协议导出工具为每条 `IRequest` 生成返回具体响应类型的异步扩展方法，无需手动转型。根据用户实际协议和字段生成对应调用：

```csharp
var response = await _session.C2X_YourRequest(field1, field2, ...);

if (response.ErrorCode != 0)
{
    Log.Error($"请求失败: {response.ErrorCode}");
    return;
}

// 根据用户实际响应字段访问 response.YourField
```

---

## 接收服务器主动推送

详见 `references/unity/unity-message-handler.md`。

---

## 断开 Session

调用 `_session.Dispose()` 会立即断开与服务器的连接，并通知服务器端该 Session 已关闭：

- 主动断开：直接调用 `_session.Dispose()`，适用于登出、切换服务器等场景
- 随 Scene 销毁断开：调用 `_scene.Dispose()` 或 `Runtime.OnDestroy()`，Session 随之一并释放，正常销毁流程走这条路
- Dispose 后 Session 进入 `IsDisposed = true` 状态，此时调用 Send / Call 会静默丢弃，不会报错
- 需要重连时，重新调用 `scene.Connect()` 或 `Runtime.Connect()` 获取新 Session，旧 Session 自动被替换

---

## 注意事项

- 所有发送方法都是线程安全的，可在任意位置调用
- RPC 请求（`Call`）没有内置超时，长时间无响应会挂起；业务层需自行处理超时逻辑
- 不要直接 `new` 消息对象后调用 `session.Send<T>()`，应使用导出工具生成的扩展方法
