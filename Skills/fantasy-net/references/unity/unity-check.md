# Unity 客户端审查清单

**本文件用于检查 Fantasy Unity 客户端代码。**

## 检查顺序

1. Unity 包版本和服务器端版本是否一致
2. 编译宏是否配置完整
3. 连接方式选择是否合理
4. Session 使用和生命周期是否正确
5. 主动推送 Handler 是否放在正确位置

## 常见问题

### 错误 1：`Fantasy.Unity` 和服务器端版本不一致

### 错误 2：漏配 `FANTASY_UNITY` 或 `FANTASY_WEBGL`

### 错误 3：连接方式混乱

需要在：

- `FantasyRuntime`
- `scene.Connect`
- `Runtime.Connect`

之间明确选一种主路径。

### 错误 4：Session 建好后生命周期管理混乱

重点检查：

- 是否正确持有 Session
- 断开连接时是否清理引用
- 重连逻辑是否完整

### 错误 5：接收服务器主动推送的 Handler 放错地方或签名不对

## 审查时重点问自己

1. 客户端初始化和连接方式是否统一
2. 协议导出产物是否同步
3. Session 生命周期是否清晰
4. 推送消息接收链路是否完整

## 相关文档

- `index.md`
- `setup-unity.md`
- `unity-connection.md`
- `unity-session.md`
- `unity-message-handler.md`
