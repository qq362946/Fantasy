<div align="center">

![Fantasy](https://socialify.git.ci/qq362946/Fantasy/image?custom_description=%F0%9F%8E%AE+%E4%B8%93%E4%B8%BA%E5%A4%A7%E5%9E%8B%E5%A4%9A%E4%BA%BA%E5%9C%A8%E7%BA%BF%E6%B8%B8%E6%88%8F%E6%89%93%E9%80%A0%E7%9A%84%E9%AB%98%E6%80%A7%E8%83%BD%E5%88%86%E5%B8%83%E5%BC%8F%E6%9C%8D%E5%8A%A1%E5%99%A8%E6%A1%86%E6%9E%B6+%0A%F0%9F%9A%80+%E7%94%A8C%23+%E6%9E%84%E5%BB%BA%E4%BD%A0%E7%9A%84%E6%B8%B8%E6%88%8F%E5%B8%9D%E5%9B%BD+%7C+%E4%BB%8E%E5%B0%8F%E5%9E%8B%E7%8B%AC%E7%AB%8B%E6%B8%B8%E6%88%8F%E5%88%B0%E5%A4%9A%E4%BA%BA%E5%9C%A8%E7%BA%BF%E5%A4%A7%E5%9E%8B%E6%B8%B8%E6%88%8F%0A%E2%9A%A1+%E9%9B%B6%E5%8F%8D%E5%B0%84+%7C+%F0%9F%9A%80+Native+AOT+%7C+%F0%9F%8C%90+%E5%A4%9A%E5%8D%8F%E8%AE%AE+%7C+%F0%9F%94%A5+%E5%88%86%E5%B8%83%E5%BC%8F&description=1&font=Inter&forks=1&issues=1&logo=data%3Aimage%2Fpng%3Bbase64%2CiVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII%3D&name=1&pattern=Signal&pulls=1&stargazers=1&theme=Auto)

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-brightgreen.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Unity](https://img.shields.io/badge/Unity-2022.3.62+-black.svg)](https://unity.com/)
[![Stars](https://img.shields.io/github/stars/qq362946/Fantasy?style=social)](https://github.com/qq362946/Fantasy/stargazers)

**[📖 官方文档](Docs/README.md)** | **[🚀 快速开始](Docs/README.md)** | **[💬 QQ 群: 569888673](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=yourqrcode)**
</div>

---

# ⭐ Fantasy 是什么？

Fantasy 是一个**零反射、高性能的 C# 游戏服务器框架**，专为大型多人在线游戏打造。

**核心特点：** ⚡ 零反射架构 | 🚀 Native AOT 支持 | 🌐 多协议支持 | 🔥 分布式架构 | 🎮 ECS 设计

## 适用场景

- 🏰 **MMORPG** - 分布式架构、跨服通信、实体寻址系统
- ⚔️ **实时对战游戏** - KCP 低延迟协议、高性能 ECS 架构
- 🎲 **回合制/卡牌游戏** - 可靠的 TCP/WebSocket 通信、数据持久化
- 🌍 **开放世界游戏** - 场景管理、实体层级系统
- 🏪 **H5/小游戏** - WebSocket 协议支持、Unity WebGL 兼容
- 🎰 **高并发游戏** - 分布式部署、对象池优化

## 多协议支持

- 🔌 **TCP** - 稳定可靠，服务器间内网通信首选
- ⚡ **KCP** - 低延迟 UDP 可靠传输协议，适合实时对战游戏
- 🌍 **WebSocket** - 原生支持 H5/小游戏，Unity WebGL 一键发布
- 🌐 **HTTP** - RESTful API、Web 后台管理、GM 工具完美集成

## 核心优势

### 📡 网络通信

```csharp
// ❌ 传统框架需要 50+ 行代码
var message = new C2G_LoginRequest();
message.Account = "player123";
message.Password = "password";
var bytes = ProtoBuf.Serialize(message);
var response = await session.Call(opCode, bytes);
// ... 还要手动处理回调、超时、错误处理

// ✅ Fantasy 只需 1 行代码(自动生成扩展方法)
var response = await session.C2G_Login("player123", "password");
```

### ⚡ 零反射 + Native AOT 极致性能

```csharp
// 传统框架: 运行时反射扫描注册(慢 + 不支持 AOT)
// Assembly.GetTypes().Where(t => typeof(IMessageHandler).IsAssignableFrom(t))...

// Fantasy: 编译时自动生成注册代码(快 + AOT 友好)
// 无需任何手动注册,源生成器自动完成一切
public class C2G_LoginHandler : Message<Session, C2G_Login, G2C_Login>
{
    protected override async FTask Run(Session session, C2G_Login request, G2C_Login response)
    {
        // 框架自动路由到这里,零反射开销
        response.Token = await AuthService.Login(request.Account);
    }
}
```
### 🌉 Roaming 路由系统

```csharp
// 定义 Roaming 消息(.proto 文件)
message C2M_EnterMap // IRoamingRequest
{
    int32 MapId = 1;
}

message M2C_EnterMap // IRoamingResponse
{
    int32 ErrorCode = 1;
    int64 SceneId = 2;
}

// Gate 服务器自动转发到目标 Map 服务器,无需任何配置
// 客户端只需连接 Gate,剩下的交给框架处理
var response = await session.C2M_EnterMap(1001);

// 服务端在 Map 服务器处理(不是 Gate 服务器!)
public class C2M_EnterMapHandler : Roaming<Session, C2M_EnterMap, M2C_EnterMap>
{
    protected override async FTask Run(Session session, C2M_EnterMap request, M2C_EnterMap response)
    {
        // 这里运行在 Map 服务器上,Gate 已自动转发
        var scene = await CreateMapScene(request.MapId);
        response.SceneId = scene.Id;
        response.ErrorCode = 0;
    }
}
```

**Roaming 核心价值**
- ✅ **自动服务器路由** - 客户端无需知道目标服务器地址
- ✅ **零配置转发** - Gate 根据消息类型自动转发到正确的服务器
- ✅ **透明的分布式** - 开发体验与单服务器一致
- ✅ **灵活的服务器扩展** - 轻松添加新的游戏服务器类型

### 🌐 跨服通信轻松实现

```csharp
// 服务器 A 发布跨服事件
await sphereEvent.PublishToRemoteSubscribers(new WorldBossDefeatedEvent
{
    BossId = 1001,
    KillerGuildId = 5201314
});

// 服务器 B/C/D... 自动接收并处理
// 跨服公告、跨服排行榜、跨服 PVP 轻松搞定
```

### 🎮 多协议支持,一套代码多场景

```csharp
// 同一个消息定义,支持 TCP/KCP/WebSocket/HTTP 全协议
// 无需修改任何代码,只需配置文件切换协议类型
var session = await NetworkHelper.Connect("127.0.0.1:20000", NetworkProtocolType.TCP);
// 或 NetworkProtocolType.KCP / WebSocket / HTTP
```

### 🚀 ECS 架构,开发效率拉满

```csharp
// 定义实体
public class Player : Entity
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// 添加组件(组合式设计,灵活扩展)
player.AddComponent<BagComponent>();
player.AddComponent<EquipmentComponent>();
player.AddComponent<SkillComponent>();

// 系统自动执行(源生成器自动注册,零配置)
public class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        Log.Info($"玩家 {self.Name} 上线了!");
    }
}
```
---

## 平台支持

| 平台 | 支持状态  | 说明               |
|------|-------|------------------|
| 🖥️ **Windows Server** | ✅ 完全支持 | 游戏服务器首选平台        |
| 🐧 **Linux Server** | ✅ 完全支持 | Docker/K8s 容器化部署 |
| 🍎 **macOS** | ✅ 完全支持 | 开发调试友好           |
| 🎮 **Unity (Win/Mac/iOS/Android)** | ✅ 完全支持 | 2022.3.62+       |
| 🌐 **Unity WebGL (H5)** | ✅ 完全支持 | WebSocket 协议     |
| 🎯 **Godot** | ❎ 暂未支持 | .NET 版本 Godot    |
| 🖼️ **WinForms / WPF** | ❎ 暂未支持 | GM 工具、服务器监控面板    |
| 📟 **Console** | ❎ 暂未支持 | 独立游戏、机器人、压测工具    |

---

## 📋 环境要求

| 组件 | 版本要求                      | 说明 |
|------|---------------------------|------|
| **.NET SDK** | 8.0+                      | [下载地址](https://dotnet.microsoft.com/download) |
| **Unity** | 2022.3.62+                | 客户端开发（可选） |
| **IDE** | VS 2022 / Rider / VS Code | 推荐 Rider 或 VS 2022 |
| **MongoDB** | 4.0+                      | 数据库（可选，使用内存模式可不装） |

---

## 🛠️ Fantasy CLI 脚手架工具

Fantasy CLI 是官方提供的项目脚手架和管理工具，帮助你快速创建和管理 Fantasy 项目。

### 安装

```bash
dotnet tool install -g Fantasy.Cli
```

**⚠️ macOS/Linux 用户注意事项**

在 macOS 或 Linux 上安装后，如果无法直接使用 `fantasy` 命令，需要将 .NET tools 路径添加到 PATH 环境变量：

**macOS (zsh - 默认 Shell):**
```bash
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc
source ~/.zshrc
```

**macOS (bash):**
```bash
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bash_profile
source ~/.bash_profile
```

**Linux (bash):**
```bash
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc
```

**验证安装:**
```bash
fantasy --version
```

### 快速使用

**创建新项目:**
```bash
fantasy init                    # 交互式创建项目
fantasy init -n MyGameServer    # 使用项目名快速创建
```

**添加框架组件:**
```bash
fantasy add                     # 交互式选择组件
fantasy add -t protocolexporttool  # 添加协议导出工具
fantasy add -t networkprotocol     # 添加网络协议
fantasy add -t nlog                # 添加 NLog 日志
```

更多详细信息，请查看 [Fantasy.Cli 文档](Fantasy.Packages/Fantasy.Cil/README.md)

---

## 💬 社区与支持

- **QQ 讨论群**: **569888673** （点击加群：[链接](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=yourqrcode)）
- **联系邮箱**: 362946@qq.com
- **GitHub Issues**: [提交问题](https://github.com/qq362946/Fantasy/issues)
- **官方网站**: [www.code-fantasy.com](https://www.code-fantasy.com/)
- **B站视频教程**: [空间主页](https://space.bilibili.com/382126312)

---

## 🙏 感谢所有为 Fantasy 做出贡献的开发者

[![Contributors](https://contrib.rocks/image?repo=qq362946/Fantasy)](https://github.com/qq362946/Fantasy/graphs/contributors)

---

## 🤝 优质开源项目推荐

- [ET Framework](https://github.com/egametang/ET) - Fantasy 的设计灵感来源,完善的分布式游戏服务器框架
- [TEngine](https://github.com/ALEXTANGXIAO/TEngine) - Unity框架解决方案,新手友好的全平台开发框架
- [Legends-Of-Heroes](https://github.com/FlameskyDexive/Legends-Of-Heroes) - LOL风格的球球大作战,基于ET框架的完整游戏项目

---

<div align="center">

### 🎉 如果 Fantasy 对你有帮助，请给个 Star ⭐

### 让更多人发现这个项目！

**Built with ❤️ by Fantasy Team | Made for Game Developers**

[⬆ 回到顶部](#fantasy-framework)

</div>
