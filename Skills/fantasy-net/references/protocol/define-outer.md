# Protocol Define Outer — 外网协议定义

> **前置检查（直接命中此文件时执行）：**
> 1. 确认协议目录已存在（参考 `references/protocol/define.md` 第 1-2 步），不存在则先创建
> 2. 确认 `Outer/` 子目录已存在，不存在则创建：`mkdir -p "{协议根目录}/Outer"`

外网协议（`Outer/`）定义客户端与服务器之间的**所有**消息，包括服务器主动推送给客户端的方向。

## 文件头部格式

每个 `.proto` 文件必须以此开头（`package` 名可自定义）：

```protobuf
syntax = "proto3";
package YourGame.Message;
```

---

## 消息命名规范

格式：`{发起方}2{目标方}_{功能名}`，`功能名`由用户根据业务自定义。

| 前缀 | 含义 |
|------|------|
| `C2G_` | Client → Gate |
| `G2C_` | Gate → Client（含服务器主动推送） |
| `C2M_` | Client → Map（通过 Addressable） |
| `M2C_` | Map → Client |
| `G2M_` | Gate → Map |
| `M2G_` | Map → Gate |

以上为常见示例，实际前缀根据项目服务器架构自定，规则不变：`{发起方}2{目标方}_`。

---

## 消息类型

消息类型通过**行尾注释**标识，导出工具解析注释生成对应 C# 接口。

### IMessage — 单向消息

发送后不等待响应，适合通知、心跳、服务器推送等高频场景。

```protobuf
/// 文档注释会生成到 C# 代码（三个斜线）
message {前缀}_{功能名} // IMessage
{
    {类型} {字段名} = {编号};
}
```

示例：

```protobuf
/// 客户端发送心跳
message C2G_Heartbeat // IMessage
{
    int64 Timestamp = 1;
}

/// 服务器主动踢下线
message G2C_Kick // IMessage
{
    string Reason = 1;
}
```

### IRequest / IResponse — RPC 请求响应

发送后等待响应，`IRequest` 注释中必须填写对应 Response 的**完整消息名**，Request 和 Response 必须成对紧邻定义。

```protobuf
message {前缀}_{功能名}Request // IRequest,{前缀对应Response}_{功能名}Response
{
    {类型} {字段名} = {编号};
}
message {前缀对应Response}_{功能名}Response // IResponse
{
    // ErrorCode 0=成功，非0=错误码（框架自带，不需要手动定义）
    {类型} {字段名} = {编号};
}
```

示例：

```protobuf
/// 客户端请求登录
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Account = 1;
    string Password = 2;
}
message G2C_LoginResponse // IResponse
{
    int64 PlayerId = 1;
    string Token = 2;
}
```

---

## 字段类型、集合、Map、枚举、序列化、代码注入等通用特性

需要时阅读 `references/protocol/define-common.md`，**不需要时跳过**。

---

## 注意事项

- 同一消息内字段编号不能重复，从 1 开始递增
- `IRequest` 注释中的 Response 名必须与实际 `message` 名**完全一致**（区分大小写）
- Request / Response 必须**成对**定义
- `///` 文档注释会生成到 C# 代码；`//` 普通注释不会
- 不要手动修改导出生成的 `.cs` 文件
- RPC的返回消息中ErrorCode框架自带，不需要手动定义

---

## 完成后

协议写入 `.proto` 文件后，运行导出工具生成 C# 类，见 `references/protocol/export.md`。
