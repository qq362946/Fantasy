# 协议审查清单

**本文件用于检查 `.proto` 协议和对应 Handler 是否符合 Fantasy 规范。**

## 检查顺序

1. 协议放在 Outer 还是 Inner 是否正确
2. 消息接口是否匹配：`IMessage` / `IRequest` / `IResponse` / `IAddressRequest` 等
3. 命名是否符合约定
4. Request / Response 注释是否写对
5. 协议修改后是否考虑导出和同步 Handler

## 常见问题

### 错误 1：Outer / Inner 放错目录

- 客户端↔服务器 -> `Outer`
- 服务器↔服务器 -> `Inner`

### 错误 2：接口类型选错

重点检查：

- 单向消息 -> `IMessage`
- RPC -> `IRequest` / `IResponse`
- 服务器间 Address RPC -> `IAddressRequest` / `IAddressResponse`

### 错误 3：Request / Response 注释不匹配

Request 上注释的响应消息名必须和实际 Response 名称完全一致。

### 错误 4：协议改了，Handler 还按旧定义写

重点检查：

- Handler 泛型类型是否还是老协议
- Response 字段是否和 proto 对齐
- 新字段是否在处理逻辑里赋值

### 错误 5：改了协议但没导出

协议定义完成后，应考虑导出生成 C# 代码。

### 错误 6：手动修改导出生成文件

不要修改导出生成的 `.cs` 文件。

## 审查时重点问自己

1. 这条消息到底是 Outer、Inner、Address、Roaming 中的哪一种
2. 它是一问一答还是单向通知
3. 命名是否符合 C2G / G2C / G2M 之类现有约定
4. 协议和 Handler 是否同步变更
5. 是否遗漏导出步骤

## 相关文档

- `index.md`
- `define.md`
- `define-outer.md`
- `define-inner.md`
- `define-common.md`
- `export.md`
