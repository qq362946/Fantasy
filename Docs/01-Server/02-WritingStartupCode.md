# 编写启动代码 - 服务器端

本指南将介绍如何编写服务器端 (.NET) 的 Fantasy 框架启动代码,包括:
- `AssemblyHelper` 的作用和实现
- `[ModuleInitializer]` 与 Source Generator 的工作原理
- 服务器启动代码的编写和调试

> **📌 提示:** 如果你正在使用 Unity 客户端,请阅读 [编写启动代码 - Unity 客户端](../02-Unity/01-WritingStartupCode-Unity.md)

---

## 目录

- [前置步骤](#前置步骤)
- [什么是 AssemblyHelper?](#什么是-assemblyhelper)
  - [推荐放置位置](#📂-推荐放置位置)
  - [核心功能](#核心功能)
  - [为什么需要 AssemblyHelper?](#为什么需要-assemblyhelper)
- [AssemblyHelper 源码解析](#assemblyhelper-源码解析)
  - [代码关键点解析](#代码关键点解析)
  - [AssemblyMarker 命名规则](#assemblymarker-命名规则)
- [编写服务器启动代码](#编写服务器启动代码)
  - [准备工作](#准备工作)
  - [基础启动代码](#基础启动代码)
  - [启动流程详解](#启动流程详解)
  - [运行服务器](#运行服务器)
- [常见问题](#常见问题)

---

## 前置步骤

在开始编写服务器启动代码之前,请确保已完成以下步骤:

1. ✅ 已配置好项目结构(例如:Server、Server.Entity、Server.Hotfix)
2. ✅ 已安装 Fantasy Framework(NuGet 包或源码引用)
3. ✅ 已创建 `Fantasy.config` 配置文件

如果你还没有完成这些步骤,请先阅读:
- [快速开始 - 服务器端](../00-GettingStarted/01-QuickStart-Server.md)
- [Fantasy.config 配置文件详解](01-ServerConfiguration.md)

---

## 什么是 AssemblyHelper?

`AssemblyHelper` 是**程序集加载辅助类**需要自己实现框架并没有提供,负责在服务器启动时正确加载和初始化所有程序集。

### 📂 推荐放置位置

**建议将 `AssemblyHelper` 定义在直接引用 Fantasy Framework 的项目中。**

**原因:**
- `AssemblyHelper` 需要触发程序集的强制加载
- 将 `AssemblyHelper` 放在需要加载的项目中(如 Entity 项目),可以直接通过反射访问当前程序集
- 框架提供了 `Assembly.EnsureLoaded()` 扩展方法来简化加载流程
- 入口项目(如 `Server`)通过引用该项目即可使用 `AssemblyHelper`

**项目结构示例:**
```
YourSolution/
├── Server/                      # 入口项目
│   └── Program.cs              # 调用 AssemblyHelper.Initialize()
│
├── Server.Entity/               # Entity 项目(直接引用 Fantasy)
│   ├── AssemblyHelper.cs       # ✅ 在这里定义 AssemblyHelper
│   ├── Fantasy.config
│   └── Components.cs
│
└── Server.Hotfix/               # Hotfix 项目
    └── MessageHandlers.cs
```

### 核心功能

1. **触发 Entity 程序集加载**
   - .NET 运行时采用**延迟加载机制**:如果代码中不使用程序集的类型,程序集不会被加载
   - `AssemblyHelper` 通过 `typeof(AssemblyHelper).Assembly.EnsureLoaded()` 获取当前程序集并强制触发加载
   - `EnsureLoaded()` 是框架提供的 Assembly 扩展方法,用于触发 `ModuleInitializer` 执行
   - 确保 Source Generator 生成的注册代码被执行

2. **支持 Hotfix 程序集热重载**
   - 使用 `AssemblyLoadContext` 加载 Hotfix 程序集
   - 支持动态卸载和重新加载(`Unload()` + `LoadFromStream()`)
   - 加载后通过 `assembly.EnsureLoaded()` 强制触发初始化
   - 适用于开发环境的热更新场景

3. **初始化框架注册系统**
   - 触发 `ModuleInitializer` 执行
   - 注册实体系统、消息处理器、事件处理器等
   - 建立框架运行所需的各种映射关系

### 为什么需要 AssemblyHelper?

**问题背景:**

.NET 运行时默认使用**延迟加载**策略:
```csharp
// 如果你的代码中没有显式使用程序集中的类型
// 那么即使项目引用了该程序集,运行时也不会加载它
// 这会导致 Source Generator 生成的注册代码无法执行
```

**AssemblyHelper 解决方案:**

```csharp
// AssemblyHelper.Initialize() 会:
// 1. 通过 typeof(AssemblyHelper).Assembly.EnsureLoaded() 强制触发 Entity 程序集加载
// 2. 执行 Source Generator 生成的 ModuleInitializer
// 3. 将所有系统、处理器、事件等注册到框架中
```

---

## AssemblyHelper 源码解析

以下是 `AssemblyHelper` 的完整源码(位于 `/Examples/Server/Entity/AssemblyHelper.cs`):

> **📌 重要提示:**
> 1. **文件位置**:建议将此文件创建在直接引用 Fantasy Framework 的项目中(如 `Server.Entity/AssemblyHelper.cs`)
> 2. **简化实现**:新版本使用 `Assembly.EnsureLoaded()` 扩展方法,无需手动查找和调用 `AssemblyMarker` 类,更加简洁和通用

```csharp
using System.Runtime.Loader;
using Fantasy.Generated;
using Fantasy.Helper;

namespace Fantasy
{
    public static class AssemblyHelper
    {
        private const string HotfixDll = "Hotfix";
        private static AssemblyLoadContext? _assemblyLoadContext = null;

        public static void Initialize()
        {
            LoadEntityAssembly();
            LoadHotfixAssembly();
        }

        private static void LoadEntityAssembly()
        {
            // .NET 运行时采用延迟加载机制，如果代码中不使用程序集的类型，程序集不会被加载
            // 执行一下，触发运行时强制加载从而自动注册到框架中
            // 因为AssemblyHelper代码在Entity项目里，所以需要获取这个项目的Assembly
            // 然后调用EnsureLoaded方法强制加载一下
            typeof(AssemblyHelper).Assembly.EnsureLoaded();
        }

        public static System.Reflection.Assembly LoadHotfixAssembly()
        {
            if (_assemblyLoadContext != null)
            {
                _assemblyLoadContext.Unload();
                System.GC.Collect();
            }

            _assemblyLoadContext = new AssemblyLoadContext(HotfixDll, true);
            var dllBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, $"{HotfixDll}.dll"));
            var pdbBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, $"{HotfixDll}.pdb"));
            var assembly = _assemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
            // 强制触发 ModuleInitializer 执行
            // AssemblyLoadContext.LoadFromStream 只加载程序集到内存，不会自动触发 ModuleInitializer
            // 必须访问程序集中的类型才能触发初始化，这里通过反射调用生成的 AssemblyMarker
            // 注意：此方法仅用于热重载场景（JIT），Native AOT 不支持动态加载
            // 拿到Assembly就用EnsureLoaded()方法强制触发
            assembly.EnsureLoaded();
            return assembly;
        }
    }
}
```

### 代码关键点解析

| 方法 | 作用 | 关键技术 |
|------|------|---------|
| `Initialize()` | 启动时调用,初始化所有程序集 | 组合调用 Entity 和 Hotfix 加载 |
| `LoadEntityAssembly()` | 强制加载 Entity 程序集 | `typeof(AssemblyHelper).Assembly.EnsureLoaded()` - 获取当前程序集并触发加载 |
| `LoadHotfixAssembly()` | 动态加载 Hotfix 程序集(支持热重载) | `AssemblyLoadContext` + `LoadFromStream` + `assembly.EnsureLoaded()` |

**重要说明:**

1. **Assembly.EnsureLoaded() 扩展方法**
   - 这是框架提供的 Assembly 扩展方法(位于 `Fantasy.Helper` 命名空间)
   - 内部会自动查找并触发 Source Generator 生成的 `ModuleInitializer`
   - 相比旧版本手动查找 `AssemblyMarker` 类和反射调用,更加简洁和可靠
   - 适用于任何程序集,无需关心程序集名称

2. **为什么使用 typeof(AssemblyHelper).Assembly?**
   - `AssemblyHelper` 类定义在 Entity 项目中
   - `typeof(AssemblyHelper).Assembly` 会返回 Entity 程序集的引用
   - 这样可以获取到当前所在的程序集,无需硬编码程序集名称

3. **热重载机制**
   - 使用 `AssemblyLoadContext` 的 `isCollectible: true` 参数
   - 支持运行时卸载(`Unload()`)和重新加载
   - **仅适用于 JIT 模式**,Native AOT 不支持动态加载
---

## 编写服务器启动代码

### 准备工作

在编写启动代码之前,请确保:
1. ✅ 已在 Entity 项目(如 `Server.Entity`)中创建 `AssemblyHelper.cs` 文件
2. ✅ 入口项目(如 `Server`)已引用 Entity 项目

### 基础启动代码

在你的服务器入口项目(如 `Server/Program.cs`)中编写以下代码:

```csharp
using Fantasy;

try
{
    // 1. 初始化程序集(触发 Source Generator 生成的代码)
    AssemblyHelper.Initialize();
    // 2. 启动 Fantasy 框架
    await Fantasy.Platform.Net.Entry.Start();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"服务器启动失败:{ex}");
    Environment.Exit(1);
}
```

**就这么简单!2 行核心代码即可启动服务器。**

---

### 启动流程详解

```
启动流程:
┌─────────────────────────────────────────────────────────────┐
│ 1. AssemblyHelper.Initialize()  [定义在 Server.Entity]     │
│    ├─ LoadEntityAssembly()                                 │
│    │   └─ typeof(AssemblyHelper).Assembly.EnsureLoaded()  │
│    │       └─ 触发 ModuleInitializer [Source Generator生成]│
│    │           └─ 注册实体系统、消息处理器、事件等           │
│    │                                                        │
│    └─ LoadHotfixAssembly()                                 │
│        ├─ AssemblyLoadContext.LoadFromStream()            │
│        └─ assembly.EnsureLoaded()                         │
│            └─ 触发 ModuleInitializer [Source Generator生成]│
│                └─ 注册热更新逻辑                            │
├─────────────────────────────────────────────────────────────┤
│ 2. Fantasy.Platform.Net.Entry.Start()  [框架入口]          │
│    ├─ 创建 Scene                                           │
│    ├─ 初始化网络监听                                        │
│    └─ 启动框架核心系统                                      │
└─────────────────────────────────────────────────────────────┘
```

---

### 运行服务器

**构建并运行**

进入你的服务器入口项目目录:

```bash
cd Server
dotnet build
dotnet run
```

**预期输出**

如果一切正常,你会看到类似以下的输出:

```
[INFO] 加载程序集:Entity
[INFO] 加载程序集:Hotfix
[INFO] Fantasy.Net 初始化完成
[INFO] 场景创建:SceneId=1001, SceneType=Gate
[INFO] Gate 场景监听:0.0.0.0:20000 (KCP)
[INFO] 服务器启动完成
```

---

## 常见问题

### Q1: 为什么必须调用 AssemblyHelper.Initialize()?

**原因:**

.NET 运行时的延迟加载机制会导致:
- 如果你的代码中不显式使用 Entity 程序集的类型,程序集不会被加载
- Source Generator 生成的注册代码在 `ModuleInitializer` 中,需要程序集加载后才会执行
- 不调用 `Initialize()` 会导致框架无法找到任何注册的系统、处理器、事件等

**解决方案:**

在 `Entry.Start()` 之前调用 `AssemblyHelper.Initialize()`,确保所有程序集被正确加载。

---

### Q2: 找不到 EnsureLoaded 方法或 Fantasy.Helper 命名空间

**错误信息:**
```
error CS1061: 'Assembly' does not contain a definition for 'EnsureLoaded'
error CS0234: The type or namespace name 'Helper' does not exist in the namespace 'Fantasy'
```

**原因:**
- 缺少 `using Fantasy.Helper;` 引用
- Fantasy Framework 版本过旧,不包含 `Assembly.EnsureLoaded()` 扩展方法
- Source Generator 没有正确生成代码
- 项目中未定义 `FANTASY_NET` 宏(仅源码引用)

**解决:**

1. **确保引用了 Fantasy.Helper 命名空间**:
   ```csharp
   using System.Runtime.Loader;
   using Fantasy.Generated;
   using Fantasy.Helper;  // ← 确保添加这一行
   ```

2. **检查 Fantasy Framework 版本**:
   - 确保使用最新版本的 Fantasy Framework
   - 如果是 NuGet 包,更新到最新版本
   - 如果是源码引用,拉取最新代码

3. **检查 Source Generator 引用**(源码引用时):
   ```xml
   <ItemGroup>
       <ProjectReference Include="path/to/Fantasy.SourceGenerator/Fantasy.SourceGenerator.csproj"
                         OutputItemType="Analyzer"
                         ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```

4. **检查宏定义**(源码引用时):
   ```xml
   <PropertyGroup>
       <DefineConstants>TRACE;FANTASY_NET</DefineConstants>
   </PropertyGroup>
   ```

5. **清理并重新构建**:
   ```bash
   dotnet clean
   dotnet build
   ```

6. **如果仍然报错,检查生成的代码**:
   ```bash
   # 查看生成的文件
   ls obj/Debug/net8.0/generated/Fantasy.SourceGenerator/
   ```

---

### Q3: Hotfix 程序集加载失败

**错误信息:**
```
FileNotFoundException: Could not find file 'Hotfix.dll'
```

**原因:**
- Hotfix 项目未正确构建
- Hotfix.dll 未复制到运行目录
- AssemblyHelper 中的文件名不匹配

**解决:**

1. **检查 Hotfix 项目是否构建成功**:
   ```bash
   dotnet build Server.Hotfix
   ls Server/bin/Debug/net8.0/Hotfix.dll
   ```

2. **确保 Hotfix 项目被 Server 项目引用**:
   ```xml
   <!-- Server.csproj -->
   <ItemGroup>
       <ProjectReference Include="../Server.Hotfix/Server.Hotfix.csproj" />
   </ItemGroup>
   ```

3. **如果你的 Hotfix 项目名称不是 "Hotfix"**,修改 `AssemblyHelper`:
   ```csharp
   private const string HotfixDll = "YourHotfixProjectName";
   ```

---

### Q4: 服务器启动后没有任何输出

**可能原因:**

1. **日志未配置或被抑制**
   ```csharp
   // 确保传递了日志实例
   var logger = new ConsoleLog();
   await Fantasy.Platform.Net.Entry.Start(logger);
   ```

2. **Fantasy.config 配置错误**
   - 检查配置文件是否存在
   - 检查 Scene 配置是否正确
   - 查看 [Fantasy.config 配置文件详解](01-ServerConfiguration.md)

3. **程序集未正确加载**
   - 在 `Initialize()` 后添加日志确认
   ```csharp
   AssemblyHelper.Initialize();
   Console.WriteLine("程序集初始化完成");
   ```

---

## 下一步

现在你已经掌握了服务器端的启动代码编写,接下来可以:

1. ⚙️ 阅读 [服务器启动命令行参数配置](03-CommandLineArguments.md) 学习如何配置命令行参数
2. 🎯 阅读 [OnCreateScene 事件使用指南](04-OnCreateScene.md) 学习如何在场景启动时初始化逻辑
3. 📖 阅读 [配置系统使用指南](05-ConfigUsage.md) 学习如何在代码中使用配置
4. 🎮 阅读 [ECS 系统](06-ECS.md) 学习实体组件系统(待完善)
5. 🌐 阅读 [网络开发](09-Network.md) 学习消息处理(待完善)
6. 🔧 阅读 [协议定义](11-Protocol.md) 学习 .proto 文件(待完善)
7. 📚 查看 `Examples/Server` 目录下的完整示例
8. 🎲 阅读 [编写启动代码 - Unity 客户端](../02-ClientGuide/01-WritingStartupCode-Unity.md) 了解Unity客户端启动流程

## 获取帮助

- **GitHub**: https://github.com/qq362946/Fantasy
- **文档**: https://www.code-fantasy.com/
- **Issues**: https://github.com/qq362946/Fantasy/issues

---
