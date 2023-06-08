// namespace Fantasy;
//
// public sealed partial class MachineConfigData
// {
//     private readonly Dictionary<int, MachineConfig> _machinesByServerId = new();
//
//     public MachineConfig GetMachineByServerId(int serverId)
//     {
//         if (_machinesByServerId.TryGetValue(serverId, out var machineConfig))
//         {
//             return machineConfig;
//         }
//
//         machineConfig = Get(ServerConfigData.Instance.Get(serverId).MachineId);
//         _machinesByServerId.Add(serverId, machineConfig);
//         return machineConfig;
//     }
//     
//     public MachineConfig GetMachineByServerId(ServerConfig serverConfig)
//     {
//         if (_machinesByServerId.TryGetValue(serverConfig.Id, out var machineConfig))
//         {
//             return machineConfig;
//         }
//
//         machineConfig = Get(serverConfig.MachineId);
//         _machinesByServerId.Add(serverConfig.Id, machineConfig);
//         return machineConfig;
//     }
// }