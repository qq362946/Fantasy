using System;

namespace Fantasy
{
    /// <summary>
    /// 线程调度器接口。
    /// </summary>
    public interface IThreadScheduler : IDisposable
    {
        /// <summary>
        /// 添加一个场景。
        /// </summary>
        /// <param name="sceneSchedulerId"></param>
        void Add(long sceneSchedulerId);
        /// <summary>
        /// 更新线程。
        /// </summary>
        void Update();
    }
}