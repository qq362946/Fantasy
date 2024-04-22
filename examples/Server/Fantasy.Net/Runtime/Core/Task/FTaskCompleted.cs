using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fantasy
{
    /// <summary>
    /// 已完成的异步任务结构。
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncFTaskCompletedMethodBuilder))]
    [StructLayout(LayoutKind.Auto)]
    public struct FTaskCompleted : INotifyCompletion
    {
        /// <summary>
        /// 获取一个等待器以等待此已完成的异步任务。
        /// </summary>
        /// <returns>一个等待器。</returns>
        [DebuggerHidden]
        public FTaskCompleted GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// 获取一个值，表示异步任务是否已经完成。
        /// </summary>
        [DebuggerHidden] public bool IsCompleted => true;

        /// <summary>
        /// 获取异步任务的结果。
        /// </summary>
        [DebuggerHidden]
        public void GetResult()
        {
            // 由于任务已完成，无需返回结果。这个方法保持为空。
        }

        /// <summary>
        /// 指定在异步操作完成时要执行的继续操作。
        /// </summary>
        /// <param name="continuation">要执行的继续操作。</param>
        [DebuggerHidden]
        public void OnCompleted(Action continuation)
        {
            // 由于任务已完成，不需要执行额外的继续操作，因此保持为空。
        }

        /// <summary>
        /// 指定在异步操作完成时要执行的不安全继续操作。
        /// </summary>
        /// <param name="continuation">要执行的不安全继续操作。</param>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation)
        {
        }
    }
}