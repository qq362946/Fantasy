# 网络协议导出工具使用指南

本文档介绍 Fantasy Framework 的网络协议导出工具,将 `.proto` 协议定义文件自动生成为 C# 代码。

---

## 工具概述

Fantasy 提供两种协议导出工具:

### 1. 命令行工具 (Fantasy.ProtocolExportTool)
- 适合 CI/CD 集成和自动化脚本
- 支持交互式和静默模式
- 位置: `/Fantasy.Packages/Fantasy.ProtocolExportTool`

### 2. 可视化编辑器 (Fantasy.ProtocolEditor)
- 基于 Avalonia 的跨平台桌面应用
- 内置 .proto 文件编辑器,支持语法高亮和代码补全
- 可视化配置编辑 (RoamingType.Config / RouteType.Config)
- 工作区管理,保存编辑状态
- 位置: `/Fantasy.Packages/Fantasy.ProtocolEditor`

### 核心功能
1. **解析 .proto 协议文件**: 读取协议定义并生成 C# 代码
2. **生成协议类**: 自动生成消息类、OpCode 枚举、Helper 扩展方法
3. **支持多种序列化**: ProtoBuf、MemoryPack、Bson
4. **格式验证**: 检测重复字段、错误接口类型等问题
5. **增量更新**: 通过 OpCode.Cache 确保协议 ID 稳定性
6. **子包协议导出**: 允许 Server 端子包使用独立的 proto 目录和代码输出目录
7. **共享 OpCode 缓存**: 主协议与所有子包共用一份 OpCode.Cache，避免协议 ID 冲突

---

## 一、命令行工具使用

### 安装与编译

**方式 1: 使用 Fantasy CLI (推荐)**

```bash
# 安装 Fantasy CLI (如果尚未安装)
dotnet tool install -g Fantasy.Cli

# 验证安装
fantasy --version
```

> **⚠️ macOS/Linux 用户注意：**
>
> 如果安装后无法直接使用 `fantasy` 命令，需要配置 PATH 环境变量。
>
> **详细配置步骤请查看：** [Fantasy CLI 完整文档](../../Fantasy.Packages/Fantasy.Cil/README.md)（查看"安装"章节）

```bash
# 使用 CLI 添加协议导出工具
fantasy add -t protocolexporttool
```

工具将被安装到 `Tools/Exporter/NetworkProtocol/` 目录。

**方式 2: 使用源码**

```bash
# 编译工具
dotnet build Fantasy.Packages/Fantasy.ProtocolExportTool/Fantasy.ProtocolExportTool.csproj

# 运行工具
dotnet run --project Fantasy.Packages/Fantasy.ProtocolExportTool/Fantasy.ProtocolExportTool.csproj
```

**方式 3: 发布为独立可执行文件**

```bash
# 发布为当前平台可执行文件
cd Fantasy.Packages/Fantasy.ProtocolExportTool
dotnet publish -c Release -r osx-arm64      # macOS ARM (M1/M2/M3)
dotnet publish -c Release -r osx-x64        # macOS Intel
dotnet publish -c Release -r win-x64        # Windows 64位
dotnet publish -c Release -r linux-x64      # Linux 64位
```

### 配置文件

创建 `ExporterSettings.json` 配置文件。下面的示例同时导出项目主协议和一个 `Fantasy.Room` 子包的协议。示例使用以 Fantasy 仓库根目录为基准的相对路径:

```json
{
    "Export": {
        "NetworkProtocolDirectory": {
            "Value": "Examples/Config/NetworkProtocol",
            "Comment": "主协议文件所在目录"
        },
        "NetworkProtocolServerDirectory": {
            "Value": "Examples/Server/APP/Entity/Generate/NetworkProtocol",
            "Comment": "主协议生成到服务端的目录"
        },
        "NetworkProtocolClientDirectory": {
            "Value": "Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol",
            "Comment": "主协议生成到客户端的目录"
        },
        "SharedOpCodeCacheFile": {
            "Value": "Examples/Config/NetworkProtocol/OpCode.Cache",
            "Comment": "主协议与子包共享的 OpCode 缓存文件"
        },
        "ExportType": "All",
        "PackageExports": [
            {
                "NetworkProtocolDirectory": {
                    "Value": "Fantasy.Packages/Fantasy.Room/Protocol",
                    "Comment": "子包协议文件所在目录"
                },
                "NetworkProtocolServerDirectory": {
                    "Value": "Fantasy.Packages/Fantasy.Room/Runtime/Protocol",
                    "Comment": "子包协议生成到服务端的目录"
                },
                "NetworkProtocolClientDirectory": {
                    "Value": "Fantasy.Packages/Fantasy.Room.Unity/Runtime/Protocol",
                    "Comment": "子包协议生成到客户端的目录"
                },
                "ExportType": "All"
            }
        ]
    }
}
```

