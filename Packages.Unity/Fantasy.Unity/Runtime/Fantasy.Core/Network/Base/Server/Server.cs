#if FANTASY_NET

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

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
    /// 网络中的服务器。
    /// </summary>
    public sealed class Server
    {
        /// <summary>
        /// 获取服务器的唯一标识符。
        /// </summary>
        public uint Id { get; private set; }
        /// <summary>
        /// 获取与服务器关联的场景。
        /// </summary>
        public Scene Scene { get; private set; }

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
            public Session Session;
            /// <summary>
            /// 获取与连接信息关联的网络组件。
            /// </summary>
            public Entity NetworkComponent;

            /// <summary>
            /// 初始化 <see cref="ConnectInfo"/> 类的新实例。
            /// </summary>
            /// <param name="session">要关联的会话。</param>
            /// <param name="networkComponent">要关联的网络组件。</param>
            public ConnectInfo(Session session, Entity networkComponent)
            {
                Session = session;
                NetworkComponent = networkComponent;
            }

            /// <summary>
            /// 释放连接信息所持有的资源。
            /// </summary>
            public void Dispose()
            {
                if (Session != null)
                {
                    Session.Dispose();
                    Session = null;
                }
                
                if (NetworkComponent != null)
                {
                    NetworkComponent.Dispose();
                    NetworkComponent = null;
                }
            }
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
            
            // 同一个Server下、只需要内部走下消息派发就可以

            if (Id == targetServerId)
            {
                var serverNetworkComponent = Scene.GetComponent<ServerNetworkComponent>();
                var session = Session.Create(serverNetworkComponent.Network);
                _sessions.Add(targetServerId, new ConnectInfo(session, null));
                return session;
            }
            
            // 不同的Server下需要建立网络连接

            var serverConfigInfo = ConfigTableManage.ServerConfig(targetServerId);

            if (serverConfigInfo == null)
            {
                throw new Exception($"The server with ServerId {targetServerId} was not found in the configuration file");
            }

            var machineConfigInfo = ConfigTableManage.MachineConfig(serverConfigInfo.MachineId);

            if (machineConfigInfo == null)
            {
                throw new Exception($"Server.cs The specified MachineId was not found: {serverConfigInfo.MachineId}");
            }

            var ipEndPoint = NetworkHelper.ToIPEndPoint($"{machineConfigInfo.InnerBindIP}:{serverConfigInfo.InnerPort}");
            var clientNetworkComponent = Entity.Create<ClientNetworkComponent>(Scene);
            clientNetworkComponent.Initialize(AppDefine.InnerNetwork, NetworkTarget.Inner);
            clientNetworkComponent.Connect(ipEndPoint, null, () =>
            {
                Log.Error($"Unable to connect to the target server sourceServerId:{Id} targetServerId:{targetServerId}");
            }, null);
            _sessions.Add(targetServerId, new ConnectInfo(clientNetworkComponent.Session, clientNetworkComponent));
            return clientNetworkComponent.Session;
        }

        /// <summary>
        /// 移除与指定目标服务器关联的会话。
        /// </summary>
        /// <param name="targetServerId">目标服务器的标识符。</param>
        public void RemoveSession(uint targetServerId)
        {
            if (!_sessions.Remove(targetServerId, out var connectInfo))
            {
                return;
            }
            
            connectInfo.Dispose();
        }

        #region Static
        // 存储已创建的服务器的字典。
        private static readonly Dictionary<uint, Server> Servers = new Dictionary<uint, Server>();

        /// <summary>
        /// 创建并初始化具有指定服务器配置的服务器。
        /// </summary>
        /// <param name="serverConfigId">服务器配置的标识符。</param>
        public static async FTask Create(uint serverConfigId)
        {
            var serverConfigInfo = ConfigTableManage.ServerConfig(serverConfigId);
            
            if (serverConfigInfo == null)
            {
                Log.Error($"not found server by Id:{serverConfigId}");
                return;
            }

            var machineConfigInfo = ConfigTableManage.MachineConfig(serverConfigInfo.MachineId);

            if (machineConfigInfo == null)
            {
                Log.Error($"not found machine by Id:{serverConfigInfo.MachineId}");
                return;
            }
            
            var sceneInfos = Scene.GetSceneInfoByServerConfigId(serverConfigId);
            await Create(serverConfigId, machineConfigInfo.InnerBindIP, serverConfigInfo.InnerPort, machineConfigInfo.OuterBindIP, sceneInfos);
            Log.Debug($"ServerId:{serverConfigId} is start complete");
        }

        /// <summary>
        /// 创建一个新的服务器实例或获取现有服务器实例。
        /// </summary>
        /// <param name="serverConfigId">要创建的服务器的配置标识符。</param>
        /// <param name="innerBindIp">服务器的内部绑定 IP 地址。</param>
        /// <param name="innerPort">服务器的内部端口。</param>
        /// <param name="outerBindIp">服务器的外部绑定 IP 地址。</param>
        /// <param name="sceneInfos">要创建的场景配置信息列表。</param>
        /// <returns>创建或获取的服务器实例。</returns>
        public static async FTask<Server> Create(uint serverConfigId, string innerBindIp, int innerPort, string outerBindIp, List<SceneConfigInfo> sceneInfos)
        {
            if (Servers.TryGetValue(serverConfigId, out var server))
            {
                return server;
            }

            // 创建一个新的Server、创建一个临时Scene给Server当做Scene使用

            server = new Server
            {
                Id = serverConfigId
            };

            server.Scene = await Scene.Create(server);

            // 创建网络、Server下的网络只能是内部网络、外部网络是在Scene中定义
            
            if (!string.IsNullOrEmpty(innerBindIp) && innerPort != 0)
            {
                var address = NetworkHelper.ToIPEndPoint(innerBindIp, innerPort);
                var serverNetworkComponent = server.Scene.AddComponent<ServerNetworkComponent>();
                serverNetworkComponent.Initialize(AppDefine.InnerNetwork, NetworkTarget.Inner, address);
            }

            // 创建Server拥有所有的Scene
            
            foreach (var sceneConfig in sceneInfos)
            {
                await Scene.Create(server, sceneConfig.SceneType, sceneConfig.SceneSubType, sceneConfig.EntityId,
                    sceneConfig.WorldId, sceneConfig.NetworkProtocol, outerBindIp, sceneConfig.OuterPort);
            }

            Servers.Add(serverConfigId, server);
            await EventSystem.Instance.PublishAsync(new OnServerStartComplete(server));
            return server;
        }

        /// <summary>
        /// 根据路由标识符获取服务器实例。
        /// </summary>
        /// <param name="routeId">服务器的路由标识符。</param>
        /// <returns>找到的服务器实例，如果未找到则返回 null。</returns>
        public static Server Get(uint routeId)
        {
            return Servers.TryGetValue(routeId, out var server) ? server : null;
        }

        #endregion
    }
}
#endif