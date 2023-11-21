using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 表示一个场景实体，用于创建与管理特定的游戏场景信息。
    /// </summary>
    public class Scene : Entity, INotSupportedPool
    {
        /// <summary>
        /// 获取场景的位置ID。
        /// </summary>
        public uint LocationId { get; private set; }
#if FANTASY_UNITY
        /// <summary>
        /// 获取或设置与此场景关联的会话。
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// 获取或设置场景配置信息。
        /// </summary>
        public SceneConfigInfo SceneInfo { get; private set; }
#endif
#if FANTASY_NET
        /// <summary>
        /// 获取或设置场景类型。
        /// </summary>
        public int SceneType { get; private set; }
        /// <summary>
        /// 获取或设置场景子类型。
        /// </summary>
        public int SceneSubType { get; private set; }
        /// <summary>
        /// 获取或设置所属世界。
        /// </summary>
        public World? World { get; private set; }
        /// <summary>
        /// 获取或设置所属服务器。
        /// </summary>
        public Server Server { get; private set; }
#endif
        /// <summary>
        /// 存储所有已创建的场景实体。
        /// </summary>
        public static readonly List<Scene> Scenes = new List<Scene>();

        /// <summary>
        /// 释放场景实体及其资源。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            base.Dispose();
            LocationId = 0;
#if FANTASY_NET
            World = null;
            Server = null;
            SceneType = 0;
            SceneSubType = 0;
#endif
#if FANTASY_UNITY
            SceneInfo = null;
            if (Session is { IsDisposed: false })
            {
                Session.Dispose();
                Session = null;
            }
#endif
            Scenes.Remove(this);
        }

        /// <summary>
        /// 释放所有已创建的场景实体及其资源。
        /// </summary>
        public static void DisposeAllScene()
        {
            foreach (var scene in Scenes.ToArray())
            {
                scene.Dispose();
            }
        }
#if FANTASY_UNITY
        /// <summary>
        /// 创建一个新的场景实体。
        /// </summary>
        /// <returns>创建的场景实体。</returns>
        public static Scene Create()
        {
            var sceneId = IdFactory.NextRunTimeId();
            var scene = Create<Scene>(sceneId, sceneId);
            scene.Scene = scene;
            scene.Parent = scene;
            Scenes.Add(scene);
            return scene;
        }

        /// <summary>
        /// 为场景创建一个网络会话。
        /// </summary>
        /// <param name="remoteAddress">远程地址。</param>
        /// <param name="networkProtocolType">网络协议类型。</param>
        /// <param name="onConnectComplete">连接成功回调。</param>
        /// <param name="onConnectFail">连接失败回调。</param>
        /// <param name="onConnectDisconnect">连接断开回调。</param>
        /// <param name="connectTimeout">连接超时时间（毫秒）。</param>
        public void CreateSession(string remoteAddress, NetworkProtocolType networkProtocolType, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000)
        {
            var address = NetworkHelper.ToIPEndPoint(remoteAddress);
            var clientNetworkComponent = GetComponent<ClientNetworkComponent>() ?? AddComponent<ClientNetworkComponent>();
            clientNetworkComponent.Initialize(networkProtocolType, NetworkTarget.Outer);
            clientNetworkComponent.Connect(address, onConnectComplete, onConnectFail, onConnectDisconnect, connectTimeout);
            Session = clientNetworkComponent.Session;
        }
#else
        /// <summary>
        /// 创建一个新的场景实体，通过复制已有场景并设置特定属性来实现。
        /// </summary>
        /// <typeparam name="T">要创建的场景类型。</typeparam>
        /// <param name="scene">要复制的场景。</param>
        /// <param name="sceneType">场景类型。</param>
        /// <param name="sceneSubType">场景子类型。</param>
        /// <returns>新创建的场景实体。</returns>
        public static async FTask<T> Create<T>(Scene scene, int sceneType, int sceneSubType) where T : Scene, new()
        {
            var newScene = Create<T>(scene);
            newScene.Scene = newScene;
            newScene.Parent = scene;
            newScene.SceneType = sceneType;
            newScene.SceneSubType = sceneSubType;
            newScene.Server = scene.Server;
            newScene.LocationId = scene.Server.Id;

            if (scene.World != null)
            {
                newScene.World = scene.World;
            }

            if (sceneType > 0)
            {
                await EventSystem.Instance.PublishAsync(new OnCreateScene(newScene));
            }

            Scenes.Add(newScene);
            return newScene;
        }

        /// <summary>
        /// 创建一个新的场景实体，并根据提供的参数配置场景属性。
        /// </summary>
        /// <param name="server">所属服务器实体。</param>
        /// <param name="sceneType">场景类型。</param>
        /// <param name="sceneSubType">场景子类型。</param>
        /// <param name="sceneId">场景ID。</param>
        /// <param name="worldId">世界ID。</param>
        /// <param name="networkProtocol">网络协议。</param>
        /// <param name="outerBindIp">外部绑定IP。</param>
        /// <param name="outerPort">外部端口。</param>
        /// <returns>新创建的场景实体。</returns>
        public static async FTask<Scene> Create(Server server, int sceneType = 0, int sceneSubType = 0, long sceneId = 0, uint worldId = 0, string networkProtocol = null, string outerBindIp = null, int outerPort = 0)
        {
            if (sceneId == 0)
            {
                sceneId = new EntityIdStruct(server.Id, 0, 0);
            }
            
            var scene = Create<Scene>(sceneId, sceneId);
            scene.Scene = scene;
            scene.Parent = scene;
            scene.SceneType = sceneType;
            scene.SceneSubType = sceneSubType;
            scene.Server = server;
            scene.LocationId = server.Id;

            if (worldId != 0)
            {
                // 有可能不需要数据库、所以这里默认0的情况下就不创建数据库了
                scene.World = World.Create(worldId);
            }

            if (!string.IsNullOrEmpty(networkProtocol) && !string.IsNullOrEmpty(outerBindIp) && outerPort != 0)
            {
                // 设置Scene的网络、目前只支持KCP和TCP
                var networkProtocolType = Enum.Parse<NetworkProtocolType>(networkProtocol);
                var serverNetworkComponent = scene.AddComponent<ServerNetworkComponent>();
                var address = NetworkHelper.ToIPEndPoint($"{outerBindIp}:{outerPort}");
                serverNetworkComponent.Initialize(networkProtocolType, NetworkTarget.Outer, address);
            }

            if (sceneType > 0)
            {
                await EventSystem.Instance.PublishAsync(new OnCreateScene(scene));
            }

            Scenes.Add(scene);
            return scene;
        }

        /// <summary>
        /// 根据服务器配置ID获取与之关联的场景配置信息列表。
        /// </summary>
        /// <param name="serverConfigId">服务器配置ID。</param>
        /// <returns>与服务器配置ID关联的场景配置信息列表。</returns>
        public static List<SceneConfigInfo> GetSceneInfoByServerConfigId(uint serverConfigId)
        {
            var list = new List<SceneConfigInfo>();
            var allSceneConfig = ConfigTableManage.AllSceneConfig();

            foreach (var sceneConfigInfo in allSceneConfig)
            {
                if (sceneConfigInfo.ServerConfigId != serverConfigId)
                {
                    continue;
                }
                
                list.Add(sceneConfigInfo);
            }

            return list;
        }
#endif
    }
}
