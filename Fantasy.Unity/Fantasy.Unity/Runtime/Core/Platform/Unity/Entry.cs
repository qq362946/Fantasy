#if FANTASY_UNITY
using System.Reflection;
using UnityEngine;

namespace Fantasy
{
    public sealed class FantasyObject : MonoBehaviour
    {
        public static GameObject FantasyObjectGameObject { get; private set; }
        // 这个方法将在游戏启动时自动调用
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            FantasyObjectGameObject = new GameObject("Fantasy.Net");
            DontDestroyOnLoad(FantasyObjectGameObject);
        }
        private void OnApplicationQuit()
        {
            Destroy(FantasyObjectGameObject);
        }
    }
    
    public struct OnFantasyInit
    {
        public Scene Scene;
    }
    
    public class Entry : MonoBehaviour
    {
        public static Scene Scene { get; private set; }
        /// <summary>
        /// 初始化框架
        /// </summary>
        public static async FTask<Scene> Initialize(params Assembly[] assemblies)
        {
            Scene?.Dispose();
            // 初始化程序集管理系统
            AssemblySystem.Initialize(assemblies);
#if FANTASY_WEBGL
            ThreadSynchronizationContext.Initialize();
#endif
            FantasyObject.FantasyObjectGameObject.AddComponent<Entry>();
            Scene = await Scene.Create(SceneRuntimeType.MainThread);
            await Scene.EventComponent.PublishAsync(new OnFantasyInit()
            {
                Scene = Scene
            });
            return Scene;
        }
        
        private void Update()
        {
            ThreadScheduler.Update();
        }

        private void OnDestroy()
        {
            AssemblySystem.Dispose();
            if (Scene != null)
            {
                Scene?.Dispose();
                Scene = null;
            }
        }
    }
}
#endif