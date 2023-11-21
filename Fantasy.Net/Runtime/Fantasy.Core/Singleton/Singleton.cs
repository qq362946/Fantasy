using System.Threading.Tasks;
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 抽象的单例基类，继承自 <see cref="ISingleton"/> 接口。
    /// </summary>
    /// <typeparam name="T">单例类型。</typeparam>
    public abstract class Singleton<T> : ISingleton where T : ISingleton, new()
    {
        /// <summary>
        /// 获取或设置单例是否已被销毁。
        /// </summary>
        public bool IsDisposed { get; set; }

        /// <summary>
        /// 获取单例的实例。
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// 注册单例的方法。
        /// </summary>
        /// <param name="singleton">单例对象。</param>
        private void RegisterSingleton(ISingleton singleton)
        {
            Instance = (T) singleton;
            AssemblyManager.OnLoadAssemblyEvent += OnLoad;
            AssemblyManager.OnUnLoadAssemblyEvent += OnUnLoad;
        }

        /// <summary>
        /// 初始化单例的方法。
        /// </summary>
        /// <returns>表示异步操作的任务。</returns>
        public virtual Task Initialize()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 在程序集加载时执行的方法。
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        protected virtual void OnLoad(int assemblyName) { }

        /// <summary>
        /// 在程序集卸载时执行的方法。
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        protected virtual void OnUnLoad(int assemblyName) { }

        /// <summary>
        /// 释放单例的方法。
        /// </summary>
        public virtual void Dispose()
        {
            IsDisposed = true;
            Instance = default;
            AssemblyManager.OnLoadAssemblyEvent -= OnLoad;
            AssemblyManager.OnUnLoadAssemblyEvent -= OnUnLoad;
        }
    }
}