#if FANTASY_UNITY
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Serialize;
using UnityEngine;

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
            if (FantasyObjectGameObject != null)
            {
                return;
            }
            
            FantasyObjectGameObject = new GameObject("Fantasy.Net");
            FantasyObjectGameObject.AddComponent<FantasyObject>();
            DontDestroyOnLoad(FantasyObjectGameObject);
        }
        private void OnApplicationQuit()
        {
            Destroy(FantasyObjectGameObject);
        }
    }
    
    public class Entry : MonoBehaviour
    {
        private static bool _isInit;

        /// <summary>
        /// 初始化框架
        /// </summary>
        public static async FTask Initialize(ILog logger = null)
        {
            if (_isInit)
            {
                Log.Error("Fantasy has already been initialized and does not need to be initialized again!");
                return;
            }

            Log.Initialize(logger ?? new UnityLog());
            FantasyObject.OnRuntimeMethodLoad();
            Log.Info($"Fantasy Version:{ProgramDefine.VERSION}");
            // 初始化序列化
            await SerializerManager.Initialize();
#if FANTASY_WEBGL
            ThreadSynchronizationContext.Initialize();
#endif
            _isInit = true;
            // 设置当前程序已经在运行中
            ProgramDefine.IsAppRunning = true;
            FantasyObject.FantasyObjectGameObject.AddComponent<Entry>();
            Log.Debug("Fantasy Initialize Complete!");
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
            SerializerManager.Dispose();
            _isInit = false;
            // 设置当前程序已经在停止中
            ProgramDefine.IsAppRunning = false;
        }
    }
}
#endif