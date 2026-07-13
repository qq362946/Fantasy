#if !FANTASY_WEBGL && !UNITY_WEBGL
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable CheckNamespace
// ReSharper disable ConditionIsAlwaysTrueOrFalse
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.Async
{
    [AsyncMethodBuilder(typeof(AsyncFThreadTaskMethodBuilder))]
    public sealed partial class FThreadTask : ICriticalNotifyCompletion
    {
        private static readonly Action CompletedSentinel = static () => { };

        private Action _callBack;
        private ExceptionDispatchInfo _exception;
        private int _status = (int)STaskStatus.Pending;

        public bool IsCompleted
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ReferenceEquals(Volatile.Read(ref _callBack), CompletedSentinel);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FThreadTask GetAwaiter() => this;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async FVoid InnerCoroutine()
        {
            await this;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Coroutine()
        {
            InnerCoroutine().Coroutine();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            if (!IsCompleted)
            {
                throw new NotSupportedException("Direct call to getResult is not allowed");
            }

            var status = (STaskStatus)Volatile.Read(ref _status);
            var exception = status == STaskStatus.Faulted
                ? Interlocked.Exchange(ref _exception, null)
                : null;

            Return();

            if (status == STaskStatus.Faulted)
            {
                exception?.Throw();
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (Interlocked.CompareExchange(
                    ref _status,
                    (int)STaskStatus.Succeeded,
                    (int)STaskStatus.Pending) != (int)STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            InvokeContinuation();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action action)
        {
            UnsafeOnCompleted(action);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var previous = Interlocked.CompareExchange(ref _callBack, action, null);

            if (previous == null)
            {
                return;
            }

            if (ReferenceEquals(previous, CompletedSentinel))
            {
                action.Invoke();
                return;
            }

            throw new InvalidOperationException("FThreadTask only supports one awaiter");
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);

            if (Interlocked.CompareExchange(
                    ref _status,
                    (int)STaskStatus.Faulted,
                    (int)STaskStatus.Pending) != (int)STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            _exception = exceptionDispatchInfo;
            InvokeContinuation();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeContinuation()
        {
            var callBack = Interlocked.Exchange(ref _callBack, CompletedSentinel);

            if (callBack != null && !ReferenceEquals(callBack, CompletedSentinel))
            {
                callBack.Invoke();
            }
        }
    }

    [AsyncMethodBuilder(typeof(AsyncFThreadTaskMethodBuilder<>))]
    public sealed partial class FThreadTask<T> : ICriticalNotifyCompletion
    {
        private static readonly Action CompletedSentinel = static () => { };

        private T _value;
        private Action _callBack;
        private ExceptionDispatchInfo _exception;
        private int _status = (int)STaskStatus.Pending;

        public bool IsCompleted
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ReferenceEquals(Volatile.Read(ref _callBack), CompletedSentinel);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FThreadTask<T> GetAwaiter() => this;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async FVoid InnerCoroutine()
        {
            await this;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Coroutine()
        {
            InnerCoroutine().Coroutine();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult()
        {
            if (!IsCompleted)
            {
                throw new NotSupportedException("Direct call to getResult is not allowed");
            }

            var status = (STaskStatus)Volatile.Read(ref _status);
            var value = _value;
            var exception = status == STaskStatus.Faulted
                ? Interlocked.Exchange(ref _exception, null)
                : null;

            Return();

            if (status == STaskStatus.Faulted)
            {
                exception?.Throw();
                return default;
            }

            return value;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T value)
        {
            if (Interlocked.CompareExchange(
                    ref _status,
                    (int)STaskStatus.Succeeded,
                    (int)STaskStatus.Pending) != (int)STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            _value = value;
            InvokeContinuation();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action action)
        {
            UnsafeOnCompleted(action);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var previous = Interlocked.CompareExchange(ref _callBack, action, null);

            if (previous == null)
            {
                return;
            }

            if (ReferenceEquals(previous, CompletedSentinel))
            {
                action.Invoke();
                return;
            }

            throw new InvalidOperationException("FThreadTask only supports one awaiter");
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);

            if (Interlocked.CompareExchange(
                    ref _status,
                    (int)STaskStatus.Faulted,
                    (int)STaskStatus.Pending) != (int)STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            _exception = exceptionDispatchInfo;
            InvokeContinuation();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeContinuation()
        {
            var callBack = Interlocked.Exchange(ref _callBack, CompletedSentinel);

            if (callBack != null && !ReferenceEquals(callBack, CompletedSentinel))
            {
                callBack.Invoke();
            }
        }
    }
}
#endif
