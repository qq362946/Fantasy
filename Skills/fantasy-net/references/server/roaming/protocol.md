# Roaming 协议定义

## 第 1 步：配置 RoamingType

**确定协议目录。** 参考 `references/protocol/define.md` 第 1 步，找到协议根目录（即 `ExporterSettings.json` 中 `NetworkProtocolDirectory` 的值，或用户提供的路径）。

**找到或创建 `RoamingType.Config`。** 在协议根目录下搜索是否存在该文件：
- **存在** → 在文件末尾追加新的定义
- **不存在** → 在协议根目录下创建该文件

文件格式为每行一条定义：`<名称> = <数值>`

- 名称：`XXXRoamingType` 形式，根据用户需要的服务器类型命名
- 数值：必须 >= 10000（10000 以下框架预留），每条唯一，依次递增
- 每种后端服务器类型定义一条，有多少种后端服务器就定义多少条

导出后自动生成 `RoamingType.cs` 常量类，由源码生成器完成，无需手动注册。

---

## 第 2 步：定义漫游消息

每条消息通过注释末尾的 `RoamingType名称` 指定路由到哪个后端服务器。有多少种后端服务器，就可以定义多少组消息，各组之间完全独立。

### 单向消息 `IRoamingMessage`（只发不回）

格式：`// IRoamingMessage,<RoamingType名称>`

```protobuf
// Outer/OuterMessage.proto（客户端 → 后端）

// 路由到 Chat 服务器
message C2Chat_SendMessage // IRoamingMessage,ChatRoamingType
{
    string Content = 1;
}

// 路由到 Battle 服务器
message C2Battle_Ready // IRoamingMessage,BattleRoamingType
{
    bool IsReady = 1;
}

// 路由到 Map 服务器
message C2Map_Move // IRoamingMessage,MapRoamingType
{
    float X = 1;
    float Y = 2;
}
```

```protobuf
// Inner/InnerMessage.proto（服务器间）

// Gate → Chat
message G2Chat_Broadcast // IRoamingMessage,ChatRoamingType
{
    string Content = 1;
}

// Gate → Battle
message G2Battle_ForceEnd // IRoamingMessage,BattleRoamingType
{
    int32 Reason = 1;
}
```

### RPC 消息 `IRoamingRequest / IRoamingResponse`（有响应）

格式：`// IRoamingRequest,<响应类名>,<RoamingType名称>`；响应固定用 `// IRoamingResponse`

```protobuf
// 客户端 → Chat
message C2Chat_GetHistory // IRoamingRequest,Chat2C_GetHistoryResponse,ChatRoamingType
{
    int32 Count = 1;
}
message Chat2C_GetHistoryResponse // IRoamingResponse
{
    repeated string Messages = 1;
}

// 客户端 → Battle
message C2Battle_GetState // IRoamingRequest,Battle2C_GetStateResponse,BattleRoamingType
{
    int64 BattleId = 1;
}
message Battle2C_GetStateResponse // IRoamingResponse
{
    int32 State = 1;
}

// Gate → Map（Inner）
message G2Map_GetPlayerInfo // IRoamingRequest,Map2G_GetPlayerInfoResponse,MapRoamingType
{
    int64 PlayerId = 1;
}
message Map2G_GetPlayerInfoResponse // IRoamingResponse
{
    string Name = 1;
    int32 Level = 2;
}
```

---

## 第 3 步：导出生成 C# 类

参考 `references/protocol/export.md` 运行导出工具生成 C# 类。
