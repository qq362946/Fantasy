# 快速开始 - 服务器端

本指南将帮助你快速创建一个 Fantasy Framework 服务器项目。

## 前提条件

- **.NET SDK**: .NET 8.0 或 .NET 9.0
- **IDE**: Visual Studio 2022、Rider 或 VS Code

检查你的 .NET 版本：

```bash
dotnet --version
```

> **📌 版本说明：**
> - Fantasy Framework 当前主版本为 **2.x**
> - 本文档基于 2.0.0 版本编写，但建议使用最新稳定版本
> - 框架支持 .NET 8.0 和 .NET 9.0
> - 查看最新版本和更新日志：[NuGet](https://www.nuget.org/packages/Fantasy-Net) | [GitHub Releases](https://github.com/qq362946/Fantasy/releases)

---

## 🎯 使用 Fantasy CLI 脚手架（强烈推荐）

Fantasy CLI 是官方提供的脚手架工具，可以**一键生成完整的项目结构**，包括配置文件、工具和示例代码，是最快速、最简单的入门方式。

### 安装 Fantasy CLI

将 Fantasy CLI 安装为全局 .NET 工具：

```bash
dotnet tool install -g Fantasy.Cli
```

更新到最新版本：

```bash
dotnet tool update -g Fantasy.Cli
```

### 创建项目

**方式一：交互模式（推荐）**

```bash
fantasy init
```

工具会引导你完成以下配置：
- 项目名称
- 目标框架 (.NET 8.0 或 9.0)
- 是否添加协议导出工具
- 是否添加网络协议定义
- 是否添加 NLog 日志组件

**方式二：快速创建**

```bash
fantasy init -n MyGameServer
```

直接使用项目名创建，其他选项使用默认值。

### 生成的项目结构

```
MyGameServer/
├── Server/
│   ├── Main/                   # 服务器入口点
│   ├── Entity/                 # 游戏实体
│   │   └── Fantasy.config      # 主配置文件（已自动生成）
│   ├── Hotfix/                 # 热重载逻辑
│   └── Server.sln
├── Config/                     # 配置目录
├── Tools/                      # 工具目录
│   ├── NetworkProtocol/        # 协议定义
│   └── ProtocolExportTool/     # 协议导出工具
```

### 构建和运行

```bash
cd MyGameServer

# 构建服务器
dotnet build Server/Server.sln

# 运行服务器
dotnet run --project Server/Main/Main.csproj
```

### 添加更多组件或工具

> **⚠️ 重要限制警告：**
>
> **`fantasy add` 命令目前仅支持向新创建的目录中添加组件，不能直接附加到已有项目中！**
>
> **这意味着：**
> - ❌ **不能**在已经创建的项目中运行 `fantasy add` 来添加组件
> - ❌ **不能**在已经存在代码的项目目录中使用此命令
> - ✅ **只能**在新创建的空目录中使用 `fantasy add`
>
> **如果你已经创建了项目并想添加更多组件，有以下两种方法：**
>
> 1. **在新目录中生成组件，然后手动复制**
>    ```bash
>    # 在临时目录中生成组件
>    mkdir temp && cd temp
>    fantasy add -t networkprotocol
>    # 然后手动复制生成的文件到你的项目
>    ```
>
> 2. **使用手动方式添加（推荐）**
>    - 直接下载或复制需要的组件到项目中
>    - 参考下方的[手动集成到现有项目](#手动集成到现有项目)章节

**在空目录中使用 `fantasy add` 的命令：**

```bash
# 交互式选择组件
fantasy add

# 添加特定组件
fantasy add -t protocolexporttool  # 协议导出工具
fantasy add -t networkprotocol     # 网络协议定义
fantasy add -t nlog                # NLog 日志
fantasy add -t fantasynet          # Fantasy.Net 框架
fantasy add -t fantasyunity        # Fantasy.Unity 客户端
fantasy add -t all                 # 添加所有组件
```

### 可用组件

| 组件 | 描述 |
|------|------|
| **Fantasy.Net** | 核心框架库（包含运行时和源代码生成器） |
| **Fantasy.Unity** | Unity 客户端框架（Unity 项目专用） |
| **ProtocolExportTool** | 协议导出工具（从 .proto 文件生成代码） |
| **NetworkProtocol** | 网络协议定义文件和模板 |
| **NLog** | NLog 日志组件配置 |

### 配置语言

Fantasy CLI 支持中文和英文界面。设置环境变量可跳过语言选择：

**Windows (PowerShell)：**
```powershell
$env:FANTASY_CLI_LANG = "Chinese"  # 或 "English"
```

**Linux/macOS：**
```bash
export FANTASY_CLI_LANG=Chinese  # 或 English
```

**✅ 使用 Fantasy CLI 创建项目后，可以直接跳到 [下一步：编写启动代码](#下一步编写启动代码) 章节。**

---

## 其他安装方式

如果你不想使用脚手架工具，或者需要将 Fantasy 集成到现有项目中，可以使用以下方式：

## 推荐的项目结构

虽然不强制，但建议使用分层结构：

```
YourSolution/
├── YourSolution.sln
├── Server/                   # 入口项目（Console 应用）
│   ├── Program.cs           # 启动代码
│   └── Server.csproj        # 引用 → Server.Entity和Server.Hotfix
│
├── Server.Entity/            # 实体项目（Class Library）
│   ├── Fantasy.config       # 配置文件
│   ├── Components.cs        # 实体、组件定义
│   ├── Generate             # 生成固定代码，比如网络协议等不需要热重载的数据
│   └── Server.Entity.csproj # 引用 → Fantasy（直接引用）
│
└── Server.Hotfix/            # 热更新项目（可选）
    ├── MessageHandlers.cs   # 消息处理器
    └── Server.Hotfix.csproj # 引用 → Server.Entity
```

**项目引用链：**

```
Server (入口)
  └─引用→ Server.Entity
              ├─引用→ Fantasy ⭐ (只有这里直接引用 Fantasy)
              └─被引用← Server.Hotfix
```

**分层说明：**

| 项目 | 职责 | 引用关系 | 是否需要引用 Fantasy |
|------|------|----------|---------------------|
| **Server** | 服务器启动入口，包含 `Program.cs` | 引用 `Server.Entity` | ❌ 不需要（通过 Entity 传递） |
| **Server.Entity** | 包含实体、组件、数据定义等 `Fantasy.config`| **直接引用 Fantasy** | ✅ **需要** |
| **Server.Hotfix** | 热更新逻辑：消息处理器、事件处理器等 | 引用 `Server.Entity` | ❌ 不需要（通过 Entity 传递） |

**🔑 关键理解：**
- **只有 `Server.Entity` 需要直接引用 Fantasy 框架**
- 其他项目通过引用 `Server.Entity` 就能自动获得 Fantasy 的功能（引用传递）
- 这种设计减少了重复配置，便于维护

## 手动集成到现有项目

### 方式一：NuGet 包引用（推荐）✨

**适用场景：** 大多数项目，快速上手

**在你的项目中**添加 NuGet 包 ：

```bash
# 添加最新版本
dotnet add package Fantasy-Net

# 或指定版本号
dotnet add package Fantasy-Net --version 2025.2.0
```

或直接编辑 `Server.Entity.csproj` 文件：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <!-- 使用最新版本（推荐） -->
        <PackageReference Include="Fantasy-Net" Version="*" />

        <!-- 或指定具体版本 -->
        <!-- <PackageReference Include="Fantasy-Net" Version="2.0.0" /> -->
    </ItemGroup>
</Project>
```

> **💡 提示：**
> - 建议使用最新稳定版本，使用 `dotnet add package Fantasy-Net` 会自动安装最新版本
> - 查看所有可用版本：https://www.nuget.org/packages/Fantasy-Net
> - 生产环境建议锁定具体版本号以保证稳定性

**✅ 完成！NuGet 包会自动配置所有必要的编译选项和 Source Generator，无需手动配置。**

**🎯 其他项目不需要直接引用：**
- `Server` 项目：引用 `Server.Entity`和`Server.Hotfix` 即可
- `Server.Hotfix` 项目：引用 `Server.Entity` 即可
- 它们会通过项目引用自动获得 Fantasy 的功能

完成此步骤后，直接跳到 **[步骤 2：创建配置文件](#步骤-2创建配置文件)**。

---

### 方式二：源码引用

**适用场景：** 需要自定义框架或深度开发

#### 2.1 Clone 项目源码

```bash
git clone https://github.com/qq362946/Fantasy.git
```

#### 2.2 添加项目引用

**只在你的项目**的 `.csproj` 中添加引用：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <!-- 引用核心框架 -->
        <ProjectReference Include="path/to/Fantasy/Fantasy.Packages/Fantasy.Net/Fantasy.Net.csproj" />

        <!-- 引用 Source Generator（必须！） -->
        <ProjectReference Include="path/to/Fantasy/Fantasy.Packages/Fantasy.SourceGenerator/Fantasy.SourceGenerator.csproj"
                          OutputItemType="Analyzer"
                          ReferenceOutputAssembly="false" />
    </ItemGroup>
</Project>
```

#### 2.3 配置项目属性

**源码引用时必须在项目中进行以下配置：**

编辑 `.csproj` 文件，添加必要的编译配置：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <!-- ==================== 必需配置 ==================== -->

    <!-- Debug 配置 -->
    <PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
        <!-- FANTASY_NET 宏：激活 Source Generator 代码生成 -->
        <DefineConstants>TRACE;FANTASY_NET</DefineConstants>
        <!-- AllowUnsafeBlocks：允许 unsafe 代码 -->
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>

    <!-- Release 配置 -->
    <PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
        <DefineConstants>TRACE;FANTASY_NET</DefineConstants>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>

    <!-- 项目引用 -->
    <ItemGroup>
        <ProjectReference Include="path/to/Fantasy/Fantasy.Packages/Fantasy.Net/Fantasy.Net.csproj" />
        <ProjectReference Include="path/to/Fantasy/Fantasy.Packages/Fantasy.SourceGenerator/Fantasy.SourceGenerator.csproj"
                          OutputItemType="Analyzer"
                          ReferenceOutputAssembly="false" />
    </ItemGroup>
</Project>
```
**重要说明：**
- 项目中只要使用了Fantasy相关的逻辑就必须要添加`Fantasy.SourceGenerator`的引用
- `Fantasy.SourceGenerator`会自动生成框架所需要的注册代码
- 如果不添加`Fantasy.SourceGenerator`代码会无法注册的框架中
```xml
<!-- 项目添加Fantasy.SourceGenerator -->
<ItemGroup>
    <ProjectReference Include="path/to/Fantasy/Fantasy.Packages/Fantasy.SourceGenerator/Fantasy.SourceGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```

**配置说明：**

| 配置项 | 用途 | 影响 |
|--------|------|------|
| `FANTASY_NET` | 激活 Source Generator 进行编译时代码生成 | 缺少此宏会导致框架无法生成注册代码，运行时出错 |
| `AllowUnsafeBlocks` | 允许使用 unsafe 代码 | Fantasy 使用 unsafe 代码优化性能，缺少会导致编译错误 |

---

### 步骤 2：创建配置文件

**⚠️ 重要：配置文件放在引用`Fantasy.net`项目根目录就可以，不需要非要放在入口项目！**

#### 方式一：NuGet 包（自动创建）

当你添加 NuGet 包后，`Fantasy.config` 和 `Fantasy.xsd` 会**自动**在项目根目录下创建。

你只需要根据实际需求修改配置内容即可。

#### 方式二：源码引用（手动复制）

源码中的配置文件位置：
- `Fantasy.config` 位于：`Fantasy/Fantasy.Packages/Fantasy.Net/Fantasy.config`
- `Fantasy.xsd` 位于：`Fantasy/Fantasy.Packages/Fantasy.Net/Fantasy.xsd`

将这两个文件复制到你引用了 Fantasy 的项目根目录（例如 `Server.Entity/`）即可。

#### 配置文件内容

无论使用哪种方式，`Fantasy.config` 的内容示例如下：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<fantasy xmlns="http://fantasy.net/config"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://fantasy.net/config Fantasy.xsd">

    <!-- 网络配置 -->
    <network inner="TCP" maxMessageSize="1048560" />

    <!-- 会话配置 -->
    <session idleTimeout="8000" idleInterval="5000" />

    <server>
        <!-- 机器配置 -->
        <machines>
            <machine id="1" outerIP="127.0.0.1" outerBindIP="127.0.0.1" innerBindIP="127.0.0.1" />
        </machines>

        <!-- 进程配置 -->
        <processes>
            <process id="1" machineId="1" startupGroup="0" />
        </processes>

        <!-- 世界配置 -->
        <worlds>
            <world id="1" worldName="MainWorld">
                <!-- 数据库配置(可选) -->
                <database dbType="MongoDB" dbName="game" dbConnection="mongodb://localhost:27017/" />
            </world>
        </worlds>

        <!-- 场景配置 -->
        <scenes>
            <!-- Gate 场景：处理客户端连接 -->
            <scene id="1001" processConfigId="1" worldConfigId="1"
                   sceneRuntimeMode="MultiThread" sceneTypeString="Gate"
                   networkProtocol="KCP" outerPort="20000" innerPort="11001" />
        </scenes>
    </server>
</fantasy>
```

**配置要点：**

以下是配置文件中最重要的几个参数：

| 配置项 | 说明 | 示例值 |
|--------|------|--------|
| `<machine>` | 定义服务器的IP地址<br>• `outerIP`: 客户端连接的IP<br>• `innerBindIP`: 服务器间通信的IP | 本地开发都用 `127.0.0.1`<br>生产环境使用实际IP |
| `<process>` | 定义进程运行在哪台机器上<br>• `machineId`: 引用机器ID<br>• `startupGroup`: 启动顺序 | 相同分组的进程同时启动 |
| `<world>` | 定义游戏世界和数据库<br>• 可配置多个数据库（主库、从库等）<br>• `dbConnection` 为空则不连接 | 开发环境可不配置数据库 |
| `<scene>` | **核心配置**，定义业务场景<br>• `outerPort`: 客户端连接端口<br>• `innerPort`: 服务器间通信端口<br>• `networkProtocol`: 网络协议 | Gate场景使用 KCP 协议<br>Map场景不对外监听 |

**💡 快速理解：**
- 本地开发：所有 IP 都用 `127.0.0.1`，配置一个 Gate 场景即可
- 生产环境：配置实际IP地址，根据业务需求配置多个场景
- 数据库可选：开发环境可以不连接数据库（`dbConnection=""`）

> **📖 详细说明：** 完整的配置参数说明请查看 [Fantasy.config 配置文件详解](../01-Server/01-ServerConfiguration.md)

---

#### 📌 为什么配置文件要放在引用 Fantasy 的项目？

**原因：**
1. **代码生成依赖**：框架会根据 `Fantasy.config` 生成注册代码（通过 Source Generator）
2. **引用链传递**：生成的代码在配置文件所在的项目中，其他项目通过引用该项目自动获得这些代码
3. **避免依赖问题**：如果放在没有被其他项目引用的项目中，生成的代码无法被其他项目使用

**示例：**
- ✅ 放在 `Server.Entity`（被 Server 和 Hotfix 引用）→ 所有项目都能使用生成的代码
- ❌ 放在 `Server` 入口项目（Hotfix 不引用 Server）→ Hotfix 无法使用生成的代码

---

#### ⚠️ 重要：配置文件必须复制到输出目录

**无论使用 NuGet 包还是源码引用，都必须在引用 Fantasy 的项目（如 `Server.Entity`）的 `.csproj` 中包含以下配置：**

```xml
<ItemGroup>
    <!-- 将配置文件复制到输出目录 -->
    <None Update="Fantasy.config">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="Fantasy.xsd">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>

    <!-- 重要：将配置文件添加为 AdditionalFiles，使 Source Generator 能够读取 -->
    <AdditionalFiles Include="Fantasy.config" />
</ItemGroup>
```

**配置说明：**

| 配置项 | 作用 | 缺少会导致 |
|--------|------|-----------|
| `<None Update>` | 确保配置文件在编译时复制到输出目录（`bin/Debug` 或 `bin/Release`），使运行时能够读取 | ❌ 运行时找不到配置文件，程序无法启动 |
| `<AdditionalFiles Include>` | 使 Source Generator 在编译时能够读取配置文件并生成相应代码（数据库名称常量、场景类型枚举等） | ❌ 无法生成数据库相关的代码，导致编译错误或运行时异常 |

**不同方式的处理：**

- **NuGet 包方式**：**必须手动添加**上述配置到 `.csproj` 文件中，否则程序无法正常运行。
- **源码引用方式**：**必须手动添加**上述配置到 `.csproj` 文件中，否则程序无法正常运行。

---

## 下一步：编写启动代码

完成框架集成和配置文件创建后，下一步是编写服务器启动代码。

请继续阅读 **[编写启动代码](../01-Server/02-WritingStartupCode.md)**，学习：
- 如何编写 `Program.cs` 启动代码
- `AssemblyHelper` 的作用和原理
- 程序集加载机制详解
- 热重载支持
- 常见问题和最佳实践

---

## 常见问题

### Q1: 如何卸载 Fantasy CLI?

**使用以下命令卸载：**
```bash
dotnet tool uninstall -g Fantasy.Cli
```

### Q2: 找不到 Fantasy 命名空间

**原因：**
- 未安装 NuGet 包或未正确引用源码
- NuGet 包版本不兼容（需要 2.x 版本）
- 未定义 `FANTASY_NET` 宏（仅源码引用）

**解决：**
```bash
# 检查已安装的包版本
dotnet list package

# 清理并重新安装
dotnet clean
dotnet restore
dotnet build

# 如果需要，更新到最新版本
dotnet add package Fantasy-Net
```

### Q3: Source Generator 没有生成代码

**使用 NuGet 包：**
- NuGet 包会自动配置 Source Generator，通常不会出现这个问题
- 如果出现问题，尝试：`dotnet clean && dotnet build`

**使用源码引用时检查清单：**
- [ ] 是否定义了 `FANTASY_NET` 宏
- [ ] 是否设置了 `AllowUnsafeBlocks=true`
- [ ] 是否添加了 `Fantasy.SourceGenerator.csproj` 引用
- [ ] 是否成功编译（Source Generator 在编译时工作）

**调试方法：**
```bash
# 清理并重新生成
dotnet clean
dotnet build -v detailed

# 查看生成的代码
ls obj/Debug/net8.0/generated/Fantasy.SourceGenerator/
```

### Q4: 端口被占用

**错误信息：**
```
System.Net.Sockets.SocketException: Address already in use
```

**解决：**
- 修改 `Fantasy.config` 中的 `outerPort` 端口号
- 或关闭占用端口的程序

### Q5: 配置文件未找到

**错误信息：**
```
Could not find Fantasy.config
```

**原因：**
- 配置文件位置错误（应该在引用了 Fantasy 的项目根目录）
- 配置文件未复制到输出目录

**解决：**

1. **检查配置文件位置**
   ```bash
   # 配置文件应该在引用了 Fantasy 的项目根目录（如 Server.Entity）
   ls Server.Entity/Fantasy.config
   ```

2. **源码引用时：确保在项目的 `.csproj` 中配置了文件复制**
   ```xml
   <ItemGroup>
       <None Update="Fantasy.config">
           <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       </None>
       <None Update="Fantasy.xsd">
           <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       </None>
       <!-- 重要：将配置文件添加为 AdditionalFiles，使 Source Generator 能够读取 -->
       <AdditionalFiles Include="Fantasy.config" />
   </ItemGroup>
   ```

3. **NuGet 包方式时**
   - NuGet 包会自动创建配置文件并配置复制，通常不会出现此问题
   - 如果出现，尝试清理后重新构建：`dotnet clean && dotnet build`

### Q6: 生成的代码无法在其他项目中使用

**症状：**
- 其他项目中无法使用框架生成的代码
- 提示找不到类型或命名空间

**原因：**
配置文件放在了错误的位置，导致代码生成在没有被其他项目引用的项目中

**解决：**
1. 确保 `Fantasy.config` 在直接引用了 Fantasy 的项目根目录（如 `Server.Entity`）
2. 确保需要使用生成代码的项目正确引用了该项目
3. 检查项目引用链是否正确
4. 重新构建解决方案：`dotnet clean && dotnet build`

## 下一步

完成 Fantasy Framework 的安装和配置后，建议按照以下顺序学习：

### 📖 推荐学习路径

1. **配置文件详解** 📋
   - [Fantasy.config 配置文件详解](../01-Server/01-ServerConfiguration.md)
   - 深入了解网络配置、场景配置、数据库配置等

2. **编写启动代码** 💻
   - [编写启动代码](../01-Server/02-WritingStartupCode.md)
   - 学习 AssemblyHelper、程序集加载、启动流程

3. **命令行参数配置** ⚙️
    - [命令行参数配置](../01-Server/03-CommandLineArguments.md)
    - 配置开发环境和生产环境的启动参数

4. **场景初始化** 🎬
    - [OnCreateScene 事件使用指南](../01-Server/04-OnCreateScene.md)
    - 学习如何在场景启动时初始化逻辑

5. **配置系统使用** 🔧
   - [配置系统使用指南](../01-Server/05-ConfigUsage.md)
   - 学习如何在代码中读取和使用配置


### 🎯 其他资源

- 📱 [Unity 客户端快速开始](02-QuickStart-Unity.md) - 创建 Unity 客户端
- 📚 查看 `Examples/Server` 目录下的完整示例
- 📖 返回 [文档首页](../README.md) 查看完整文档结构

## 获取帮助

- **GitHub**: https://github.com/qq362946/Fantasy
- **文档**: https://www.code-fantasy.com/
- **Issues**: https://github.com/qq362946/Fantasy/issues

---
