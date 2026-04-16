# Protocol Define Inner — 内网协议定义

> **前置检查（直接命中此文件时执行）：**
> 1. 确认协议目录已存在（参考 `references/protocol/define.md` 第 1-2 步），不存在则先创建
> 2. 确认 `Inner/` 子目录已存在，不存在则创建：`mkdir -p "{协议根目录}/Inner"`

内网协议（`Inner/`）定义服务器与服务器之间的消息，不对客户端开放。

## 文件头部格式

```protobuf
syntax = "proto3";
package YourGame.Inner;
```

---

## 消息命名规范

格式：`{发起方}2{目标方}_{功能名}`，`功能名`由用户根据业务自定义。

| 前缀示例 | 含义 |
|------|------|
| `G2M_` | Gate → Map |
| `M2G_` | Map → Gate |
| `G2Chat_` | Gate → Chat |
| `Chat2G_` | Chat → Gate |
| `M2M_` | Map → Map |

以上为常见示例，实际前缀根据项目服务器架构自定，规则不变：`{发起方}2{目标方}_`。

---

## 消息类型

### IAddressMessage — 服务器间单向消息

```protobuf
message {前缀}_{功能名} // IAddressMessage
{
    {类型} {字段名} = {编号};
}
```

示例：

```protobuf
/// Gate 通知 Map 同步玩家数据
message G2M_SyncPlayerData // IAddressMessage
{
    int64 PlayerId = 1;
    bytes Data = 2;
}
```

### IAddressRequest / IAddressResponse — 服务器间 RPC

```protobuf
message {前缀}_{功能名}Request // IAddressRequest,{Response消息名}
{
    {类型} {字段名} = {编号};
}
message {Response消息名} // IAddressResponse
{
    // ErrorCode 0=成功，非0=错误码（框架自带，不需要手动定义）
    {类型} {字段名} = {编号};
}
```

示例：

```protobuf
/// Gate 请求 Map 创建玩家
message G2M_CreatePlayerRequest // IAddressRequest,M2G_CreatePlayerResponse
{
    int64 AccountId = 1;
    string Name = 2;
}
message M2G_CreatePlayerResponse // IAddressResponse
{
    int64 PlayerId = 1;
}
```

---

## 字段类型、集合、Map、枚举、序列化、代码注入等通用特性

需要时阅读 `references/protocol/define-common.md`，**不需要时跳过**。

---

## 注意事项

- 同一消息内字段编号不能重复，从 1 开始递增
- `IAddressRequest` 注释中的 Response 名必须与实际 `message` 名**完全一致**（区分大小写）
- Request / Response 必须**成对**定义
- 内网协议文件放在 `Inner/` 目录下，不要混入 `Outer/`
- 不要手动修改导出生成的 `.cs` 文件
- RPC的返回消息中ErrorCode框架自带，不需要手动定义

---

## 完成后

协议写入 `.proto` 文件后，运行导出工具生成 C# 类，见 `references/protocol/export.md`。
