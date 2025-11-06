# 配置系统使用指南

本文档介绍如何在 Fantasy Framework 代码中读取和使用 `Fantasy.config` 配置文件的数据。

## 目录

- [配置系统概述](#配置系统概述)
- [配置类结构](#配置类结构)
- [机器配置（MachineConfig）](#机器配置machineconfig)
- [进程配置（ProcessConfig）](#进程配置processconfig)
- [世界配置（WorldConfig）](#世界配置worldconfig)
- [场景配置（SceneConfig）](#场景配置sceneconfig)
- [实用示例](#实用示例)
- [最佳实践](#最佳实践)

---

## 配置系统概述

Fantasy Framework 的配置系统在服务器启动时自动加载 `Fantasy.config` 文件，并将配置数据解析为对应的配置类。所有配置类都使用**单例模式**，可以在代码的任何地方访问。

### 配置加载流程

```
启动服务器
    ↓
读取 Fantasy.config
    ↓
解析 XML 配置
    ↓
Source Generator 生成代码 ✨
    ├── SceneType 常量类 (场景类型)
    ├── DatabaseName 常量类 (数据库名称)
    └── 其他生成代码...
    ↓
初始化配置类
    ↓
MachineConfigData.Instance ✅
ProcessConfigData.Instance ✅
WorldConfigData.Instance ✅
SceneConfigData.Instance ✅
    ↓
业务代码可以使用
```

### Source Generator 自动生成

框架使用 **Roslyn Source Generator** 在编译时自动生成辅助代码：

#### 1. SceneType 场景类型常量

根据 `Fantasy.config` 中的 `sceneTypeString` 和 `sceneType` 自动生成：

```csharp
// 自动生成的代码（位于 obj/Debug/net8.0/generated/ 目录）
public static class SceneType
{
    /// <summary>
    /// Addressable
    /// </summary>
    public const int Addressable = 1;

    /// <summary>
    /// Gate
    /// </summary>
    public const int Gate = 2;

    /// <summary>
    /// Map
    /// </summary>
    public const int Map = 3;

    /// <summary>
    /// Chat
    /// </summary>
    public const int Chat = 4;
}
```

**使用建议：**
- ✅ 使用常量：`SceneType.Gate` 而不是硬编码 `1`
- ✅ 提高代码可读性和可维护性
- ✅ 避免魔法数字，减少错误

#### 2. DatabaseName 数据库名称常量

根据 `Fantasy.config` 中所有 `<database>` 的 `dbName` 自动生成整数常量：

```csharp
// 自动生成的代码（位于 obj/Debug/net8.0/generated/ 目录）
public static class DatabaseName
{
    /// <summary>
    /// master
    /// </summary>
    public const int Master = 1;

    /// <summary>
    /// replica
    /// </summary>
    public const int Replica = 2;

    /// <summary>
    /// doc
    /// </summary>
    public const int Doc = 3;
}
```

**生成规则：**
- 扫描配置文件中所有 `<database>` 的 `dbName`
- 去重后按字母顺序排序，从 1 开始分配整数值
- 生成对应的常量（首字母大写）

**配置示例：**
```xml
<worlds>
    <world id="1" worldName="Game1">
        <database dbType="MongoDB" dbName="master" dbConnection="..." />
        <database dbType="MongoDB" dbName="replica" dbConnection="..." />
    </world>
    <world id="2" worldName="Game2">
        <database dbType="MongoDB" dbName="doc" dbConnection="..." />
    </world>
</worlds>
```

**使用建议：**
- ✅ 使用常量：`DatabaseName.Master` 而不是硬编码字符串 `"master"`
- ✅ 在代码中引用数据库时更加类型安全
- ✅ 重构时 IDE 可以自动追踪引用

### 配置类关系

```
MachineConfigData (机器配置管理)
    └── List<MachineConfig>          # 所有机器配置

ProcessConfigData (进程配置管理)
    └── List<ProcessConfig>          # 所有进程配置
           └── MachineId → 引用 MachineConfig.Id

WorldConfigData (世界配置管理)
    └── List<WorldConfig>            # 所有世界配置
           └── DatabaseConfig[]      # 数据库配置数组

SceneConfigData (场景配置管理)
    └── List<SceneConfig>            # 所有场景配置
           ├── ProcessConfigId → 引用 ProcessConfig.Id
           └── WorldConfigId → 引用 WorldConfig.Id
```

---

## 配置类结构

### 通用访问模式

所有配置类都遵循相同的访问模式：

```csharp
// 1. 通过单例访问
var instance = XxxConfigData.Instance;

// 2. 获取单个配置（找不到抛异常）
var config = instance.Get(id);

// 3. 安全获取单个配置（找不到返回 false）
if (instance.TryGet(id, out var config))
{
    // 配置存在
}

// 4. 遍历所有配置
foreach (var config in instance.List)
{
    // 处理每个配置
}
```

---

## 机器配置（MachineConfig）

### 配置类定义

```csharp
// 单例管理类
public sealed class MachineConfigData
{
    public static MachineConfigData Instance { get; }     // 单例实例
    public List<MachineConfig> List { get; }              // 所有配置

    public MachineConfig Get(uint id);                    // 获取配置（找不到抛异常）
    public bool TryGet(uint id, out MachineConfig config); // 安全获取
}

// 配置数据类
public sealed class MachineConfig
{
    public uint Id { get; set; }              // 机器ID
    public string OuterIP { get; set; }       // 外网IP
    public string OuterBindIP { get; set; }   // 外网绑定IP
    public string InnerBindIP { get; set; }   // 内网绑定IP
}
```

### 使用示例

```csharp
using Fantasy.Platform.Net;

// 示例1：获取指定机器的配置
var machineConfig = MachineConfigData.Instance.Get(machineId: 1);
Log.Info($"机器1的外网IP: {machineConfig.OuterIP}");
Log.Info($"机器1的内网绑定IP: {machineConfig.InnerBindIP}");

// 示例2：安全获取机器配置
if (MachineConfigData.Instance.TryGet(machineId: 999, out var config))
{
    Log.Info($"找到机器999: {config.OuterIP}");
}
else
{
    Log.Warning("机器999不存在");
}

// 示例3：遍历所有机器
foreach (var machine in MachineConfigData.Instance.List)
{
    Log.Info($"机器 {machine.Id}: 外网IP={machine.OuterIP}, 内网IP={machine.InnerBindIP}");
}

// 示例4：根据进程配置查找机器
var processConfig = ProcessConfigData.Instance.Get(processId: 1);
var machineOfProcess = MachineConfigData.Instance.Get(processConfig.MachineId);
Log.Info($"进程1运行在机器{machineOfProcess.Id}上，IP地址: {machineOfProcess.OuterIP}");
```

### 应用场景

| 场景 | 用途 | 代码示例 |
|------|------|----------|
| **网络连接** | 获取目标服务器的IP地址 | `var targetIP = MachineConfigData.Instance.Get(machineId).OuterIP;` |
| **服务器信息展示** | 显示所有服务器列表 | `foreach (var m in MachineConfigData.Instance.List) { ... }` |
| **运维监控** | 检查服务器是否在配置中 | `if (TryGet(id, out var config)) { ... }` |

---

## 进程配置（ProcessConfig）

### 配置类定义

```csharp
// 单例管理类
public sealed class ProcessConfigData
{
    public static ProcessConfigData Instance { get; }     // 单例实例
    public List<ProcessConfig> List { get; }              // 所有配置

    public ProcessConfig Get(uint id);                    // 获取配置（找不到抛异常）
    public bool TryGet(uint id, out ProcessConfig config); // 安全获取

    // 按启动分组遍历进程
    public IEnumerable<ProcessConfig> ForEachByStartupGroup(uint startupGroup);
}

// 配置数据类
public sealed class ProcessConfig
{
    public uint Id { get; set; }              // 进程ID
    public uint MachineId { get; set; }       // 所属机器ID
    public uint StartupGroup { get; set; }    // 启动分组
}
```

### 使用示例

```csharp
using Fantasy.Platform.Net;

// 示例1：获取进程配置
var processConfig = ProcessConfigData.Instance.Get(processId: 1);
Log.Info($"进程1运行在机器{processConfig.MachineId}上");
Log.Info($"启动分组: {processConfig.StartupGroup}");

// 示例2：获取进程所在的机器信息
var processConfig = ProcessConfigData.Instance.Get(processId: 1);
var machineConfig = MachineConfigData.Instance.Get(processConfig.MachineId);
Log.Info($"进程1运行在 {machineConfig.OuterIP}");

// 示例3：按启动分组遍历进程
// 获取所有启动分组为0的进程（第一批启动的进程）
foreach (var process in ProcessConfigData.Instance.ForEachByStartupGroup(startupGroup: 0))
{
    Log.Info($"启动组0包含进程: {process.Id}");
}

// 示例4：按顺序启动所有进程
uint maxStartupGroup = ProcessConfigData.Instance.List.Max(p => p.StartupGroup);
for (uint group = 0; group <= maxStartupGroup; group++)
{
    Log.Info($"正在启动第 {group} 批进程...");

    foreach (var process in ProcessConfigData.Instance.ForEachByStartupGroup(group))
    {
        Log.Info($"  启动进程 {process.Id} (机器 {process.MachineId})");
        // 在这里启动进程...
    }

    // 等待这一批进程启动完成
    await Task.Delay(1000);
}
```

### 应用场景

| 场景 | 用途 | 代码示例 |
|------|------|----------|
| **分组启动** | 按启动顺序控制服务启动 | `ForEachByStartupGroup(0)` |
| **进程定位** | 查找进程运行在哪台机器上 | `Get(processId).MachineId` |
| **运维管理** | 查询所有进程的配置信息 | `ProcessConfigData.Instance.List` |

---

## 世界配置（WorldConfig）

### 配置类定义

```csharp
// 单例管理类
public sealed class WorldConfigData
{
    public static WorldConfigData Instance { get; }     // 单例实例
    public List<WorldConfig> List { get; }              // 所有配置

    public WorldConfig Get(uint id);                    // 获取配置（找不到抛异常）
    public bool TryGet(uint id, out WorldConfig config); // 安全获取
}

// 配置数据类
public sealed class WorldConfig
{
    public uint Id { get; set; }                        // 世界ID
    public string WorldName { get; set; }               // 世界名称
    public DatabaseConfig[]? DatabaseConfig { get; set; } // 数据库配置数组
}

// 数据库配置（record 类型）
public sealed record DatabaseConfig(
    string? DbConnection,  // 数据库连接字符串
    string DbName,         // 数据库名称
    string DbType          // 数据库类型（MongoDB、PostgreSQL等）
);
```

### 使用示例

```csharp
using Fantasy.Platform.Net;

// 示例1：获取世界配置
var worldConfig = WorldConfigData.Instance.Get(worldId: 1);
Log.Info($"世界名称: {worldConfig.WorldName}");

// 示例2：访问数据库配置
var worldConfig = WorldConfigData.Instance.Get(worldId: 1);
if (worldConfig.DatabaseConfig != null)
{
    foreach (var dbConfig in worldConfig.DatabaseConfig)
    {
        Log.Info($"数据库: {dbConfig.DbName}");
        Log.Info($"  类型: {dbConfig.DbType}");
        Log.Info($"  连接: {dbConfig.DbConnection ?? "未配置"}");
    }
}

// 示例3：使用 DatabaseName 常量查找特定数据库（推荐）
var worldConfig = WorldConfigData.Instance.Get(worldId: 1);

// ✅ 推荐：使用 DatabaseName 常量
var masterDb = worldConfig.DatabaseConfig?
    .FirstOrDefault(db => db.DbName == "master"); // 或者根据其他条件查找

if (masterDb != null && !string.IsNullOrEmpty(masterDb.DbConnection))
{
    Log.Info($"主库: {masterDb.DbName} ({masterDb.DbType})");
    // 连接数据库...
}

// 示例4：获取特定类型的数据库配置
var worldConfig = WorldConfigData.Instance.Get(worldId: 1);
var mongoDb = worldConfig.DatabaseConfig?
    .FirstOrDefault(db => db.DbType == "MongoDB");

if (mongoDb != null && !string.IsNullOrEmpty(mongoDb.DbConnection))
{
    Log.Info($"MongoDB数据库: {mongoDb.DbName}");
    // 连接 MongoDB...
}

// 示例5：遍历所有世界
foreach (var world in WorldConfigData.Instance.List)
{
    Log.Info($"世界 {world.Id} ({world.WorldName}):");

    if (world.DatabaseConfig != null)
    {
        foreach (var db in world.DatabaseConfig)
        {
            Log.Info($"  - {db.DbName} ({db.DbType})");
        }
    }
}

// 示例6：从 Scene 获取世界配置
public class MyComponent : Entity
{
    public void DoSomething()
    {
        // 通过 Scene 获取当前世界的配置
        var worldConfig = Scene.World.WorldConfig;
        Log.Info($"当前世界: {worldConfig.WorldName}");

        // 访问数据库配置
        if (worldConfig.DatabaseConfig != null)
        {
            foreach (var db in worldConfig.DatabaseConfig)
            {
                Log.Info($"数据库: {db.DbName}");
            }
        }
    }
}

// 示例7：使用 DatabaseName 常量进行数据库选择（最佳实践）
public class DatabaseSelector
{
    /// <summary>
    /// 获取指定数据库的连接信息
    /// </summary>
    public DatabaseConfig GetDatabaseByName(WorldConfig worldConfig, string dbName)
    {
        if (worldConfig.DatabaseConfig == null)
        {
            throw new Exception($"世界 {worldConfig.WorldName} 没有配置数据库");
        }

        var db = worldConfig.DatabaseConfig.FirstOrDefault(d => d.DbName == dbName);
        if (db == null)
        {
            throw new Exception($"世界 {worldConfig.WorldName} 中找不到数据库 {dbName}");
        }

        return db;
    }

    /// <summary>
    /// 实际使用示例
    /// </summary>
    public void ConnectToDatabase()
    {
        var worldConfig = Scene.World.WorldConfig;

        // ✅ 使用 DatabaseName 常量的好处：
        // 1. 编译时检查，避免拼写错误
        // 2. IDE 智能提示
        // 3. 重构时自动更新所有引用

        // 注意：这里实际上 DatabaseName 是整数常量
        // 但我们通常通过字符串名称查找数据库配置
        // DatabaseName 常量主要用于业务逻辑中的标识和判断

        // 查找主库
        var masterDb = GetDatabaseByName(worldConfig, "master");
        Log.Info($"连接主库: {masterDb.DbConnection}");

        // 查找文档库
        var docDb = GetDatabaseByName(worldConfig, "doc");
        Log.Info($"连接文档库: {docDb.DbConnection}");
    }
}
```

### 应用场景

| 场景 | 用途 | 代码示例 |
|------|------|----------|
| **数据库连接** | 根据世界ID获取数据库连接信息 | `worldConfig.DatabaseConfig[0].DbConnection` |
| **多区服管理** | 区分不同的游戏世界 | `worldConfig.WorldName` |
| **数据库切换** | 动态选择不同的数据库 | `FirstOrDefault(db => db.DbType == "MongoDB")` |

---

## 场景配置（SceneConfig）

### 配置类定义

```csharp
// 单例管理类
public sealed class SceneConfigData
{
    public static SceneConfigData Instance { get; }     // 单例实例
    public List<SceneConfig> List { get; }              // 所有配置

    // 基本查询
    public SceneConfig Get(uint id);                    // 获取配置（找不到抛异常）
    public bool TryGet(uint id, out SceneConfig config); // 安全获取

    // 高级查询
    public List<SceneConfig> GetByProcess(uint processId);              // 根据进程ID获取所有场景
    public List<SceneConfig> GetSceneBySceneType(int sceneType);        // 根据场景类型获取所有场景
    public List<SceneConfig> GetSceneBySceneType(int world, int sceneType); // 根据世界和场景类型获取场景
}

// 配置数据类
public sealed class SceneConfig
{
    public uint Id { get; set; }                    // 场景ID
    public uint ProcessConfigId { get; set; }       // 所属进程ID
    public uint WorldConfigId { get; set; }         // 所属世界ID
    public string SceneRuntimeMode { get; set; }    // 运行模式（MainThread/MultiThread/ThreadPool）
    public string SceneTypeString { get; set; }     // 场景类型名称（Gate、Map等）
    public string NetworkProtocol { get; set; }     // 网络协议（TCP/KCP/WebSocket/HTTP）
    public int OuterPort { get; set; }              // 外部端口
    public int InnerPort { get; set; }              // 内部端口
    public int SceneType { get; set; }              // 场景类型数值
    public long RouteId { get; }                    // 路由ID（自动生成）
}
```

### 使用示例

#### 1. 基本查询

```csharp
using Fantasy.Platform.Net;

// 获取指定场景的配置
var sceneConfig = SceneConfigData.Instance.Get(sceneId: 1001);
Log.Info($"场景 {sceneConfig.Id}:");
Log.Info($"  类型: {sceneConfig.SceneTypeString}");
Log.Info($"  运行模式: {sceneConfig.SceneRuntimeMode}");
Log.Info($"  外部端口: {sceneConfig.OuterPort}");
Log.Info($"  内部端口: {sceneConfig.InnerPort}");
Log.Info($"  路由ID: {sceneConfig.RouteId}");

// 安全获取场景配置
if (SceneConfigData.Instance.TryGet(sceneId: 999, out var config))
{
    Log.Info($"找到场景: {config.SceneTypeString}");
}
else
{
    Log.Warning("场景999不存在");
}
```

#### 2. 根据进程查询场景

```csharp
// 获取某个进程上的所有场景
var scenesOnProcess = SceneConfigData.Instance.GetByProcess(processId: 1);
Log.Info($"进程1上有 {scenesOnProcess.Count} 个场景:");
foreach (var scene in scenesOnProcess)
{
    Log.Info($"  - 场景 {scene.Id} ({scene.SceneTypeString})");
}

// 应用示例：启动进程时，加载该进程上的所有场景
public async FTask StartProcess(uint processId)
{
    var scenesOnProcess = SceneConfigData.Instance.GetByProcess(processId);

    foreach (var sceneConfig in scenesOnProcess)
    {
        Log.Info($"正在启动场景: {sceneConfig.Id} ({sceneConfig.SceneTypeString})");

        // 根据配置创建场景
        var scene = await CreateScene(sceneConfig);
        Log.Info($"场景 {sceneConfig.Id} 启动完成");
    }
}
```

#### 3. 根据场景类型查询

```csharp
// ✅ 推荐：使用 SceneType 常量
var gateScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
Log.Info($"共有 {gateScenes.Count} 个 Gate 场景:");
foreach (var scene in gateScenes)
{
    Log.Info($"  - Gate场景 {scene.Id}, 端口: {scene.OuterPort}");
}

// ❌ 不推荐：硬编码数字
// var gateScenes = SceneConfigData.Instance.GetSceneBySceneType(1); // 不要这样写！

// 应用示例：随机选择一个 Gate 场景
public SceneConfig GetRandomGateScene()
{
    var gateScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
    if (gateScenes.Count == 0)
    {
        throw new Exception("没有可用的 Gate 场景");
    }

    var random = new Random();
    return gateScenes[random.Next(gateScenes.Count)];
}

// 应用示例：查询不同类型的场景
public void QueryScenesByType()
{
    var addressableScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Addressable);
    var gateScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
    var mapScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map);
    var chatScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat);

    Log.Info($"Addressable场景: {addressableScenes.Count} 个");
    Log.Info($"Gate场景: {gateScenes.Count} 个");
    Log.Info($"Map场景: {mapScenes.Count} 个");
    Log.Info($"Chat场景: {chatScenes.Count} 个");
}
```

#### 4. 根据世界和场景类型查询

```csharp
// ✅ 推荐：使用 SceneType 常量
var mapScenes = SceneConfigData.Instance.GetSceneBySceneType(world: 1, SceneType.Map);
Log.Info($"世界1中有 {mapScenes.Count} 个 Map 场景:");
foreach (var scene in mapScenes)
{
    Log.Info($"  - Map场景 {scene.Id}");
}

// 应用示例：跨世界场景查找
public List<SceneConfig> FindMapScenesInWorld(int worldId)
{
    return SceneConfigData.Instance.GetSceneBySceneType(worldId, SceneType.Map);
}

// 应用示例：为玩家分配地图场景
public async FTask<Scene> AssignMapScene(Player player)
{
    var worldId = player.WorldId;
    var mapScenes = SceneConfigData.Instance.GetSceneBySceneType(worldId, SceneType.Map);

    if (mapScenes.Count == 0)
    {
        throw new Exception($"世界 {worldId} 中没有可用的 Map 场景");
    }

    // 选择负载最低的场景（示例）
    var targetScene = mapScenes.OrderBy(s => GetSceneLoad(s.Id)).First();

    Log.Info($"为玩家 {player.Name} 分配场景 {targetScene.Id}");
    return await GetScene(targetScene.RouteId);
}

// 应用示例：查询不同世界的场景
public void QueryScenesAcrossWorlds()
{
    // 世界1的所有 Gate 场景
    var world1Gates = SceneConfigData.Instance.GetSceneBySceneType(world: 1, SceneType.Gate);

    // 世界2的所有 Map 场景
    var world2Maps = SceneConfigData.Instance.GetSceneBySceneType(world: 2, SceneType.Map);

    Log.Info($"世界1的Gate场景: {world1Gates.Count} 个");
    Log.Info($"世界2的Map场景: {world2Maps.Count} 个");
}
```

#### 5. 从 Scene 获取场景配置

```csharp
public class MyComponent : Entity
{
    public void DoSomething()
    {
        // 通过 Scene 获取当前场景的配置
        var sceneConfig = Scene.SceneConfig;

        Log.Info($"当前场景ID: {sceneConfig.Id}");
        Log.Info($"场景类型: {sceneConfig.SceneTypeString}");
        Log.Info($"运行模式: {sceneConfig.SceneRuntimeMode}");

        // 获取场景所属的世界配置
        var worldConfig = WorldConfigData.Instance.Get(sceneConfig.WorldConfigId);
        Log.Info($"所属世界: {worldConfig.WorldName}");

        // 获取场景所属的进程配置
        var processConfig = ProcessConfigData.Instance.Get(sceneConfig.ProcessConfigId);
        Log.Info($"所属进程: {processConfig.Id}");

        // 获取进程所属的机器配置
        var machineConfig = MachineConfigData.Instance.Get(processConfig.MachineId);
        Log.Info($"运行在机器: {machineConfig.OuterIP}");
    }
}
```

### 应用场景

| 场景 | 用途 | 方法 |
|------|------|------|
| **场景路由** | 根据 RouteId 查找场景 | `scene.SceneConfig.RouteId` |
| **负载均衡** | 获取某类型的所有场景，选择负载最低的 | `GetSceneBySceneType(SceneType.Map)` |
| **跨服通信** | 查找目标世界的特定场景 | `GetSceneBySceneType(world, SceneType.Gate)` |
| **服务启动** | 启动进程时加载该进程的所有场景 | `GetByProcess(processId)` |
| **动态扩展** | 遍历所有场景，动态添加新功能 | `SceneConfigData.Instance.List` |

**💡 重要提示：**
- ✅ **始终使用 `SceneType` 常量**，而不是硬编码数字
- ✅ 例如：`SceneType.Gate` 而不是 `1`
- ✅ 这样代码更易读、更安全，避免魔法数字

---

## 实用示例

### 示例1：跨场景消息转发

```csharp
/// <summary>
/// 将消息转发到 Map 场景
/// </summary>
public async FTask ForwardToMapScene(IMessage message)
{
    // ✅ 使用 SceneType 常量
    var mapScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map);

    if (mapScenes.Count == 0)
    {
        Log.Error("找不到 Map 场景");
        return;
    }

    // 选择第一个场景（也可以实现负载均衡）
    var targetSceneConfig = mapScenes[0];

    // 通过 RouteId 发送消息
    var response = await Scene.Call(targetSceneConfig.RouteId, message);
    Log.Info($"消息已转发到 Map 场景 {targetSceneConfig.Id}");
}

/// <summary>
/// 通用方法：转发到指定类型的场景
/// </summary>
public async FTask ForwardToSceneType(IMessage message, int sceneType)
{
    var targetScenes = SceneConfigData.Instance.GetSceneBySceneType(sceneType);

    if (targetScenes.Count == 0)
    {
        Log.Error($"找不到类型为 {sceneType} 的场景");
        return;
    }

    var targetSceneConfig = targetScenes[0];
    var response = await Scene.Call(targetSceneConfig.RouteId, message);
    Log.Info($"消息已转发到场景 {targetSceneConfig.Id}");
}

// 使用示例
public async FTask Example()
{
    // 转发到 Gate 场景
    await ForwardToSceneType(message, SceneType.Gate);

    // 转发到 Chat 场景
    await ForwardToSceneType(message, SceneType.Chat);
}
```

### 示例2：服务器信息统计

```csharp
/// <summary>
/// 统计服务器配置信息
/// </summary>
public class ServerStats
{
    public void PrintServerInfo()
    {
        Log.Info("========== 服务器配置统计 ==========");

        // 统计机器数量
        var machineCount = MachineConfigData.Instance.List.Count;
        Log.Info($"总机器数: {machineCount}");

        // 统计进程数量
        var processCount = ProcessConfigData.Instance.List.Count;
        Log.Info($"总进程数: {processCount}");

        // 统计世界数量
        var worldCount = WorldConfigData.Instance.List.Count;
        Log.Info($"总世界数: {worldCount}");

        // 统计场景数量
        var sceneCount = SceneConfigData.Instance.List.Count;
        Log.Info($"总场景数: {sceneCount}");

        // 按场景类型统计
        var sceneByType = SceneConfigData.Instance.List
            .GroupBy(s => s.SceneTypeString)
            .ToDictionary(g => g.Key, g => g.Count());

        Log.Info("\n场景类型分布:");
        foreach (var (sceneType, count) in sceneByType)
        {
            Log.Info($"  {sceneType}: {count} 个");
        }

        Log.Info("====================================");
    }
}
```

### 示例3：动态场景分配

```csharp
/// <summary>
/// 动态分配场景（负载均衡）
/// </summary>
public class SceneAllocator
{
    private readonly Dictionary<uint, int> _sceneLoads = new Dictionary<uint, int>();

    /// <summary>
    /// 为玩家分配一个 Map 场景
    /// </summary>
    public async FTask<long> AllocateMapScene(int worldId)
    {
        // ✅ 使用 SceneType 常量
        var mapScenes = SceneConfigData.Instance.GetSceneBySceneType(worldId, SceneType.Map);

        if (mapScenes.Count == 0)
        {
            throw new Exception($"世界 {worldId} 中没有可用的 Map 场景");
        }

        // 选择负载最低的场景
        var bestScene = mapScenes
            .OrderBy(s => GetSceneLoad(s.Id))
            .First();

        // 更新负载统计
        _sceneLoads[bestScene.Id] = GetSceneLoad(bestScene.Id) + 1;

        Log.Info($"分配 Map 场景 {bestScene.Id}, 当前负载: {_sceneLoads[bestScene.Id]}");

        return bestScene.RouteId;
    }

    /// <summary>
    /// 为玩家分配 Gate 场景（负载均衡）
    /// </summary>
    public long AllocateGateScene()
    {
        var gateScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);

        if (gateScenes.Count == 0)
        {
            throw new Exception("没有可用的 Gate 场景");
        }

        // 选择负载最低的 Gate 场景
        var bestGate = gateScenes
            .OrderBy(s => GetSceneLoad(s.Id))
            .First();

        _sceneLoads[bestGate.Id] = GetSceneLoad(bestGate.Id) + 1;

        Log.Info($"分配 Gate 场景 {bestGate.Id}, 当前负载: {_sceneLoads[bestGate.Id]}");

        return bestGate.RouteId;
    }

    private int GetSceneLoad(uint sceneId)
    {
        return _sceneLoads.TryGetValue(sceneId, out var load) ? load : 0;
    }
}
```

### 示例4：配置验证和检查

```csharp
/// <summary>
/// 配置验证工具
/// </summary>
public class ConfigValidator
{
    /// <summary>
    /// 验证配置的完整性
    /// </summary>
    public bool ValidateConfig()
    {
        bool isValid = true;

        // 验证进程引用的机器是否存在
        foreach (var process in ProcessConfigData.Instance.List)
        {
            if (!MachineConfigData.Instance.TryGet(process.MachineId, out _))
            {
                Log.Error($"进程 {process.Id} 引用的机器 {process.MachineId} 不存在");
                isValid = false;
            }
        }

        // 验证场景引用的进程和世界是否存在
        foreach (var scene in SceneConfigData.Instance.List)
        {
            if (!ProcessConfigData.Instance.TryGet(scene.ProcessConfigId, out _))
            {
                Log.Error($"场景 {scene.Id} 引用的进程 {scene.ProcessConfigId} 不存在");
                isValid = false;
            }

            if (!WorldConfigData.Instance.TryGet(scene.WorldConfigId, out _))
            {
                Log.Error($"场景 {scene.Id} 引用的世界 {scene.WorldConfigId} 不存在");
                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>
    /// 检查端口冲突
    /// </summary>
    public bool CheckPortConflicts()
    {
        var portUsage = new Dictionary<(uint machineId, int port), uint>();
        bool hasConflict = false;

        foreach (var scene in SceneConfigData.Instance.List)
        {
            var process = ProcessConfigData.Instance.Get(scene.ProcessConfigId);
            var machineId = process.MachineId;

            // 检查外部端口
            if (scene.OuterPort > 0)
            {
                var key = (machineId, scene.OuterPort);
                if (portUsage.TryGetValue(key, out var existingSceneId))
                {
                    Log.Error($"端口冲突: 场景 {scene.Id} 和 {existingSceneId} 都使用机器 {machineId} 的端口 {scene.OuterPort}");
                    hasConflict = true;
                }
                else
                {
                    portUsage[key] = scene.Id;
                }
            }

            // 检查内部端口
            var innerKey = (machineId, scene.InnerPort);
            if (portUsage.TryGetValue(innerKey, out var existingInnerSceneId))
            {
                Log.Error($"端口冲突: 场景 {scene.Id} 和 {existingInnerSceneId} 都使用机器 {machineId} 的端口 {scene.InnerPort}");
                hasConflict = true;
            }
            else
            {
                portUsage[innerKey] = scene.Id;
            }
        }

        return !hasConflict;
    }
}
```

---

## 最佳实践

### 1. 优先使用 TryGet 而不是 Get

```csharp
// ❌ 不推荐：可能抛出异常
var config = SceneConfigData.Instance.Get(sceneId);

// ✅ 推荐：安全获取
if (SceneConfigData.Instance.TryGet(sceneId, out var config))
{
    // 使用 config
}
else
{
    Log.Warning($"场景 {sceneId} 不存在");
    // 处理错误情况
}
```

### 2. 缓存常用配置

如果某个配置会被频繁访问，建议在初始化时缓存：

```csharp
public class MyComponent : Entity
{
    private SceneConfig _sceneConfig;

    protected override void Awake()
    {
        // 缓存当前场景的配置
        _sceneConfig = Scene.SceneConfig;
    }

    public void DoSomething()
    {
        // 直接使用缓存的配置，避免重复查询
        Log.Info($"场景类型: {_sceneConfig.SceneTypeString}");
    }
}
```

### 3. 通过 Scene 访问配置

在 Entity 或 Component 中，优先通过 `Scene` 访问配置：

```csharp
public class MyComponent : Entity
{
    public void DoSomething()
    {
        // ✅ 推荐：通过 Scene 访问
        var sceneConfig = Scene.SceneConfig;
        var worldConfig = Scene.World.WorldConfig;

        // ❌ 不推荐：直接访问单例（除非必要）
        var sceneConfig2 = SceneConfigData.Instance.Get(Scene.Id);
    }
}
```

### 4. 使用 RouteId 进行场景通信

```csharp
// ✅ 推荐：使用 RouteId
var targetSceneConfig = SceneConfigData.Instance.GetSceneBySceneType(sceneType).First();
var response = await Scene.Call(targetSceneConfig.RouteId, message);

// ❌ 不推荐：手动构造 RouteId（容易出错）
var routeId = IdFactoryHelper.RuntimeId(...);
```

### 5. 配置驱动的设计

让业务逻辑依赖配置，而不是硬编码：

```csharp
// ❌ 不推荐：硬编码场景类型（魔法数字）
if (sceneType == 1) // 这个 1 是什么意思？Gate？Addressable？
{
    // Gate 场景逻辑
}

// ✅ 推荐：使用 SceneType 常量（最佳）
if (sceneConfig.SceneType == SceneType.Gate)
{
    // Gate 场景逻辑
    Log.Info("这是 Gate 场景");
}

// ✅ 也可以：使用字符串判断（但不如常量好）
if (sceneConfig.SceneTypeString == "Gate")
{
    // Gate 场景逻辑
}

// 完整示例：根据场景类型执行不同逻辑
public void HandleSceneLogic()
{
    var sceneConfig = Scene.SceneConfig;

    // ✅ 使用 SceneType 常量进行判断
    if (sceneConfig.SceneType == SceneType.Gate)
    {
        Log.Info("Gate 场景：处理玩家登录");
        // Gate 场景特有逻辑
    }
    else if (sceneConfig.SceneType == SceneType.Map)
    {
        Log.Info("Map 场景：处理游戏逻辑");
        // Map 场景特有逻辑
    }
    else if (sceneConfig.SceneType == SceneType.Chat)
    {
        Log.Info("Chat 场景：处理聊天消息");
        // Chat 场景特有逻辑
    }
}

// 更优雅的方式：使用 switch
public void HandleSceneLogicWithSwitch()
{
    var sceneConfig = Scene.SceneConfig;

    switch (sceneConfig.SceneType)
    {
        case SceneType.Gate:
            Log.Info("Gate 场景逻辑");
            break;

        case SceneType.Map:
            Log.Info("Map 场景逻辑");
            break;

        case SceneType.Chat:
            Log.Info("Chat 场景逻辑");
            break;

        case SceneType.Addressable:
            Log.Info("Addressable 场景逻辑");
            break;

        default:
            Log.Warning($"未知场景类型: {sceneConfig.SceneType}");
            break;
    }
}
```

### 6. 避免在循环中频繁查询配置

```csharp
// ❌ 不推荐：在循环中重复查询
for (int i = 0; i < 1000; i++)
{
    var config = SceneConfigData.Instance.Get(sceneId);
    // 使用 config
}

// ✅ 推荐：查询一次，重复使用
var config = SceneConfigData.Instance.Get(sceneId);
for (int i = 0; i < 1000; i++)
{
    // 使用 config
}
```

### 7. 在启动时验证配置

在服务器启动时，主动验证配置的完整性：

```csharp
public class OnApplicationStartEvent : EventSystem<OnApplicationStart>
{
    protected override void Handler(OnApplicationStart self)
    {
        // 验证配置
        var validator = new ConfigValidator();

        if (!validator.ValidateConfig())
        {
            Log.Error("配置验证失败，服务器即将退出");
            Environment.Exit(1);
        }

        if (!validator.CheckPortConflicts())
        {
            Log.Error("端口冲突，服务器即将退出");
            Environment.Exit(1);
        }

        Log.Info("配置验证通过");
    }
}
```

---

## 总结

Fantasy Framework 的配置系统提供了强大而灵活的配置管理功能：

### 核心特点

1. **单例模式**：所有配置类都是单例，全局可访问
2. **类型安全**：强类型的配置类，避免字符串魔法值
3. **Source Generator 自动生成**：自动生成 `SceneType`、`DatabaseName` 等常量类
4. **高级查询**：支持多种查询方式（按ID、按类型、按进程等）
5. **自动初始化**：框架启动时自动加载和初始化
6. **关系引用**：配置之间通过ID相互引用，支持级联查询

### 使用建议

- ✅ **使用 SceneType 常量**：用 `SceneType.Gate` 而不是硬编码 `1`
- ✅ 优先使用 `TryGet` 进行安全查询
- ✅ 通过 `Scene` 访问配置，而不是直接访问单例
- ✅ 缓存常用配置，避免重复查询
- ✅ 使用配置驱动的设计，而不是硬编码
- ✅ 在启动时验证配置的完整性

### Source Generator 生成的代码

框架会自动生成以下常量类，让代码更易读、更安全：

| 生成的类 | 用途 | 示例 |
|---------|------|------|
| `SceneType` | 场景类型常量 | `SceneType.Gate`、`SceneType.Map` |
| `DatabaseName` | 数据库名称常量 | `DatabaseName.master`、`DatabaseName.doc` |

**重要提示：**
- ✅ 这些常量类在编译时自动生成，无需手动创建
- ✅ 生成的代码位于 `obj/Debug/net8.0/generated/` 目录
- ✅ 始终使用这些常量，避免魔法数字和硬编码字符串

### 相关文档

- 🔧 阅读 [协议定义指南](08-Protocol.md) 学习 .proto 文件 (待完善)
- 🌐 阅读 [网络消息处理](07-Network.md) 学习消息处理器 (待完善)
- 📖 阅读 [ECS 系统详解](06-ECS.md) 学习实体组件系统 (待完善)

---