#### 配置项说明

| 配置项 | 说明 |
|---------|------|
| `NetworkProtocolDirectory` | 项目主协议根目录，其下可包含 `Outer/`、`Inner/`、`RouteType.Config` 和 `RoamingType.Config` |
| `NetworkProtocolServerDirectory` | 主协议的服务端 C# 代码输出目录 |
| `NetworkProtocolClientDirectory` | 主协议的客户端 C# 代码输出目录 |
| `SharedOpCodeCacheFile` | 主协议和所有子包共用的 OpCode 缓存文件；建议显式配置。留空时默认使用主协议目录下的 `OpCode.Cache`，子包仍会共用该文件 |
| `ExportType` | 当前协议的导出目标：`Server`、`Client` 或 `All`；未配置时兼容为 `All` |
| `PackageExports` | 子包导出配置列表，可添加多个子包；每项都有独立的协议根目录、输出目录和 `ExportType` |

> **路径解析规则:** 绝对路径可直接使用；相对路径优先以 `ExporterSettings.json` 所在目录为基准。为兼容旧配置，如果主协议目录只在当前工作目录下存在，则该配置中的整组相对路径会继续以当前工作目录为基准。新配置建议统一按配置文件目录填写相对路径。

#### 子包协议目录结构

子包可以将 proto 文件放在自己的包目录中，不需要合并到项目主 `NetworkProtocol` 目录。导出工具仍然会在每个协议根目录下递归读取 `Outer/**/*.proto` 和 `Inner/**/*.proto`:

```text
Fantasy.Packages/Fantasy.Room/
├── Protocol/
│   ├── Outer/
│   │   └── RoomOuter.proto
│   ├── Inner/
│   │   └── RoomInner.proto
│   ├── RouteType.Config       # 按需添加
│   └── RoamingType.Config     # 按需添加
└── Runtime/Protocol/           # 服务端生成代码

Fantasy.Packages/Fantasy.Room.Unity/
└── Runtime/Protocol/           # 客户端生成代码
```

导出时会先统一检查主协议和所有子包协议。如果不同协议目录中存在同名消息，工具会终止导出并报告两个消息的文件位置。协议根目录必须已存在；静默模式下，不存在的代码输出目录会自动创建。

#### OpCode 缓存安全机制

- 已存在的消息会按消息名复用原 OpCode；新增、插入、调整文件顺序或重新导出不会改变已有编号。
- 删除或重命名消息时，旧缓存项会继续保留，避免旧编号被其他消息复用。
- 修改消息的序列化方式或接口类型时必须生成新编号；旧编号会写成 `@reserved:旧编号:原消息名` 历史保留项，之后不会分配给其他消息。
- 主协议和所有 `PackageExports` 会在同一次缓存会话中完成解析。只有所有目标验证和代码生成成功后，才会提交一次缓存。
- 缓存存在非法行、重复消息名或重复编号时，导出会停止并报告具体行号，不会静默修复或覆盖原文件。
- 同一份缓存同时只能由一个导出进程使用；重复启动的进程会给出“缓存正在被另一个导出进程使用”的错误。
- 缓存使用临时文件原子替换，避免程序异常退出时留下截断文件。协议验证失败时命令返回非零退出码，方便 CI/CD 正确中止。

`@reserved:` 行由工具自动维护，不要删除或手动修改。例如：

```text
C2G_LoginRequest = 268445457
@reserved:134227729:C2G_OldMessage = 134227729
```

### 使用方法

**交互式模式 (推荐)**

```bash
# 直接运行,工具会引导您完成配置
dotnet Fantasy.ProtocolExportTool.dll export
```

**静默模式 (CI/CD)**

```bash
# 从 ExporterSettings.json 读取配置并执行导出
dotnet Fantasy.ProtocolExportTool.dll export --silent

# 简写
dotnet Fantasy.ProtocolExportTool.dll export -S

# 指定另一份配置文件
dotnet Fantasy.ProtocolExportTool.dll export --silent \
  --config "/path/to/ServerPackageExporterSettings.json"
```

