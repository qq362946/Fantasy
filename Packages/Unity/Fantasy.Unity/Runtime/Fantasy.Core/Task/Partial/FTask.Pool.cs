using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Fantasy
{
    /// <summary>
    /// 提供用于异步任务操作的静态方法和对象创建。
    /// </summary>
    public partial class FTask
    {
        /// <summary>
        /// 创建一个新的异步任务。
        /// </summary>
        /// <param name="isFromPool">是否从对象池中创建。</param>
        /// <returns>新的异步任务实例。</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FTask Create(bool isFromPool = true)
        {
            var task = isFromPool ? Pool<FTask>.Rent() : new FTask();
            task._isFromPool = isFromPool;
            return task;
        }
    }

    /// <summary>
    /// 提供用于异步任务操作的泛型静态方法和对象创建。
    /// </summary>
    /// <typeparam name="T">任务结果的类型。</typeparam>
    public partial class FTask<T>
    {
        /// <summary>
        /// 创建一个新的异步任务。
        /// </summary>
        /// <param name="isFromPool">是否从对象池中创建。</param>
        /// <returns>新的异步任务实例。</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FTask<T> Create(bool isFromPool = true)
        {
            var task = isFromPool ? Pool<FTask<T>>.Rent() : new FTask<T>();
            task._isFromPool = isFromPool;
            return task;
        }
    }
}