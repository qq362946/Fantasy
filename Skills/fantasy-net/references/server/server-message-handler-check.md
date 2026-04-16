# 服务器消息 Handler 审查清单

**本文件用于检查客户端到服务器的消息 Handler。**

## 检查顺序

1. Handler 基类是否与消息类型匹配
2. Handler 是否 `sealed class`
3. 业务错误是否通过 `response.ErrorCode` 返回
4. `reply()` 使用是否正确
5. Session 生命周期和断线检查是否合理

## 常见问题

### 错误 1：`IMessage` / `IRequest` 对应的 Handler 基类选错

- 单向消息 -> `Message<T>`
- RPC -> `MessageRPC<TReq, TRes>`

### 错误 2：Handler 不是 `sealed class`

### 错误 3：业务错误直接抛异常

优先用 `response.ErrorCode` 表达业务失败。

### 错误 4：`reply()` 后还继续修改 `response`

`reply()` 后后续对 `response` 的修改不会再生效。

### 错误 5：耗时操作前不检查 `session.IsDisposed`

## 审查时重点问自己

1. 消息类型和 Handler 基类是否一一对应
2. 错误码模式是否统一
3. 是否错误使用 `reply()`
4. Session 生命周期检查是否充分

## 相关文档

- `server-message-handler.md`
- `protocol/index.md`