不传 `--config` 时，工具默认读取当前工作目录下的 `ExporterSettings.json`。`PackageExports` 与 `SharedOpCodeCacheFile` 通过静默模式的配置文件生效。

**命令行参数模式**

```bash
# 完整参数
dotnet Fantasy.ProtocolExportTool.dll export \
  --name "/path/to/protocols" \
  --server "/path/to/server/output" \
  --client "/path/to/client/output" \
  --type "all"

# 简写参数
dotnet Fantasy.ProtocolExportTool.dll export \
  -n "/path/to/protocols" \
  -s "/path/to/server/output" \
  -c "/path/to/client/output" \
  -t "all"
```

**参数说明:**

| 参数 | 简写 | 说明 | 可选值 |
|-----|------|------|--------|
| `--name` | `-n` | 协议文件目录 | 目录路径 |
| `--server` | `-s` | 服务端输出目录 | 目录路径 |
| `--client` | `-c` | 客户端输出目录 | 目录路径 |
| `--type` | `-t` | 导出类型 | `server` / `client` / `all` |
| `--silent` | `-S` | 静默模式 | 无值参数 |
| `--config` | 无 | 静默模式下的 `ExporterSettings.json` 路径 | 文件路径 |

> **模式区别:** `--name`、`--server`、`--client` 适合一次导出一组协议；需要在一次执行中导出主协议和多个子包时，请使用 `--silent` 加配置文件。

---

## 二、可视化编辑器使用

### 安装与运行

**方式 1: 下载已编译版本 (推荐)**

可以直接下载已编译好的编辑器，无需自己编译：

- **百度网盘**: https://pan.baidu.com/s/1eGk-e8dkkU7QamsSRZqojQ?pwd=niyx (提取码: niyx)
- **QQ群**: [569888673](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=569888673) (在 QQ 中搜索群号加入，然后在群文件「框架工具」中下载)

**方式 2: 从源码编译**

```bash
# 编译编辑器
dotnet build Fantasy.Packages/Fantasy.ProtocolEditor/Fantasy.ProtocolEditor.csproj

# 运行编辑器
dotnet run --project Fantasy.Packages/Fantasy.ProtocolEditor/Fantasy.ProtocolEditor.csproj
```

**方式 3: 发布为独立应用**

```bash
cd Fantasy.Packages/Fantasy.ProtocolEditor

# macOS (生成 .app 包)
dotnet publish -c Release -r osx-arm64

# Windows (生成 .exe)
dotnet publish -c Release -r win-x64

# Linux
dotnet publish -c Release -r linux-x64
```

### 主要功能

#### 1. 工作区管理
- **切换协议工作区**: `文件 → 切换工作区` - 选择包含 `Inner/Outer` 的 NetworkProtocol 文件夹
- **自动保存**: 编辑器自动保存工作区状态(打开的文件、光标位置)
- **文件树**: 显示 Inner/Outer 文件夹下的 .proto 文件
- **工作区配置**: `ExporterSettings.json` 和 `editor-state.json` 固定保存在协议工作区根目录
- **共享导出配置**: 编辑器和命令行导出工具共同使用工作区中的 `ExporterSettings.json`
- **多开支持**: 通过 `--config` 为每个编辑器进程指定独立协议工作区

#### 2. .proto 文件编辑
- **语法高亮**: 支持 protobuf 语法高亮
- **代码补全**: 输入时自动提示消息类型和字段
- **多标签编辑**: 支持同时打开多个文件
- **快捷保存**: `Ctrl+S` (Windows/Linux) / `Cmd+S` (macOS)

#### 3. 配置文件编辑
- **RoamingType.Config**: 可视化编辑 Roaming 消息类型配置
- **RouteType.Config**: 可视化编辑 Route 消息类型配置
- **表格编辑**: 添加、删除、修改配置项

#### 4. 导出设置
- **主协议配置**: `文件 → 导出设置` - 配置主协议的服务器/客户端输出路径和导出目标
- **子包配置**: 在 `PackageExports` 区域新增、删除子包，并配置各自的协议目录、输出目录和导出目标
- **一键导出**: `工具 → 导出协议` - 生成 C# 代码
- **输出日志**: 底部面板显示导出进度和错误信息

### 界面布局

