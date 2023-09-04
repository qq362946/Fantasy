using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fantasy
{
    /// <summary>
    /// 用于异步任务方法的构建器。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct AsyncFTaskMethodBuilder
    {
        // 1. 静态的 Create 方法。
        /// <summary>
        /// 创建一个新的异步任务构建器。
        /// </summary>
        /// <returns>异步任务构建器。</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncFTaskMethodBuilder Create()
        {
            return new AsyncFTaskMethodBuilder(FTask.Create());
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AsyncFTaskMethodBuilder(FTask fTask)
        {
            Task = fTask;
        }

        // 4. 返回任务
        /// <summary>
        /// 获取由该构建器创建的异步任务。
        /// </summary>
        public FTask Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        // 2. 开始
        /// <summary>
        /// 启动异步状态机以开始执行异步任务。
        /// </summary>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        // 3. 设置结果
        /// <summary>
        /// 将异步任务标记为已完成。
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            Task.SetResult();
        }

        /// <summary>
        /// 将异步任务标记为已完成，但带有异常信息。
        /// </summary>
        /// <param name="exception">表示任务失败的异常信息。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            Task.SetException(exception);
        }

        /// <summary>
        /// 在任务完成时异步等待操作完成，并在操作完成时继续异步执行。
        /// </summary>
        /// <typeparam name="TAwaiter">等待操作的awaiter类型。</typeparam>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="awaiter">等待操作的awaiter实例的引用。</param>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        /// <summary>
        /// 在任务完成时异步等待操作完成（不安全），并在操作完成时继续异步执行。
        /// </summary>
        /// <typeparam name="TAwaiter">等待操作的awaiter类型。</typeparam>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="awaiter">等待操作的awaiter实例的引用。</param>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
            ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
        /// <summary>
        /// 设置异步状态机的状态。
        /// </summary>
        /// <param name="stateMachine">异步状态机实例。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }

    /// <summary>
    /// 表示用于构建泛型异步任务方法的构建器。
    /// </summary>
    /// <typeparam name="T">异步任务的结果类型。</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct AsyncFTaskMethodBuilder<T>
    {
        // 1. 静态的 Create 方法。
        /// <summary>
        /// 创建一个新的泛型异步任务构建器。
        /// </summary>
        /// <returns>泛型异步任务构建器。</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncFTaskMethodBuilder<T> Create()
        {
            return new AsyncFTaskMethodBuilder<T>(FTask<T>.Create());
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AsyncFTaskMethodBuilder(FTask<T> fTask)
        {
            Task = fTask;
        }

        // 4. 返回任务
        /// <summary>
        /// 获取由该构建器创建的泛型异步任务。
        /// </summary>
        public FTask<T> Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        // 2. 开始
        /// <summary>
        /// 启动异步状态机以开始执行泛型异步任务。
        /// </summary>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        // 3. 设置结果
        /// <summary>
        /// 将泛型异步任务标记为已完成，并设置结果值。
        /// </summary>
        /// <param name="value">泛型异步任务的结果值。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T value)
        {
            Task.SetResult(value);
        }

        /// <summary>
        /// 将异步任务标记为已完成，但带有异常信息。
        /// </summary>
        /// <param name="exception">表示任务失败的异常信息。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            Task.SetException(exception);
        }

        /// <summary>
        /// 在任务完成时异步等待操作完成，并在操作完成时继续异步执行。
        /// </summary>
        /// <typeparam name="TAwaiter">等待操作的awaiter类型。</typeparam>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="awaiter">等待操作的awaiter实例的引用。</param>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        /// <summary>
        /// 在任务完成时异步等待操作完成（不安全），并在操作完成时继续异步执行。
        /// </summary>
        /// <typeparam name="TAwaiter">等待操作的awaiter类型。</typeparam>
        /// <typeparam name="TStateMachine">异步状态机的类型。</typeparam>
        /// <param name="awaiter">等待操作的awaiter实例的引用。</param>
        /// <param name="stateMachine">异步状态机实例的引用。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
            ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        /// <summary>
        /// 设置异步状态机的状态。
        /// </summary>
        /// <param name="stateMachine">异步状态机实例。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }
}