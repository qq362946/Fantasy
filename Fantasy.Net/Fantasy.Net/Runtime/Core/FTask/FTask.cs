using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
#pragma warning disable CS8601
#pragma warning disable CS8603
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 表示异步任务的状态。
    /// </summary>
    public enum STaskStatus : byte
    {
        Pending = 0, // The operation has not yet completed.
        Succeeded = 1, // The operation completed successfully.
        Faulted = 2 // The operation completed with an error.
    }
    
    /// <summary>
    /// 轻量级异步任务类。
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncFTaskMethodBuilder))]
    public sealed partial class FTask : ICriticalNotifyCompletion
    {
        private Action _callBack;
        private ExceptionDispatchInfo _exception;
        private STaskStatus _status;

        /// <summary>
        /// 获取一个值，表示异步任务是否已经完成。
        /// </summary>
        /// <returns>如果异步任务已完成，则为 true；否则为 false。</returns>
        public bool IsCompleted
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _status != STaskStatus.Pending;
        }

        /// <summary>
        /// 在异步任务完成时执行指定的操作。
        /// </summary>
        /// <param name="continuation">完成时要执行的操作。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        /// <summary>
        /// 获取用于等待异步任务完成的等待器。
        /// </summary>
        /// <returns>一个异步任务等待器。</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FTask GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// 将当前任务用于异步协程的等待。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        private async FVoid InnerCoroutine()
        {
            await this;
        }

        /// <summary>
        /// 启动当前任务作为一个协程。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void Coroutine()
        {
            InnerCoroutine().Coroutine();
        }

        /// <summary>
        /// 获取异步任务的执行结果。
        /// </summary>
        /// <exception cref="NotSupportedException">不支持直接调用 GetResult。</exception>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            switch (_status)
            {
                case STaskStatus.Succeeded:
                {
                    Recycle();
                    break;
                }
                case STaskStatus.Faulted:
                {
                    Recycle();
                    
                    if (_exception != null)
                    {
                        var exception = _exception;
                        _exception = null;
                        exception.Throw();
                    }

                    break;
                }
                default:
                    throw new NotSupportedException("Direct call to getResult is not allowed");
            }
        }

        /// <summary>
        /// 将异步任务对象进行回收，以供后续重用。
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Recycle()
        {
            if (!IsPool)
            {
                return;
            }

            _status = STaskStatus.Pending;
            _callBack = null;
#if FANTASY_WEBGL
            Pool<FTask>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }

        /// <summary>
        /// 设置异步任务的执行结果。
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (_status != STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            _status = STaskStatus.Succeeded;

            if (_callBack == null)
            {
                return;
            }

            var callBack = _callBack;
            _callBack = null;
            callBack.Invoke();
        }

        /// <summary>
        /// 在任务未完成时，注册一个操作，以便在任务完成时执行。
        /// 如果任务已经完成，操作将立即执行。
        /// </summary>
        /// <param name="continuation">要注册的操作。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (_status != STaskStatus.Pending)
            {
                continuation?.Invoke();
                return;
            }

            _callBack = continuation;
        }

        /// <summary>
        /// 设置任务为异常完成状态，并指定异常信息。
        /// 如果任务已经完成，将引发异常。
        /// </summary>
        /// <param name="exception">要关联的异常信息。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_status != STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            _status = STaskStatus.Faulted;
            _exception = ExceptionDispatchInfo.Capture(exception);
            _callBack?.Invoke();
        }
    }

    /// <summary>
    /// 表示一个轻量级的异步任务（Future Task），提供类似于 Task 的异步编程模型，但仅适用于某些简单的异步操作。
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncFTaskMethodBuilder<>))]
    public sealed partial class FTask<T> : ICriticalNotifyCompletion
    {
        private Action _callBack;
        private ExceptionDispatchInfo _exception;
        private STaskStatus _status;
        private T _value;
        public object UserToKen;

        /// <summary>
        /// 获取一个值，表示异步任务是否已经完成。
        /// </summary>
        public bool IsCompleted
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _status != STaskStatus.Pending;
        }

        /// <summary>
        /// 在任务未完成时，注册一个操作，以便在任务完成时执行。
        /// 如果任务已经完成，操作将立即执行。
        /// </summary>
        /// <param name="continuation">要注册的操作。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        /// <summary>
        /// 获取一个等待任务完成的 awaiter。
        /// </summary>
        /// <returns>用于等待异步任务的 awaiter。</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FTask<T> GetAwaiter()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        private async FVoid InnerCoroutine()
        {
            await this;
        }

        /// <summary>
        /// 将任务转换为协程进行等待。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void Coroutine()
        {
            InnerCoroutine().Coroutine();
        }

        /// <summary>
        /// 获取异步任务的结果。
        /// </summary>
        /// <returns>异步任务的结果。</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult()
        {
            switch (_status)
            {
                case STaskStatus.Succeeded:
                {
                    var value = _value;
                    Recycle();
                    return value;
                }
                case STaskStatus.Faulted:
                {
                    Recycle();
                    
                    if (_exception == null)
                    {
                        return default;
                    }

                    var exception = _exception;
                    _exception = null;
                    exception.Throw();
                    return default;
                }
                default:
                    throw new NotSupportedException("Direct call to getResult is not allowed");
            }
        }

        /// <summary>
        /// 回收任务对象，将其放回对象池。
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Recycle()
        {
            if (!IsPool)
            {
                return;
            }

            _status = STaskStatus.Pending;
            _callBack = null;
            _value = default;
#if FANTASY_WEBGL
            Pool<FTask<T>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }

        /// <summary>
        /// 设置异步任务的成功结果。
        /// </summary>
        /// <param name="value">异步任务的结果值。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T value)
        {
            if (_status != STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            _value = value;
            _status = STaskStatus.Succeeded;

            if (_callBack == null)
            {
                return;
            }

            var callBack = _callBack;
            _callBack = null;
            callBack.Invoke();
        }

        /// <summary>
        /// 在任务未完成时，注册一个操作，以便在任务完成时执行。
        /// 如果任务已经完成，操作将立即执行。
        /// </summary>
        /// <param name="continuation">要注册的操作。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (_status != STaskStatus.Pending)
            {
                continuation?.Invoke();
                return;
            }

            _callBack = continuation;
        }

        /// <summary>
        /// 设置异步任务的异常结果。
        /// </summary>
        /// <param name="exception">要关联的异常信息。</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_status != STaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has been completed");
            }

            _status = STaskStatus.Faulted;
            _exception = ExceptionDispatchInfo.Capture(exception);
            _callBack?.Invoke();
        }
    }
}

