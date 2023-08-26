using System;

namespace Fantasy
{
    /// <summary>
    /// 提供用于异步任务操作的静态方法和对象创建。
    /// </summary>
    public partial class FTask
    {
        /// <summary>
        /// 获取一个已完成的异步任务实例。
        /// </summary>
        public static FTaskCompleted CompletedTask => new();

        /// <summary>
        /// 使用指定的工厂方法创建并运行一个异步任务。
        /// </summary>
        /// <param name="factory">用于创建异步任务的工厂方法。</param>
        /// <returns>创建并运行的异步任务。</returns>
        public static FTask Run(Func<FTask> factory)
        {
            return factory();
        }

        /// <summary>
        /// 使用指定的工厂方法创建并运行一个带有结果的异步任务。
        /// </summary>
        /// <typeparam name="T">异步任务的结果类型。</typeparam>
        /// <param name="factory">用于创建异步任务的工厂方法。</param>
        /// <returns>创建并运行的异步任务。</returns>
        public static FTask<T> Run<T>(Func<FTask<T>> factory)
        {
            return factory();
        }

        /// <summary>
        /// 从指定的值创建一个已完成的异步任务。
        /// </summary>
        /// <typeparam name="T">异步任务的结果类型。</typeparam>
        /// <param name="value">异步任务的结果值。</param>
        /// <returns>已完成的异步任务。</returns>
        public static FTask<T> FromResult<T>(T value)
        {
            var sAwaiter = FTask<T>.Create();
            sAwaiter.SetResult(value);
            return sAwaiter;
        }
    }
}