```
┌─────────────────────────────────────────────────┐
│ 菜单栏: 文件 | 编辑 | 工具 | 帮助                 │
├──────────┬──────────────────────────────────────┤
│          │  [Tab1] [Tab2] [Tab3] ...           │
│          ├──────────────────────────────────────┤
│ 文件树   │                                      │
│  Inner/  │         编辑器区域                    │
│  Outer/  │    (代码编辑器 / 配置编辑器)          │
│  Config  │                                      │
│          │                                      │
├──────────┴──────────────────────────────────────┤
│ 输出面板: 显示导出日志、错误信息                │
└─────────────────────────────────────────────────┘
```

### 协议工作区配置

编辑器只需要选择一个协议工作区，不再区分“协议目录”和“配置目录”。`ExporterSettings.json` 与 `editor-state.json` 都固定保存在协议工作区根目录：

| 文件 | 用途 |
|------|------|
| `ExporterSettings.json` | 服务端/客户端输出目录、子包协议和 OpCode 缓存；主协议目录固定为当前工作区（配置值为 `.`），并与 `Fantasy.ProtocolExportTool` 共用 |
| `editor-state.json` | 编辑器打开的标签、光标位置和当前标签；只属于界面状态 |

编辑器启动时会依次尝试 `--config` 指定的工作区、当前工作目录和上次使用的协议工作区。如果都不可用，会先显示“选择协议工作区”引导，说明应选择包含 `Inner/Outer` 的 NetworkProtocol 目录，以及将自动创建的两份配置文件，确认后才打开系统目录选择器。通过 `文件 → 切换工作区` 也可以选择其他工作区；如果所选目录中没有 `Inner` 和 `Outer`，编辑器会要求确认或重新选择。工作区中缺少配置时，会自动创建默认的 `ExporterSettings.json` 和 `editor-state.json`；如果只缺少其中一个，也会自动补建。

`workspace.json` 已完全废弃，不再创建或读取。编辑器只在当前用户应用数据目录记录上次选择的协议工作区，不保存项目配置。

#### 多开并指定外部配置

```bash
# 已发布的可执行程序
Fantasy.ProtocolEditor --config "/path/to/project-a/NetworkProtocol"

# 从源码运行
dotnet run \
  --project Fantasy.Packages/Fantasy.ProtocolEditor/Fantasy.ProtocolEditor.csproj \
  -- --config "/path/to/project-b/NetworkProtocol"
```

编辑器中的 `--config` 表示协议工作区路径，`--config=/path/to/NetworkProtocol` 写法也受支持。启动参数指定的工作区只对当前进程生效，因此可以同时启动多个编辑器并使用不同工作区。多个进程确实需要共用同一工作区时，工具会使用跨进程锁和原子文件替换来避免 JSON 损坏；界面状态仍以最后一次保存为准。

`editor-state.json` 只保存界面状态：

```json
{
  "OpenedTabs": [
    {
      "FilePath": "/path/to/OuterMessage.proto",
      "CaretOffset": 245,
      "EditorType": "TextEditor"
    }
  ],
  "ActiveTabFilePath": "/path/to/OuterMessage.proto"
}
```

---

## 生成的代码结构

### 1. 协议类

**文件**: `{协议文件名}.cs`

```csharp
namespace Fantasy.Network.Message
{
    [ProtoContract]
    public sealed partial class C2G_LoginRequest : IRequest
    {
        public uint OpCode() => OuterOpCode.C2G_LoginRequest;

        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string Password { get; set; }

        public void Dispose()
        {
            Username = default;
            Password = default;
        }
    }
}
```

### 2. OpCode 枚举

**文件**: `InnerOpCode.cs` / `OuterOpCode.cs`

```csharp
public static class OuterOpCode
{
    public const uint C2G_LoginRequest = 10001;
    public const uint G2C_LoginResponse = 10002;
    // ...
}
```

### 3. Helper 扩展方法

**文件**: `NetworkProtocolHelper.cs`

```csharp
public static class NetworkProtocolHelper
{
    // IMessage - 单向发送
    public static void C2G_PlayerMove(this Session session, float x, float y, float z)
    {
        session.Send(new C2G_PlayerMove { X = x, Y = y, Z = z });
    }

    // IRequest - 异步请求
    public static async FTask<G2C_LoginResponse> C2G_LoginRequest(
        this Session session, string username, string password)
    {
        var request = new C2G_LoginRequest { Username = username, Password = password };
        return await session.Call<G2C_LoginResponse>(request);
    }
}
```

**使用示例:**

