using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Fantasy
{
    /// <summary>
    /// 用于构建已完成的异步任务方法的构建器。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncFTaskCompletedMethodBuilder
    {
        // 1. 静态的 Create 方法。
        /// <summary>
        /// 创建一个新的已完成的异步任务构建器。
        /// </summary>
        /// <returns>已完成的异步任务构建器。</returns>
        [DebuggerHidden]
        public static AsyncFTaskCompletedMethodBuilder Create()
        {
            return new AsyncFTaskCompletedMethodBuilder();
        }

        // 2. TaskLike Task property(void)
        /// <summary>
        /// 获取表示已完成的异步任务。
        /// </summary>
        public FTaskCompleted Task => default;

        // 3. SetException
        /// <summary>
        /// 将已完成的异步任务标记为发生异常。
        /// </summary>
        /// <param name="exception">表示任务失败的异常信息。</param>
        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            Log.Error(exception);
            // ExceptionDispatchInfo.Capture(exception).Throw();
        }

        // 4. SetResult
        /// <summary>
        /// 将已完成的异步任务标记为已完成。
        /// </summary>
        [DebuggerHidden]
        public void SetResult()
        {
        }

        // 5. AwaitOnCompleted
        /// <summary>
        /// 在任务完成时异步等待操作完成，并在操作完成时继续异步执行。
        /// </summary>
        /// <typeparam name="TAwaiter">等待操作的awaiter类型。</typeparam>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="awaiter">等待操作的awaiter实例的引用。</param>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        // 6. AwaitUnsafeOnCompleted
        /// <summary>
        /// 在任务完成时异步等待操作完成（不安全），并在操作完成时继续异步执行。
        /// </summary>
        /// <typeparam name="TAwaiter">等待操作的awaiter类型。</typeparam>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="awaiter">等待操作的awaiter实例的引用。</param>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
            ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        // 7. Start
        /// <summary>
        /// 启动异步状态机以开始执行已完成的异步任务。
        /// </summary>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        // 8. SetStateMachine
        /// <summary>
        /// 设置异步状态机的状态。
        /// </summary>
        /// <param name="stateMachine">异步状态机实例。</param>
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }
}