#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.IdFactory;
using Fantasy.Network;
using Fantasy.Platform.Net;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8604 // Possible null reference argument.

namespace Fantasy;

/// <summary>
/// Fantasy框架XML配置文件加载器
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// 当前启用的Control Center客户端。
    /// 未启用或者URL无效时为null。
    /// </summary>
    internal static ControlCenterClient? ControlCenter { get; private set; }
    
    /// <summary>
    /// 从XML配置文件初始化所有配置
    /// </summary>
    /// <param name="configPath"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static async FTask InitializeFromXml(string configPath)
    {
        var configText = await FileHelper.GetTextByRelativePath(configPath);
        var doc = new XmlDocument();
        doc.LoadXml(configText);

        var root = doc.DocumentElement;
        if (root?.LocalName != "fantasy")
        {
            throw new InvalidOperationException("Invalid Fantasy config file format");
        }

        // 创建命名空间管理器
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("f", "http://fantasy.net/config");

        // 加载不依赖配置来源的框架运行参数。
        LoadRuntimeConfig(root, nsManager);
        // 支持测试或同一进程中重新初始化配置。
        ControlCenter = null;
        
        ProgramDefine.ControlCenterHeartbeatIntervalSeconds = ProgramDefine.DefaultControlCenterHeartbeatIntervalSeconds;
        ProgramDefine.ControlCenterLeaseSeconds = ProgramDefine.DefaultControlCenterLeaseSeconds;
        
        var controlCenterNode = root.SelectSingleNode("f:controlCenter", nsManager);
        var controlCenterEnabled = bool.Parse(GetOptionalAttribute(controlCenterNode, "enabled") ?? "false");
        var requireLocalIsolationIds = false;

        if (controlCenterEnabled)
        {
            var fallbackToLocal = bool.Parse(GetOptionalAttribute(controlCenterNode, "fallbackToLocal") ?? "true");
            
            var heartbeatIntervalText = GetOptionalAttribute(controlCenterNode, "heartbeatIntervalSeconds");
            
            var heartbeatIntervalSeconds =
                heartbeatIntervalText == null
                    ? ProgramDefine
                        .DefaultControlCenterHeartbeatIntervalSeconds
                    : int.Parse(heartbeatIntervalText);
            var leaseSecondsText = GetOptionalAttribute(controlCenterNode, "leaseSeconds");

            var leaseSeconds = leaseSecondsText == null
                    ? ProgramDefine.DefaultControlCenterLeaseSeconds
                    : int.Parse(leaseSecondsText);

            if (heartbeatIntervalSeconds <= 0)
            {
                throw new InvalidOperationException(
                    "controlCenter heartbeatIntervalSeconds " +
                    "must be greater than 0.");
            }

            if (leaseSeconds is < 5 or > 300)
            {
                throw new InvalidOperationException(
                    "controlCenter leaseSeconds must be " +
                    "between 5 and 300.");
            }

            if (heartbeatIntervalSeconds >= leaseSeconds)
            {
                throw new InvalidOperationException(
                    "controlCenter heartbeatIntervalSeconds " +
                    "must be less than leaseSeconds.");
            }

            ProgramDefine.ControlCenterHeartbeatIntervalSeconds = heartbeatIntervalSeconds;
            ProgramDefine.ControlCenterLeaseSeconds = leaseSeconds;
            
            try
            {
                var controlCenterUrl = GetRequiredAttribute(controlCenterNode!, "url");
                var controlCenterClient = new ControlCenterClient(controlCenterUrl);
                
                var targetProcessId =
                    ProgramDefine.RuntimeMode == ProcessMode.Release &&
                    ProgramDefine.StartupGroup == 0 &&
                    ProgramDefine.ProcessId > 0
                        ? ProgramDefine.ProcessId
                        : (uint?)null;

                var snapshot = await controlCenterClient.GetRuntimeConfigAsync(targetProcessId);

                var (
                    machines,
                    processes,
                    worlds,
                    scenes) = RuntimeConfigMapper.Map(snapshot);

                ValidateAndInitialize(machines, processes, worlds, scenes);

                // 只有远程配置完整加载成功，才启用本次启动的服务发现。
                ControlCenter = controlCenterClient;

                Log.Info(
                    $"Runtime configuration loaded from Control Center. " +
                    $"Revision: {snapshot.Revision}");

                return;
            }
            catch (Exception exception) when (fallbackToLocal)
            {
                requireLocalIsolationIds = true;
                Log.Warning(
                    "Failed to load runtime configuration from " +
                    "Control Center. Falling back to local server " +
                    $"configuration. Error: {exception}");
            }
        }

        // 未启用Control Center，或者Control Center失败并允许本地回退。
        var serverNode = root.SelectSingleNode("f:server", nsManager);

        if (serverNode == null)
        {
            throw new InvalidOperationException("Missing server configuration in Fantasy config file");
        }

        var localMachines = LoadMachineConfig(serverNode, nsManager);
        var localProcesses = LoadProcessConfig(serverNode, nsManager, requireLocalIsolationIds);
        var localWorlds = LoadWorldConfig(serverNode, nsManager, requireLocalIsolationIds);
        var localScenes = LoadSceneConfig(serverNode, nsManager);

        ValidateAndInitialize( localMachines, localProcesses, localWorlds, localScenes);
        Log.Info("Runtime configuration loaded from local Fantasy.config.");
    }

    private static void LoadRuntimeConfig(XmlNode root, XmlNamespaceManager nsManager)
    {
        // 加载IdFactory配置
        XmlNode? idFactoryNode = root.SelectSingleNode("f:idFactory", nsManager);
        string idFactoryType = GetOptionalAttribute(idFactoryNode, "type") ?? "Default";
        IdFactoryHelper.Initialize(Enum.Parse<IdFactoryType>(idFactoryType));
        // 加载网络配置
        XmlNode? networkNode = root.SelectSingleNode("f:network", nsManager);
        ProgramDefine.InnerNetwork = Enum.Parse<NetworkProtocolType>(GetOptionalAttribute(networkNode, "inner") ?? "TCP");
        ProgramDefine.MaxMessageSize = int.Parse(GetOptionalAttribute(networkNode, "maxMessageSize") ?? "1048560");
        // 加载会话配置
        XmlNode? sessionNode = root.SelectSingleNode("f:session", nsManager);
        ProgramDefine.SessionIdleCheckerTimeout = int.Parse(GetOptionalAttribute(sessionNode, "idleTimeout") ?? "8000");
        ProgramDefine.SessionIdleCheckerInterval = int.Parse(GetOptionalAttribute(sessionNode, "idleInterval") ?? "5000");
        
        Log.Info($"Current inner network protocol:{ProgramDefine.InnerNetwork.ToString()}");
        Log.Info($"Max Message Size(byte):{ProgramDefine.MaxMessageSize}");
        Log.Info($"Current session idle timeout:{ProgramDefine.SessionIdleCheckerTimeout}");
        Log.Info($"Session-check interval:{ProgramDefine.SessionIdleCheckerInterval} ");
    }

    private static List<MachineConfig> LoadMachineConfig(XmlNode serverNode, XmlNamespaceManager nsManager)
    {
        var machinesNode = serverNode.SelectSingleNode("f:machines", nsManager);
        if (machinesNode == null)
        {
            throw new InvalidOperationException("Missing machines configuration - at least one machine must be configured");
        }
        
        var machineNodes = machinesNode.SelectNodes("f:machine", nsManager);
        if (machineNodes == null || machineNodes.Count == 0)
        {
            throw new InvalidOperationException("No machine configurations found - at least one machine must be configured");
        }
        
        var machineList = new List<MachineConfig>();
        foreach (XmlNode machineNode in machineNodes)
        {
            var machine = new MachineConfig
            {
                Id = uint.Parse(GetRequiredAttribute(machineNode, "id")),
                OuterIP = GetRequiredAttribute(machineNode, "outerIP"),
                OuterBindIP = GetRequiredAttribute(machineNode, "outerBindIP"),
                InnerBindIP = GetRequiredAttribute(machineNode, "innerBindIP")
            };
            machineList.Add(machine);
        }
        
        return machineList;
    }

    private static List<ProcessConfig> LoadProcessConfig(
        XmlNode serverNode,
        XmlNamespaceManager nsManager,
        bool requireNamespaceId)
    {
        var processesNode = serverNode.SelectSingleNode("f:processes", nsManager);
        if (processesNode == null)
        {
            throw new InvalidOperationException("Missing processes configuration - at least one process must be configured");
        }

        var processNodes = processesNode.SelectNodes("f:process", nsManager);
        if (processNodes == null || processNodes.Count == 0)
        {
            throw new InvalidOperationException("No process configurations found - at least one process must be configured");
        }

        var processList = new List<ProcessConfig>();
        foreach (XmlNode processNode in processNodes)
        {
            var id = uint.Parse(GetRequiredAttribute(processNode, "id"));
            var namespaceIdText = GetOptionalAttribute(processNode, "namespaceId");
            var namespaceId = namespaceIdText == null ? 0 : uint.Parse(namespaceIdText);
            if (requireNamespaceId && namespaceId == 0)
            {
                throw new InvalidOperationException(
                    $"Process {id}: namespaceId must be greater than 0 " +
                    "when falling back from Control Center.");
            }

            var process = new ProcessConfig
            {
                Id = id,
                NamespaceId = namespaceId,
                MachineId = uint.Parse(GetRequiredAttribute(processNode, "machineId")),
                StartupGroup = uint.Parse(GetRequiredAttribute(processNode, "startupGroup"))
            };
            processList.Add(process);
        }

        return processList;
    }

    private static List<WorldConfig> LoadWorldConfig(
        XmlNode serverNode,
        XmlNamespaceManager nsManager,
        bool requireGroupId)
    {
        var worldsNode = serverNode.SelectSingleNode("f:worlds", nsManager);
        if (worldsNode == null)
        {
            throw new InvalidOperationException("Missing worlds configuration - at least one world must be configured");
        }
        
        var worldNodes = worldsNode.SelectNodes("f:world", nsManager);
        if (worldNodes == null || worldNodes.Count == 0)
        {
            throw new InvalidOperationException("No world configurations found - at least one world must be configured");
        }
        
        var worldList = new List<WorldConfig>();

        // 解析world和所有database
        foreach (XmlNode worldNode in worldNodes)
        {
            var id = uint.Parse(GetRequiredAttribute(worldNode, "id"));
            var namespaceIdText = GetOptionalAttribute(worldNode, "namespaceId");
            var namespaceId = namespaceIdText == null ? 0 : uint.Parse(namespaceIdText);
            var groupIdText = GetOptionalAttribute(worldNode, "groupId");
            var groupId = groupIdText == null ? 0 : uint.Parse(groupIdText);

            if (requireGroupId && (namespaceId == 0 || groupId == 0))
            {
                throw new InvalidOperationException(
                    $"World {id}: namespaceId and groupId must be greater than 0 " +
                    "when falling back from Control Center.");
            }

            var worldName = GetRequiredAttribute(worldNode, "worldName");
            var databaseNodes = GetChildNodes(worldNode, "f:database", nsManager);
            
            var worldConfig = new WorldConfig
            {
                Id = id,
                NamespaceId = namespaceId,
                GroupId = groupId,
                WorldName = worldName,
                DatabaseConfig = new DatabaseConfig[databaseNodes.Count]
            };

            if (databaseNodes.Count > 0)
            {
                for (var index = 0; index < databaseNodes.Count; index++)
                {
                    var databaseNode = databaseNodes[index];
                    var databaseConfig = GetDatabaseConfig(databaseNode);

                    if (worldConfig.Default == null && databaseConfig.IsDefault)
                    {
                        worldConfig.Default = databaseConfig;
                    }

                    worldConfig.DatabaseConfig[index] = databaseConfig;
                }

                worldConfig.Default ??= worldConfig.DatabaseConfig[0];
            }
            
            worldList.Add(worldConfig);
        }

        return worldList;
    }

    private static DatabaseConfig GetDatabaseConfig(XmlNode dbNode)
    {
        var dbConnection = GetOptionalAttribute(dbNode, "dbConnection");
        var dbType = GetRequiredAttribute(dbNode, "dbType");
        var dbName = GetRequiredAttribute(dbNode, "dbName");
        var isDefault = GetOptionalAttribute(dbNode, "isDefault") ?? "false";
        
        if (string.IsNullOrWhiteSpace(dbConnection))
        {
            // Log.Warning($"(Fantasy.config) \"DbConnection\" is empty, thus the database-config \"{dbName}({dbType})\" in {worldName} (World Id: {id}) will be ignoured.");
            dbConnection = string.Empty; 
        }
        
        return new DatabaseConfig(dbConnection, dbName, dbType,bool.Parse(isDefault));
    }
    
    private static List<SceneConfig> LoadSceneConfig(XmlNode serverNode, XmlNamespaceManager nsManager)
    {
        var scenesNode = serverNode.SelectSingleNode("f:scenes", nsManager);
        if (scenesNode == null)
        {
            throw new InvalidOperationException("Missing scenes configuration - at least one scene must be configured");
        }
        
        var sceneNodes = scenesNode.SelectNodes("f:scene", nsManager);
        if (sceneNodes == null || sceneNodes.Count == 0)
        {
            throw new InvalidOperationException("No scene configurations found - at least one scene must be configured");
        }
        
        var sceneList = new List<SceneConfig>();
        foreach (XmlNode sceneNode in sceneNodes)
        {
            var scene = new SceneConfig
            {
                Id = uint.Parse(GetRequiredAttribute(sceneNode, "id")),
                ProcessConfigId = uint.Parse(GetRequiredAttribute(sceneNode,  "processConfigId")),
                WorldConfigId = uint.Parse(GetRequiredAttribute(sceneNode, "worldConfigId")),
                SceneRuntimeMode = GetRequiredAttribute(sceneNode, "sceneRuntimeMode"),
                SceneTypeString = GetRequiredAttribute(sceneNode, "sceneTypeString"),
                NetworkProtocol = GetOptionalAttribute(sceneNode, "networkProtocol") ?? string.Empty,
                OuterPort = int.Parse(GetOptionalAttribute(sceneNode, "outerPort") ?? "0"),
                InnerPort = int.Parse(GetRequiredAttribute(sceneNode, "innerPort"))
            };
            
            sceneList.Add(scene);
        }
        
        return sceneList;
    }

    /// <summary>
    /// 验证并发布运行时配置。
    /// 只有所有配置校验成功后，才会初始化全局ConfigData。
    /// </summary>
    private static void ValidateAndInitialize(List<MachineConfig> machines, List<ProcessConfig> processes, List<WorldConfig> worlds, List<SceneConfig> scenes)
    {
        ConfigValidator.Validate( machines, processes, worlds, scenes);

        MachineConfigData.Initialize(machines);
        ProcessConfigData.Initialize(processes);
        WorldConfigData.Initialize(worlds);
        SceneConfigData.Initialize(scenes);
    }
    
    /// <summary>
    /// 获取 XMl 父节点下所有指定名称的子节点
    /// </summary>
    private static XmlNodeList GetChildNodes(XmlNode parentNode, string childNodeName, XmlNamespaceManager nsManager)
    {
        return parentNode.SelectNodes(childNodeName, nsManager) ?? throw new InvalidOperationException($"No child nodes named '{childNodeName}' found under parent node");
    }
    
    /// <summary>
    /// 获取必填的属性
    /// </summary>
    private static string GetRequiredAttribute(XmlNode node, string attributeName)
    {
        return node.Attributes?[attributeName]?.Value ?? throw new InvalidOperationException($"Required attribute '{attributeName}' is missing or null");
    }

    /// <summary>
    /// 获取可选的属性
    /// </summary>
    private static string? GetOptionalAttribute(XmlNode? node, string attributeName)
    {
        var value = node?.Attributes?[attributeName]?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
#endif
