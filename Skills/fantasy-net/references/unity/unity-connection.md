# Unity 客户端：初始化框架与连接服务器

## Workflow

```
询问用户：使用组件方式 还是 纯代码方式？
│
├─► [方式 A：FantasyRuntime 组件] — 推荐，零代码，Inspector 配置
│    ├─ 创建 GameObject，挂载 FantasyRuntime 组件
│    ├─ 配置连接参数（IP、端口、协议等）
│    ├─ 配置心跳参数
│    ├─ 配置事件回调
│    └─ 组件在 Start() 时自动完成初始化和连接
│
└─► [方式 B：纯代码] — 需要动态控制连接时机时使用
     ├─ 选择 scene.Connect（有独立 Scene 实例需求）
     └─ 选择 Runtime.Connect（全局单一连接，静态访问）
```

---

## 方式 A：FantasyRuntime 组件（推荐）

创建 GameObject → 挂载 `FantasyRuntime` 组件 → Inspector 配置以下字段：

| 字段 | 说明 | 默认值 |
|---|---|---|
| `remoteIP` / `remotePort` | 服务器地址和端口 | `127.0.0.1` / `20000` |
| `protocol` | `TCP` / `KCP` / `WebSocket` | `TCP` |
| `enableHttps` | WebSocket 时是否用 WSS | `false` |
| `connectTimeout` | 连接超时（毫秒） | `5000` |
| `enableHeartbeat` | 启用心跳 | `true` |
| `heartbeatInterval` / `heartbeatTimeOut` | 心跳间隔 / 超时（毫秒） | `2000` / `30000` |
| `isRuntimeInstance` | 勾选后可用 `Runtime.Session` / `Runtime.Scene` 静态访问 | `false` |
| `onConnectComplete/Fail/Disconnect` | 连接事件回调 | — |

组件在 `Start()` 时自动完成初始化和连接，无需额外代码。

---

## 方式 B：纯代码

### scene.Connect

适用于有独立 Scene 实例需求、或需要精确控制连接时机的场景。  
连接后如何持有和使用 Session，见 `references/unity/unity-session.md` — 获取 Session。

### Runtime.Connect

适用于全局单一连接、需要在项目任意处通过 `Runtime.Session` / `Runtime.Scene` 静态访问的场景。  
连接后如何持有和使用 Session，见 `references/unity/unity-session.md` — 获取 Session。

---

## 协议选择

| 协议 | 适用场景 |
|---|---|
| KCP | 实时对战、低延迟（推荐） |
| TCP | 稳定连接、聊天类 |
| WebSocket | H5 / WebGL 必选（需 `FANTASY_WEBGL` 编译符号） |

WebGL 平台会自动路由到 WebSocket，无需修改连接代码，只需确保编译符号正确。

---

## 参考

- `references/unity/unity-session.md` — 连接成功后如何使用 Session 发送消息、接收推送
