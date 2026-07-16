#if FANTASY_NET
using System;
using System.Collections.Generic;
using Fantasy.Platform.Net;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy;

/// <summary>
/// 将Control Center运行时协议快照转换为Fantasy.Net配置。
/// 所有配置验证通过后才发布，避免留下部分初始化状态。
/// </summary>
internal static class RuntimeConfigMapper
{
    /// <summary>
    /// 将Control Center配置快照映射为Fantasy.Net配置集合。
    /// 返回值使用ValueTuple，不产生额外包装对象。
    /// </summary>
    /// <param name="snapshot">已通过协议结构版本检查的运行时配置快照。</param>
    /// <returns>可交给 Fantasy.Net 配置容器发布的四类配置集合。</returns>
    internal static (List<MachineConfig> Machines, List<ProcessConfig> Processes, List<WorldConfig> Worlds, List<SceneConfig> Scenes) Map(RuntimeConfigSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        // 这里只检查协议结构，业务规则统一交给ConfigValidator。
        var machineDefinitions = snapshot.Machines
                                 ?? throw new InvalidOperationException(
                                     "Runtime configuration Machines cannot be null.");

        var processDefinitions = snapshot.Processes
                                 ?? throw new InvalidOperationException(
                                     "Runtime configuration Processes cannot be null.");

        var worldDefinitions = snapshot.Worlds
                               ?? throw new InvalidOperationException(
                                   "Runtime configuration Worlds cannot be null.");

        var sceneDefinitions = snapshot.Scenes
                               ?? throw new InvalidOperationException(
                                   "Runtime configuration Scenes cannot be null.");
        
        var machines = MapMachines(machineDefinitions);
        var processes = MapProcesses(processDefinitions);
        var worlds = MapWorlds(worldDefinitions);
        var scenes = MapScenes(sceneDefinitions);

        return (machines, processes, worlds, scenes);
    }
    
    /// <summary>
    /// 映射服务器配置，并保持 Control Center 返回的顺序。
    /// </summary>
    /// <param name="definitions">服务器协议配置。</param>
    /// <returns>Fantasy.Net 服务器配置。</returns>
    private static List<MachineConfig> MapMachines(List<MachineConfigContract> definitions)
    {
        var result = new List<MachineConfig>(definitions.Count);

        for (var index = 0; index < definitions.Count; index++)
        {
            var source = definitions[index]
                         ?? throw new InvalidOperationException(
                             $"Machine at index {index} cannot be null.");

            result.Add(new MachineConfig
            {
                Id = source.Id,
                OuterIP = source.OuterIp,
                OuterBindIP = source.OuterBindIp,
                InnerBindIP = source.InnerBindIp
            });
        }

        return result;
    }
    
    /// <summary>
    /// 映射进程配置，并保留 Namespace 和启动分组信息。
    /// </summary>
    /// <param name="definitions">进程协议配置。</param>
    /// <returns>Fantasy.Net 进程配置。</returns>
    private static List<ProcessConfig> MapProcesses( List<ProcessConfigContract> definitions)
    {
        var result = new List<ProcessConfig>(definitions.Count);

        for (var index = 0; index < definitions.Count; index++)
        {
            var source = definitions[index]
                         ?? throw new InvalidOperationException(
                             $"Process at index {index} cannot be null.");

            result.Add(new ProcessConfig
            {
                Id = source.Id,
                NamespaceId = source.NamespaceId,
                MachineId = source.MachineId,
                StartupGroup = source.StartupGroup
            });
        }

        return result;
    }
    
    /// <summary>
    /// 映射 World 及其数据库子配置。
    /// </summary>
    /// <param name="definitions">World 协议配置。</param>
    /// <returns>Fantasy.Net World 配置。</returns>
    private static List<WorldConfig> MapWorlds(List<WorldConfigContract> definitions)
    {
        var result = new List<WorldConfig>(definitions.Count);

        for (var index = 0; index < definitions.Count; index++)
        {
            var source = definitions[index]
                         ?? throw new InvalidOperationException(
                             $"World at index {index} cannot be null.");

            var databases = MapDatabases(
                source.Id,
                source.Databases,
                out var defaultDatabase);

            result.Add(new WorldConfig
            {
                Id = source.Id,
                NamespaceId = source.NamespaceId,
                GroupId = source.GroupId,
                WorldName = source.WorldName,
                DatabaseConfig = databases,
                Default = defaultDatabase
            });
        }

        return result;
    }
    
    /// <summary>
    /// 映射一个 World 的数据库配置并选出默认数据库。
    /// </summary>
    /// <param name="worldId">用于生成校验错误信息的 World ID。</param>
    /// <param name="definitions">数据库协议配置；为空表示当前 World 不使用数据库。</param>
    /// <param name="defaultDatabase">映射后的默认数据库；没有数据库时为空。</param>
    /// <returns>按原顺序映射的数据库配置数组。</returns>
    private static DatabaseConfig[] MapDatabases(uint worldId, List<DatabaseConfigContract>? definitions, out DatabaseConfig? defaultDatabase)
    {
        if (definitions == null || definitions.Count == 0)
        {
            defaultDatabase = null;
            return Array.Empty<DatabaseConfig>();
        }

        var result = new DatabaseConfig[definitions.Count];
        defaultDatabase = null;

        for (var index = 0; index < definitions.Count; index++)
        {
            var source = definitions[index]
                         ?? throw new InvalidOperationException(
                             $"World {worldId}: Database at index " +
                             $"{index} cannot be null.");

            var database = new DatabaseConfig(
                source.DbConnection ?? string.Empty,
                source.DbName,
                source.DbType,
                source.IsDefault);

            result[index] = database;

            // 保持Fantasy.config原有行为：
            // 多个数据库标记为默认时，第一个生效。
            if (defaultDatabase == null && source.IsDefault)
            {
                defaultDatabase = database;
            }
        }

        // 没有显式默认数据库时，使用第一个数据库。
        defaultDatabase ??= result[0];

        return result;
    }
    
    /// <summary>
    /// 映射 Scene 配置，SceneType 保持字符串形式交给源生成配置解析逻辑处理。
    /// </summary>
    /// <param name="definitions">Scene 协议配置。</param>
    /// <returns>Fantasy.Net Scene 配置。</returns>
    private static List<SceneConfig> MapScenes( List<SceneConfigContract> definitions)
    {
        var result = new List<SceneConfig>(definitions.Count);

        for (var index = 0; index < definitions.Count; index++)
        {
            var source = definitions[index]
                         ?? throw new InvalidOperationException(
                             $"Scene at index {index} cannot be null.");

            result.Add(new SceneConfig
            {
                Id = source.Id,
                ProcessConfigId = source.ProcessId,
                WorldConfigId = source.WorldId,
                SceneRuntimeMode = source.RuntimeMode,
                SceneTypeString = source.SceneType,
                NetworkProtocol =
                    source.NetworkProtocol ?? string.Empty,
                OuterPort = source.OuterPort,
                InnerPort = source.InnerPort
            });
        }

        return result;
    }
}
#endif
