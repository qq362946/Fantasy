using System;
using System.Threading.Tasks;

namespace Fantasy
{
    /// <summary>
    /// 定义一个单例接口，表示可以被初始化并在销毁时进行处理。
    /// </summary>
    public interface ISingleton : IDisposable
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
}