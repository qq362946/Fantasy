# 网络协议导出工具使用指南

本文档详细介绍如何使用 Fantasy Framework 的网络协议导出工具,将 `.proto` 协议定义文件自动生成为 C# 代码。

---

## 目录

- [工具概述](#工具概述)
  - [工具功能](#工具功能)
- [获取导出工具](#获取导出工具)
  - [方式1: 使用 Fantasy CLI](#方式1-使用-fantasy-cli)
  - [方式2: 使用已编译的工具(推荐)](#方式2-使用已编译的工具推荐)
  - [方式3: 从源码编译](#方式3-从源码编译)
- [配置导出工具](#配置导出工具)
  - [ExporterSettings.json 配置说明](#exportersettingsjson-配置说明)
  - [自定义序列化器配置](#自定义序列化器配置)
- [使用导出工具](#使用导出工具)
  - [交互式运行(推荐)](#交互式运行推荐)
  - [命令行参数运行](#命令行参数运行)
- [生成的代码结构](#生成的代码结构)
  - [协议类代码](#协议类代码)
  - [OpCode 枚举](#opcode-枚举)
  - [NetworkProtocolHelper 扩展方法](#networkprotocolhelper-扩展方法)
- [自定义代码模板](#自定义代码模板)
  - [模板文件位置](#模板文件位置)
  - [模板占位符说明](#模板占位符说明)
  - [自定义模板示例](#自定义模板示例)
- [错误检测与验证](#错误检测与验证)
  - [格式验证](#格式验证)
  - [常见错误与解决方法](#常见错误与解决方法)
- [最佳实践](#最佳实践)
  - [团队协作](#1-团队协作)
  - [版本控制](#2-版本控制)
  - [持续集成](#3-持续集成)
- [相关文档](#相关文档)

---

## 工具概述

### 工具功能

`Fantasy.Tools.NetworkProtocol` 是一个命令行工具,用于:

1. **解析 .proto 协议文件**: 读取 `NetworkProtocol/` 目录下的所有协议定义
2. **生成 C# 代码**: 自动生成协议类、OpCode 枚举、Helper 扩展方法
3. **支持多种序列化**: ProtoBuf、MemoryPack、Bson 序列化方式
4. **格式验证**: 检测协议定义中的错误和冲突
5. **增量更新**: 通过 OpCode.Cache 确保协议 ID 稳定性

---

## 获取导出工具

### 方式1: 使用 Fantasy CLI

Fantasy CLI 提供了便捷的命令来安装和管理协议导出工具。

**前提条件:**

首先需要安装 Fantasy CLI 工具:

```bash
# 安装 Fantasy CLI
dotnet tool install -g Fantasy.Cli

# 验证安装
fantasy --version
```

> **⚠️ macOS/Linux 用户注意：**
>
> 如果安装后无法直接使用 `fantasy` 命令，需要配置 PATH 环境变量。
>
> **详细配置步骤请查看：** [Fantasy CLI 完整文档](../../Fantasy.Packages/Fantasy.Cil/README.md)（查看"安装"章节）

**安装协议导出工具:**

```bash
# 使用 Fantasy CLI 安装协议导出工具
fantasy add -t protocolexporttool
```

**说明:**
- 此命令会自动下载并配置协议导出工具到您的项目中
- 工具会被安装到 `Tools/Exporter/NetworkProtocol/` 目录

**安装后使用:**

安装完成后,使用运行工具:

```bash
# 进入工具目录
cd Tools/Exporter/NetworkProtocol

# Windows 运行
Run.bat

# Unix/Mac 运行
./Run.sh
```

### 方式2: 使用已编译的工具(推荐)

Fantasy Framework 提供了预编译的导出工具,可直接使用。

**工具位置:**
```
/Tools/Exporter/NetworkProtocol/
```

**文件清单:**
```
NetworkProtocol/
├── Fantasy.Tools.NetworkProtocol.dll     # 主程序
├── Fantasy.Tools.NetworkProtocol         # Unix/Mac 可执行文件
├── ExporterSettings.json                 # 配置文件
├── NetworkProtocolTemplate.txt           # 代码生成模板
├── Run.bat                               # Windows 运行脚本
└── Run.sh                                # Unix/Mac 运行脚本
```

**运行要求:**
- .NET 8.0 或更高版本的运行时

**检查 .NET 版本:**
```bash
dotnet --version
```

### 方式3: 从源码编译

如果需要修改工具或从源码构建,可以从源码编译。

**源码位置:**
```
/Tools/SourceCode/Fantasy.Tools.NetworkProtocol/
```

**编译命令:**

```bash
# 编译工具
dotnet build Tools/SourceCode/Fantasy.Tools.NetworkProtocol/Fantasy.Tools.NetworkProtocol.csproj

# 或使用 Release 配置
dotnet build Tools/SourceCode/Fantasy.Tools.NetworkProtocol/Fantasy.Tools.NetworkProtocol.csproj --configuration Release

# 编译完成后,输出文件在 bin/Debug/net8.0/ 或 bin/Release/net8.0/ 目录
```

**运行编译后的工具:**
```bash
dotnet run --project Tools/SourceCode/Fantasy.Tools.NetworkProtocol/Fantasy.Tools.NetworkProtocol.csproj
```

---

## 配置导出工具

### ExporterSettings.json 配置说明

在使用导出工具前,需要配置 `ExporterSettings.json` 文件,该文件定义了协议文件的位置和生成代码的输出路径。

**配置文件位置:**
```
/Tools/Exporter/NetworkProtocol/ExporterSettings.json
```

**配置文件结构:**

```json
{
    "Export": {
        "NetworkProtocolDirectory": {
            "Value": "../../../Examples/Config/NetworkProtocol/",
            "Comment": "ProtoBuf文件所在的文件夹位置"
        },
        "NetworkProtocolServerDirectory": {
            "Value": "../../../Examples/Server/Entity/Generate/NetworkProtocol/",
            "Comment": "ProtoBuf生成到服务端的文件夹位置"
        },
        "NetworkProtocolClientDirectory": {
            "Value": "../../../Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol/",
            "Comment": "ProtoBuf生成到客户端的文件夹位置"
        },
        "Serializes": {
            "Value": [],
            "Comment": "自定义序列化器"
        }
    }
}
```

**配置项说明:**

| 配置项 | 说明 | 必填 | 示例值 |
|-------|------|------|--------|
| `NetworkProtocolDirectory` | `.proto` 协议文件所在目录(包含 Inner/ 和 Outer/ 文件夹) | ✅ | `"../../../Examples/Config/NetworkProtocol/"` |
| `NetworkProtocolServerDirectory` | 服务端 C# 代码输出目录 | ✅ | `"../../../Examples/Server/Entity/Generate/NetworkProtocol/"` |
| `NetworkProtocolClientDirectory` | 客户端 C# 代码输出目录 | ✅ | `"../../../Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol/"` |
| `Serializes` | 自定义序列化器配置 | ❌ | 见下方说明 |

**路径说明:**
- 路径可以使用相对路径或绝对路径
- 相对路径是相对于 `ExporterSettings.json` 文件所在目录
- Windows 系统使用 `\` 或 `/` 都可以,Unix/Mac 使用 `/`

**配置示例(绝对路径):**

```json
{
    "Export": {
        "NetworkProtocolDirectory": {
            "Value": "/Users/yourname/Projects/MyGame/Config/NetworkProtocol/"
        },
        "NetworkProtocolServerDirectory": {
            "Value": "/Users/yourname/Projects/MyGame/Server/Generated/NetworkProtocol/"
        },
        "NetworkProtocolClientDirectory": {
            "Value": "/Users/yourname/Projects/MyGame/Client/Unity/Assets/Scripts/Generated/NetworkProtocol/"
        }
    }
}
```

### 自定义序列化器配置

如果您使用了自定义序列化器(如 MemoryPack),可以在 `Serializes` 中配置。

**MemoryPack 配置示例:**

```json
{
    "Export": {
        "Serializes": {
            "Value": [
                {
                    "KeyIndex": 0,
                    "NameSpace": "MemoryPack",
                    "SerializeName": "MemoryPack",
                    "Attribute": "\t[MemoryPackable]",
                    "Ignore": "\t\t[MemoryPackIgnore]",
                    "Member": "MemoryPackOrder"
                }
            ]
        }
    }
}
```

**参数说明:**

| 参数 | 说明 | 示例 |
|-----|------|------|
| `KeyIndex` | 序列化器索引(从 0 开始) | `0` |
| `NameSpace` | 命名空间 | `"MemoryPack"` |
| `SerializeName` | 序列化器名称 | `"MemoryPack"` |
| `Attribute` | 类特性标记 | `"\t[MemoryPackable]"` |
| `Ignore` | 忽略字段特性 | `"\t\t[MemoryPackIgnore]"` |
| `Member` | 字段顺序特性 | `"MemoryPackOrder"` |

**在协议中使用自定义序列化:**

```protobuf
// Protocol MemoryPack
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
}
```

---

## 使用导出工具

### 交互式运行(推荐)

最简单的使用方式是运行提供的脚本,工具会提示您选择导出目标。

**Windows 系统:**

```bash
# 进入工具目录
cd Tools/Exporter/NetworkProtocol

# 运行批处理脚本
Run.bat
```

**Unix/Mac 系统:**

```bash
# 进入工具目录
cd Tools/Exporter/NetworkProtocol

# 添加执行权限(首次运行需要)
chmod +x Run.sh

# 运行 Shell 脚本
./Run.sh
```

**交互界面:**

```
Please select an option:
1. Client
2. Server
3. All

Please select an option:
```

**选项说明:**

| 选项 | 说明 | 生成内容 |
|-----|------|---------|
| `1. Client` | 仅生成客户端代码 | 生成到 `NetworkProtocolClientDirectory` |
| `2. Server` | 仅生成服务端代码 | 生成到 `NetworkProtocolServerDirectory` |
| `3. All` | 同时生成客户端和服务端代码 | 生成到两个目录 |

### 命令行参数运行

您也可以直接使用命令行参数运行工具,适合自动化脚本和 CI/CD 集成。

**命令行参数:**

| 参数 | 简写 | 说明 | 可选值 | 默认值 |
|-----|------|------|--------|--------|
| `--ExportPlatform` | `-p` | 导出目标平台 | `1`(Client), `2`(Server), `3`(All) | `None` |
| `--Folder` | `-f` | ExporterSettings.json 文件所在目录 | 目录路径 | 当前目录 |

**使用示例:**

```bash
# 生成所有代码(客户端和服务端)
dotnet Fantasy.Tools.NetworkProtocol.dll --p 3

# 仅生成客户端代码
dotnet Fantasy.Tools.NetworkProtocol.dll --p 1

# 仅生成服务端代码
dotnet Fantasy.Tools.NetworkProtocol.dll --p 2

# 指定配置文件目录
dotnet Fantasy.Tools.NetworkProtocol.dll --p 3 --f /path/to/config/folder

# 使用简写参数
dotnet Fantasy.Tools.NetworkProtocol.dll -p 3 -f /path/to/config/folder
```
---

## 生成的代码结构

导出工具会生成以下几类代码文件:

### 协议类代码

**文件命名规则:** `{协议文件名}.cs`

**生成位置:**
- 服务端: `NetworkProtocolServerDirectory/Inner/` 和 `NetworkProtocolServerDirectory/Outer/`
- 客户端: `NetworkProtocolClientDirectory/Outer/`(仅外网协议)

**生成的类结构:**

```csharp
// 示例:生成的协议类
using ProtoBuf;
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS8603

namespace Fantasy.Network.Message
{
    /// <summary>
    /// 客户端发送给服务器通知玩家位置变化
    /// </summary>
    [ProtoContract]
    public sealed partial class C2G_PlayerMove : Fantasy.Network.Interface.IMessage
    {
        public uint OpCode() { return OuterOpCode.C2G_PlayerMove; }

        /// <summary>
        /// 目标位置 X 坐标
        /// </summary>
        [ProtoMember(1)]
        public float TargetX { get; set; }

        /// <summary>
        /// 目标位置 Y 坐标
        /// </summary>
        [ProtoMember(2)]
        public float TargetY { get; set; }

        /// <summary>
        /// 目标位置 Z 坐标
        /// </summary>
        [ProtoMember(3)]
        public float TargetZ { get; set; }

        public void Dispose()
        {
            TargetX = 0;
            TargetY = 0;
            TargetZ = 0;
        }
    }
}
```

**特点:**
- ✅ 自动实现接口(`IMessage`, `IRequest`, `IResponse` 等)
- ✅ 包含 `OpCode()` 方法返回协议编号
- ✅ 包含 `Dispose()` 方法支持对象池
- ✅ 支持 ProtoBuf、MemoryPack、Bson 序列化特性
- ✅ 生成 XML 文档注释(从 `.proto` 文件的 `///` 注释)

### OpCode 枚举

**文件名:** `InnerOpCode.cs` 和 `OuterOpCode.cs`

**生成位置:**
- 服务端: `NetworkProtocolServerDirectory/`
- 客户端: `NetworkProtocolClientDirectory/`

**生成的枚举结构:**

```csharp
namespace Fantasy.Network.Message
{
    /// <summary>
    /// Outer 协议 OpCode 枚举
    /// </summary>
    public static class OuterOpCode
    {
        public const uint C2G_TestMessage = 10001;
        public const uint C2G_LoginRequest = 10002;
        public const uint G2C_LoginResponse = 10003;
        public const uint C2G_PlayerMove = 10004;
        // ... 更多 OpCode
    }

    /// <summary>
    /// Inner 协议 OpCode 枚举
    /// </summary>
    public static class InnerOpCode
    {
        public const uint G2M_CreateEntityRequest = 20001;
        public const uint M2G_CreateEntityResponse = 20002;
        // ... 更多 OpCode
    }
}
```

### NetworkProtocolHelper 扩展方法

**文件名:** `NetworkProtocolHelper.cs`

**生成位置:**
- 服务端: `NetworkProtocolServerDirectory/`
- 客户端: `NetworkProtocolClientDirectory/`

**生成的扩展方法结构:**

```csharp
namespace Fantasy.Network.Message
{
    public static class NetworkProtocolHelper
    {
        #region IMessage 扩展方法

        /// <summary>
        /// 发送 C2G_TestMessage 消息
        /// </summary>
        public static void C2G_TestMessage(this Session session, string tag)
        {
            var message = new C2G_TestMessage { Tag = tag };
            session.Send(message);
        }

        /// <summary>
        /// 发送 C2G_PlayerMove 消息
        /// </summary>
        public static void C2G_PlayerMove(this Session session, float targetX, float targetY, float targetZ)
        {
            var message = new C2G_PlayerMove
            {
                TargetX = targetX,
                TargetY = targetY,
                TargetZ = targetZ
            };
            session.Send(message);
        }

        #endregion

        #region IRequest 扩展方法

        /// <summary>
        /// 发送 C2G_LoginRequest 请求
        /// </summary>
        public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, string username, string password)
        {
            var request = new C2G_LoginRequest
            {
                Username = username,
                Password = password
            };
            return await session.Call<G2C_LoginResponse>(request);
        }

        #endregion
    }
}
```

**Helper 方法特点:**

- ✅ **简化调用**: 无需手动创建消息对象
- ✅ **类型安全**: 参数和返回值都是强类型
- ✅ **智能生成**: 仅为包含字段的消息生成参数化方法
- ✅ **分类清晰**: 按接口类型分组(IMessage, IRequest 等)

**使用 Helper 方法示例:**

```csharp
// ✅ 推荐: 使用 Helper 方法
session.C2G_PlayerMove(100.5f, 50.2f, 30.1f);
var response = await session.C2G_LoginRequest("player1", "password123");

// ❌ 不推荐: 手动创建消息对象
var message = new C2G_PlayerMove
{
    TargetX = 100.5f,
    TargetY = 50.2f,
    TargetZ = 30.1f
};
session.Send(message);
```

---

## 自定义代码模板

### 模板文件位置

**模板文件:** `NetworkProtocolTemplate.txt`

**位置:**
```
/Tools/Exporter/NetworkProtocol/NetworkProtocolTemplate.txt
```

### 模板占位符说明

模板文件包含两个特殊占位符,会在代码生成时被替换:

| 占位符 | 说明 | 替换内容 |
|-------|------|---------|
| `(UsingNamespace)` | 命名空间引用 | 根据序列化器配置插入 `using` 语句 |
| `(Content)` | 协议类内容 | 生成的所有协议类代码 |

**模板结构:**

```csharp
#if SERVER
// 服务端特有引用
using MongoDB.Bson.Serialization.Attributes;
#endif

using ProtoBuf;
(UsingNamespace)  // <-- 自定义序列化器的 using 语句会插入到这里

#pragma warning disable CS8618
// 更多编译器警告抑制

namespace Fantasy.Network.Message
{
(Content)  // <-- 生成的协议类代码会插入到这里
}
```

### 自定义模板示例

**场景1: 添加自定义命名空间**

如果您想在所有生成的代码中添加自定义命名空间引用:

```csharp
using ProtoBuf;
using MyCompany.Serialization;  // <-- 添加自定义引用
(UsingNamespace)

namespace Fantasy.Network.Message
{
(Content)
}
```

**场景2: 修改命名空间结构**

如果您想使用不同的命名空间:

```csharp
using ProtoBuf;
(UsingNamespace)

namespace MyGame.Network.Protocol  // <-- 自定义命名空间
{
(Content)
}
```

**场景3: 添加全局特性**

如果您想为所有类添加全局特性:

```csharp
using ProtoBuf;
(UsingNamespace)

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyGame.Tests")]

namespace Fantasy.Network.Message
{
(Content)
}
```

**注意事项:**

⚠️ **不要删除占位符**: `(UsingNamespace)` 和 `(Content)` 必须保留,否则代码生成会失败

⚠️ **保持条件编译**: `#if SERVER` / `#else` / `#endif` 用于区分服务端和客户端代码

⚠️ **编码格式**: 模板文件应使用 UTF-8 编码

---

## 错误检测与验证

导出工具内置了完善的错误检测机制,能够在生成代码前发现并报告协议定义中的问题。

### 格式验证

工具会自动检测以下错误:

#### 1. 重复的消息名称

**错误示例:**

```protobuf
// ❌ 错误: 重复的消息名称
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
}

message C2G_LoginRequest // IMessage
{
    string Tag = 1;
}
```

**错误信息:**
```
[错误] 协议格式错误
文件: OuterMessage.proto
消息: C2G_LoginRequest
错误: 消息名称重复,已存在同名消息
```

#### 2. 重复的字段编号

**错误示例:**

```protobuf
// ❌ 错误: 重复的字段编号
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 1;  // 字段编号重复!
}
```

**错误信息:**
```
[错误] 协议格式错误
文件: OuterMessage.proto
消息: C2G_LoginRequest
字段编号: 1
错误: 字段编号重复
```

#### 3. 重复的字段名称

**错误示例:**

```protobuf
// ❌ 错误: 重复的字段名称
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Username = 2;  // 字段名称重复!
}
```

**错误信息:**
```
[错误] 协议格式错误
文件: OuterMessage.proto
消息: C2G_LoginRequest
字段名称: Username
错误: 字段名称重复
```

#### 4. 缺少响应消息

**错误示例:**

```protobuf
// ❌ 错误: IRequest 必须指定响应消息
message C2G_LoginRequest // IRequest
{
    string Username = 1;
}
```

**错误信息:**
```
[错误] 协议格式错误
文件: OuterMessage.proto
消息: C2G_LoginRequest
错误: IRequest 类型必须指定响应消息,格式: // IRequest,ResponseName
```

#### 5. 接口类型拼写错误

**错误示例:**

```protobuf
// ❌ 错误: 接口类型拼写错误
message C2G_TestMessage // iMessage
{
    string Tag = 1;
}
```

**错误信息:**
```
[错误] 协议格式错误
文件: OuterMessage.proto
消息: C2G_TestMessage
错误: 未知的接口类型 'iMessage',请检查拼写(区分大小写)
```

### 常见错误与解决方法

| 错误类型 | 原因 | 解决方法 |
|---------|------|---------|
| **消息名称重复** | 定义了两个同名消息 | 检查协议文件,重命名重复的消息 |
| **字段编号重复** | 同一消息中使用了相同的字段编号 | 为每个字段分配唯一的编号 |
| **字段名称重复** | 同一消息中定义了同名字段 | 重命名重复的字段 |
| **缺少响应消息** | IRequest 未指定响应类型 | 添加响应消息名称: `// IRequest,ResponseName` |
| **接口类型错误** | 接口类型名称拼写错误或不存在 | 检查并修正接口类型名称(区分大小写) |
| **OpCode 冲突** | 手动修改了 OpCode.Cache | 删除 OpCode.Cache 文件重新生成 |
| **配置路径错误** | ExporterSettings.json 中的路径不存在 | 检查并修正配置文件中的路径 |

---

## 最佳实践

### 1. 团队协作

**OpCode.Cache 版本控制:**

```bash
# 将 OpCode.Cache 加入版本控制
git add Examples/Config/NetworkProtocol/OpCode.Cache
git commit -m "Update protocol OpCode cache"
```

**作用:**
- ✅ 确保团队成员的协议 ID 一致
- ✅ 避免不同开发者生成的 OpCode 冲突
- ✅ 便于协议变更追踪

**协议修改流程:**

```
1. 开发者 A 修改 .proto 文件
2. 运行导出工具生成代码(OpCode.Cache 自动更新)
3. 提交 .proto 文件和 OpCode.Cache 到 Git
4. 其他开发者拉取更新
5. 其他开发者运行导出工具(使用相同的 OpCode.Cache)
```

### 2. 版本控制

**应该提交到 Git 的文件:**

```
✅ Examples/Config/NetworkProtocol/**/*.proto        # 协议定义文件
✅ Examples/Config/NetworkProtocol/OpCode.Cache      # OpCode 缓存
✅ Tools/Exporter/NetworkProtocol/ExporterSettings.json  # 配置文件
✅ Tools/Exporter/NetworkProtocol/NetworkProtocolTemplate.txt  # 代码模板(如有修改)
```

**不应该提交到 Git 的文件:**

```
❌ Examples/Server/Entity/Generate/NetworkProtocol/**/*.cs  # 生成的服务端代码
❌ Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol/**/*.cs  # 生成的客户端代码
```

**.gitignore 配置示例:**

```gitignore
# 生成的协议代码(可选,根据团队习惯决定是否提交)
**/Generate/NetworkProtocol/*.cs

# 不要忽略 OpCode.Cache
!**/NetworkProtocol/OpCode.Cache
```

**是否提交生成的代码:**

| 方案 | 优点 | 缺点 | 适用场景 |
|-----|------|------|---------|
| **提交生成的代码** | 拉取代码后即可编译,无需运行工具 | Git 仓库体积变大,代码审查时有噪音 | 小团队,不频繁修改协议 |
| **不提交生成的代码** | Git 仓库干净,仅关注源文件 | 每次拉取代码后需要运行导出工具 | 大团队,频繁修改协议 |

### 3. 持续集成

**在 CI/CD 中自动生成代码:**

**GitHub Actions 示例:**

```yaml
name: Generate Network Protocol

on:
  push:
    paths:
      - 'Examples/Config/NetworkProtocol/**/*.proto'

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Generate Network Protocol
        run: |
          cd Tools/Exporter/NetworkProtocol
          dotnet Fantasy.Tools.NetworkProtocol.dll --p 3

      - name: Commit Generated Code
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add Examples/Server/Entity/Generate/NetworkProtocol/
          git add Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol/
          git diff --quiet && git diff --staged --quiet || git commit -m "Auto-generate network protocol code"
          git push
```

**Jenkins 示例:**

```groovy
pipeline {
    agent any

    stages {
        stage('Generate Protocol') {
            when {
                changeset "**/NetworkProtocol/**/*.proto"
            }
            steps {
                sh '''
                    cd Tools/Exporter/NetworkProtocol
                    dotnet Fantasy.Tools.NetworkProtocol.dll --p 3
                '''
            }
        }

        stage('Commit Changes') {
            steps {
                sh '''
                    git add Examples/Server/Entity/Generate/NetworkProtocol/
                    git add Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol/
                    git commit -m "Auto-generate network protocol code" || true
                    git push origin main
                '''
            }
        }
    }
}
```

**Pre-commit Hook 示例:**

在 `.git/hooks/pre-commit` 中添加:

```bash
#!/bin/bash

# 检查是否有 .proto 文件被修改
if git diff --cached --name-only | grep -q "NetworkProtocol/.*\.proto$"; then
    echo "检测到协议文件变更,正在生成代码..."

    cd Tools/Exporter/NetworkProtocol
    dotnet Fantasy.Tools.NetworkProtocol.dll --p 3

    if [ $? -eq 0 ]; then
        echo "协议代码生成成功"
        git add ../../Examples/Server/Entity/Generate/NetworkProtocol/
        git add ../../Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol/
    else
        echo "协议代码生成失败,提交被中止"
        exit 1
    fi
fi
```

---

## 相关文档

- 📖 阅读 [网络协议目录结构说明](07-NetworkProtocol.md) 学习协议定义规范
- ⚙️ 阅读 [服务器配置](01-ServerConfiguration.md) 学习配置文件
- 🎯 阅读 [配置系统使用指南](05-ConfigUsage.md) 学习如何使用配置
- 🚀 阅读 [编写启动代码](02-WritingStartupCode.md) 学习如何启动框架

---
