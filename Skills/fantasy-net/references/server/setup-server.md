# 服务器端：项目创建与集成 Fantasy 框架

Fantasy 通过 NuGet 包 `Fantasy-Net` 安装（已内置 SourceGenerator，无需单独引用）。

## Workflow

```
询问用户：全新项目 还是 现有项目集成？
│
├─► [全新项目]
│    ├─ Step 1：询问项目名称和目标框架版本（最低 net8.0）
│    ├─ Step 2：创建解决方案与三层项目结构
│    ├─ Step 3：配置 Entity / Hotfix / Main .csproj
│    └─ Step 4-7：→ 公共步骤
│
└─► [现有项目集成]
     ├─ Step 1：分析现有结构（.sln / .csproj / 目录）
     ├─ Step 2：检查框架版本
     │           ├─ [< net8.0] → 询问升级 → 升级所有关联项目 → dotnet build 验证
     │           └─ [>= net8.0] → 继续
     ├─ Step 3：确定集成目标项目（询问用户）
     ├─ Step 4：添加 Fantasy-Net 引用和 FANTASY_NET 宏
     └─ Step 5-8：→ 公共步骤
```

---

## 全新项目

### Step 1：确认项目名称与目标框架版本

- 项目名称（后续替换 `YourGame`）
- 框架版本（最低 net8.0），后续替换所有 `--framework` 和 `<TargetFramework>`

### Step 2：创建解决方案与三层项目结构

推荐三层 assembly 分离以支持热更新（Hotfix 可运行时重载，无需重启进程）：

```
YourGame/
├── YourGame.sln
└── APP/
    ├── Entity/   # 数据层：Entity、Component，直接引用 Fantasy-Net
    ├── Hotfix/   # 逻辑层：Handler、System，引用 Entity（支持热重载）
    └── Main/     # 入口层：Program.cs，引用 Entity + Hotfix
```

> 不需要热更新时 Entity 和 Hotfix 可合并，但建议始终分离。

```bash
dotnet new sln -n YourGame
dotnet new classlib -n Entity -o APP/Entity --framework net8.0
dotnet new classlib -n Hotfix -o APP/Hotfix --framework net8.0
dotnet new console  -n Main   -o APP/Main   --framework net8.0
dotnet sln add APP/Entity/Entity.csproj APP/Hotfix/Hotfix.csproj APP/Main/Main.csproj
```

### Step 3：配置 .csproj 文件

- **Entity.csproj**：参考 `templates/Entity.csproj`，替换 `<TargetFramework>`
- **Hotfix.csproj**：参考 `templates/Hotfix.csproj`，替换框架版本；项目名不同时调整 `ProjectReference` 路径
- **Main.csproj**：参考 `templates/Main.csproj`，替换框架版本；项目名不同时调整两个 `ProjectReference` 路径

---

## 现有项目集成

### Step 1：分析现有项目结构

读取 `.sln` 及所有 `.csproj`，确认入口项目（`OutputType=Exe`）、是否有分层、各项目 `<TargetFramework>`。

### Step 2：检查目标框架版本

若存在任何项目 < net8.0：询问用户是否升级 → 找出所有直接/间接引用链上的 `.csproj` 一并升级到同一版本 → 检查 `.sln` 确保不遗漏 → `dotnet build` 验证通过后继续。

### Step 3：确定集成目标项目

| 情况 | 建议 |
|---|---|
| 单一项目 | 直接在该项目添加引用 |
| 已有多 assembly | 最底层公共项目添加引用；确认 Handler assembly 的 SourceGenerator 可用 |

### Step 4：添加 Fantasy-Net 引用和编译宏

在目标 `.csproj` 中添加：

```xml
<PackageReference Include="Fantasy-Net" Version="*" />
```

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>TRACE;FANTASY_NET</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DefineConstants>TRACE;FANTASY_NET</DefineConstants>
</PropertyGroup>
```

---

## 公共步骤

以下步骤两条路均需执行。**放置位置**：直接引用 Fantasy-Net 包的项目根目录下（三层结构中为 `APP/Entity/`；现有项目集成时为用户选择的目标项目根目录）。

### Fantasy.config

参考 `templates/Fantasy.config` 创建（引用包后通常自动生成，若已存在直接修改）。模板为最小可用配置：单机器、单进程、单 World（MongoDB）、单 Gate Scene。需修改字段：
- `dbName` / `dbConnection` — 数据库名和连接串
- `outerPort` / `innerPort` — 对外和内部通信端口
- 详细字段说明见 `references/config.md`

### AssemblyHelper.cs

参考 `templates/AssemblyHelper.cs` 创建。.NET 延迟加载机制要求手动触发程序集加载，否则 SourceGenerator 注册不会生效。

### 日志系统

读取 `references/logging.md`，按其中流程询问用户并完成配置。

### Program.cs

在入口项目（`OutputType=Exe`）的 `Program.cs` 中添加，`logger` 由日志步骤决定：

```csharp
using Fantasy;

try
{
    AssemblyHelper.Initialize();
    await Fantasy.Platform.Net.Entry.Start(logger);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex}");
    Environment.Exit(1);
}
```
