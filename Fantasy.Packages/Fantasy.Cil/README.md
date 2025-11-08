# Fantasy CLI
Fantasy CLI 是 [Fantasy 框架](https://github.com/qq362946/Fantasy) 的官方脚手架和项目管理工具。它提供了直观的命令行界面，用于创建和管理 Fantasy 游戏服务器项目。

### 功能特性

- 🚀 **快速项目脚手架** - 几秒钟内创建完整配置的 Fantasy 项目
- 🌍 **多语言支持** - 英文和中文界面（自动检测或手动选择）
- 🛠️ **模块化工具管理** - 添加框架组件和工具
- 📦 **智能包管理** - 自动依赖解析和包还原
- 🎨 **交互模式** - 用户友好的交互式提示，美观的终端界面
- ⚡ **多框架支持** - 支持 .NET 8.0 和 .NET 9.0

### 安装

将 Fantasy CLI 安装为全局 .NET 工具：

```bash
dotnet tool install -g Fantasy.Cli
```

更新到最新版本：

```bash
dotnet tool update -g Fantasy.Cli
```

卸载：

```bash
dotnet tool uninstall -g Fantasy.Cli
```

### 快速开始

#### 创建新项目

**交互模式（推荐）：**
```bash
fantasy init
```

**使用项目名快速开始：**
```bash
fantasy init -n MyGameServer
```

这将创建一个完整的 Fantasy 项目结构：
```
MyGameServer/
├── Server/
│   ├── Main/                   # 服务器入口点
│   ├── Entity/                 # 游戏实体
│       ├── Fantasy.config      # 主配置文件
│   ├── Hotfix/                 # 热重载逻辑
│   └── Server.sln
├── Config/
├── Tools/                      # 工具
│   ├── NetworkProtocol/        # 协议定义
│   └── ProtocolExportTool/     # 协议导出工具

```

#### 添加框架组件和工具

**交互模式：**
```bash
cd MyGameServer
fantasy add
```

**添加特定组件和工具：**
```bash
# 添加协议导出工具
fantasy add -t protocolexporttool

# 添加网络协议定义
fantasy add -t networkprotocol

# 添加 NLog 日志组件
fantasy add -t nlog

# 添加 Fantasy.Net 框架
fantasy add -t fantasynet

# 添加 Fantasy.Unity 客户端框架
fantasy add -t fantasyunity

# 添加所有组件
fantasy add -t all

# 添加到特定项目路径
fantasy add -p /path/to/project -t protocolexporttool
```

### 命令

#### `fantasy init`

初始化一个新的 Fantasy 项目。

**选项：**
- `-n, --name <name>` - 项目名称
- `-i, --interactive` - 以交互模式运行（默认：true）

**示例：**
```bash
# 交互模式，完整配置
fantasy init

# 仅使用名称快速开始
fantasy init -n MyGame

# 非交互模式（使用默认值）
fantasy init -n MyGame --interactive false
```

#### `fantasy add`

向现有的 Fantasy 项目添加工具和组件。

**选项：**
- `-p, --path <path>` - Fantasy 项目目录路径（默认：当前目录）
- `-t, --tool <tool>` - 要添加的工具：`protocolexporttool`、`networkprotocol`、`nlog`、`fantasynet`、`fantasyunity`、`all`

**示例：**
```bash
# 交互式选择
fantasy add

# 向当前目录添加特定工具
fantasy add -t protocolexporttool

# 向特定项目添加工具
fantasy add -p ~/projects/MyGame -t networkprotocol
```

### 配置

#### 语言选择

Fantasy CLI 支持自动语言检测。要在启动时跳过语言选择，请设置环境变量：

**Windows (PowerShell)：**
```powershell
$env:FANTASY_CLI_LANG = "Chinese"  # 或 "English"
```

**Linux/macOS：**
```bash
export FANTASY_CLI_LANG=Chinese  # 或 English
```

**永久设置（添加到配置文件）：**
```bash
# ~/.bashrc, ~/.zshrc, 或 ~/.profile
export FANTASY_CLI_LANG=Chinese
```

### 可用组件

| 组件 | 描述 |
|------|------|
| **Fantasy.Net** | 核心框架库（包含运行时和源代码生成器） |
| **Fantasy.Unity** | Unity 客户端框架（Unity 项目专用） |
| **ProtocolExportTool** | 协议导出工具（从 .proto 文件生成代码） |
| **NetworkProtocol** | 网络协议定义文件和模板 |
| **NLog** | NLog 日志组件配置 |

### 构建和运行

创建项目后：

```bash
cd MyGameServer

# 构建服务器
dotnet build Server/Server.sln

# 运行服务器
dotnet run --project Server/Main/Main.csproj
```

### 系统要求

- .NET SDK 8.0 或 9.0
- 操作系统：Windows、Linux 或 macOS

### 相关链接

- **框架仓库**: https://github.com/qq362946/Fantasy
- **文档**: https://github.com/qq362946/Fantasy/tree/main/Docs
- **问题反馈**: https://github.com/qq362946/Fantasy/issues

### 许可证

MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。
