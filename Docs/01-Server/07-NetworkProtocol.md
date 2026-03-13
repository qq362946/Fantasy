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
  - [协议定义语法速查](#协议定义语法速查)
  - [核心对照表](#核心对照表)
  - [定义模板](#定义模板)
  - [枚举定义格式](#枚举定义格式)
  - [message 普通消息（IMessage）](#message-普通消息imessage)
  - [message 请求/返回消息（IRequest/IResponse）](#message-请求返回消息irequestiresponse)
  - [网络消息对象池管理](#网络消息对象池管理)
  - [协议注释格式与校验清单](#协议注释格式与校验清单)
  - [文档注释 - 自动生成代码注释](#文档注释---自动生成代码注释)
  - [接口类型对比总结](#接口类型对比总结)
  - [常见使用场景](#常见使用场景)
  - [选择合适的接口类型](#选择合适的接口类型)
- [协议定义规范](#协议定义规范)
  - [序列化方式声明](#序列化方式声明)
  - [原生代码注入](#原生代码注入)
  - [自定义命名空间](#自定义命名空间)
  - [支持的数据类型](#支持的数据类型)
  - [集合类型](#集合类型)
  - [Map/字典类型](#map字典类型)
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
- **序列化支持**: 支持 ProtoBuf、MemoryPack 两种序列化方式

---

## 如何获取 NetworkProtocol 目录

如果您正在创建新项目,需要获取 NetworkProtocol 目录模板,Fantasy Framework 提供了两种方式:

### 方式1: 通过 CLI 工具获取(推荐)

Fantasy CLI 提供了便捷的命令来初始化网络协议目录结构:

```bash
# 使用 Fantasy CLI 获得NetworkProtocol文件夹
fantasy add -t networkprotocol
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
dotnet tool install -g Fantasy.Cli

# 验证安装
fantasy --version
```

> **⚠️ macOS/Linux 用户注意：**
>
> 如果安装后无法直接使用 `fantasy` 命令，需要配置 PATH 环境变量。
>
> **详细配置步骤请查看：** [Fantasy CLI 完整文档](../../Fantasy.Packages/Fantasy.Cil/README.md)（查看"安装"章节）

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

### 使用示例

```protobuf
syntax = "proto3";
package Sining.Message;

// 默认使用 ProtoBuf 序列化
message G2A_TestMessage // IMessage
{
    string Tag = 1;
}

// 使用 MemoryPack 序列化
// Protocol MemoryPack
message M2M_SendUnitRequest // IRequest,M2M_SendUnitResponse
{
    Unit Unit = 1;
}

// Protocol MemoryPack
message M2M_SendUnitResponse // IResponse
{

}
```

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

在 Fantasy Framework 中，网络协议定义主要由 `enum` 和 `message` 两部分组成。

- `enum`：定义枚举类型（不需要接口注释）
- `message`：定义消息结构（需要通过注释标记消息接口类型）

> **📌 重要:**
> 协议接口类型是通过 `.proto` 文件中的**注释**来标识的，而不是在 `.proto` 中写接口继承。
> 代码生成工具会解析这些注释，自动生成实现对应接口的 C# 类。

### 协议定义语法速查

先看一眼“定义格式”全景，再看各类型细节。

### 核心对照表

| 定义对象 | `.proto` 定义格式 | 接口注释要求 | 是否成对 | 是否等待响应 | 典型用途 |
|---------|-------------------|-------------|---------|-------------|---------|
| **枚举** | `enum EnumName` | 无 | 否 | 否 | 状态、类型、错误码常量 |
| **普通消息** | `message Xxx // IMessage` | 必须 `// IMessage` | 否 | 否 | 通知、广播、状态同步 |
| **请求消息** | `message XxxRequest // IRequest,XxxResponse` | 必须 `// IRequest,响应名` | 是 | 是 | 查询、执行操作 |
| **返回消息** | `message XxxResponse // IResponse` | 必须 `// IResponse` | 是（与 Request 配对） | - | 返回请求结果 |

---

### 定义模板

```protobuf
syntax = "proto3";
package Fantasy.Network.Message;

// 1) 枚举定义：不需要接口注释
enum ErrorCode
{
    Success = 0,
    InvalidToken = 1,
}

// 2) 普通消息：IMessage
message C2G_Heartbeat // IMessage
{
    int64 Timestamp = 1;
}

// 3) 请求 + 返回：IRequest / IResponse
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
}

message G2C_LoginResponse // IResponse
{
    int64 PlayerId = 1;
    string Token = 2;
}
```

### 枚举定义格式

**定义格式:**

```protobuf
enum EnumName
{
    ValueA = 0,
    ValueB = 1,
}
```

**说明:**

- 枚举用于定义一组离散常量（状态、类型、错误码）
- 枚举不属于消息接口体系，因此不使用 `// IMessage` 等注释
- 枚举值建议显式编号，便于协议长期维护

**使用示例:**

```protobuf
enum ChatChannel
{
    World = 0,
    Guild = 1,
    Private = 2,
}
```

### message 普通消息（IMessage）

**定义格式:**

```protobuf
message MessageName // IMessage
{
    字段定义...
}
```

**说明:**

- 单向消息，发送后不等待响应
- 性能高，适合高频通信
- 接收方处理消息，但不返回结果

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

### message 请求/返回消息（IRequest/IResponse）

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

- `IRequest` 必须在注释中指定对应的响应消息名
- 请求和响应必须成对定义
- 发送请求后会等待响应，支持异步操作
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
    int64 PlayerId = 1;       // 玩家ID
    string Token = 2;         // 会话Token
    // int32 ErrorCode = 3;   // 0=成功, 非0=错误码, 生成器自动添加该字段
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

### 网络消息对象池管理

Fantasy Framework 为所有网络消息提供了高效的对象池管理机制，通过 `autoReturn` 参数控制消息对象的自动回收行为。

#### Create 方法语法

```csharp
// 所有网络消息都支持 autoReturn 参数
MessageType.Create(autoReturn: bool = true)
```

#### autoReturn 参数说明

| 参数值 | 回收行为 | 适用场景 |
|-------|---------|---------|
| `true`（默认） | Send/Call 后自动回收到对象池 | 单次发送、常规场景 |
| `false` | 手动控制回收时机，需调用 `Return()` | 消息群发、并发查询 |

#### 使用示例

**场景1: 单次发送（默认行为）**

```csharp
// IMessage 单向消息
var message = C2G_Heartbeat.Create(); // autoReturn = true（默认）
message.Timestamp = DateTime.UtcNow.Ticks;
session.Send(message); // 发送后自动回收到对象池

// IRequest 请求消息
var request = C2G_LoginRequest.Create(); // autoReturn = true（默认）
request.Username = "player1";
request.Password = "password123";
var response = await session.Call<G2C_LoginResponse>(request); // 发送后自动回收
```

**场景2: 消息群发（手动回收）**

```csharp
// 创建消息，禁用自动回收
var message = G2C_NotifyMessage.Create(autoReturn: false);
message.Content = "系统公告";
message.MessageType = 1;

// 群发给多个客户端
foreach (var clientSession in clientSessions)
{
    clientSession.Send(message); // 不会自动回收
}

// 使用完成后手动回收
message.Return();
```

**场景3: 并发查询多个服务器**

```csharp
// 创建请求，禁用自动回收
var request = C2G_GetPlayerInfoRequest.Create(autoReturn: false);
request.PlayerId = targetPlayerId;

// 并发查询多个服务器
var tasks = new List<Task<G2C_GetPlayerInfoResponse>>();
foreach (var serverSession in serverSessions)
{
    tasks.Add(serverSession.Call<G2C_GetPlayerInfoResponse>(request));
}

// 等待所有查询完成
var responses = await Task.WhenAll(tasks);

// 处理响应...
foreach (var response in responses)
{
    Log.Info($"玩家名称: {response.PlayerName}");
}

// 手动回收请求对象
request.Return();
```

#### 优势说明

使用 `autoReturn = false` 的优势：

- ✅ **减少 GC 压力**: 群发或并发场景下只需创建一次对象
- ✅ **提升性能**: 避免重复 Create/Dispose 的开销
- ✅ **灵活控制**: 根据实际场景选择合适的回收策略
- ✅ **内存高效**: 对象池复用，减少内存分配

#### 注意事项

- ⚠️ 使用 `autoReturn = false` 时，**必须手动调用 `Return()`** 回收对象，否则会导致内存泄漏
- ⚠️ 不要在 `Send()`/`Call()` 之后继续使用消息对象（autoReturn = true 时）
- ⚠️ 群发场景下，确保所有发送操作完成后再调用 `Return()`

---

### 协议注释格式与校验清单

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

**校验清单（推荐提交前自检）:**

- [ ] 每个 `message` 都写了正确接口注释（`IMessage` / `IRequest,...` / `IResponse`）
- [ ] 每个 `IRequest` 都有且仅有一个对应的 `IResponse`
- [ ] `IRequest` 注释中的响应名与真实 `message` 名字完全一致（区分大小写）
- [ ] 枚举统一使用 `enum` 定义，没有误写为 `message` 或注释接口类型
- [ ] 字段编号稳定递增，未复用、未随意改号

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
    int64 PlayerId = 1;
    string SessionToken = 2;
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

### 序列化方式声明

Fantasy Framework 支持两种序列化方式，可以通过 `// Protocol` 注释指定每个消息的序列化方式。

#### 支持的序列化方式

| 序列化方式 | 适用场景 | 性能 | 可读性 | 支持范围 |
|-----------|---------|------|--------|---------|
| **ProtoBuf** | 通用场景,跨语言支持 | 高 | 低(二进制) | Outer/Inner |
| **MemoryPack** | .NET 高性能序列化 | 极高 | 低(二进制) | Outer/Inner |

#### 声明格式

```protobuf
// Protocol SerializationType
message MessageName // IMessage
{
    字段定义...
}
```

#### 使用示例

```protobuf
// 默认使用 ProtoBuf 序列化（可省略 Protocol 声明）
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
}

// 显式声明使用 ProtoBuf 序列化
// Protocol ProtoBuf
message G2A_TestMessage // IMessage
{
    string Tag = 1;
}

// 使用 MemoryPack 序列化（Outer/Inner 都支持）
// Protocol MemoryPack
message C2G_HighPerformanceRequest // IRequest,G2C_HighPerformanceResponse
{
    int32 Data = 1;
}

// Protocol MemoryPack
message M2M_SendUnitRequest // IRequest,M2M_SendUnitResponse
{
    Unit Unit = 1;
}
```

#### 重要说明

- **Outer 和 Inner 协议**都可以使用 `ProtoBuf` 或 `MemoryPack` 序列化
- 未声明 `// Protocol` 时默认使用 `ProtoBuf`
- 同一个 `.proto` 文件中可以混合使用不同的序列化方式
- `MemoryPack` 性能更高，但仅限 .NET 环境使用

---

### 原生代码注入

Fantasy Framework 支持通过 `////` 前缀标记将原生 C# 代码注入到生成的协议文件中。

#### 声明格式

```protobuf
////代码内容
```

#### 功能说明

- 以 `////` 开头的行会**原样输出**到生成的 C# 代码中（去除 `////` 前缀）
- 适用于条件编译指令、平台特性标注等场景
- 可以注入任何合法的 C# 代码

#### 使用示例

```protobuf
////#if FANTASY_UNITY
////[Serializable]
////#endif
message PlayerData // IMessage
{
    string Name = 1;
    int32 Level = 2;
}
```

#### 生成的 C# 代码

```csharp
#if FANTASY_UNITY
[Serializable]
#endif
public partial class PlayerData : AMessage, IMessage
{
    [ProtoMember(1)]
    public string Name { get; set; }

    [ProtoMember(2)]
    public int Level { get; set; }
}
```

#### 应用场景

- ✅ 添加条件编译指令（#if/#endif）
- ✅ 为 Unity 平台添加 `[Serializable]` 特性
- ✅ 添加自定义特性标注
- ✅ 注入平台特定代码

#### 注意事项

- ⚠️ 确保注入的代码语法正确，否则会导致生成的 C# 代码编译失败
- ⚠️ 条件编译指令要成对出现（#if 和 #endif）
- ⚠️ 注入代码会影响所有目标平台，建议配合条件编译使用

---

### 自定义命名空间

Fantasy Framework 支持通过 `// using` 注释在生成的 C# 代码中添加自定义命名空间引用。

#### 声明格式

```protobuf
// using NamespaceName
```

#### 使用示例

```protobuf
// 添加自定义命名空间
// using System.Runtime
// using System.Reflection
// using MyProject.CustomTypes

message PlayerData
{
    // 现在可以使用这些命名空间中的类型
    CustomPlayerInfo Info = 1;
}
```

#### 生成的 C# 代码

```csharp
using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;
using System.Runtime;          // 自定义命名空间
using System.Reflection;        // 自定义命名空间
using MyProject.CustomTypes;    // 自定义命名空间

namespace Fantasy
{
    public partial class PlayerData : AMessage
    {
        // ...
    }
}
```

#### 应用场景

- 引用项目中的自定义类型
- 使用第三方库的类型
- 引用特殊的系统命名空间
- 在协议中使用枚举或复杂类型

---

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

#### 6. 自定义类型（原样保留）

**重要说明：** 如果您定义的类型不是框架识别的基本类型，协议导出工具会**原封不动**地按照您定义的样子生成到 C# 代码中。

这个特性允许您使用：
- 项目中自定义的结构体类型
- Unity 的内置类型（如 `Vector3`、`Quaternion` 等）
- 第三方库的类型
- 任何其他自定义类型

**使用示例：**

```protobuf
// 使用 Unity 的 float2 类型（需要配合 // using 引入命名空间）
// using Unity.Mathematics

message PlayerPosition
{
    float2 Position = 1;      // 生成: float2 Position { get; set; }
    float3 Rotation = 2;      // 生成: float3 Rotation { get; set; }
}

// 使用自定义结构体类型
// using MyProject.Types

message GameState
{
    CustomVector Position = 1;     // 生成: CustomVector Position { get; set; }
    MyCustomType Data = 2;          // 生成: MyCustomType Data { get; set; }
}
```

**生成的 C# 代码：**

```csharp
using Unity.Mathematics;
using MyProject.Types;

public partial class PlayerPosition : AMessage
{
    [ProtoMember(1)]
    public float2 Position { get; set; }    // 原样保留

    [ProtoMember(2)]
    public float3 Rotation { get; set; }    // 原样保留
}
```

**注意事项：**
- ⚠️ 确保使用的自定义类型在项目中可访问
- ⚠️ 需要通过 `// using` 引入正确的命名空间
- ⚠️ 自定义类型必须支持所选的序列化方式（ProtoBuf/MemoryPack）
- ⚠️ 类型名称区分大小写

**适用场景：**
- ✅ 使用 Unity 的 Mathematics 库类型（float2、float3、float4 等）
- ✅ 使用项目中定义的结构体或类
- ✅ 使用第三方库的数据类型
- ✅ 需要特定类型以保持代码一致性

---

### 集合类型

Fantasy Framework 支持三种重复字段(数组/列表)类型，用于存储多个相同类型的元素。

#### 重复字段类型对比

| 关键字 | 生成类型 | 初始化 | Dispose 行为 | 适用场景 |
|-------|---------|--------|-------------|---------|
| `repeated` | `List<T>` | ✅ 自动初始化 | `Clear()` | 默认选择，适合大多数场景 |
| `repeatedList` | `List<T>` | ❌ 不初始化 | `= null` | 节省内存，允许 null 值 |
| `repeatedArray` | `T[]` | ❌ 不初始化 | `= null` | 需要固定大小数组时使用 |

#### 使用示例

```protobuf
message TestMessage // IMessage
{
    repeated int32 Ids = 1;              // 生成: List<int> Ids = new List<int>();
    repeatedList string Names = 2;       // 生成: List<string> Names;
    repeatedArray float Scores = 3;      // 生成: float[] Scores;
}
```

#### 生成的 C# 代码

```csharp
public partial class TestMessage : AMessage, IMessage
{
    public static TestMessage Create()
    {
        return MessageObjectPool<TestMessage>.Rent();
    }

    public void Dispose()
    {
        Ids.Clear();          // repeated 调用 Clear()
        Names = null;         // repeatedList 置为 null
        Scores = null;        // repeatedArray 置为 null
    }

    [ProtoMember(1)]
    public List<int> Ids { get; set; } = new List<int>();

    [ProtoMember(2)]
    public List<string> Names { get; set; }

    [ProtoMember(3)]
    public float[] Scores { get; set; }
}
```

#### 类型选择建议

**使用 `repeated` (推荐):**
- ✅ 默认选择，适合大多数场景
- ✅ 自动初始化，避免空引用异常
- ✅ Dispose 时调用 `Clear()` 清空元素，List 对象可复用

**使用 `repeatedList`:**
- ✅ 需要区分"空列表"和"null"的语义
- ✅ 节省内存（不需要时可以为 null）
- ⚠️ 使用前需要检查 null

**使用 `repeatedArray`:**
- ✅ 需要固定大小的数组
- ✅ 与某些 API 要求数组类型
- ⚠️ 不支持动态添加元素

---

### Map/字典类型

Fantasy Framework 支持 `map` 类型，用于存储键值对(Key-Value)数据，生成 C# 的 `Dictionary<TKey, TValue>` 类型。

#### 语法格式

```protobuf
map<KeyType, ValueType> FieldName = FieldNumber;
```

#### 支持的 Key 类型

Map 的 Key 类型**必须是基本类型或枚举**，不能是复杂对象：

| Key 类型分类 | 支持的类型 |
|------------|-----------|
| **整数类型** | `int`, `int32`, `uint`, `uint32`, `long`, `int64`, `ulong`, `uint64`, `byte` |
| **字符串类型** | `string` |
| **布尔类型** | `bool` |
| **枚举类型** | 任何自定义枚举（以大写字母开头的类型） |

#### 支持的 Value 类型

Value 类型支持：
- ✅ 所有基本类型（int, string, bool, float, double 等）
- ✅ 自定义枚举类型
- ✅ 自定义消息类型（以大写字母开头的类型）

#### 使用示例

```protobuf
/// 玩家数据
message PlayerData
{
    /// 玩家属性 (属性ID -> 属性值)
    map<int32, int32> Attributes = 1;

    /// 玩家装备 (装备槽位 -> 装备ID)
    map<int32, int64> Equipment = 2;

    /// 玩家好友列表 (好友ID -> 好友名称)
    map<int64, string> Friends = 3;

    /// 背包物品 (物品ID -> 物品数据)
    map<int32, ItemData> Inventory = 4;
}

/// 物品数据
message ItemData
{
    int32 ItemId = 1;
    int32 Count = 2;
    int32 Quality = 3;
}

/// 使用枚举作为 Key
enum AttributeType
{
    Strength = 0,
    Agility = 1,
    Intelligence = 2
}

message PlayerAttributes
{
    /// 使用枚举作为 Key
    map<AttributeType, int32> Attributes = 1;
}
```

#### 生成的 C# 代码

```csharp
public partial class PlayerData : AMessage
{
    public static PlayerData Create()
    {
        return MessageObjectPool<PlayerData>.Rent();
    }

    public void Dispose()
    {
        Attributes.Clear();
        Equipment.Clear();
        Friends.Clear();
        Inventory.Clear();
    }

    /// <summary>
    /// 玩家属性 (属性ID -> 属性值)
    /// </summary>
    [ProtoMember(1)]
    public Dictionary<int, int> Attributes { get; set; } = new Dictionary<int, int>();

    /// <summary>
    /// 玩家装备 (装备槽位 -> 装备ID)
    /// </summary>
    [ProtoMember(2)]
    public Dictionary<int, long> Equipment { get; set; } = new Dictionary<int, long>();

    /// <summary>
    /// 玩家好友列表 (好友ID -> 好友名称)
    /// </summary>
    [ProtoMember(3)]
    public Dictionary<long, string> Friends { get; set; } = new Dictionary<long, string>();

    /// <summary>
    /// 背包物品 (物品ID -> 物品数据)
    /// </summary>
    [ProtoMember(4)]
    public Dictionary<int, ItemData> Inventory { get; set; } = new Dictionary<int, ItemData>();
}
```

#### Map 特性说明

| 特性 | 说明 |
|-----|------|
| **自动初始化** | 生成的字段会自动初始化为 `new Dictionary<TKey, TValue>()` |
| **Dispose 行为** | 调用 `Clear()` 清空所有元素，Dictionary 对象可复用 |
| **Key 唯一性** | Dictionary 的 Key 必须唯一，重复 Key 会覆盖值 |
| **性能** | 查找性能为 O(1)，适合频繁查询的场景 |

#### 使用示例代码

```csharp
// 使用 map 字段
var player = PlayerData.Create();

// 添加属性
player.Attributes[1] = 100;  // 力量
player.Attributes[2] = 50;   // 敏捷

// 添加装备
player.Equipment[1] = 10001;  // 武器槽
player.Equipment[2] = 10002;  // 护甲槽

// 添加好友
player.Friends[1001] = "Alice";
player.Friends[1002] = "Bob";

// 添加物品
player.Inventory[1] = new ItemData
{
    ItemId = 1,
    Count = 10,
    Quality = 3
};

// 查询
if (player.Attributes.TryGetValue(1, out var strength))
{
    Log.Info($"力量值: {strength}");
}

// 遍历
foreach (var (itemId, itemData) in player.Inventory)
{
    Log.Info($"物品 {itemId}: 数量 {itemData.Count}");
}

// Dispose 清理
player.Dispose();  // 调用 Clear() 清空所有字典
```

#### 错误示例

```protobuf
// ❌ 错误：Key 类型为空
map<, string> Data = 1;

// ❌ 错误：Value 类型为空
map<int, > Data = 1;

// ❌ 错误：Key 类型是复杂对象（不支持）
map<PlayerData, int> Data = 1;

// ✅ 正确：使用基本类型作为 Key
map<int, PlayerData> Data = 1;

// ✅ 正确：使用枚举作为 Key
map<AttributeType, int> Data = 1;
```

#### 应用场景

**使用 map 的常见场景：**
- ✅ 玩家属性系统（属性ID -> 属性值）
- ✅ 背包系统（物品ID -> 物品数据）
- ✅ 装备系统（装备槽位 -> 装备ID）
- ✅ 好友列表（好友ID -> 好友信息）
- ✅ 成就系统（成就ID -> 进度）
- ✅ 任务系统（任务ID -> 任务状态）
- ✅ 配置缓存（配置ID -> 配置数据）

**不适合使用 map 的场景：**
- ❌ 需要保持元素顺序（使用 `repeated` 代替）
- ❌ 需要重复的 Key（使用 `repeated` 存储键值对对象）
- ❌ Key 类型是复杂对象（不支持）

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

- 🛠️ 阅读 [网络协议导出工具使用指南](08-NetworkProtocolExporter.md) 学习如何生成协议代码
- 📖 阅读 [日志系统使用指南](06-LogSystem.md) 学习如何使用日志
- ⚙️ 阅读 [服务器配置](01-ServerConfiguration.md) 学习配置文件
- 🎯 阅读 [配置系统使用指南](05-ConfigUsage.md) 学习如何使用配置
- 🚀 阅读 [编写启动代码](02-WritingStartupCode.md) 学习如何启动框架

---
