using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    public class SceneRuntimeType
    {
        public const string MainThread = "MainThread";
        public const string MultiThread = "MultiThread";
        public const string ThreadPool = "ThreadPool";
    }
    
    /// <summary>
    /// 表示一个场景实体，用于创建与管理特定的游戏场景信息。
    /// </summary>
    public sealed class Scene : Entity
    {
        internal void Update()
        {
            try
            {
                EntityComponent.Update();
                TimerComponent.Update();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
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
            ProcessId = 0;
#if FANTASY_NET
            // World = null;
            Server = null;
            SceneType = 0;
            SceneConfigId = 0;
            Server = null;
#endif
#if FANTASY_UNITY
            if (Session is { IsDisposed: false })
            {
                Session.Dispose();
                Session = null;
            }
#endif
            Scenes.Remove(SchedulerId, out _);
            SchedulerId = 0;
            ThreadSynchronizationContext = null;
            EntityComponent.Dispose();
            EntityComponent = null;
            Pool.Dispose();
            
            TimerComponent = null;
            EntityComponent = null;
            CoroutineLockComponent = null;
            MessageDispatcherComponent = null;
            NetworkMessagingComponent = null;
            Network.Dispose();
            Network = null;
#if FANTASY_UNITY
            AssetBundleComponent = null;
#endif
        }
        
        #region Static

        private static long _idGenerator = long.MinValue;
        public static readonly ConcurrentDictionary<long, Scene> Scenes = new ConcurrentDictionary<long, Scene>();
        
        /// <summary>
        /// 创建一个空的Scene
        /// </summary>
        /// <param name="id"></param>
        /// <param name="runtimeId"></param>
        /// <returns></returns>
        private static Scene CreateScene(long id, long runtimeId)
        {
            var scene = (Scene)Activator.CreateInstance(typeof(Scene));
            scene.Id = id;
            scene.RuntimeId = runtimeId;
            scene.Scene = scene;
            scene.Parent = scene;
            return scene;
        }
#if FANTASY_UNITY
        private AClientNetwork Network { get; set; }
        public static Scene Create(Scene scene = null, string sceneRuntimeType = SceneRuntimeType.MainThread)
        {
            var sceneId = IdFactory.NextRunTimeId();
            var sceneSchedulerId = Interlocked.Increment(ref _idGenerator);
            var newScene = CreateScene(sceneId, sceneId);

            if (scene == null)
            {
                newScene.Scene = newScene;
                newScene.Parent = newScene;
            }
            else
            {
                newScene.Scene = scene;
                newScene.Parent = scene;
            }
            
            newScene.ProcessId = 0;
            newScene.SchedulerId = sceneSchedulerId;

            newScene.Initialize(scene).Coroutine();
            Scenes.TryAdd(sceneSchedulerId, newScene);
            SetScheduler(newScene, sceneSchedulerId, sceneRuntimeType);
            return newScene;
        }

        /// <summary>
        /// 为场景创建一个远程连接会话
        /// </summary>
        /// <param name="remoteAddress">远程地址。</param>
        /// <param name="networkProtocolType">网络协议类型。</param>
        /// <param name="onConnectComplete">连接成功回调。</param>
        /// <param name="onConnectFail">连接失败回调。</param>
        /// <param name="onConnectDisconnect">连接断开回调。</param>
        /// <param name="connectTimeout">连接超时时间（毫秒）。</param>
        public Session Connect(string remoteAddress, NetworkProtocolType networkProtocolType, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000)
        {
            if (Network != null && !Network.IsDisposed)
            {
                Network.Dispose();
            }

            if (Session != null && !Session.IsDisposed)
            {
                Session.Dispose();
            }
            
            Network = NetworkProtocolFactory.CreateClient(this, networkProtocolType, NetworkTarget.Outer);
            Session = Network.Connect(NetworkHelper.ToIPEndPoint(remoteAddress), onConnectComplete, onConnectFail, onConnectDisconnect, connectTimeout);
            return Session;
        }
#endif
#if FANTASY_NET
        /// <summary>
        /// 创建一个新的场景实体，并根据提供的参数配置场景属性。
        /// </summary>
        /// <param name="server">所属服务器实体。</param>
        /// <param name="sceneRuntimeType">Scene运行的类型</param>
        /// <param name="sceneConfigId">SceneConfig表的Id</param>
        /// <param name="scene">所属于的Scene。</param>
        /// <param name="sceneType">场景类型。</param>
        /// <param name="entityId">场景ID。</param>
        /// <param name="worldId">世界ID。</param>
        /// <param name="networkProtocol">网络协议。</param>
        /// <param name="outerBindIp">外部绑定IP。</param>
        /// <param name="outerPort">外部端口。</param>
        /// <returns>新创建的场景实体。</returns>
        public static async FTask<Scene> Create(Server server, string sceneRuntimeType, uint sceneConfigId = 0, long entityId = 0, Scene scene = null, int sceneType = 0, uint worldId = 0, string networkProtocol = null, string outerBindIp = null, int outerPort = 0)
        {
            if (entityId == 0)
            {
                entityId = new EntityIdStruct(server.Id, 0, 0);
            }
            
            var sceneSchedulerId = Interlocked.Increment(ref _idGenerator);
            var newScene = CreateScene(entityId, entityId);
            
            if (scene == null)
            {
                newScene.Scene = newScene;
                newScene.Parent = newScene;
            }
            else
            {
                newScene.Scene = scene;
                newScene.Parent = scene;
            }
            
            newScene.SceneType = sceneType;
            newScene.Server = server;
            newScene.ProcessId = server.Id;
            newScene.SceneConfigId = sceneConfigId;
            newScene.SchedulerId = sceneSchedulerId;
            await newScene.Initialize(scene);
            
            if (worldId != 0)
            {
                // 有可能不需要数据库、所以这里默认0的情况下就不创建数据库了
                scene.World = World.Create(newScene, worldId);
            }
            
            Scenes.TryAdd(sceneSchedulerId, newScene);

            if (!string.IsNullOrEmpty(outerBindIp) && outerPort != 0)
            {
                var networkProtocolType = Enum.Parse<NetworkProtocolType>(networkProtocol);
                var address = NetworkHelper.ToIPEndPoint(outerBindIp, outerPort);
                newScene.Network = NetworkProtocolFactory.CreateServer(server.Scene, networkProtocolType, NetworkTarget.Outer, address);
            }

            SetScheduler(newScene, sceneSchedulerId, sceneRuntimeType);

            if (sceneConfigId != 0)
            {
                Log.Debug($"ServerConfigId:{server.Id} SceneConfigId:{sceneConfigId} RouteId:{entityId} SceneRuntimeType:{sceneRuntimeType} is start complete");
            }

            return newScene;
        }
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetScheduler(Scene scene, long sceneSchedulerId, string sceneRuntimeType)
        {
            switch (sceneRuntimeType)
            {
                case "MainThread":
                {
                    scene.ThreadSynchronizationContext = ThreadSynchronizationContext.Main;
                    ThreadScheduler.AddToMainThreadScheduler(sceneSchedulerId);
                    break;
                }
                case "MultiThread":
                {
                    scene.ThreadSynchronizationContext = new ThreadSynchronizationContext(sceneSchedulerId);
                    ThreadScheduler.AddToMultiThreadScheduler(sceneSchedulerId);
                    break;
                }
                case "ThreadPool":
                {
                    scene.ThreadSynchronizationContext = new ThreadSynchronizationContext(sceneSchedulerId);
                    ThreadScheduler.AddToThreadPoolScheduler(sceneSchedulerId);
                    break;
                }
            }
        }
        #endregion
        
        #region Members

        /// <summary>
        /// 当前Scene的调度Id
        /// </summary>
        public long SchedulerId { get; private set; }
        /// <summary>
        /// 获取场景的位置ID。
        /// </summary>
        public uint ProcessId { get; private set; }
        /// <summary>
        /// 同步上下文
        /// </summary>
        public ThreadSynchronizationContext ThreadSynchronizationContext { get; private set; }
        /// <summary>
        /// Scene的对象池
        /// </summary>
        public SingleThreadPool Pool { get; private set; } = new SingleThreadPool();
#if FANTASY_UNITY
        /// <summary>
        /// 获取或设置与此场景关联的会话。
        /// </summary>
        public Session Session { get; private set; }
        public AssetBundleComponent AssetBundleComponent { get; private set; }
        public static Scene Instance { get; private set; }
#endif
#if FANTASY_NET
        /// <summary>
        /// 获取或设置场景类型。
        /// </summary>
        public int SceneType { get; private set; }
        /// <summary>
        /// 获取或设置所属世界。
        /// </summary>
        public World? World { get; private set; }
        /// <summary>
        /// 获取或设置所属服务器。
        /// </summary>
        public Server Server { get; private set; }
        /// <summary>
        /// SceneConfig对应的表Id。
        /// </summary>
        public uint SceneConfigId { get; private set; }
        /// <summary>
        /// 获取关联的服务端Network网络实例。
        /// </summary>
        public ANetwork Network { get; private set; }
#endif

        #endregion

        #region Component

        public TimerComponent TimerComponent { get; private set; }
        public EntityComponent EntityComponent { get; private set; }
        public CoroutineLockComponent CoroutineLockComponent { get; private set; }
        public MessageDispatcherComponent MessageDispatcherComponent { get; private set; }
        public NetworkMessagingComponent NetworkMessagingComponent { get; private set; }
        
        private async FTask Initialize(Scene? scene = null)
        {
            if (scene == null)
            {
                EntityComponent = Create<EntityComponent>(Scene, false, false);
                await EntityComponent.Initialize();
                TimerComponent = AddComponent<TimerComponent>(false);
                CoroutineLockComponent = AddComponent<CoroutineLockComponent>(false);
                MessageDispatcherComponent = AddComponent<MessageDispatcherComponent>(false);
                NetworkMessagingComponent = AddComponent<NetworkMessagingComponent>(false);
                await MessageDispatcherComponent.Initialize();
#if FANTASY_UNITY
                AssetBundleComponent = AddComponent<AssetBundleComponent>(false);
                await AssetBundleComponent.Initialize();
                Instance = this;
#endif
                return;
            }

            TimerComponent = scene.TimerComponent;
            EntityComponent = scene.EntityComponent;
            CoroutineLockComponent = scene.CoroutineLockComponent;
            MessageDispatcherComponent = scene.MessageDispatcherComponent;
#if FANTASY_UNITY
            AssetBundleComponent = scene.AssetBundleComponent;
#endif
        }

        #endregion

        #region Entities

        public readonly Dictionary<long, Entity> Entities = new Dictionary<long, Entity>();
        
        /// <summary>
        /// 获取指定运行时ID的实体对象
        /// </summary>
        /// <param name="runTimeId">运行时ID</param>
        /// <returns>实体对象</returns>
        public Entity GetEntity(long runTimeId)
        {
            return Entities.GetValueOrDefault(runTimeId);
        }

        /// <summary>
        /// 获取指定运行时ID的实体对象。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="runTimeId">要获取的实体的运行时ID。</param>
        /// <returns>找到的实体对象，如果不存在则返回默认值。</returns>
        public T GetEntity<T>(long runTimeId) where T : Entity, new()
        {
            if (!Entities.TryGetValue(runTimeId, out var entity))
            {
                return default;
            }

            return (T)entity;
        }

        /// <summary>
        /// 尝试获取指定运行时ID的实体对象
        /// </summary>
        /// <param name="runTimeId">运行时ID</param>
        /// <param name="entity">输出参数，实体对象</param>
        /// <returns>是否获取成功</returns>
        public bool TryGetEntity(long runTimeId, out Entity entity)
        {
            return Entities.TryGetValue(runTimeId, out entity);
        }

        #endregion
    }
}