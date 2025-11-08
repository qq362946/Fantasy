# 网络协议目录结构说明

本文档详细介绍 Fantasy Framework 中网络协议配置文件的目录结构、文件作用以及如何组织和管理网络协议定义。

---

## 目录

- [网络协议目录概述](#网络协议目录概述)
- [如何获取 NetworkProtocol 目录](#如何获取-networkprotocol-目录)
  - [方式1: 通过 CLI 工具获取](#方式1-通过-cli-工具获取推荐)
  - [方式2: 从示例项目拷贝](#方式2-从示例项目拷贝)
- [Outer 文件夹 - 客户端服务器通信协议](#outer-文件夹---客户端服务器通信协议)
- [Inner 文件夹 - 服务器间通信协议](#inner-文件夹---服务器间通信协议)
- [OpCode.Cache - 协议代码缓存](#opcodecache---协议代码缓存)
- [协议接口类型说明](#协议接口类型说明)
  - [基础协议接口类型](#基础协议接口类型)
  - [协议注释格式详解](#协议注释格式详解)
  - [文档注释 - 自动生成代码注释](#文档注释---自动生成代码注释)
  - [接口类型对比总结](#接口类型对比总结)
  - [常见使用场景](#常见使用场景)
  - [选择合适的接口类型](#选择合适的接口类型)
- [协议定义规范](#协议定义规范)
  - [支持的数据类型](#支持的数据类型)
  - [字段重复类型](#字段重复类型)
- [最佳实践](#最佳实践)
  - [协议组织](#1-协议组织)
  - [命名规范](#2-命名规范)
- [相关文档](#相关文档)

---

## 网络协议目录概述

这个目录包含了 Fantasy Framework 中所有网络协议的定义文件,是整个分布式网络系统的核心配置之一。

### 主要功能

1. **协议定义**: 使用 `.proto` 格式定义消息结构
2. **类型管理**: 通过 `.Config` 文件管理路由和漫游类型
3. **代码生成**: 协议文件作为代码生成工具的输入源
4. **协议隔离**: 通过文件夹区分内网和外网协议

### 核心设计理念

- **内外分离**: Inner(服务器间) 和 Outer(客户端-服务器) 协议分离
- **类型枚举**: RouteType 和 RoamingType 提供协议类型的统一管理
- **自动生成**: 配合 `Fantasy.Tools.NetworkProtocol` 工具自动生成 C# 代码
- **序列化支持**: 支持 ProtoBuf、MemoryPack、Bson 三种序列化方式

---

## 如何获取 NetworkProtocol 目录

如果您正在创建新项目,需要获取 NetworkProtocol 目录模板,Fantasy Framework 提供了两种方式:

### 方式1: 通过 CLI 工具获取(推荐)

Fantasy CLI 提供了便捷的命令来初始化网络协议目录结构:

```bash
# 使用 Fantasy CLI 获得NetworkProtocol文件夹
fantasy add NetworkProtocol
```

**CLI 工具会自动:**
- 创建标准的 `NetworkProtocol` 目录结构
- 生成 `Inner/` 和 `Outer/` 文件夹
- 创建示例 `.proto` 文件
- 初始化 `RouteType.Config` 和 `RoamingType.Config` 配置文件
- 设置正确的文件权限和目录结构

**安装 Fantasy CLI:**

```bash
# 通过 .NET 工具安装
dotnet tool install -g Fantasy.CLI

# 验证安装
fantasy --version
```

### 方式2: 从示例项目拷贝

如果您已经克隆了 Fantasy Framework 源码仓库,可以直接从示例项目中拷贝 NetworkProtocol 目录到您的项目:

**源码位置:**
```
/Examples/Config/NetworkProtocol/
```

> **⚠️ 重要提示:**
> 从示例项目拷贝的 NetworkProtocol 目录包含**框架的示例网络协议**,这些协议仅用于演示和测试框架功能。在实际项目中使用时,**请务必手动删除所有示例协议**,然后根据您自己的业务需求重新定义协议。
>
> **需要清理的示例协议包括:**
> - `Outer/OuterMessage.proto` 中的所有示例消息(如 `C2G_TestMessage`、`C2M_TestRequest` 等)
> - `Inner/InnerMessage.proto` 中的所有示例消息(如 `G2A_TestMessage`、`M2M_SendUnitRequest` 等)
> - `RouteType.Config` 中的示例路由类型(根据需要保留或删除)
> - `RoamingType.Config` 中的示例漫游类型(根据需要保留或删除)
>
> **保留目录结构和配置文件格式即可,协议内容需要自行定义。**

### 各部分作用总览

| 文件/文件夹 | 用途                   | 修改方式 |
|------------|----------------------|---------|
| **Inner/** | 定义服务器间通信协议           | 手动编辑 `.proto` 文件 |
| **Outer/** | 定义客户端-服务器通信协议        | 手动编辑 `.proto` 文件 |
| **RouteType.Config** | 定义自定义路由类型枚举（后面会详细介绍） | 手动编辑,格式: `Name = ID // 注释` |
| **RoamingType.Config** | 定义漫游类型枚举 （后面会详细介绍）            | 手动编辑,格式: `Name = ID` |
| **OpCode.Cache** | OpCode 缓存,避免协议 ID 冲突 | 自动生成,不要手动修改 |

---

## Outer 文件夹 - 客户端服务器通信协议

### 文件位置
```
/NetworkProtocol/Outer/OuterMessage.proto
```

### 作用

Outer 文件夹包含所有**客户端-服务器通信(Client-to-Server)**的网络协议定义。这些协议是客户端和服务器之间通信的唯一接口。

### 使用示例

```protobuf
syntax = "proto3";
package Fantasy.Network.Message;

// 普通客户端消息(IMessage,单向发送)
message C2G_TestMessage // IMessage
{
    string Tag = 1;
}

// RPC 请求/响应(IRequest/IResponse)
message C2G_TestRequest // IRequest,G2C_TestResponse
{
    string Tag = 1;
}

message G2C_TestResponse // IResponse
{
    string Tag = 1;
}
```

---

## Inner 文件夹 - 服务器间通信协议

### 文件位置
```
/NetworkProtocol/Inner/InnerMessage.proto
```

### 作用

Inner 文件夹包含所有**服务器间通信(Server-to-Server)**的网络协议定义。这些协议仅在服务器内部使用,不对客户端开放。

### 支持的序列化方式

Inner 协议支持两种序列化方式:

| 序列化方式 | 适用场景 | 性能 | 可读性 |
|-----------|---------|------|--------|
| **ProtoBuf** | 通用场景,跨语言支持 | 高 | 低(二进制) |
| **Bson** | 需要可读性或动态数据 | 中 | 高(类JSON) |

**注意**: `Bson` 序列化**仅支持在 Inner 文件中使用**,不能在 Outer 文件中使用。

### 使用示例

```protobuf
syntax = "proto3";
package Sining.Message;

//、 默认使用 ProtoBuf 序列化
message G2A_TestMessage // IMessage
{
    string Tag = 1;
}

/// 使用 Bson 序列化(仅 Inner 支持)
// Protocol Bson
message M2M_SendUnitRequest // Iequest,M2M_SendUnitResponse
{
    Unit Unit = 1;
}

// Protocol Bson
message M2M_SendUnitResponse // IResponse
{

}
```

---

## OpCode.Cache - 协议代码缓存

### 文件位置
```
/NetworkProtocol/OpCode.Cache
```

### 作用

OpCode.Cache 是由 `Fantasy.Tools.NetworkProtocol` 工具**自动生成**的缓存文件,用于:

1. **记录协议 OpCode**: 每个消息协议都有唯一的 OpCode(协议编号)
2. **避免 ID 冲突**: 确保每次生成代码时 OpCode 保持一致
3. **增量更新**: 新增协议时分配未使用的 OpCode

### 文件内容示例

```
// OpCode.Cache 文件内容(示例)
C2G_TestMessage = 10001
C2G_TestRequest = 10002
G2C_TestResponse = 10003
G2A_TestMessage = 20001
...
```

### 重要事项

- **不要手动修改**: 此文件由工具自动维护
- **版本控制**: 建议将此文件加入 Git,确保团队成员协议 ID 一致
- **清理重置**: 如需重新生成所有 OpCode,删除此文件后重新运行工具

### OpCode 分配规则

框架会根据消息类型自动分配 OpCode 范围:

| OpCode 范围 | 消息类型 | 说明 |
|-----------|---------|------|
| 1-999 | 框架保留 | 不要使用 |
| 1000-9999 | Outer 协议 | 客户端-服务器协议 |
| 10000-19999 | Inner 协议 | 服务器间协议 |
| 20000+ | 扩展协议 | 可自定义范围 |

---

## 协议接口类型说明

在 Fantasy Framework 中,所有网络协议消息都需要通过**注释标记**指定接口类型。这些接口类型定义了消息的传输方式、通信模式和路由行为。

> **📌 重要:**
> 协议接口类型是通过 `.proto` 文件中的**注释**来标识的,而不是通过继承接口。
> 代码生成工具会解析这些注释,自动生成实现对应接口的 C# 类。

### 消息类型分类

Fantasy Framework 的网络协议主要分为两大类:

| 消息类型 | 通信模式 | 特点 | 适用场景 |
|---------|---------|------|---------|
| **单向消息** | 发送即忘(Fire-and-Forget) | 不需要响应,性能高 | 通知、状态同步、广播 |
| **RPC 消息** | 请求-响应(Request-Response) | 需要等待响应,支持异步 | 查询数据、执行操作 |

---

### 基础协议接口类型

以下接口类型在 **Outer(客户端-服务器)** 和 **Inner(服务器间)** 协议中都适用:

#### 1. IMessage - 单向消息

**定义格式:**
```protobuf
message MessageName // IMessage
{
    字段定义...
}
```

**说明:**
- 单向消息,发送后不等待响应
- 性能最高,适合高频通信
- 接收方处理消息,但不返回结果

**使用示例:**

```protobuf
/// 客户端通知服务器心跳
message C2G_Heartbeat // IMessage
{
    int64 Timestamp = 1;
}

/// 服务器推送消息给客户端
message G2C_NotifyMessage // IMessage
{
    string Content = 1;
    int32 MessageType = 2;
}
```

**发送消息:**

```csharp
// 使用 Helper 方法发送
session.C2G_Heartbeat(DateTime.UtcNow.Ticks);

// 或手动创建发送
var heartbeat = new C2G_Heartbeat { Timestamp = DateTime.UtcNow.Ticks };
session.Send(heartbeat);
```
---

#### 2. IRequest / IResponse - RPC 请求响应

**定义格式:**
```protobuf
// 请求消息
message RequestName // IRequest,ResponseName
{
    请求字段...
}

// 响应消息
message ResponseName // IResponse
{
    响应字段...
}
```

**说明:**
- `IRequest` 消息必须在注释中指定对应的 `IResponse` 消息名
- 请求和响应是成对定义的
- 发送请求后会等待响应,支持异步操作
- 框架自动处理请求-响应匹配

**使用示例:**

```protobuf
// 客户端请求登录
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
}

// 服务器返回登录结果
message G2C_LoginResponse // IResponse
{
    int32 ErrorCode = 1;      // 0=成功, 非0=错误码
    int64 PlayerId = 2;       // 玩家ID
    string Token = 3;         // 会话Token
}
```

**发送 RPC 请求:**

```csharp
// 使用 Helper 方法发送(推荐)
var response = await session.C2G_LoginRequest("player1", "password123");
if (response.ErrorCode == 0)
{
    Log.Info($"登录成功,玩家ID: {response.PlayerId}");
}
else
{
    Log.Error($"登录失败,错误码: {response.ErrorCode}");
}

// 或手动创建发送
var request = new C2G_LoginRequest
{
    Username = "player1",
    Password = "password123"
};
var response = await session.Call<G2C_LoginResponse>(request);
```

---

### 协议注释格式详解

协议接口类型是通过**消息定义后的注释**来标识的,格式如下:

```protobuf
message MessageName // InterfaceType[,AdditionalParameters]
{
    字段定义...
}
```

**格式规则:**

1. **注释标记以 `//` 开始**
2. **接口类型名称必须准确**（区分大小写）
3. **多个参数用逗号分隔**
4. **顺序有要求**（Request 必须先指定 Response 名称）

**示例:**

```protobuf
// ✅ 正确: 单向消息
message C2G_Ping // IMessage
{
}

// ✅ 正确: RPC 请求响应
message C2G_GetPlayerInfoRequest // IRequest,G2C_GetPlayerInfoResponse
{
    int64 PlayerId = 1;
}

message G2C_GetPlayerInfoResponse // IResponse
{
    string PlayerName = 1;
    int32 Level = 2;
}

// ❌ 错误: 缺少响应消息名称
message C2G_BadRequest // IRequest
{
}

// ❌ 错误: 接口类型拼写错误
message C2G_BadMessage // iMessage
{
}
```

---

### 文档注释 - 自动生成代码注释

Fantasy Framework 的协议导出工具支持**文档注释**功能,使用 `///` 标记的注释会被自动生成到 C# 代码中,作为 XML 文档注释。

#### 文档注释格式

```protobuf

/// 消息或字段的描述信息
message MessageName // IMessage
{
    /// 字段说明
    int32 FieldName = 1;
}
```

**注释规则:**

| 注释类型 | 格式 | 用途 | 生成结果 |
|---------|------|------|---------|
| `///` | 文档注释 | 为消息和字段添加说明文档 | 生成为 C# XML 文档注释 |
| `//` | 普通注释 | 接口类型标识或临时说明 | 不会生成到 C# 代码中 |

#### 文档注释示例

```protobuf

/// 客户端发送给服务器通知玩家位置变化
message C2G_PlayerMove // IMessage
{
    /// 目标位置 X 坐标
    float TargetX = 1;
    /// 目标位置 Y 坐标
    float TargetY = 2;
    /// 目标位置 Z 坐标
    float TargetZ = 3;
}
```

---

### 接口类型对比总结

| 接口类型 | 通信模式 | 是否等待响应 | 性能 | Helper 方法 | 适用场景 |
|---------|---------|------------|------|-----------|---------|
| **IMessage** | 单向发送 | ❌ 否 | 高 | `session.MessageName(params)` | 通知、心跳、广播 |
| **IRequest** | RPC 请求 | ✅ 是 | 中 | `await session.RequestName(params)` | 查询数据、执行操作 |
| **IResponse** | RPC 响应 | - | - | 无(自动匹配) | 返回 Request 的结果 |

---

### 常见使用场景

#### 场景1: 心跳和状态同步(使用 IMessage)

```protobuf
// 客户端定期发送心跳
message C2G_Heartbeat // IMessage
{
    int64 Timestamp = 1;
}

// 服务器同步玩家位置(不需要客户端回复)
message G2C_SyncPosition // IMessage
{
    int64 EntityId = 1;
    float X = 2;
    float Y = 3;
    float Z = 4;
}
```

#### 场景2: 登录和认证(使用 IRequest/IResponse)

```protobuf
// 登录请求
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
    int32 ClientVersion = 3;
}

message G2C_LoginResponse // IResponse
{
    int32 ErrorCode = 1;
    int64 PlayerId = 2;
    string SessionToken = 3;
}
```

#### 场景3: 查询玩家信息(使用 IRequest/IResponse)

```protobuf
// 查询背包信息
message C2G_GetInventoryRequest // IRequest,G2C_GetInventoryResponse
{
    int64 PlayerId = 1;
}

message G2C_GetInventoryResponse // IResponse
{
    repeated Item Items = 1;     // 使用 repeated 定义列表
    int32 MaxSlots = 2;
}

message Item
{
    int32 ItemId = 1;
    int32 Count = 2;
    int64 ExpireTime = 3;
}
```

#### 场景4: 服务器主动推送(使用 IMessage)

```protobuf
// 服务器通知客户端获得奖励
message G2C_RewardNotify // IMessage
{
    int32 RewardType = 1;
    int32 ItemId = 2;
    int32 Count = 3;
    string Reason = 4;
}

// 服务器广播世界消息
message G2C_WorldMessage // IMessage
{
    string Content = 1;
    int32 MessageType = 2;  // 1=系统公告, 2=世界聊天
}
```

---

### 选择合适的接口类型

**使用 IMessage 的场景:**
- ✅ 不需要返回结果的操作
- ✅ 高频率的状态同步
- ✅ 服务器向客户端推送通知
- ✅ 心跳、Ping等保活消息

**使用 IRequest/IResponse 的场景:**
- ✅ 需要获取服务器返回的数据
- ✅ 执行操作后需要确认结果
- ✅ 登录、查询、购买等业务操作
- ✅ 需要处理成功/失败的业务逻辑

---

## 协议定义规范

### 支持的数据类型

Fantasy Framework 基于 Protocol Buffers 3 (proto3) 标准，支持以下数据类型：

#### 1. 整数类型

| 类型 | C# 类型 | 说明 | 取值范围 |
|------------|---------|------|---------|
| `int32` | `int` | 32位有符号整数 | -2,147,483,648 到 2,147,483,647 |
| `uint32` | `uint` | 32位无符号整数 | 0 到 4,294,967,295 |
| `int64` | `long` | 64位有符号整数 | -9,223,372,036,854,775,808 到 9,223,372,036,854,775,807 |
| `uint64` | `ulong` | 64位无符号整数 | 0 到 18,446,744,073,709,551,615 |

**类型选择建议:**
- **int32/int64**: 默认选择，适合大多数场景
- **uint32/uint64**: 仅存储非负数时使用

#### 2. 浮点类型

| 类型 | C# 类型 | 说明 | 精度 |
|------------|---------|------|------|
| `float` | `float` | 32位单精度浮点数 | 约 7 位小数精度 |
| `double` | `double` | 64位双精度浮点数 | 约 15-16 位小数精度 |

**使用示例:**
```protobuf
message PlayerPosition
{
    float X = 1;          // 位置 X 坐标
    float Y = 2;          // 位置 Y 坐标
    float Z = 3;          // 位置 Z 坐标
    double Precision = 4; // 高精度数值
}
```

#### 3. 布尔类型

| 类型 | C# 类型 | 说明 | 取值 |
|------------|---------|------|------|
| `bool` | `bool` | 布尔值 | true 或 false |

**使用示例:**
```protobuf
message PlayerState
{
    bool IsOnline = 1;    // 是否在线
    bool IsDead = 2;      // 是否死亡
    bool CanMove = 3;     // 是否可移动
}
```

#### 4. 字符串类型

| 类型 | C# 类型 | 说明 | 编码 |
|------------|---------|------|------|
| `string` | `string` | UTF-8 或 ASCII 字符串 | UTF-8 |

**使用示例:**
```protobuf
message PlayerInfo
{
    string Username = 1;   // 用户名
    string Nickname = 2;   // 昵称
    string Email = 3;      // 邮箱
}
```

#### 5. 自定义消息类型

可以使用其他 message 作为字段类型：

```protobuf
/// 道具信息
message ItemInfo
{
    int32 ItemId = 1;
    int32 Count = 2;
}

/// 玩家背包
message InventoryInfo
{
    repeated ItemInfo Items = 1;  // 使用自定义类型
    int32 MaxSlots = 2;
}
```
#### 6. 数组类型

支持三种重复字段类型:

| 关键字 | 生成类型 | 说明 |
|-------|---------|------|
| `repeated` | `List<T> = new List<T>()` | 带初始化的 List |
| `repeatedArray` | `T[]` | 数组类型 |
| `repeatedList` | `List<T>` | 不带初始化的 List |

**示例:**

```protobuf
message TestMessage // IMessage
{
    repeated int32 Ids = 1;              // 生成: List<int> Ids = new List<int>();
    repeatedArray string Names = 2;      // 生成: string[] Names;
    repeatedList float Scores = 3;       // 生成: List<float> Scores;
}
```

---

## 最佳实践

### 1. 协议组织

**按功能模块拆分协议:**

```protobuf
// ========== 登录模块 ==========
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
}

// ========== 聊天模块 ==========
message C2Chat_SendMessage // ICustomRouteMessage,ChatRoute
{
    string Content = 1;
}

// ========== 背包模块 ==========
message C2G_GetItemsRequest // IRequest,G2C_GetItemsResponse
{
}
```

**使用注释分隔不同功能:**

```protobuf
// ========================================
// 玩家基础功能
// ========================================
message C2G_GetPlayerInfoRequest // IRequest,G2C_GetPlayerInfoResponse
{
}

// ========================================
// 战斗系统
// ========================================
message C2G_AttackRequest // IRequest,G2C_AttackResponse
{
    int64 TargetId = 1;
}
```

### 2. 命名规范

**消息命名格式: `Source2Target_ActionName[Request/Response/Message]`**

```protobuf
// 客户端到 Gate 服务器
message C2G_LoginRequest        // Client to Gate
message G2C_LoginResponse       // Gate to Client

// 客户端到 Map 服务器(Addressable)
message C2M_MoveRequest         // Client to Map
message M2C_MoveResponse        // Map to Client

// Gate 到 Map 服务器
message G2M_CreateEntityRequest // Gate to Map
message M2G_CreateEntityResponse // Map to Gate
```

**常用前缀:**
- `C` - Client(客户端)
- `G` - Gate(网关服务器)
- `M` - Map(地图服务器)
- `Chat` - Chat(聊天服务器)
- `A` - Auth(认证服务器)

**协议兼容性原则:**

1. **不要删除字段**: 使用新的消息类型替代
2. **不要修改字段编号**: 会导致序列化失败
3. **添加字段要向后兼容**: 使用可选字段

```protobuf
// ❌ 错误: 修改了字段编号
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 2;  // 原来是 1,不要修改!
}

// ✅ 正确: 添加新字段,保留旧字段
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
    string DeviceId = 3;  // 新增字段,使用新编号
}
```
---

### 相关文档

- 📖 阅读 [日志系统使用指南](06-LogSystem.md) 学习如何使用日志
- ⚙️ 阅读 [服务器配置](01-ServerConfiguration.md) 学习配置文件
- 🎯 阅读 [配置系统使用指南](05-ConfigUsage.md) 学习如何使用配置
- 🚀 阅读 [编写启动代码](02-WritingStartupCode.md) 学习如何启动框架

---
