# 日志系统

Fantasy 通过 `ILog` 接口抽象日志实现，任何实现该接口的类都可注册到框架中。

接口定义见 `templates/nlog/ILog.cs`。关键点：
- `Initialize(ProcessMode)` 在 `Entry.Start()` 内部调用，`processMode` 取值 `Develop`（开发，默认）或 `Release`（发布）
- 其余方法对应 Trace / Debug / Info / Warning / Error 五个日志级别，各有纯字符串和格式化两个重载

## 选择日志方式

**首先询问用户选择以下哪种方式，再执行对应步骤，不要同时输出全部三种的内容：**

| 选项 | 适用场景 |
|---|---|
| 1. 默认 ConsoleLog | 快速验证，无需额外配置 |
| 2. Fantasy.NLog（推荐） | 生产环境，按运行模式自动切换控制台/文件输出 |
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

Fantasy.NLog 基于 [NLog](https://nlog-project.org/)，根据 `ProcessMode` 自动切换控制台/文件输出：
- `Develop` 模式 → 仅输出到彩色控制台
- `Release` 模式 → 仅输出到按日期滚动的文件

### 安装

1. 安装 NLog NuGet 包（添加到直接引用 Fantasy-Net 的项目，通常是 `Entity.csproj`）：

```bash
dotnet add APP/Entity/Entity.csproj package NLog
```

2. 将 `templates/nlog/` 下的三个文件复制到同一项目目录（三层结构中为 `APP/Entity/`）：

```
APP/Entity/
├── NLog.cs          # 来自 templates/nlog/NLog.cs
├── NLog.config      # 来自 templates/nlog/NLog.config
└── NLog.xsd         # 来自 templates/nlog/NLog.xsd（可选，IDE 智能提示）
```

3. 在 `Entity.csproj` 中设置配置文件始终复制：

```xml
<ItemGroup>
  <PackageReference Include="NLog" Version="6.0.7" />
</ItemGroup>
<ItemGroup>
  <None Update="NLog.config">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="NLog.xsd">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
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

默认配置定义了两组规则，`Initialize()` 会根据 `ProcessMode` 移除不需要的一组：

| ruleName 前缀 | 输出目标 | 保留时机 |
|---|---|---|
| `Console*`（ConsoleTrace/Debug/Info/Warn/Error） | 彩色控制台 | `Develop` 模式 |
| `Server*`（ServerTrace/Debug/Info/Warn/Error） | 按日期分割的日志文件 | `Release` 模式 |

**自定义 ruleName 注意事项：** `NLog.cs` 中 `Initialize()` 通过 `LogManager.Configuration.RemoveRuleByName()` 按名称移除规则。修改了 `NLog.config` 中的 ruleName 后，必须同步修改 `NLog.cs` 中对应的名称，否则模式切换失效。

默认文件日志路径：`../Logs/Server/Server{yyyyMMdd}/{loggerName}.{appId}.{yyyyMMddHH}.{Level}.log`（`${basedir}` 指向可执行文件所在目录）。如需修改，编辑 `NLog.config` 中对应 target 的 `fileName` 属性。

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

注册流程：`Entry.Start(log)` → `Log.Initialize(log)` → `log.Initialize(processMode)`。之后整个进程中通过 `Log.Debug/Info/Error()` 等静态方法使用日志。

---

## 常见问题

**Q: Develop 模式下日志没有输出到文件**  
正常行为。`Initialize()` 在 Develop 模式下会移除所有 `Server*` 规则（文件目标），仅保留控制台。

**Q: 修改了 NLog.config 的 ruleName 后模式切换失效**  
需要同步修改 `NLog.cs` 的 `Initialize()` 方法中 `RemoveRuleByName()` 的参数。

**Q: 日志文件路径不对**  
默认 `${basedir}` 指向输出目录（`bin/Debug/net8.0/`），文件写到 `../Logs/`。修改 `NLog.config` 中 target 的 `fileName` 属性即可自定义路径。
