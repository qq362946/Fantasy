# Fantasy.SourceGenerator

Fantasy 框架的增量源代码生成器（Incremental Source Generator），在编译时自动生成注册代码，消除反射开销，提升性能并支持 Native AOT。

## 功能特性

### ✅ 已实现的生成器

- **AssemblyInitializerGenerator**: 自动生成程序集初始化器（ModuleInitializer），在程序集加载时自动注册到框架
- **EntitySystemGenerator**: 自动生成 Entity System（Awake/Update/Destroy/Deserialize/LateUpdate）注册代码
- **EventSystemGenerator**: 自动生成 Event System（EventSystem/AsyncEventSystem/SphereEventSystem）注册代码
- **MessageDispatcherGenerator**: 自动生成消息调度器（网络协议、消息处理器、路由处理器）注册代码
- **EntityTypeCollectionGenerator**: 自动生成 Entity 类型集合，用于框架类型管理
- **ProtoBufGenerator**: 自动生成 ProtoBuf 类型注册代码，用于序列化系统

## 使用方法

### 自动集成（推荐）

Framework 已经完全集成了 Source Generator，开发者**无需手动配置**。

#### 工作流程

1. **编译时自动生成**: 当你构建项目时，Source Generator 会自动扫描代码并生成注册器
2. **自动注册**: `AssemblyInitializerGenerator` 生成的 `ModuleInitializer` 会在程序集加载时自动注册所有生成器到框架
3. **透明运行**: 框架内部自动使用生成的注册器，无需任何手动调用

#### 示例：添加一个 Entity System

```csharp
// 只需按照 Fantasy 框架的规范编写代码
public class MyEntityAwakeSystem : AwakeSystem<MyEntity>
{
    protected override void Awake(MyEntity self)
    {
        // 你的逻辑
    }
}
```

编译后，Source Generator 会自动：
- 生成 `EntitySystemRegistrar.g.cs` 包含此 System
- 在程序集加载时自动注册到框架
- 无需任何额外步骤

### 手动集成（高级用途）

如果你需要在自己的项目中引用 Source Generator：

```xml
<ItemGroup>
  <ProjectReference Include="..\Fantasy.SourceGenerator\Fantasy.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

确保项目定义了 `FANTASY_NET` 或 `FANTASY_UNITY` 预编译符号：

```xml
<PropertyGroup>
  <DefineConstants>FANTASY_NET</DefineConstants>
</PropertyGroup>
```

## 生成的代码示例

Source Generator 会在 `obj/.../generated/Fantasy.SourceGenerator/` 目录下自动生成多个注册器类：

### AssemblyInitializer.g.cs（核心）

```csharp
// 程序集初始化器 - 在程序集加载时自动执行
namespace Fantasy.Generated
{
    internal static class AssemblyInitializer
    {
        [ModuleInitializer]  // .NET 使用 ModuleInitializer
        // [RuntimeInitializeOnLoadMethod]  // Unity 使用此属性
        internal static void Initialize()
        {
            var assembly = typeof(AssemblyInitializer).Assembly;
            var assemblyManifestId = HashCodeHelper.ComputeHash64(assembly.GetName().Name);

            // 创建所有生成的注册器
            var protoBufRegistrar = new Fantasy.Generated.ProtoBufRegistrar();
            var eventSystemRegistrar = new Fantasy.Generated.EventSystemRegistrar();
            var entitySystemRegistrar = new Fantasy.Generated.EntitySystemRegistrar();
            var messageDispatcherRegistrar = new Fantasy.Generated.MessageDispatcherRegistrar();
            var entityTypeCollectionRegistrar = new Fantasy.Generated.EntityTypeCollectionRegistrar();

            // 一次性注册到框架
            Fantasy.Assembly.AssemblyManifest.Register(
                assemblyManifestId,
                assembly,
                protoBufRegistrar,
                eventSystemRegistrar,
                entitySystemRegistrar,
                messageDispatcherRegistrar,
                entityTypeCollectionRegistrar);
        }
    }

