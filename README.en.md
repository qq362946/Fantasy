<div align="center">

![Fantasy](https://socialify.git.ci/qq362946/Fantasy/image?custom_description=%F0%9F%8E%AE+High-performance+distributed+game+server+framework+for+large-scale+multiplayer+online+games%0A%F0%9F%9A%80+Build+your+game+empire+with+C%23+%7C+From+indie+games+to+large-scale+MMOs%0A%E2%9A%A1+Zero+Reflection+%7C+%F0%9F%9A%80+Native+AOT+%7C+%F0%9F%8C%90+Multi-Protocol+%7C+%F0%9F%94%A5+Distributed&description=1&font=Inter&forks=1&issues=1&logo=data%3Aimage%2Fpng%3Bbase64%2CiVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII%3D&name=1&pattern=Signal&pulls=1&stargazers=1&theme=Auto)

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-brightgreen.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Unity](https://img.shields.io/badge/Unity-2022.3.62+-black.svg)](https://unity.com/)
[![Stars](https://img.shields.io/github/stars/qq362946/Fantasy?style=social)](https://github.com/qq362946/Fantasy/stargazers)

**[简体中文](README.md)** | **English**

**[📖 Documentation (中文)](Docs/README.md)** | **[🚀 Quick Start (中文)](Docs/README.md)** | **[💬 QQ Group: 569888673](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=yourqrcode)**
</div>

---

# ⭐ What is Fantasy?

Fantasy is a **zero-reflection, high-performance C# game server framework** built for large-scale multiplayer online games.

## ✨ Highlights

⚡ **Zero-Reflection** · 🚀 **Native AOT** · 🧭 **Control Center & Service Discovery**  
🌐 **Multi-Protocol** · 🔥 **Distributed Architecture** · 🎮 **ECS Design** · 🤖 **AI-Assisted Development**

## 🤖 AI-Assisted Development

Fantasy provides a dedicated `fantasy-net` Skill that works with AI agents such as Claude, Codex, OpenCode, and OpenClaw.

It can help you:

- Learn the Fantasy framework and its core mechanisms
- Write Entity / Component / System code, protocols, handlers, configuration, and database code
- Design Roaming, Address, SphereEvent, HTTP, Unity integration, and related features
- Review Fantasy project code against framework conventions and quickly locate problems

This makes Fantasy more than a framework that can be used with AI. It already includes AI-oriented knowledge organization and development support, which lowers the learning curve and makes it well suited for vibe coding.

You can ask an AI agent questions like:

- `Create a Player entity and its AwakeSystem in Fantasy`
- `Write an EventAwaiter-based player confirmation flow`
- `Check whether this Roaming code has problems and follows Fantasy conventions`
- `Write a Fantasy.config that includes Gate, Map, MapControl, and MongoDB`

👉 **[AI Guide and Fantasy Skill Usage (中文)](Docs/05-AI/01-FantasySkill.md)**
👉 **[AI Skill Installation and Feature Overview (中文)](Skills/README.md)**

## Use Cases

- 🏰 **MMORPG** - Distributed architecture, cross-server communication, entity addressing
- ⚔️ **Real-time battle games** - Low-latency KCP protocol and high-performance ECS architecture
- 🎲 **Turn-based / card games** - Reliable TCP/WebSocket communication and data persistence
- 🌍 **Open-world games** - Scene management and hierarchical entity systems
- 🏪 **H5 / mini games** - WebSocket support and Unity WebGL compatibility
- 🎰 **High-concurrency games** - Distributed deployment and object-pool optimization

## Multi-Protocol Support

- 🔌 **TCP** - Stable and reliable, ideal for internal server-to-server communication
- ⚡ **KCP** - Low-latency reliable UDP transport, suitable for real-time battle games
- 🌍 **WebSocket** - Native support for H5 / mini games and one-click Unity WebGL publishing
- 🌐 **HTTP** - Integrates cleanly with REST APIs, web admin panels, and GM tools

## Core Advantages

### 📡 Network Communication

```csharp
// ❌ Traditional frameworks may require 50+ lines of code
var message = new C2G_LoginRequest();
message.Account = "player123";
message.Password = "password";
var bytes = ProtoBuf.Serialize(message);
var response = await session.Call(opCode, bytes);
// ... plus manual callbacks, timeouts, and error handling

// ✅ Fantasy needs only one line of code, with extensions generated automatically
var response = await session.C2G_Login("player123", "password");
```

### ⚡ Zero Reflection + Native AOT Performance

```csharp
// Traditional frameworks: runtime reflection scanning and registration
// This is slower and does not work well with AOT
// Assembly.GetTypes().Where(t => typeof(IMessageHandler).IsAssignableFrom(t))...

// Fantasy: registration code is generated at compile time
// Fast, AOT-friendly, and requires no manual registration
public class C2G_LoginHandler : Message<Session, C2G_Login, G2C_Login>
{
    protected override async FTask Run(Session session, C2G_Login request, G2C_Login response)
    {
        // The framework automatically routes here with zero reflection overhead
        response.Token = await AuthService.Login(request.Account);
    }
}
```

### 🧭 Built-in Control Center and Service Discovery

Replace fixed server lists and modulo-based routing with a ready-to-use management UI and high-performance service discovery. Server topology can scale dynamically as your game grows.

- ✅ **Visual topology management** - Manage Machines, Processes, Worlds, Databases, and Scenes with local persistence
- ✅ **Automatic service lifecycle** - Scene registration, batched heartbeats, graceful offline reporting, and lease-based expiry
- ✅ **Multi-environment isolation** - First-class Namespace, WorldGroup, and World-scoped discovery
- ✅ **Stable load routing** - Random selection and Rendezvous Hash with minimal key movement when nodes change
- ✅ **Fast routing path** - Hot-path queries use local snapshots, and discovered Addresses work directly with `Scene.Send` and `Scene.Call`

```csharp
using NetServiceDiscovery = Fantasy.ServiceDiscovery;

var mapAddress = await NetServiceDiscovery.DiscoverAddressByHashAsync(
    SceneType.Map,
    routingKey: accountId,
    worldId: worldId);

if (mapAddress == 0)
    throw new InvalidOperationException("No online Map scene was discovered.");

var response = await scene.Call(mapAddress, new G2A_TestRequest());
```

👉 **[Service Discovery Guide (Chinese)](Docs/04-Advanced/NetworkDevelopment/11-ServiceDiscovery.md)**

### 🌉 Roaming Routing System

```csharp
// Define a Roaming message in a .proto file
message C2M_EnterMap // IRoamingRequest
{
    int32 MapId = 1;
}

message M2C_EnterMap // IRoamingResponse
{
    int32 ErrorCode = 1;
    int64 SceneId = 2;
}

// The Gate server automatically forwards the request to the target Map server
// The client only connects to Gate; the framework handles the rest
var response = await session.C2M_EnterMap(1001);

// The server handler runs on the Map server, not on the Gate server
public class C2M_EnterMapHandler : Roaming<Session, C2M_EnterMap, M2C_EnterMap>
{
    protected override async FTask Run(Session session, C2M_EnterMap request, M2C_EnterMap response)
    {
        // This runs on the Map server because Gate has already forwarded it
        var scene = await CreateMapScene(request.MapId);
        response.SceneId = scene.Id;
        response.ErrorCode = 0;
    }
}
```

**Core value of Roaming**

- ✅ **Automatic server routing** - Clients do not need to know target server addresses
- ✅ **Zero-configuration forwarding** - Gate forwards messages to the correct server by message type
- ✅ **Transparent distributed development** - Development feels the same as a single-server setup
- ✅ **Flexible server expansion** - Add new game server types easily

### 🌐 Easy Cross-Server Communication

```csharp
// Server A publishes a cross-server event
await sphereEvent.PublishToRemoteSubscribers(new WorldBossDefeatedEvent
{
    BossId = 1001,
    KillerGuildId = 5201314
});

// Servers B/C/D... receive and process it automatically
// Cross-server announcements, leaderboards, and PvP become straightforward
```

### 🎮 Multiple Protocols, One Codebase

```csharp
// The same message definitions support TCP/KCP/WebSocket/HTTP
// No code changes are required; switch protocols through configuration
var session = await NetworkHelper.Connect("127.0.0.1:20000", NetworkProtocolType.TCP);
// Or NetworkProtocolType.KCP / WebSocket / HTTP
```

### 🚀 ECS Architecture for Fast Development

```csharp
// Define an entity
public class Player : Entity
{
    public string Name { get; set; }
    public int Level { get; set; }
}

// Add components with a flexible composition model
player.AddComponent<BagComponent>();
player.AddComponent<EquipmentComponent>();
player.AddComponent<SkillComponent>();

// Systems are registered automatically by the source generator
public class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        Log.Info($"Player {self.Name} is online!");
    }
}
```

---

## Platform Support

| Platform | Status | Notes |
|------|-------|------------------|
| 🖥️ **Windows Server** | ✅ Fully supported | Recommended game server platform |
| 🐧 **Linux Server** | ✅ Fully supported | Docker/K8s containerized deployment |
| 🍎 **macOS** | ✅ Fully supported | Friendly for development and debugging |
| 🎮 **Unity (Win/Mac/iOS/Android)** | ✅ Fully supported | 2022.3.62+ |
| 🌐 **Unity WebGL (H5)** | ✅ Fully supported | WebSocket protocol |
| 🎯 **Godot** | ❌ Not supported yet | .NET version of Godot |
| 🖼️ **WinForms / WPF** | ❌ Not supported yet | GM tools and server monitoring panels |
| 📟 **Console** | ❌ Not supported yet | Indie games, bots, and load-testing tools |

---

## 📋 Requirements

| Component | Version | Notes |
|------|---------------------------|------|
| **.NET SDK** | 8.0+ | [Download](https://dotnet.microsoft.com/download) |
| **Unity** | 2022.3.62+ | Optional, for client development |
| **IDE** | VS 2022 / Rider / VS Code | Rider or VS 2022 recommended |
| **MongoDB** | 4.0+ | Optional database; memory mode can run without it |

---

## 🛠️ Fantasy CLI Scaffolding Tool

Fantasy CLI is the official project scaffolding and management tool. It helps you create and manage Fantasy projects quickly.

### Installation

```bash
dotnet tool install -g Fantasy.Cli
```

**⚠️ Notes for macOS/Linux users**

After installation on macOS or Linux, if the `fantasy` command is not available directly, add the .NET tools path to your PATH environment variable:

**macOS (zsh - default shell):**

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

**Verify installation:**

```bash
fantasy --version
```

### Quick Usage

**Create a new project:**

```bash
fantasy init                    # Create a project interactively
fantasy init -n MyGameServer    # Create a project with a project name
```

**Add framework components:**

```bash
fantasy add                     # Select components interactively
fantasy add -t protocolexporttool  # Add the protocol export tool
fantasy add -t networkprotocol     # Add network protocol support
fantasy add -t nlog                # Add NLog logging
```

For more details, see the [Fantasy.Cli documentation (中文)](Fantasy.Packages/Fantasy.Cil/README.md).

---

## 💬 Community and Support

- **QQ discussion group**: **569888673** ([join link](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=yourqrcode))
- **Email**: 362946@qq.com
- **GitHub Issues**: [Submit an issue](https://github.com/qq362946/Fantasy/issues)
- **Official website**: [www.code-fantasy.com](https://www.code-fantasy.com/)
- **Bilibili video tutorials**: [channel homepage](https://space.bilibili.com/382126312)

---

## 🙏 Thanks to All Fantasy Contributors

[![Contributors](https://contrib.rocks/image?repo=qq362946/Fantasy)](https://github.com/qq362946/Fantasy/graphs/contributors)

---

## 🤝 Recommended Open-Source Projects

- [ET Framework](https://github.com/egametang/ET) - A mature distributed game server framework and a source of design inspiration for Fantasy
- [TEngine](https://github.com/ALEXTANGXIAO/TEngine) - A beginner-friendly, cross-platform Unity framework solution
- [Legends-Of-Heroes](https://github.com/FlameskyDexive/Legends-Of-Heroes) - A League of Legends-style Agar.io game based on ET Framework

---

<div align="center">

### 🎉 If Fantasy helps you, please give it a Star ⭐

### Help more developers discover this project!

**Built with ❤️ by Fantasy Team | Made for Game Developers**

[⬆ Back to top](#what-is-fantasy)

</div>
