using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

#if FANTASY_NET
namespace Fantasy
{
    /// <summary>
    /// 当Server启动完成时触发。
    /// </summary>
    public struct OnServerStartComplete
    {
        /// <summary>
        /// 获取启动完成的服务器。
        /// </summary>
        public readonly Server Server;
        /// <summary>
        /// 初始化一个新的 OnServerStartComplete 实例。
        /// </summary>
        /// <param name="server"></param>
        public OnServerStartComplete(Server server)
        {
            Server = server;
        }
    }
    
    /// <summary>
    /// 代表一个服务器或一个进程、具体看使用情况而定
    /// 如果在同一个服务器上运行多个进程，那么每个进程都是一个服务器
    /// 如果在不同的服务器上运行多个进程，那么每个进程都是一个服务器
    /// </summary>
    public sealed class Server
    {
        /// <summary>
        /// 获取服务器的唯一标识符。
        /// </summary>
        public uint Id { get; private set; }
        /// <summary>
        /// 所属于的Scene。
        /// </summary>
        /// <returns></returns>
        public Scene Scene { get; private set; }
        /// <summary>
        /// 获取关联的服务端Network网络实例。
        /// </summary>
        public ANetwork Network { get; private set; }
        // 存储与服务器关联的场景的字典。
        private readonly Dictionary<uint, Scene> _scenes = new Dictionary<uint, Scene>();
        // 存储与其他服务器建立的会话的字典。
        private readonly Dictionary<uint, ConnectInfo> _sessions = new Dictionary<uint, ConnectInfo>();
        
        /// <summary>
        /// 与其他服务器建立的连接信息。
        /// </summary>
        private sealed class ConnectInfo : IDisposable
        {
            /// <summary>
            /// 获取与连接信息关联的会话。
            /// </summary>
            public readonly Session Session;
            /// <summary>
            /// 获取与连接信息关联的网络。
            /// </summary>
            public readonly AClientNetwork AClientNetwork;

            /// <summary>
            /// 初始化 <see cref="ConnectInfo"/> 类的新实例。
            /// </summary>
            /// <param name="session">要关联的会话。</param>
            /// <param name="aClientNetwork">要关联的网络。</param>
            public ConnectInfo(Session session, AClientNetwork aClientNetwork)
            {
                Session = session;
                AClientNetwork = aClientNetwork;
            }

            /// <summary>
            /// 释放连接信息所持有的资源。
            /// </summary>
            public void Dispose()
            {
                Session.Dispose();
            }
        }
        
        private Server() { }
        
        /// <summary>
        /// 创建并初始化具有指定服务器配置的服务器。
        /// </summary>
        /// <param name="serverConfigId">服务器配置的标识符。</param>
        public static async FTask Create(uint serverConfigId)
        {
            if (!ServerConfigData.Instance.TryGet(serverConfigId, out var serverConfig))
            {
                Log.Error($"not found serverConfig by Id:{serverConfigId}");
                return;
            }

            if (!MachineConfigData.Instance.TryGet(serverConfig.MachineId, out var machineConfig))
            {
                Log.Error($"not found machineConfig by Id:{serverConfig.MachineId}");
                return;
            }

            var sceneConfigs = SceneConfigData.Instance.GetByServerConfigId(serverConfigId);
            await Create(serverConfigId, machineConfig.InnerBindIP, serverConfig.InnerPort, machineConfig.OuterBindIP, sceneConfigs);
        }

        /// <summary>
        /// 创建一个新的服务器实例或获取现有服务器实例。
        /// </summary>
        /// <param name="serverConfigId"></param>
        /// <param name="innerBindIp"></param>
        /// <param name="innerPort"></param>
        /// <param name="outerBindIp"></param>
        /// <param name="sceneConfigs"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async FTask<Server> Create(uint serverConfigId, string innerBindIp, int innerPort, string outerBindIp, List<SceneConfig> sceneConfigs)
        {
            Log.Debug($"serverId:{serverConfigId} Starting...");
            
            var server = new Server
            {
                Id = serverConfigId
            };

            server.Scene = await Scene.Create(server, SceneRuntimeType.MainThread);
            
            if (!string.IsNullOrEmpty(innerBindIp) && innerPort != 0)
            {
                var address = NetworkHelper.ToIPEndPoint(innerBindIp, innerPort);
                server.Network = NetworkProtocolFactory.CreateServer(server.Scene, AppDefine.InnerNetwork, NetworkTarget.Inner, address);
            }

            foreach (var sceneConfig in sceneConfigs)
            {
                var scene = await Scene.Create(server,
                    sceneConfig.SceneRuntimeType,
                    sceneConfig.Id,
                    sceneConfig.EntityId,
                    null,
                    sceneConfig.SceneType,
                    sceneConfig.WorldConfigId,
                    sceneConfig.NetworkProtocol,
                    outerBindIp,
                    sceneConfig.OuterPort);
                server._scenes.Add(scene.SceneConfigId, scene);
            }
            
            Log.Debug($"ServerId:{serverConfigId} Startup Complete SceneCount:{server._scenes.Count}");
            
            server.Scene.ThreadSynchronizationContext.Post(() =>
            {
                EventSystem.Instance.PublishAsync(new OnServerStartComplete(server)).Coroutine();
            });
            
            return server;
        }
        
        /// <summary>
        /// 获取用于与指定目标服务器通信的会话。
        /// </summary>
        /// <param name="targetServerId">目标服务器的标识符。</param>
        /// <returns>与目标服务器的会话。</returns>
        public Session GetSession(uint targetServerId)
        {
            if (_sessions.TryGetValue(targetServerId, out var connectInfo))
            {
                if (!connectInfo.Session.IsDisposed)
                {
                    return connectInfo.Session;
                }
                
                _sessions.Remove(targetServerId);
            }

            Session session;

            if (Id == targetServerId)
            {
                session = Session.Create(Scene.Server.Network);
                _sessions.Add(targetServerId, new ConnectInfo(session, null));
                return session;
            }

            // 不同的Server下需要建立网络连接
            
            if (!ServerConfigData.Instance.TryGet(targetServerId, out var serverConfig))
            {
                throw new Exception($"The server with ServerId {targetServerId} was not found in the configuration file");
            }

            if (!MachineConfigData.Instance.TryGet(serverConfig.MachineId, out var machineConfig))
            {
                throw new Exception($"The machine with MachineId {serverConfig.MachineId} was not found in the configuration file");
            }

            var ipEndPoint = NetworkHelper.ToIPEndPoint($"{machineConfig.InnerBindIP}:{serverConfig.InnerPort}");
            var client = NetworkProtocolFactory.CreateClient(this.Scene, AppDefine.InnerNetwork, NetworkTarget.Inner);
            session = client.Connect(ipEndPoint, null, () =>
            {
                Log.Error($"Unable to connect to the target server sourceServerId:{Id} targetServerId:{targetServerId}");
            }, null);
            _sessions.Add(targetServerId, new ConnectInfo(session, client));
            return session;
        }
    }
}
#endif