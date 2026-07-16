# 日志系统

Fantasy 通过 `ILog` 接口抽象日志实现，任何实现该接口的类都可注册到框架中。

接口定义见 `templates/nlog/ILog.cs`。关键点：
- `Initialize(string appId, ProcessMode processMode)` 在 `Entry.Start()` 内部调用；`appId` 是当前 Process ID
- Trace / Debug / Info / Warning / Error 各有普通、格式化、指定 Scene 名称三组重载

## 选择日志方式

**首先询问用户选择以下哪种方式，再执行对应步骤，不要同时输出全部三种的内容：**

| 选项 | 适用场景 |
|---|---|
| 1. 默认 ConsoleLog | 快速验证，无需额外配置 |
| 2. Fantasy.NLog（推荐） | 生产环境，按运行模式调整控制台输出与文件刷新 |
| 3. 自定义 ILog 实现 | 已有日志基础设施（Serilog、Log4Net 等） |

---

## 选项一：默认 ConsoleLog（无需任何额外配置）

不向 `Entry.Start()` 传入日志实例，框架自动使用内置 `ConsoleLog`，日志直接输出到标准输出。

```csharp
await Fantasy.Platform.Net.Entry.Start(); // 省略参数
```

适合快速原型验证；生产环境建议替换为文件日志。

---

## 选项二：Fantasy.NLog（推荐生产环境）

Fantasy.NLog 基于 [NLog](https://nlog-project.org/)，根据 `ProcessMode` 调整输出和文件刷新：

- `Develop`：彩色控制台和文件同时输出；文件立即刷新，便于调试
- `Release`：移除控制台规则，只输出文件；使用缓冲刷新降低 I/O 开销

### 安装

1. 安装 NLog NuGet 包（添加到直接引用 Fantasy-Net 的项目，通常是 `Entity.csproj`）：

```bash
dotnet add APP/Entity/Entity.csproj package NLog
```

2. 将 `templates/nlog/` 下的三个 NLog 文件复制到同一项目目录（三层结构中为 `APP/Entity/`）：

```
APP/Entity/
├── NLog.cs          # 来自 templates/nlog/NLog.cs
├── NLog.config      # 来自 templates/nlog/NLog.config
└── NLog.xsd         # 来自 templates/nlog/NLog.xsd（可选，IDE 智能提示）
```

3. 在 `Entity.csproj` 中设置配置文件始终复制：

```xml
<ItemGroup>
  <PackageReference Include="NLog" Version="6.1.3" />
</ItemGroup>
<ItemGroup>
  <None Update="NLog.config">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
  <None Update="NLog.xsd">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### 使用

```csharp
// Program.cs（入口项目）
AssemblyHelper.Initialize();
var logger = new Fantasy.NLog("Server"); // 参数为 NLog logger 名称，与 NLog.config 中的 name 对应
await Fantasy.Platform.Net.Entry.Start(logger);
```

### NLog.config 配置说明

默认配置定义了两组规则，`Initialize()` 在 Release 模式移除控制台规则：

| ruleName 前缀 | 输出目标 | 保留时机 |
|---|---|---|
| `Console*`（ConsoleTrace/Debug/Info/Warn/Error） | 彩色控制台 | 仅 `Develop` |
| `Server*`（ServerTrace/Debug/Info/Warn/Error） | 按日期、Process、Scene 分割文件 | `Develop` 和 `Release` |

**自定义 ruleName 注意事项：** `NLog.cs` 通过 `RemoveRuleByName()` 移除五条 `Console*` 规则。修改这些 ruleName 时必须同步修改 `NLog.cs`，否则 Release 模式仍会输出控制台。

默认文件日志路径：`../Logs/Server/{yyyyMMdd}/{appId}/{sceneName}.{appId}.{yyyyMMddHH}.{Level}.log`（`${basedir}` 指向可执行文件所在目录）。

不传 Scene 的 `Log.Info(...)` 等方法使用 `Log` 作为文件名前缀；传入 Scene 的 `Log.Info(scene, ...)` 会使用框架生成的 `{SceneType}_{SceneId}`，例如 `Map_1002`：

```csharp
Log.Info("Server started");
Log.Info(scene, "Player {0} entered", playerId);
```

---

## 选项三：自定义 ILog 实现

实现 `ILog` 接口后，以相同方式传入 `Entry.Start()`。

- 最小实现模板：`templates/nlog/CustomLog.cs`
- Serilog 集成示例：`templates/nlog/SerilogLog.cs`（含所需 NuGet 包注释）

---

## 注册方式（三种选项统一）

```csharp
// Program.cs
AssemblyHelper.Initialize();

ILog logger = new Fantasy.NLog("Server");   // 选项二
// ILog logger = new MyLog();               // 选项三（自定义）
// 省略参数 → 选项一（默认 ConsoleLog）

await Fantasy.Platform.Net.Entry.Start(logger);
```

注册流程：`Entry.Start(log)` → `Log.Initialize(appId, log)` → `log.Initialize(appId, processMode)`。之后整个进程中通过 `Log.Debug/Info/Error()` 等静态方法使用日志。

---

## 常见问题

**Q: Develop 模式为什么同时输出控制台和文件？**

这是当前默认行为。Develop 模式保留全部规则并启用文件立即刷新；Release 模式才移除控制台规则并使用缓冲刷新。

**Q: 修改了 NLog.config 的 ruleName 后模式切换失效**

如果修改的是 `Console*` 规则，需要同步修改 `NLog.cs` 的 `Initialize()` 中 `RemoveRuleByName()` 参数。

**Q: 日志文件路径不对**

默认 `${basedir}` 指向输出目录，文件写到 `../Logs/Server/{日期}/{appId}/`。修改 `NLog.config` 中 target 的 `fileName` 属性即可自定义路径。
