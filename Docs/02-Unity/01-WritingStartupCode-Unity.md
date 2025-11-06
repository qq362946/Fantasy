# 编写启动代码 - Unity 客户端

本指南将介绍如何在 Unity 项目中编写 Fantasy 框架的启动代码,包括:
- **基础 Unity 启动流程**与 `[RuntimeInitializeOnLoadMethod]` 的使用
- **手动加载程序集** (`Assembly.Load`) 后如何触发 Fantasy 注册
- **HybridCLR 热更新**环境下的使用方法
- **与 .NET 服务器端的差异**说明

---

## 目录

- [前置步骤](#前置步骤)
- [Unity 与 .NET 的差异](#unity-与-net-的差异)
- [基础 Unity 启动流程](#基础-unity-启动流程)
  - [基础启动代码示例](#基础启动代码示例)
  - [启动流程详解](#启动流程详解)
  - [连接服务器示例](#连接服务器示例)
- [手动加载程序集 (Assembly.Load)](#手动加载程序集-assemblyload)
  - [为什么需要手动触发注册?](#为什么需要手动触发注册)
  - [手动触发注册](#手动触发注册)
  - [正确的加载流程](#正确的加载流程)
  - [适用场景](#适用场景)
- [HybridCLR 热更新环境](#hybridclr-热更新环境)
  - [程序集加载顺序](#1-程序集加载顺序)
  - [手动加载热更新程序集](#2-手动加载热更新程序集)
  - [link.xml 配置](#3-linkxml-配置)
  - [HybridCLR 配置](#4-hybridclr-配置)
  - [HybridCLR 完整示例](#hybridclr-完整示例)
- [常见问题](#常见问题)

---

## 前置步骤

在开始编写 Unity 启动代码之前,请确保已完成以下步骤:

1. ✅ 已安装 `Fantasy.Unity` 包
2. ✅ 项目中已定义 `FANTASY_UNITY` 宏

> **📌 提示:** 安装 `Fantasy.Unity` 包后,`Fantasy.SourceGenerator.dll` 会自动包含在包的 `RoslynAnalyzers/` 目录下 (`Packages/com.fantasy.unity/RoslynAnalyzers/`),Unity 会自动识别并使用,无需手动配置。

如果你还没有完成这些步骤,请先阅读:
- [快速开始 - Unity 客户端](../00-GettingStarted/02-QuickStart-Unity.md)

---

## Unity 与 .NET 的差异

在 Unity 环境下,Fantasy 框架的初始化机制与 .NET 服务器端有所不同:

| 特性 | .NET 服务器端 | Unity 客户端 |
|------|--------------|--------------|
| **程序集初始化** | `[ModuleInitializer]` | `[RuntimeInitializeOnLoadMethod]` |
| **初始化时机** | 程序集加载时自动执行 | Unity 引擎启动时自动执行 |
| **手动加载程序集** | 需要 `AssemblyHelper` | 不需要,框架自动处理 |
| **Source Generator** | 生成 `ModuleInitializer` | 生成 `RuntimeInitializeOnLoadMethod` |
| **支持 AOT** | 支持 Native AOT | 支持 IL2CPP |

### 核心差异说明

1. **.NET 使用 `[ModuleInitializer]`**
   - C# 9.0+ 特性,在程序集加载时自动执行
   - 需要手动触发程序集加载(通过 `AssemblyHelper`)

2. **Unity 使用 `[RuntimeInitializeOnLoadMethod]`**
   - Unity 引擎特性,在游戏启动时自动执行
   - 无需手动触发,Unity 会自动调用所有标记了此特性的方法

3. **Framework 自动处理**
   - Fantasy.SourceGenerator 会根据 `FANTASY_NET` 或 `FANTASY_UNITY` 宏自动生成对应的初始化代码
   - Unity 项目中,Source Generator 会生成带有 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]` 的初始化方法
   - 开发者无需关心底层差异,只需调用 `Entry.Initialize()` 即可

---

## 基础 Unity 启动流程

### 项目结构示例

```
Unity Project/
├── Assets/
│   └── Scripts/               # 你的游戏脚本
│       ├── GameEntry.cs       # 游戏入口脚本
│       ├── NetworkManager.cs  # 网络管理器
│       └── Entities/          # 实体和组件
│
└── Packages/
    └── com.fantasy.unity/     # Fantasy.Unity Package (UPM 包)
        ├── package.json       # Package 配置文件
        ├── Runtime/           # 运行时代码
        │   ├── Code           # Fantasy.Unity的核心代码
        │   └── Fantasy.Unity.asmdef
        └── RoslynAnalyzers/   # Source Generator (自动包含)
            └── Fantasy.SourceGenerator.dll
```

> **📌 说明:**
> - Fantasy.Unity 是一个标准的 Unity Package Manager (UPM) 包
> - 安装后会自动出现在 `Packages/` 目录下
> - Source Generator 和运行时 DLL 都包含在包中,无需手动配置

---

## 快速入门示例

下面通过一个简单的示例演示如何使用 Fantasy.Unity 连接服务器并发送消息。

### 1. 初始化框架并创建场景

```csharp
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class QuickStart : MonoBehaviour
{
    private Scene _scene;
    private Session _session;

    private void Start()
    {
        StartAsync().Coroutine();
    }

    private async FTask StartAsync()
    {
        // 1. 初始化 Fantasy 框架
        await Fantasy.Platform.Unity.Entry.Initialize();

        // 2. 创建一个 Scene (客户端场景)
        // Scene 是 Fantasy 框架的核心容器,所有功能都在 Scene 下运行
        // SceneRuntimeMode.MainThread 表示在 Unity 主线程运行
        _scene = await Scene.Create(SceneRuntimeMode.MainThread);

        Debug.Log("Fantasy 框架初始化完成!");
    }

    private void OnDestroy()
    {
        // 销毁 Scene,释放所有资源
        _scene?.Dispose();
    }
}
```

**代码说明:**

| 步骤 | 方法 | 说明 |
|------|------|------|
| 1 | `Entry.Initialize()` | 初始化 Fantasy 框架,加载必要的配置 |
| 2 | `Scene.Create()` | 创建客户端场景,返回 Scene 实例 |
| 3 | `scene.Dispose()` | 销毁场景,释放网络连接和所有资源 |

#### Fantasy.Unity 支持三种场景运行模式:

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| `SceneRuntimeMode.MainThread` | 在 Unity 主线程运行 | 与 Unity UI 交互、需要访问 Unity API |
| `SceneRuntimeMode.MultiThread` | 在独立线程运行 | 纯网络通信、不涉及 Unity API |
| `SceneRuntimeMode.ThreadPool` | 在线程池运行 | 短期任务、临时逻辑 |

**推荐使用:**

```csharp
// 客户端通常使用 MainThread 模式
_scene = await Scene.Create(SceneRuntimeMode.MainThread);
```

**注意事项:**

- ⚠️ `MultiThread` 和 `ThreadPool` 模式**不能**直接访问 Unity API
- ⚠️ 如需在子线程更新 UI,使用 `UnityMainThreadDispatcher` 或 `SynchronizationContext`

---

### 基础启动代码示例

在 Unity 中,创建一个 MonoBehaviour 脚本作为游戏入口:

```csharp
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    private Scene _scene;

    private void Start()
    {
        StartAsync().Coroutine();
    }

    private void OnDestroy()
    {
        // 销毁 Scene,清理所有网络和 Fantasy 相关资源
        _scene?.Dispose();
    }

    private async FTask StartAsync()
    {
        // 1. 初始化 Fantasy 框架
        // 此方法会自动:
        //   - 初始化日志系统(UnityLog)
        //   - 初始化序列化系统
        //   - 创建 Fantasy GameObject(DontDestroyOnLoad)
        //   - 注册 Update/LateUpdate 循环
        //   - WebGL 平台下初始化线程同步上下文
        await Fantasy.Platform.Unity.Entry.Initialize();

        // 2. 创建客户端 Scene
        // Scene 是客户端的核心容器,管理所有实体、组件和网络连接
        // 参数 arg: 传递给 OnSceneCreate 事件的自定义参数
        // 参数 sceneRuntimeMode: 场景运行模式(MainThread/MultiThread/ThreadPool)
        _scene = await Fantasy.Platform.Unity.Entry.CreateScene(
            arg: null,
            sceneRuntimeMode: SceneRuntimeMode.MainThread
        );

        Log.Debug("Fantasy 初始化完成!");
    }
}
```

---

### 启动流程详解

```
Unity 启动流程:
┌─────────────────────────────────────────────────────────────────┐
│ 1. Unity 引擎启动                                                │
│    └─ RuntimeInitializeLoadType.AfterAssembliesLoaded          │
│        └─ Fantasy.Generated.AssemblyInitializer.Initialize()   │
│            └─ AssemblyManifest.Register()                      │
│                ├─ 注册实体系统                                  │
│                ├─ 注册消息处理器                                │
│                ├─ 注册事件系统                                  │
│                └─ 注册网络协议                                  │
│                └─ ...                                         │
├─────────────────────────────────────────────────────────────────┤
│ 2. GameEntry.Start() [用户代码]                                 │
│    └─ StartAsync()                                             │
│        ├─ Entry.Initialize()                                   │
│        │   ├─ 初始化日志系统(UnityLog)                          │
│        │   ├─ 初始化序列化系统                                  │
│        │   ├─ 创建 Fantasy GameObject                          │
│        │   └─ WebGL: 初始化线程同步上下文                       │
│        │                                                        │
│        └─ Entry.CreateScene()                                  │
│            ├─ 创建 Scene 实例                                   │
│            └─ 触发 OnSceneCreate 事件                          │
└─────────────────────────────────────────────────────────────────┘
```

#### 关键点说明

1. **自动初始化**
   - Unity 引擎会在 `AfterAssembliesLoaded` 阶段自动调用 Source Generator 生成的初始化代码
   - 对于已加载的程序集,框架已自动处理注册

2. **初始化顺序**
   - `RuntimeInitializeOnLoadMethod` → `Entry.Initialize()` → `Entry.CreateScene()`
   - 确保在调用 `Entry.Initialize()` 之前,Source Generator 生成的代码已执行

3. **Scene 管理**
   - Unity 客户端通常只需要一个 Scene 实例
   - Scene 管理网络连接、实体、组件和事件
   - 在 `OnDestroy` 中正确释放 Scene 资源

---

## 手动加载程序集 (Assembly.Load)

### 为什么需要手动触发注册?

当你使用 `System.Reflection.Assembly.Load()` 手动加载 DLL 程序集时,**必须手动调用** `Assembly.EnsureLoaded()` 来触发 Fantasy 框架的注册。这是 Unity 的 `RuntimeInitializeOnLoadMethod` 机制决定的。

#### 核心原因

**1. RuntimeInitializeOnLoadMethod 只在 Unity 启动时执行一次**

```
Unity 引擎启动流程:
┌─────────────────────────────────────────────────────────┐
│ Unity 引擎启动                                           │
│  └─ 扫描所有已加载的程序集                               │
│      └─ 查找 [RuntimeInitializeOnLoadMethod] 标记的方法 │
│          └─ 自动调用这些方法 (只执行一次!)               │
└─────────────────────────────────────────────────────────┘
```

- 当 Unity 引擎启动时,会自动扫描所有已加载的程序集
- 对于标记了 `[RuntimeInitializeOnLoadMethod]` 的静态方法,Unity 会自动调用一次
- **这个过程只发生在引擎启动阶段,不会再次触发**

**2. 手动加载的 DLL 不会触发 RuntimeInitializeOnLoadMethod**

```csharp
// 使用 Assembly.Load() 加载程序集
var dllBytes = File.ReadAllBytes("MyHotfix.dll");
var assembly = System.Reflection.Assembly.Load(dllBytes);

// ⚠️ 问题: Unity 引擎不知道有新程序集被加载!
// ⚠️ 新加载程序集中的 [RuntimeInitializeOnLoadMethod] 不会被自动调用
// ⚠️ Source Generator 生成的初始化代码不会执行
```

**3. 不手动触发注册会导致的问题**

如果不调用 `Assembly.EnsureLoaded()`,Fantasy 框架无法识别新加载程序集中的:

- ❌ **实体系统**: `AwakeSystem<T>`, `UpdateSystem<T>`, `DestroySystem<T>` 等不会被注册
- ❌ **消息处理器**: `IMessageHandler` 实现类不会被识别,无法处理网络消息
- ❌ **事件处理器**: `IEvent` 实现类不会被注册,事件系统失效
- ❌ **网络协议**: OpCode 不会被注册,消息路由失败

---

### 手动触发注册

当使用 `Assembly.Load()` 手动加载程序集后，需要调用 `Assembly.EnsureLoaded()` 扩展方法来触发 Fantasy 框架的注册：

```csharp
// 加载程序集
var assembly = System.Reflection.Assembly.Load(dllBytes);

// ⚠️ 关键步骤: 手动触发 Fantasy 注册
// 这会执行该程序集中 Source Generator 生成的所有注册代码
assembly.EnsureLoaded();

Log.Debug($"已触发 Fantasy 注册: {assembly.GetName().Name}");
```

#### 注册机制说明

- Source Generator 会为每个包含 Fantasy 代码的程序集生成注册方法
- 调用 `Assembly.EnsureLoaded()` 扩展方法会自动执行该程序集中的所有注册代码
- 这包括实体系统、消息处理器、事件处理器、网络协议等的注册

---

### 正确的加载流程

以下是手动加载程序集的完整流程示例:

```csharp
using Fantasy;
using Fantasy.Async;
using System.IO;
using UnityEngine;

public class AssemblyLoaderExample : MonoBehaviour
{
    private Scene _scene;

    private void Start()
    {
        StartAsync().Coroutine();
    }

    private void OnDestroy()
    {
        _scene?.Dispose();
    }

    private async FTask StartAsync()
    {
        // 1️⃣ 加载自定义程序集 (必须第一步)
        await LoadCustomAssemblies();

        // 2️⃣ 初始化 Fantasy 框架
        await Fantasy.Platform.Unity.Entry.Initialize();

        // 3️⃣ 创建 Scene (最后一步)
        _scene = await Fantasy.Platform.Unity.Entry.CreateScene();

        Log.Debug("程序集加载完成,Fantasy 初始化成功!");
    }

    private async FTask LoadCustomAssemblies()
    {
        // 从资源加载 DLL 字节数据
        // 可以从 AssetBundle、StreamingAssets、网络下载等方式加载
        var dllPath = Path.Combine(Application.streamingAssetsPath, "MyHotfix.dll");
        byte[] dllBytes = File.ReadAllBytes(dllPath);

        // 加载程序集
        var assembly = System.Reflection.Assembly.Load(dllBytes);
        Log.Debug($"已加载程序集: {assembly.FullName}");

        // ⚠️ 关键步骤: 手动触发 Fantasy 注册
        // 调用 Assembly.EnsureLoaded() 来触发该程序集中的 Fantasy 框架注册
        // 如果不调用这个方法,Fantasy 无法识别新程序集中的:
        //   - 实体系统 (AwakeSystem, UpdateSystem 等)
        //   - 消息处理器 (IMessageHandler)
        //   - 事件处理器 (IEvent)
        //   - 网络协议 (OpCode 注册)
        assembly.EnsureLoaded();
        Log.Debug($"已触发 Fantasy 注册: {assembly.GetName().Name}");

        await FTask.CompletedTask;
    }
}
```

#### 加载顺序说明

```
正确的加载顺序:
┌────────────────────────────────────────────────────────┐
│ 1. Assembly.Load() + Assembly.EnsureLoaded()          │
│    └─ 加载自定义程序集并触发注册                       │
│                                                        │
│ 2. Entry.Initialize()                                  │
│    └─ 初始化 Fantasy 框架基础设施                      │
│                                                        │
│ 3. Entry.CreateScene()                                │
│    └─ 创建 Scene,此时所有系统已注册完成                │
└────────────────────────────────────────────────────────┘
```

> **⚠️ 重要:** 必须在 `Entry.Initialize()` 之前完成所有程序集的加载和注册,否则框架初始化时可能无法找到需要的系统

---

### 适用场景

手动加载程序集的方式适用于以下场景:

| 场景 | 说明 | 是否需要 Assembly.EnsureLoaded() |
|------|------|-----------------------------------|
| **热更新方案** | HybridCLR、ILRuntime 等热更新框架 | ✅ 需要 |
| **动态加载插件** | 运行时加载扩展功能 DLL | ✅ 需要 |
| **AssetBundle 加载** | 从 AssetBundle 中加载代码 DLL | ✅ 需要 |
| **网络下载代码** | 从服务器下载并加载 DLL | ✅ 需要 |
| **普通编译** | 直接编译到 APK/IPA 中的代码 | ❌ 不需要 (自动处理) |

---

## HybridCLR 热更新环境

### 什么是 HybridCLR?

[HybridCLR](https://github.com/focus-creative-games/hybridclr) 是一个**近乎完美的 Unity 全平台原生 C# 热更新解决方案**,支持在 iOS/Android/WebGL 等平台上动态加载和执行 C# 代码。

### HybridCLR 中使用 Fantasy 的注意事项

在 HybridCLR 环境下使用 Fantasy 框架需要特别注意以下几点:

---

#### 1. 程序集加载顺序

HybridCLR 将程序集分为两类:
- **AOT 程序集**: 编译时打包的程序集(无法热更新)
- **热更新程序集**: 运行时动态加载的程序集(可以热更新)

**推荐的程序集划分:**

```
Unity Project/
├── Assets/
│   ├── Scripts/                    # AOT 程序集
│   │   ├── GameEntry.cs           # 游戏入口
│   │   └── AssemblyLoader.cs      # 程序集加载器
│   │
│   └── HotUpdate/                  # 热更新程序集
│       ├── GameLogic.cs           # 游戏逻辑
│       ├── NetworkHandlers.cs     # 网络消息处理器
│       └── GameEntities.cs        # 游戏实体
```

---

#### 2. 手动加载热更新程序集

HybridCLR 环境下,热更新程序集需要手动加载并触发 Fantasy 注册。

> **📖 相关文档:** 如果你想详细了解为什么需要手动触发注册,请参考 [手动加载程序集 (Assembly.Load)](#手动加载程序集-assemblyload) 章节。

以下是 HybridCLR 的完整示例:

```csharp
using Fantasy;
using Fantasy.Assembly;
using Fantasy.Async;
using System.IO;
using UnityEngine;

public class HybridCLREntry : MonoBehaviour
{
    private Scene _scene;

    private void Start()
    {
        StartAsync().Coroutine();
    }

    private void OnDestroy()
    {
        _scene?.Dispose();
    }

    private async FTask StartAsync()
    {
        // 1. 加载热更新程序集
        // 注意: 必须在 Entry.Initialize() 之前加载
        await LoadHotUpdateAssemblies();

        // 2. 初始化 Fantasy 框架
        await Fantasy.Platform.Unity.Entry.Initialize();

        // 3. 创建 Scene
        _scene = await Fantasy.Platform.Unity.Entry.CreateScene();

        Log.Debug("HybridCLR + Fantasy 初始化完成!");
    }

    /// <summary>
    /// 加载热更新程序集
    /// </summary>
    private async FTask LoadHotUpdateAssemblies()
    {
        // 从 AssetBundle 或其他资源加载热更新 DLL
        // 这里以从 StreamingAssets 加载为例
        var hotUpdateDlls = new string[]
        {
            "GameLogic.dll",
            "NetworkHandlers.dll"
        };

        foreach (var dllName in hotUpdateDlls)
        {
            var dllPath = Path.Combine(Application.streamingAssetsPath, "HotUpdate", dllName);
            byte[] dllBytes = await LoadDllBytes(dllPath);

            // 加载程序集
            var assembly = System.Reflection.Assembly.Load(dllBytes);

            // ⚠️ 重要: 手动加载程序集必须手动触发 Fantasy 注册
            // RuntimeInitializeOnLoadMethod 只在 Unity 启动时自动执行一次
            // 手动加载的 DLL 不会触发 RuntimeInitializeOnLoadMethod
            // 调用 Assembly.EnsureLoaded() 来触发该程序集中的 Fantasy 框架注册
            assembly.EnsureLoaded();

            Log.Debug($"已加载热更新程序集: {dllName}");
        }
    }

    /// <summary>
    /// 加载 DLL 字节数据
    /// </summary>
    private async FTask<byte[]> LoadDllBytes(string path)
    {
        // 根据实际项目调整加载方式
        // 可以从 AssetBundle、网络下载、StreamingAssets 等加载

        // 示例: 从 StreamingAssets 同步加载
        if (File.Exists(path))
        {
            return File.ReadAllBytes(path);
        }

        // 示例: 从网络异步下载
        // using (var www = UnityWebRequest.Get(url))
        // {
        //     await www.SendWebRequest();
        //     return www.downloadHandler.data;
        // }

        throw new FileNotFoundException($"未找到 DLL 文件: {path}");
    }
}
```

---

#### 3. link.xml 配置

HybridCLR 使用 IL2CPP 编译,需要配置 `link.xml` 防止代码裁剪:

```xml
<linker>
    <!-- Fantasy 框架核心类型 -->
    <assembly fullname="Fantasy.Unity" preserve="all"/>

    <!-- Source Generator 生成的类型 -->
    <assembly fullname="Assembly-CSharp">
        <type fullname="Fantasy.Generated.AssemblyInitializer" preserve="all"/>
        <type fullname="Fantasy.Generated.EntitySystemRegistrar" preserve="all"/>
        <type fullname="Fantasy.Generated.EntityTypeCollectionRegistrar" preserve="all"/>
        <type fullname="Fantasy.Generated.EventSystemRegistrar" preserve="all"/>
        <type fullname="Fantasy.Generated.MessageHandlerResolverRegistrar" preserve="all"/>
        <type fullname="Fantasy.Generated.NetworkProtocolRegistrar" preserve="all"/>
    </assembly>

    <!-- 热更新程序集 - 保留 Fantasy.Generated 命名空间下的所有生成代码 -->
    <assembly fullname="GameLogic">
        <namespace fullname="Fantasy.Generated" preserve="all"/>
    </assembly>

    <!-- 保留所有网络协议类型 -->
    <assembly fullname="NetworkProtocol" preserve="all"/>
</linker>
```

---

#### 4. HybridCLR 配置

在 HybridCLR Settings 中配置热更新程序集:

```
HybridCLR Settings:
├── Hot Update Assemblies:
│   ├── GameLogic.dll
│   ├── NetworkHandlers.dll
│   └── GameEntities.dll
│
└── AOT Generic References:
    ├── System.Collections.Generic.List<YourEntity>
    └── Fantasy.Network.Session
```

> **📌 注意:** 记得在加载热更新程序集后调用 `assembly.EnsureLoaded()` 来触发 Fantasy 框架的注册

---

### HybridCLR 完整示例

以下是一个完整的 HybridCLR + Fantasy 示例项目结构:

```
Unity Project/
├── Assets/
│   ├── Scripts/                          # AOT 程序集
│   │   ├── HybridCLREntry.cs            # 入口脚本
│   │   └── AssemblyLoader.cs            # 程序集加载器
│   │
│   ├── HotUpdate/                        # 热更新程序集源码
│   │   ├── GameLogic/
│   │   │   ├── GameManager.cs
│   │   │   └── PlayerController.cs
│   │   └── Network/
│   │       ├── LoginHandler.cs
│   │       └── GameMessageHandler.cs
│   │
│   ├── StreamingAssets/
│   │   └── HotUpdate/                    # 热更新 DLL 存放目录
│   │       ├── GameLogic.dll
│   │       └── NetworkHandlers.dll
│   │
│   └── link.xml                          # IL2CPP 代码保留配置
│
└── Packages/
    └── com.fantasy.unity/               # Fantasy.Unity Package (UPM 包)
        └── RoslynAnalyzers/             # Source Generator (自动包含)
            └── Fantasy.SourceGenerator.dll
```

---

## 常见问题

### Q1: Unity 中需要 AssemblyHelper 吗?

**回答: 不需要。**

**原因:**

- Unity 环境下,Source Generator 会生成带有 `[RuntimeInitializeOnLoadMethod]` 特性的初始化方法
- Unity 引擎会在启动时自动调用这些方法
- 框架已自动处理程序集注册,开发者只需调用 `Entry.Initialize()` 即可

---

### Q2: 为什么手动加载程序集必须调用 Assembly.EnsureLoaded()?

**回答: 这是 Unity 的 RuntimeInitializeOnLoadMethod 机制决定的。**

**原因:**

1. **RuntimeInitializeOnLoadMethod 只在 Unity 启动时执行一次**
   - 当 Unity 引擎启动时,会自动扫描所有已加载的程序集
   - 对于标记了 `[RuntimeInitializeOnLoadMethod]` 的静态方法,Unity 会自动调用一次
   - 这个过程只发生在引擎启动阶段

2. **手动加载的 DLL 不会触发 RuntimeInitializeOnLoadMethod**
   - 使用 `Assembly.Load(dllBytes)` 加载程序集时,Unity 引擎不知道有新程序集加载
   - 新加载程序集中的 `[RuntimeInitializeOnLoadMethod]` 方法**不会被自动调用**
   - 因此,Source Generator 生成的初始化代码不会执行

3. **必须手动触发 Fantasy 注册**
   ```csharp
   var assembly = System.Reflection.Assembly.Load(dllBytes);

   // ⚠️ 必须手动调用,否则 Fantasy 框架无法识别新加载程序集中的:
   //   - 实体系统 (AwakeSystem, UpdateSystem 等)
   //   - 消息处理器 (IMessageHandler)
   //   - 事件处理器 (IEvent)
   //   - 网络协议 (OpCode 注册)
   assembly.EnsureLoaded();
   ```

**正确的加载流程:**

```csharp
// 1. 加载热更新程序集
var assembly = System.Reflection.Assembly.Load(dllBytes);

// 2. ⚠️ 手动触发注册 (必须步骤!)
assembly.EnsureLoaded();

// 3. 初始化 Fantasy 框架
await Fantasy.Platform.Unity.Entry.Initialize();

// 4. 创建 Scene
_scene = await Fantasy.Platform.Unity.Entry.CreateScene();
```

---

### Q3: HybridCLR 热更新程序集没有生效

**可能原因:**

1. **未手动触发 Fantasy 注册**
   ```csharp
   // 必须手动调用 assembly.EnsureLoaded()
   hotUpdateAssembly.EnsureLoaded();
   ```

2. **link.xml 配置不正确**
   - 检查是否保留了 `Fantasy.Generated` 命名空间下的所有类型
   - 检查是否保留了热更新程序集中的相关类型

3. **程序集加载顺序错误**
   - 必须在 `Entry.Initialize()` 之前加载热更新程序集
   - 必须在 `Entry.CreateScene()` 之前加载热更新程序集

---

### Q4: WebGL 平台连接失败

**可能原因:**

1. **协议类型错误**
   - WebGL 只支持 `NetworkProtocolType.WebSocket`
   - 不支持 KCP 和 TCP

2. **服务器地址格式错误**
   ```csharp
   // 正确: 框架会自动转换为 ws:// 或 wss://
   _scene.Connect("127.0.0.1:20000", NetworkProtocolType.WebSocket, ...);

   // 错误: 不要手动添加协议前缀
   // _scene.Connect("ws://127.0.0.1:20000", ...);
   ```

3. **HTTPS 参数设置错误**
   ```csharp
   // 服务器是 HTTPS 时,必须设置 isHttps: true
   _scene.Connect("example.com:443", NetworkProtocolType.WebSocket,
       ..., isHttps: true, ...);
   ```

---

### Q5: Source Generator 没有生成代码

**错误信息:**
```
error CS0246: The type or namespace name 'Fantasy.Generated' could not be found
```

**原因:**
- 项目中未定义 `FANTASY_UNITY` 宏
- `Fantasy.Unity` 包未正确安装或 Source Generator 未被 Unity 识别

**解决:**

1. **检查宏定义**:
   - 在 Unity 的 Player Settings → Scripting Define Symbols 中添加 `FANTASY_UNITY`

2. **检查 Fantasy.Unity 包是否正确安装**:
   - 在 Unity 的 Package Manager 中确认 `com.fantasy.unity` 包已安装
   - 检查 Source Generator 文件是否存在:
   ```bash
   ls Packages/com.fantasy.unity/RoslynAnalyzers/Fantasy.SourceGenerator.dll
   ```

3. **重新导入 Fantasy.Unity 包**:
   - 如果包或文件缺失,尝试重新安装 `Fantasy.Unity` 包

4. **清理并重新构建**:
   - Unity 菜单: Assets → Reimport All
   - 关闭 Unity 编辑器后重新打开

---

## 下一步

现在你已经掌握了 Unity 客户端的启动代码编写,接下来可以:

1. 📖 阅读 [编写启动代码 - 服务器端](../01-Server/02-WritingStartupCode.md) 了解服务器端启动流程
2. 🔧 阅读 [协议定义](11-Protocol.md) 学习 .proto 文件(待完善)
3. 🌐 阅读 [网络开发](09-Network.md) 学习消息处理(待完善)
4. 🎮 阅读 [ECS 系统](06-ECS.md) 学习实体组件系统(待完善)
5. 📚 查看 `Examples/Client/Unity` 目录下的完整示例

## 获取帮助

- **GitHub**: https://github.com/qq362946/Fantasy
- **文档**: https://www.code-fantasy.com/
- **Issues**: https://github.com/qq362946/Fantasy/issues

---
