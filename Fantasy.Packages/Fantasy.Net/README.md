# Fantasy Framework

**专为大型多人在线游戏打造的高性能分布式服务器框架**

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/qq362946/Fantasy/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-brightgreen.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Unity](https://img.shields.io/badge/Unity-2022.3.62+-black.svg)](https://unity.com/)
[![NuGet](https://img.shields.io/nuget/v/Fantasy-Net.svg)](https://www.nuget.org/packages/Fantasy-Net/)
[![Downloads](https://img.shields.io/nuget/dt/Fantasy-Net.svg)](https://www.nuget.org/packages/Fantasy-Net/)

**[📖 官方文档](https://github.com/qq362946/Fantasy/tree/main/Docs)** | **[🚀 快速开始](#-快速开始)** | **[💬 QQ群: 569888673](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=yourqrcode)**

---

## ⭐ Fantasy 是什么？

Fantasy 是一个**零反射、高性能的 C# 游戏服务器框架**，专为大型多人在线游戏打造。

**核心特点：**
- ⚡ **零反射架构** - 编译时代码生成，Native AOT 完美支持
- 🚀 **极致性能** - 对象池、内存优化、原生集合
- 🌐 **多协议支持** - TCP/KCP/WebSocket/HTTP 一键切换
- 🔥 **分布式架构** - Server-to-Server 通信、跨服事件系统
- ☸️ **Kubernetes 部署** - TCP/KCP 支持 Pod DNS 绑定，配合 StatefulSet 稳定网络身份
- 🎮 **ECS 设计** - Entity-Component-System，灵活扩展
- 📦 **完整工具链** - 脚手架工具、协议生成器、配置表导出

---

## 🚀 快速开始

### 方式一：使用 Fantasy CLI 脚手架（推荐）

**安装 Fantasy CLI：**
```bash
dotnet tool install -g Fantasy.Cli
```

**创建项目：**
```bash
fantasy init -n MyGameServer
```

一行命令即可创建完整的项目结构，包括：
- ✅ 服务器项目（Main/Entity/Hotfix 三层结构）
- ✅ Fantasy.config 配置文件
- ✅ 网络协议定义和导出工具
- ✅ NLog 日志配置

### 方式二：NuGet 包安装

**1. 安装 NuGet 包：**
```bash
dotnet add package Fantasy-Net
```

**2. 创建配置文件 `Fantasy.config`（自动生成）**

**3. 编写启动代码：**
```csharp
using Fantasy;

// 加载程序集
AssemblyHelper.Initialize(typeof(Program).Assembly);

// 启动 Fantasy 框架
await Fantasy.Entry.Initialize(args);
```

**4. 运行服务器：**
```bash
dotnet run
```

📖 **详细教程**：[快速开始文档](https://github.com/qq362946/Fantasy/blob/main/Docs/00-GettingStarted/01-QuickStart-Server.md)

---

## 🎯 适用场景

| 游戏类型 | Fantasy 优势 |
|---------|-------------|
| 🏰 **MMORPG** | 分布式架构、跨服通信、实体寻址系统 |
| ⚔️ **实时对战** | KCP 低延迟协议、高性能 ECS 架构 |
| 🎲 **回合制/卡牌** | TCP/WebSocket 可靠通信、数据持久化 |
| 🌍 **开放世界** | 场景管理、实体层级系统 |
| 🏪 **H5/小游戏** | WebSocket 支持、Unity WebGL 兼容 |

---

## 💡 核心功能

### 1. 网络协议助手 - 一行代码搞定网络通信

传统框架需要 50+ 行代码处理消息序列化、发送、回调、超时...

**Fantasy 只需 1 行：**
```csharp
// 自动生成的扩展方法，自动处理序列化、OpCode、回调
var response = await session.C2G_Login("player123", "password");
```

**协议定义：**
```protobuf
// Inner/G2G_CreateRoom.proto
message G2G_CreateRoom // IRequest
{
    int32 RoomType = 1;
    int32 MaxPlayers = 2;
}

message G2G_CreateRoomResponse // IResponse
{
    int32 ErrorCode = 1;
    int64 RoomId = 2;
}
```

**自动生成 Helper 扩展方法：**
```csharp
// 服务器间调用，框架自动生成
var response = await session.G2G_CreateRoom(roomType: 1, maxPlayers: 4);

// 客户端调用
var loginRes = await session.C2G_Login(account, password);
```

**消息处理器（自动注册，零配置）：**
```csharp
public class C2G_LoginHandler : Message<Session, C2G_Login, G2C_Login>
{
    protected override async FTask Run(Session session, C2G_Login request, G2C_Login response)
    {
        // 框架自动路由到这里，零反射开销
        response.Token = await AuthService.Login(request.Account);
    }
}
```

### 2. Roaming 路由系统 - 自动跨服务器转发

客户端只需连接 Gate 服务器，消息自动转发到目标服务器（Map/Battle/Chat...）

**协议定义：**
```protobuf
message C2M_EnterMap // IRoamingRequest
{
    int32 MapId = 1;
}

message M2C_EnterMap // IRoamingResponse
{
    int32 ErrorCode = 1;
    int64 SceneId = 2;
}
```

**客户端：**
```csharp
// Gate 服务器自动转发到 Map 服务器
var response = await session.C2M_EnterMap(mapId: 1001);
```

**服务端（运行在 Map 服务器）：**
```csharp
public class C2M_EnterMapHandler : Roaming<Session, C2M_EnterMap, M2C_EnterMap>
{
    protected override async FTask Run(Session session, C2M_EnterMap request, M2C_EnterMap response)
    {
        // 这里运行在 Map 服务器，Gate 已自动转发
        var scene = await CreateMapScene(request.MapId);
        response.SceneId = scene.Id;
    }
}
```

**核心价值：**
- ✅ 客户端无需知道目标服务器地址
- ✅ 零配置自动转发
- ✅ 开发体验与单服务器一致

### 3. 零反射 + Native AOT 支持

**传统框架：**
```csharp
// ❌ 运行时反射扫描（慢 + 不支持 AOT）
Assembly.GetTypes().Where(t => typeof(IMessageHandler).IsAssignableFrom(t))...
```

**Fantasy：**
```csharp
// ✅ 编译时自动生成注册代码（快 + AOT 友好）
// 无需任何手动注册，源生成器自动完成
public class C2G_LoginHandler : Message<Session, C2G_Login, G2C_Login>
{
    protected override async FTask Run(Session session, C2G_Login request, G2C_Login response)
    {
        // 框架自动路由，零反射开销
        response.Token = await AuthService.Login(request.Account);
    }
}
```

### 4. 跨服域事件系统 (SphereEvent)

轻松实现跨服公告、跨服排行榜、跨服 PVP：

```csharp
// 服务器 A 发布事件
await sphereEvent.PublishToRemoteSubscribers(new WorldBossDefeatedEvent
{
    BossId = 1001,
    KillerGuildId = 5201314
});

// 服务器 B/C/D... 自动接收
[SphereEvent]
public class WorldBossEventHandler : SphereEvent<WorldBossDefeatedEvent>
{
    protected override async FTask Run(Scene scene, WorldBossDefeatedEvent args)
    {
        // 所有订阅的服务器都会收到此事件
        await SendGlobalAnnouncement($"世界Boss已被击败！");
    }
}
```

### 5. ECS 架构 - 灵活的实体组件系统

```csharp
// 定义实体
public class Player : Entity
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// 添加组件（组合式设计）
player.AddComponent<BagComponent>();
player.AddComponent<EquipmentComponent>();
player.AddComponent<SkillComponent>();

// 系统自动执行（源生成器自动注册）
public class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        Log.Info($"玩家 {self.Name} 上线了!");
    }
}
```

### 6. 多协议支持

同一套代码，切换配置即可支持所有协议：

```csharp
// TCP - 稳定可靠
var session = await NetworkHelper.Connect("127.0.0.1:20000", NetworkProtocolType.TCP);

// KCP - 低延迟（实时对战首选）
var session = await NetworkHelper.Connect("127.0.0.1:20000", NetworkProtocolType.KCP);

// WebSocket - H5/WebGL
var session = await NetworkHelper.Connect("ws://127.0.0.1:20000", NetworkProtocolType.WebSocket);

// HTTP - RESTful API
var response = await httpClient.Get("/api/users/123");
```

---

## 📦 完整工具链

### Fantasy CLI 脚手架

一键生成项目结构：
```bash
# 安装
dotnet tool install -g Fantasy.Cli

# 创建项目
fantasy init -n MyGame

# 添加组件
fantasy add -t networkprotocol  # 协议定义
fantasy add -t nlog              # 日志组件
```

### 协议导出工具

从 `.proto` 文件生成 C# 代码 + 自动生成 Session 扩展方法：

```bash
# 定义协议
message C2G_Login // IRequest
{
    string Account = 1;
    string Password = 2;
}

# 自动生成
public static class NetworkProtocolHelper
{
    public static async FTask<G2C_Login> C2G_Login(
        this Session session, string account, string password)
    {
        // ... 自动生成的完整实现
    }
}
```

### 配置表导出

Excel → JSON/Binary，自动生成加载代码。

---

## 🔧 环境要求

| 组件 | 版本 | 说明 |
|------|------|------|
| **.NET SDK** | 8.0+ | [下载地址](https://dotnet.microsoft.com/download) |
| **Unity** | 2022.3.62+ | 客户端开发（可选） |
| **IDE** | VS 2022 / Rider / VS Code | 推荐 Rider 或 VS 2022 |
| **MongoDB** | 4.0+ | 数据库（可选） |

---

## 🖥️ 平台支持

| 平台 | 支持 | 说明 |
|------|------|------|
| 🖥️ Windows Server | ✅ | 游戏服务器首选 |
| 🐧 Linux Server | ✅ | Docker/K8s 部署 |
| 🍎 macOS | ✅ | 开发调试 |
| 🎮 Unity (全平台) | ✅ | Win/Mac/iOS/Android |
| 🌐 Unity WebGL | ✅ | H5 小游戏 |

---

## 📚 文档与教程

- 📖 **[官方文档](https://github.com/qq362946/Fantasy/tree/main/Docs)** - 完整的使用指南
- 🚀 **[快速开始 - 服务器](https://github.com/qq362946/Fantasy/blob/main/Docs/00-GettingStarted/01-QuickStart-Server.md)** - 5分钟上手
- 📱 **[快速开始 - Unity](https://github.com/qq362946/Fantasy/blob/main/Docs/00-GettingStarted/02-QuickStart-Unity.md)** - Unity 客户端集成
- 🎬 **[B站视频教程](https://space.bilibili.com/382126312)** - 视频讲解
- 💡 **[示例项目](https://github.com/qq362946/Fantasy/tree/main/Examples)** - 可运行的完整示例

---

## 💬 社区与支持

- **QQ 讨论群**: **569888673**
- **联系邮箱**: 362946@qq.com
- **GitHub Issues**: [提交问题](https://github.com/qq362946/Fantasy/issues)
- **官方网站**: [www.code-fantasy.com](https://www.code-fantasy.com/)
- **B站**: [@Fantasy框架](https://space.bilibili.com/382126312)

---

## 🎯 为什么选择 Fantasy？

| 对比项 | Fantasy | 传统框架 |
|-------|---------|---------|
| **网络消息** | 1 行代码 | 50+ 行代码 |
| **性能** | 零反射 + AOT | 大量反射 |
| **分布式** | 内置 Roaming/SphereEvent | 需要自己实现 |
| **协议切换** | 配置文件一键切换 | 需要重写代码 |
| **学习曲线** | 脚手架 + 文档 + 视频 | 文档不全 |
| **生产就绪** | ✅ 完整工具链 | ⚠️ 需要自己搭建 |

---

## 🤝 优质开源项目推荐

- [ET Framework](https://github.com/egametang/ET) - Fantasy 的设计灵感来源
- [TEngine](https://github.com/ALEXTANGXIAO/TEngine) - Unity 框架解决方案
- [Legends-Of-Heroes](https://github.com/FlameskyDexive/Legends-Of-Heroes) - 基于 ET 的完整游戏

---

## 📄 开源协议

本项目采用 [MIT License](https://github.com/qq362946/Fantasy/blob/main/LICENSE) 开源协议。

---

## 🙏 贡献者

感谢所有为 Fantasy 做出贡献的开发者！

[![Contributors](https://contrib.rocks/image?repo=qq362946/Fantasy)](https://github.com/qq362946/Fantasy/graphs/contributors)

---

**Built with ❤️ by Fantasy Team | Made for Game Developers**

🎉 **如果 Fantasy 对你有帮助，请给个 Star ⭐**
