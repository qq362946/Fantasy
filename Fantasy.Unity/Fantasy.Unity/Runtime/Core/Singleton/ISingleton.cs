using System;
using System.Threading.Tasks;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy
{
    /// <summary>
    /// 定义一个单例接口，表示可以被初始化并在销毁时进行处理。
    /// </summary>
    public interface ISingleton : IAssembly
    {
        /// <summary>
        /// 获取或设置一个值，指示实例是否已被销毁。
        /// </summary>
        public bool IsDisposed { get; set; }

        /// <summary>
        /// 异步初始化单例实例的方法。
        /// </summary>
        /// <returns>表示异步操作的任务。</returns>
        public Task Initialize();
    }
    
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
        private Task Register(ISingleton singleton)
        {
            if (Instance != null)
            {
                return Task.CompletedTask;
            }
            
            Instance = (T) singleton;
            return AssemblySystem.Register(singleton);
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
        /// <param name="assemblyIdentity"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual Task Load(long assemblyIdentity)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 在程序集重新加载时执行的方法。
        /// </summary>
        /// <param name="assemblyIdentity"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual Task ReLoad(long assemblyIdentity)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 在程序集卸载时执行的方法。
        /// </summary>
        /// <param name="assemblyIdentity"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual Task OnUnLoad(long assemblyIdentity)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放单例的方法。
        /// </summary>
        public virtual void Dispose()
        {
            var instance = Instance;
            IsDisposed = true;
            Instance = default;
            AssemblySystem.UnRegister(instance);
        }
    }
}