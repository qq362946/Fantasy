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

创建 `ExporterSettings.json` 配置文件:

```json
{
    "Export": {
        "NetworkProtocolDirectory": {
            "Value": "../../../Examples/Config/NetworkProtocol/",
            "Comment": "协议文件所在目录"
        },
        "NetworkProtocolServerDirectory": {
            "Value": "../../../Examples/Server/Entity/Generate/NetworkProtocol/",
            "Comment": "服务端代码输出目录"
        },
        "NetworkProtocolClientDirectory": {
            "Value": "../../../Examples/Client/Unity/Assets/Scripts/Hotfix/Generate/NetworkProtocol/",
            "Comment": "客户端代码输出目录"
        }
    }
}
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
```

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
- **打开工作区**: `文件 → 打开工作区` - 选择 NetworkProtocol 文件夹
- **自动保存**: 编辑器自动保存工作区状态(打开的文件、光标位置)
- **文件树**: 显示 Inner/Outer 文件夹下的 .proto 文件

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
- **路径配置**: `文件 → 导出设置` - 配置服务器/客户端输出路径
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

### 工作区配置文件

编辑器会在用户目录下保存配置:
- **位置**: `~/.fantasy-protocol-editor/workspace-config.json`
- **内容**: 工作区路径、打开的标签、光标位置、导出设置

```json
{
  "WorkspacePath": "/path/to/NetworkProtocol",
  "ServerOutputDirectory": "/path/to/server/output",
  "ClientOutputDirectory": "/path/to/client/output",
  "ExportToServer": true,
  "ExportToClient": true,
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
✅ Examples/Config/NetworkProtocol/**/*.proto        # 协议定义
✅ Examples/Config/NetworkProtocol/OpCode.Cache      # OpCode 缓存
✅ ExporterSettings.json                             # 配置文件
```

**不应该提交 (或可选):**
```
❌ **/Generate/NetworkProtocol/**/*.cs               # 生成的代码 (可选)
❌ ~/.fantasy-protocol-editor/workspace-config.json  # 编辑器配置
```

### CI/CD 集成

**GitHub Actions 示例:**

```yaml
name: Generate Protocol Code

on:
  push:
    paths:
      - 'Examples/Config/NetworkProtocol/**/*.proto'

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
          cd Fantasy.Packages/Fantasy.ProtocolExportTool
          dotnet run -- export --silent

      - name: Commit Changes
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add Examples/**/Generate/NetworkProtocol/
          git diff --quiet || git commit -m "chore: update protocol code"
          git push
```

### Pre-commit Hook

```bash
#!/bin/bash
# .git/hooks/pre-commit

if git diff --cached --name-only | grep -q "\.proto$"; then
    echo "检测到协议变更,正在生成代码..."
    cd Fantasy.Packages/Fantasy.ProtocolExportTool
    dotnet run -- export --silent

    if [ $? -eq 0 ]; then
        git add ../../Examples/**/Generate/NetworkProtocol/
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
