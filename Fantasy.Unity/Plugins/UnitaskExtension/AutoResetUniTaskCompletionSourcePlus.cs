using System;
using System.Diagnostics;
using System.Threading;

namespace Cysharp.Threading.Tasks
{
    public class AutoResetUniTaskCompletionSourcePlus : IUniTaskSource, ITaskPoolNode<AutoResetUniTaskCompletionSourcePlus>, IPromise
    {
        static TaskPool<AutoResetUniTaskCompletionSourcePlus> pool;
        AutoResetUniTaskCompletionSourcePlus nextNode;
        event Action onExceptionAction;
        event Action onCancelAction;
        event Action onResultAction;
        public ref AutoResetUniTaskCompletionSourcePlus NextNode => ref nextNode;

        static AutoResetUniTaskCompletionSourcePlus()
        {
            TaskPool.RegisterSizeGetter(typeof(AutoResetUniTaskCompletionSourcePlus), () => pool.Size);
        }

        UniTaskCompletionSourceCore<AsyncUnit> core;

        AutoResetUniTaskCompletionSourcePlus()
        {
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus Create()
        {
            if (!pool.TryPop(out var result))
            {
                result = new AutoResetUniTaskCompletionSourcePlus();
            }
            TaskTracker.TrackActiveTask(result, 2);
            return result;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus CreateFromCanceled(CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus CreateFromException(Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus CreateCompleted(out short token)
        {
            var source = Create();
            source.TrySetResult();
            token = source.core.Version;
            return source;
        }

        public void AddOnCancelAction(Action action)
        {
            onCancelAction += action;
        }

        public void AddOnExceptionAction(Action action)
        {
            onExceptionAction += action;
        }

        public void AddOnResultAction(Action action)
        {
            onResultAction += action;
        }

        public void RemoveOnCancelAction(Action action)
        {
            onCancelAction -= action;
        }

        public void RemoveOnExceptionAction(Action action)
        {
            onExceptionAction -= action;
        }

        public void RemoveOnResultAction(Action action)
        {
            onResultAction -= action;
        }

        public UniTask Task
        {
            [DebuggerHidden]
            get
            {
                return new UniTask(this, core.Version);
            }
        }

        [DebuggerHidden]
        public bool TrySetResult()
        {
            onResultAction?.Invoke();
            onResultAction = null;
            return core.TrySetResult(AsyncUnit.Default);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            onCancelAction?.Invoke();
            onCancelAction = null;
            return core.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            onExceptionAction?.Invoke();
            onExceptionAction = null;
            return core.TrySetException(exception);
        }

        [DebuggerHidden]
        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        [DebuggerHidden]
        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            onExceptionAction = null;
            onCancelAction = null;
            onResultAction = null;
            core.Reset();
            return pool.TryPush(this);
        }
    }
    
    public class AutoResetUniTaskCompletionSourcePlus<T> : IUniTaskSource<T>, ITaskPoolNode<AutoResetUniTaskCompletionSourcePlus<T>>, IPromise<T>
    {
        static TaskPool<AutoResetUniTaskCompletionSourcePlus<T>> pool;
        AutoResetUniTaskCompletionSourcePlus<T> nextNode;
        event Action onExceptionAction;
        event Action onCancelAction;
        event Action onResultAction;
        public ref AutoResetUniTaskCompletionSourcePlus<T> NextNode => ref nextNode;

        static AutoResetUniTaskCompletionSourcePlus()
        {
            TaskPool.RegisterSizeGetter(typeof(AutoResetUniTaskCompletionSourcePlus<T>), () => pool.Size);
        }

        UniTaskCompletionSourceCore<T> core;

        AutoResetUniTaskCompletionSourcePlus()
        {
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus<T> Create()
        {
            if (!pool.TryPop(out var result))
            {
                result = new AutoResetUniTaskCompletionSourcePlus<T>();
            }
            TaskTracker.TrackActiveTask(result, 2);
            return result;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus<T> CreateFromCanceled(CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus<T> CreateFromException(Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSourcePlus<T> CreateFromResult(T result, out short token)
        {
            var source = Create();
            source.TrySetResult(result);
            token = source.core.Version;
            return source;
        }

        public void AddOnCancelAction(Action action)
        {
            onCancelAction += action;
        }

        public void AddOnExceptionAction(Action action)
        {
            onExceptionAction += action;
        }

        public void AddOnResultAction(Action action)
        {
            onResultAction += action;
        }
        
        public void RemoveOnCancelAction(Action action)
        {
            onCancelAction -= action;
        }

        public void RemoveOnExceptionAction(Action action)
        {
            onExceptionAction -= action;
        }

        public void RemoveOnResultAction(Action action)
        {
            onResultAction -= action;
        }

        public UniTask<T> Task
        {
            [DebuggerHidden]
            get
            {
                return new UniTask<T>(this, core.Version);
            }
        }

        [DebuggerHidden]
        public bool TrySetResult(T result)
        {
            onResultAction?.Invoke();
            onResultAction = null;
            return core.TrySetResult(result);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            onCancelAction?.Invoke();
            onCancelAction = null;
            return core.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            onExceptionAction?.Invoke();
            onExceptionAction = null;
            return core.TrySetException(exception);
        }

        [DebuggerHidden]
        public T GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        [DebuggerHidden]
        void IUniTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        [DebuggerHidden]
        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            onExceptionAction = null;
            onCancelAction = null;
            onResultAction = null;
            core.Reset();
            return pool.TryPush(this);
        }
    }
}