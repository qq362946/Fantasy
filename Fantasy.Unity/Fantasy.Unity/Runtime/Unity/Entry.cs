using System.Reflection;
using UnityEngine;

namespace Fantasy
{
    public struct OnAppStart
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
            // 初始化框架
            // 初始化程序集管理系统
            AssemblySystem.Initialize(assemblies);
            // 初始化单例管理系统
            await SingletonSystem.Initialize();
            // 精度处理（只针对Windows下有作用、其他系统没有这个问题、一般也不会用Windows来做服务器的）
            WinPeriod.Initialize();
            // 创建一个GameObject到Unity中
            new GameObject("Fantasy.Unity").AddComponent<Entry>();
            // 框架需要一个Scene来驱动、所以要创建一个Scene、后面所有的框架都会在这个Scene下
            // 也就是把这个Scene给卸载掉、框架的东西都会清除掉
            Scene = Scene.Create();
            // 初始化配置表
            ConfigTableHelper.Instance.Init();
            return Scene;
        }

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            ThreadScheduler.Update();
        }
        
        private void OnApplicationQuit()
        {
            AssemblySystem.Dispose();
            SingletonSystem.Instance.Dispose();
            Scene?.Dispose();
        }
    }
}