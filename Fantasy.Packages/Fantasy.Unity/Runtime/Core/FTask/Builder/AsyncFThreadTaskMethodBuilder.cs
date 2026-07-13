#if !FANTASY_WEBGL && !UNITY_WEBGL
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Async
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct AsyncFThreadTaskMethodBuilder
    {
        public FThreadTask Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncFThreadTaskMethodBuilder Create()
        {
            return new AsyncFThreadTaskMethodBuilder(FThreadTask.Create());
        }

        public AsyncFThreadTaskMethodBuilder(FThreadTask task)
        {
            Task = task;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            Task.SetResult();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            Task.SetException(exception);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct AsyncFThreadTaskMethodBuilder<T>
    {
        public FThreadTask<T> Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncFThreadTaskMethodBuilder<T> Create()
        {
            return new AsyncFThreadTaskMethodBuilder<T>(FThreadTask<T>.Create());
        }

        public AsyncFThreadTaskMethodBuilder(FThreadTask<T> task)
        {
            Task = task;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            Task.SetResult(result);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            Task.SetException(exception);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
}
#endif
