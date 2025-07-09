#if FANTASY_UNITY
using System;
using System.Linq;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Serialize;
using UnityEngine;
using UnityEngine.Scripting;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Platform.Unity
{
    public sealed class FantasyObject : MonoBehaviour
    {
        public static GameObject FantasyObjectGameObject { get; private set; }
        
        public static void OnRuntimeMethodLoad()
        {
            FantasyObjectGameObject = new GameObject("Fantasy.Net");
            DontDestroyOnLoad(FantasyObjectGameObject);
        }
        private void OnApplicationQuit()
        {
            Destroy(FantasyObjectGameObject);
        }
    }
    
    public struct OnSceneCreate
    {
        public Scene Scene;
        public object Arg;
    }
    
    public class Entry : MonoBehaviour
    {
        private static bool _isInit;
        public static Scene Scene { get; private set; }

        /// <summary>
        /// 初始化框架
        /// </summary>
        /// <param name="assemblies"></param>
        public static async FTask Initialize(params System.Reflection.Assembly[] assemblies)
        {
            if (_isInit)
            {
                Log.Error("Fantasy has already been initialized and does not need to be initialized again!");
                return;
            }
            FantasyObject.OnRuntimeMethodLoad();
            Log.Register(new UnityLog());
            await AssemblySystem.InnerInitialize(assemblies);
            // 初始化序列化
            SerializerManager.Initialize();
#if FANTASY_WEBGL
            ThreadSynchronizationContext.Initialize();
#endif
            _isInit = true;
            FantasyObject.FantasyObjectGameObject.AddComponent<Entry>();
            Log.Debug("Fantasy Initialize Complete!");
        }

        /// <summary>
        /// 在Entry中创建一个Scene，如果Scene已经被创建过，将先销毁Scene再创建。
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="sceneRuntimeMode"></param>
        /// <returns></returns>
        public static async FTask<Scene> CreateScene(object arg = null, string sceneRuntimeMode = SceneRuntimeMode.MainThread)
        {
            Scene?.Dispose();
            Scene = await Scene.Create(sceneRuntimeMode);
            await Scene.EventComponent.PublishAsync(new OnSceneCreate()
            {
                Arg = arg,
                Scene = Scene
            });
            return Scene;
        }
        
        private void Update()
        {
            ThreadScheduler.Update();
        }

        private void LateUpdate()
        {
            ThreadScheduler.LateUpdate();
        }

        private void OnDestroy()
        {
            AssemblySystem.Dispose();
            SerializerManager.Dispose();
            if (Scene != null)
            {
                Scene?.Dispose();
                Scene = null;
            }
            _isInit = false;
        }
    }
}
#endif