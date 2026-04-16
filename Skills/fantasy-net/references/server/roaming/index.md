# Roaming 漫游系统

Roaming（漫游）让客户端通过 Gate 服务器**自动路由**到后端服务器（如 Map、Chat、Battle），无需在 Gate 写转发代码。

**核心流程：** 客户端发消息 → Gate 自动转发 → 后端处理 → 自动返回

**适用场景：** 客户端需要与多个后端服务器通信；玩家实体需要跨服传送

---

## Workflow

```
你要做什么？
│
├─► 定义漫游协议（RoamingType + 消息）──────────────► 读 protocol.md
│
├─► 建立漫游路由（登录/重连时，Gate 侧）────────────► 读 setup.md
│
├─► 后端监听连接/重连事件，创建业务实体 ────────────► 读 on-create-terminus.md
│
├─► 后端监听断开事件，保存数据/清理资源 ────────────► 读 on-dispose-terminus.md
│
├─► 发送漫游消息 / 实现后端 Handler               │
├─► 后端向客户端推送消息                          ├─► 读 handler.md
├─► 玩家实体跨服传送 ──────────────────────────────►
│
└─► 遇到 Roaming 相关错误，排查错误码 ──────────────► 读 error-codes.md
```

---

## 核心概念

**SessionRoamingComponent（Roaming 组件）**：挂在 Gate 的 Session 上，维护该玩家到各后端服务器的路由表。

**Terminus（漫游终端）**：在后端服务器（如 Chat）上代表该玩家连接的实体，可关联业务实体（如 `ChatPlayer`）。

**RoamingType**：路由标识，告诉框架把消息转发到哪个后端服务器。一个客户端可同时建立到多个服务器的路由（每个 RoamingType 对应一条）。

**消息流向：**

```
客户端
  │  发送 C2Chat_xxx（协议标注 ChatRoamingType）
  ▼
Gate（自动转发，无需手写代码）
  │
  ▼
Chat 服务器的 Terminus → 转给关联的 ChatPlayer → Handler 处理
```