    // 用于强制加载程序集的标记类
    public static class MyAssembly_AssemblyMarker
    {
        public static void EnsureLoaded() { }
    }
}
```

### EntitySystemRegistrar.g.cs

```csharp
namespace Fantasy.Generated
{
    internal sealed class EntitySystemRegistrar : IEntitySystemRegistrar
    {
        public void RegisterSystems(
            Dictionary<Type, IAwakeSystem> awakeSystems,
            Dictionary<Type, IUpdateSystem> updateSystems,
            // ... 其他系统类型
        )
        {
            awakeSystems.Add(typeof(MyEntity), new MyEntityAwakeSystem());
            updateSystems.Add(typeof(MyEntity), new MyEntityUpdateSystem());
            // ... 自动注册所有发现的 System
        }
    }
}
```

### EventSystemRegistrar.g.cs

```csharp
namespace Fantasy.Generated
{
    internal sealed class EventSystemRegistrar : IEventSystemRegistrar
    {
        private MyEventHandler _myEventHandler = new MyEventHandler();

        public void RegisterSystems(
            OneToManyList<Type, IEvent> events,
            OneToManyList<Type, IEvent> asyncEvents,
            OneToManyList<Type, IEvent> sphereEvents)
        {
            events.Add(_myEventHandler.EventType(), _myEventHandler);
        }
    }
}
```

### MessageDispatcherRegistrar.g.cs

```csharp
namespace Fantasy.Generated
{
    internal sealed class MessageDispatcherRegistrar : IMessageDispatcherRegistrar
    {
        public void RegisterSystems(
            DoubleMapDictionary<uint, Type> networkProtocols,
            Dictionary<Type, Type> responseTypes,
            Dictionary<Type, IMessageHandler> messageHandlers,
            // ...
        )
        {
            var c2GLoginRequest = new C2G_LoginRequest();
            networkProtocols.Add(c2GLoginRequest.OpCode(), typeof(C2G_LoginRequest));
            responseTypes.Add(typeof(C2G_LoginRequest), typeof(G2C_LoginResponse));
        }
    }
}
```

## 性能对比

| 场景 | 反射方式 | Source Generator | 性能提升 |
|------|---------|------------------|---------|
| 程序集加载时注册 | ~50ms（100个类型） | ~1ms | **50x** |
| 启动时间 | 受反射影响 | 几乎无影响 | **显著** |
| 运行时实例化 | ~150ns/个 | ~3ns/个 | **50x** |
| 内存分配 | 反射元数据开销 | 无额外开销 | **更优** |
| Native AOT 兼容 | ❌ 不支持 | ✅ 完全支持 | - |
| IL2CPP (Unity) | ⚠️ 性能差 | ✅ 完美支持 | **必需** |

## 调试生成的代码

### 查看生成的代码

生成的代码位于：
```
<项目目录>/obj/<配置>/<目标框架>/generated/Fantasy.SourceGenerator/
```

例如：
```
obj/Debug/net8.0/generated/Fantasy.SourceGenerator/Fantasy.SourceGenerator.Generators.AssemblyInitializerGenerator/AssemblyInitializer.g.cs
obj/Debug/net8.0/generated/Fantasy.SourceGenerator/Fantasy.SourceGenerator.Generators.EntitySystemGenerator/EntitySystemRegistrar.g.cs
```

### IDE 支持

- **Visual Studio**: Dependencies → Analyzers → Fantasy.SourceGenerator → 展开查看生成的文件
- **JetBrains Rider**: Dependencies → Source Generators → Fantasy.SourceGenerator
- **VS Code**: 需要手动浏览 `obj/` 目录

### 启用详细日志

在 `.csproj` 中添加：
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)/GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

这会将生成的文件输出到 `obj/GeneratedFiles/` 目录，方便查看和调试。

## 兼容性

### 平台支持

| 平台 | 状态 | 说明 |
|------|------|------|
| .NET 8.0+ | ✅ 完全支持 | 使用 Roslyn 4.8.0 |
| .NET Framework 4.x | ❌ 不支持 | Source Generator 需要 .NET Standard 2.0+ |
| Unity 2020.2+ | ✅ 完全支持 | 使用 Roslyn 4.0.1（兼容版本） |
| Native AOT | ✅ 完全支持 | 无反射，完美兼容 AOT |
| IL2CPP | ✅ 完全支持 | Unity IL2CPP 必需 |

### 运行模式

- **开发环境**: Source Generator 自动生成，支持热重载（重新编译）
- **生产环境**: Source Generator 自动生成，零反射开销
- **热更新程序集**: 框架会检测可收集的 AssemblyLoadContext，支持程序集卸载和重载

## 技术特性

### 增量编译支持

所有生成器都实现了 `IIncrementalGenerator` 接口：
- ✅ 仅在相关代码变化时重新生成
- ✅ 大幅提升编译速度
- ✅ 支持部分代码更新

### 条件编译

生成器会根据预编译符号调整生成代码：
- `FANTASY_NET`: .NET 平台特定功能
- `FANTASY_UNITY`: Unity 平台特定功能
- 自动检测平台并生成对应代码

### 自动清理

- 程序集卸载时自动反注册（支持热更新）
- 实现 `IDisposable` 接口，支持资源释放
- 无内存泄漏风险

## 多版本构建支持

### Unity 版本 vs .NET 版本

此项目支持两种构建配置，使用不同版本的 Roslyn 编译器：

#### Unity 配置（用于 Unity Package）

```bash
dotnet build --configuration Unity
```

- **Roslyn 版本**: 4.0.1
- **用途**: Unity 2020.2+ 项目
- **兼容性**: Unity 2020.2 - Unity 2023.x
- **输出**: `bin/Unity/netstandard2.0/Fantasy.SourceGenerator.dll`

#### Release/Debug 配置（用于 .NET 项目）

```bash
dotnet build --configuration Release
```

- **Roslyn 版本**: 4.8.0
- **用途**: .NET 8/9 项目
- **兼容性**: .NET 8.0+
- **输出**: `bin/Release/netstandard2.0/Fantasy.SourceGenerator.dll`

### 为什么需要两个版本？

Unity 使用的 Roslyn 编译器版本较旧，Source Generator 引用的 Roslyn 版本必须**小于或等于**运行时编译器版本：

| 平台 | Roslyn 版本 | Source Generator 配置 |
|------|------------|---------------------|
| Unity 2020.2-2021.x | 3.8 - 4.0 | Unity (4.0.1) |
| Unity 2022.x | 4.3 | Unity (4.0.1) |
| Unity 2023.x | 4.6+ | Unity (4.0.1) |
| .NET 8.0 | 4.8+ | Release (4.8.0) |
| .NET 9.0 | 4.10+ | Release (4.8.0) |

### 自动化更新脚本

**更新 Unity Package:**
```bash
# macOS/Linux
./update-unity-source-generator.sh