```csharp
// 使用 Helper 方法 (推荐)
session.C2G_PlayerMove(100f, 50f, 30f);
var response = await session.C2G_LoginRequest("player1", "pass123");

// 手动创建消息 (不推荐)
session.Send(new C2G_PlayerMove { X = 100f, Y = 50f, Z = 30f });
```

---

## 错误检测

工具会自动检测以下错误:

| 错误类型 | 说明 | 解决方法 |
|---------|------|---------|
| 消息名称重复 | 定义了同名消息 | 重命名消息 |
| 字段编号重复 | 字段使用了相同编号 | 修改字段编号 |
| 字段名称重复 | 字段使用了相同名称 | 重命名字段 |
| 缺少响应消息 | IRequest 未指定响应类型 | 添加响应: `// IRequest,ResponseName` |
| 接口类型错误 | 接口类型拼写错误 | 检查拼写 (区分大小写) |

**错误示例:**

```protobuf
// ❌ 错误: 重复的字段编号
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 1;  // 重复!
}

// ✅ 正确
message C2G_LoginRequest // IRequest,G2C_LoginResponse
{
    string Username = 1;
    string Password = 2;
}
```

---

## 最佳实践

### 版本控制

**应该提交:**
```
✅ Examples/Config/NetworkProtocol/**/*.proto             # 主协议定义
✅ Fantasy.Packages/*/Protocol/**/*.proto                  # 子包协议定义
✅ Examples/Config/NetworkProtocol/OpCode.Cache           # 主协议与子包共享的 OpCode 缓存
✅ Fantasy.Packages/Fantasy.ProtocolExportTool/ExporterSettings.json  # 导出配置
```

**不应该提交 (或可选):**
```
❌ **/Generate/NetworkProtocol/**/*.cs               # 生成的代码 (可选)
❌ **/editor-state.json                              # 编辑器个人状态
```

> `ExporterSettings.json` 建议提交给团队共用；`editor-state.json` 通常不应提交。

### CI/CD 集成

**GitHub Actions 示例:**

```yaml
name: Generate Protocol Code

on:
  push:
    paths:
      - 'Examples/Config/NetworkProtocol/**/*.proto'
      - 'Fantasy.Packages/*/Protocol/**/*.proto'
      - 'Fantasy.Packages/*/Protocol/*.Config'
      - 'Fantasy.Packages/Fantasy.ProtocolExportTool/ExporterSettings.json'

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Generate Protocol
        run: |
          dotnet run \
            --project Fantasy.Packages/Fantasy.ProtocolExportTool/Fantasy.ProtocolExportTool.csproj \
            -- export --silent \
            --config Fantasy.Packages/Fantasy.ProtocolExportTool/ExporterSettings.json

      - name: Commit Changes
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add Examples/**/Generate/NetworkProtocol/
          git add Fantasy.Packages/*/Runtime/Protocol/
          git add Examples/Config/NetworkProtocol/OpCode.Cache
          git diff --cached --quiet || git commit -m "chore: update protocol code"
          git push
```

> 示例中的配置文件路径需要按仓库结构调整；配置内部的相对路径建议以配置文件目录为基准。无论是主协议还是子包协议，都应提交同一份共享 `OpCode.Cache`。建议显式配置 `SharedOpCodeCacheFile`；留空时工具使用主协议目录下的缓存。

### Pre-commit Hook

```bash
#!/bin/bash
# .git/hooks/pre-commit

if git diff --cached --name-only | grep -q "\.proto$"; then
    echo "检测到协议变更,正在生成代码..."
    cd "$(git rev-parse --show-toplevel)"
    dotnet run \
        --project Fantasy.Packages/Fantasy.ProtocolExportTool/Fantasy.ProtocolExportTool.csproj \
        -- export --silent \
        --config Fantasy.Packages/Fantasy.ProtocolExportTool/ExporterSettings.json

    if [ $? -eq 0 ]; then
        git add Examples/**/Generate/NetworkProtocol/
        git add Fantasy.Packages/*/Runtime/Protocol/
        git add Examples/Config/NetworkProtocol/OpCode.Cache
    else
        echo "协议生成失败,提交被中止"
        exit 1
    fi
fi
```

---

## 相关文档

- 📖 [网络协议目录结构说明](07-NetworkProtocol.md) - 协议定义规范
- ⚙️ [服务器配置](01-ServerConfiguration.md) - 服务器配置说明
- 🚀 [编写启动代码](02-WritingStartupCode.md) - 框架启动指南

---
