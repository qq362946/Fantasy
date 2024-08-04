using System;
using System.Collections.Generic;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy
{
    public sealed class CoroutineLockPool : PoolCore<CoroutineLock>
    {
        public CoroutineLockPool() : base(2000) { }
    }
    
    public sealed class CoroutineLock : IPool, IDisposable
    {
        private Scene _scene;
        private CoroutineLockComponent _coroutineLockComponent;
        private readonly Dictionary<long, CoroutineLockQueue> _queue = new Dictionary<long, CoroutineLockQueue>();

        public bool IsPool { get; set; }
        public long CoroutineLockType { get; private set; }

        internal void Initialize(CoroutineLockComponent coroutineLockComponent, ref long coroutineLockType)
        {
            _scene = coroutineLockComponent.Scene;
            CoroutineLockType = coroutineLockType;
            _coroutineLockComponent = coroutineLockComponent;
        }
        
        public void Dispose()
        {
            foreach (var (_, coroutineLockQueue) in _queue)
            {
                while (TryCoroutineLockQueueDequeue(coroutineLockQueue)) { }
            }
            
            _queue.Clear();
            _scene = null;
            CoroutineLockType = 0;
            _coroutineLockComponent = null;
        }

        public async FTask<WaitCoroutineLock> Wait(long coroutineLockQueueKey, string tag = null, int timeOut = 30000)
        {
            var waitCoroutineLock = _coroutineLockComponent.WaitCoroutineLockPool.Rent(this, ref coroutineLockQueueKey, tag, timeOut);
            
            if (!_queue.TryGetValue(coroutineLockQueueKey, out var queue))
            {
                queue = _coroutineLockComponent.CoroutineLockQueuePool.Rent();
                _queue.Add(coroutineLockQueueKey, queue);
                return waitCoroutineLock;
            }
            
            queue.Enqueue(waitCoroutineLock);
            return await waitCoroutineLock.Tcs;
        }

        public void Release(long coroutineLockQueueKey)
        {
            if (!_queue.TryGetValue(coroutineLockQueueKey, out var coroutineLockQueue))
            {
                return;
            }
            
            if (!TryCoroutineLockQueueDequeue(coroutineLockQueue))
            {
                _queue.Remove(coroutineLockQueueKey);
            }
        }

        private bool TryCoroutineLockQueueDequeue(CoroutineLockQueue coroutineLockQueue)
        {
            if (!coroutineLockQueue.TryDequeue(out var waitCoroutineLock))
            {
                _coroutineLockComponent.CoroutineLockQueuePool.Return(coroutineLockQueue);
                return false;
            }
            
            if (waitCoroutineLock.TimerId != 0)
            {
                _scene.TimerComponent.Net.Remove(waitCoroutineLock.TimerId);
            }

            try
            {
                // 放到下一帧执行,如果不这样会导致逻辑的顺序不正常。
                _scene.ThreadSynchronizationContext.Post(waitCoroutineLock.SetResult);
            }
            catch (Exception e)
            {
                Log.Error($"Error in disposing CoroutineLock: {e}");
            }

            return true;
        }
    }
}