# Windows
update-unity-source-generator.bat
```

这些脚本会自动使用 Unity 配置构建并更新到 Fantasy.Unity package。在项目的Tools/Update-Unity-Source-Generator下。

### 技术实现

在 `.csproj` 中使用条件 PackageReference：

```xml
<!-- Unity 版本 - Roslyn 4.0.1 -->
<ItemGroup Condition="'$(Configuration)' == 'Unity' OR '$(Configuration)' == 'UnityDebug'">
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.0.1" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.3" PrivateAssets="all" />
</ItemGroup>

<!-- .NET 版本 - Roslyn 4.8.0 -->
<ItemGroup Condition="'$(Configuration)' != 'Unity' AND '$(Configuration)' != 'UnityDebug'">
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
</ItemGroup>
```

### 故障排查

**CS9057 警告/错误:**
```
warning CS9057: The analyzer assembly references version 'X' of the compiler,
which is newer than the currently running version 'Y'.
```

**解决方案**: 确保使用 Unity 配置构建用于 Unity 的版本。

## 常见问题（FAQ）

### Q: 为什么看不到生成的代码？
A: 生成的代码在 `obj/` 目录下，需要编译项目后才能看到。可以在 IDE 的 Analyzers/Source Generators 节点中查看。

### Q: 可以禁用某个生成器吗？
A: 生成器会自动检测代码特征，如果你的项目中没有相关类型，对应的生成器不会生成代码。无需手动禁用。

### Q: Source Generator 会影响编译速度吗？
A: 首次编译会扫描所有代码，后续使用增量编译，仅在相关代码变化时重新生成，对编译速度影响很小。

### Q: Unity 项目如何使用？
A: Fantasy.Unity package 已经包含了兼容版本的 Source Generator（Roslyn 4.0.1），无需额外配置。

### Q: 支持热重载吗？
A: 支持。框架会检测 AssemblyLoadContext 的卸载事件，自动反注册。重新编译后自动注册新版本。

### Q: Native AOT 部署需要注意什么？
A: 无需特别注意，Source Generator 生成的代码不使用反射，完全兼容 Native AOT。

## 贡献指南

欢迎贡献代码！如果你想添加新的生成器：

1. 在 `Generators/` 目录下创建新的生成器类
2. 实现 `IIncrementalGenerator` 接口
3. 在 `AssemblyInitializerGenerator` 中添加对应的注册器接口
4. 更新本 README 文档

---

**维护者**: 初见
**更新日期**: 2025-10-19
**版本**: 1.0.0
