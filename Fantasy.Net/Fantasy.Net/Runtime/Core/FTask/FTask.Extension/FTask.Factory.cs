#if !FANTASY_WEBGL
using System.Collections.Concurrent;
#endif
using System.Diagnostics;
using System.Runtime.CompilerServices;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy
{
    public partial class FTask
    {
        private bool _isPool;
#if FANTASY_WEBGL
        private static readonly Queue<FTask> Caches = new Queue<FTask>();
#else
        private static readonly ConcurrentQueue<FTask> Caches = new ConcurrentQueue<FTask>();
#endif
        public static FTaskCompleted CompletedTask => new FTaskCompleted();

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FTask Create(bool isPool = true)
        {
            if (!isPool)
            {
                return new FTask();
            }

            if (!Caches.TryDequeue(out var fTask))
            {
                fTask = new FTask();
            }

            fTask._isPool = true;
            return fTask;
        }

        private FTask()
        {
            FTaskType = FTaskType.Task;
        }

        private void Return()
        {
            if (!_isPool || Caches.Count > 2000)
            {
                return;
            }

            _callBack = null;
            UserToKen = null;
            FTaskType = FTaskType.Task;
            _status = STaskStatus.Pending;
            Caches.Enqueue(this);
        }
    }

    public partial class FTask<T>
    {
        private bool _isPool;
#if FANTASY_WEBGL
        private static readonly Queue<FTask<T>> Caches = new Queue<FTask<T>>();
#else
        private static readonly ConcurrentQueue<FTask<T>> Caches = new ConcurrentQueue<FTask<T>>();
#endif
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FTask<T> Create(bool isPool = true)
        {
            if (!isPool)
            {
                return new FTask<T>();
            }

            if (!Caches.TryDequeue(out var fTask))
            {
                fTask = new FTask<T>();
            }

            fTask._isPool = true;
            return fTask;
        }
        
        private FTask()
        {
            FTaskType = FTaskType.Task;
        }

        private void Return()
        {
            if (!_isPool || Caches.Count > 2000)
            {
                return;
            }
            
            _callBack = null;
            UserToKen = null;
            FTaskType = FTaskType.Task;
            _status = STaskStatus.Pending;
            Caches.Enqueue(this);
        }
    }
}