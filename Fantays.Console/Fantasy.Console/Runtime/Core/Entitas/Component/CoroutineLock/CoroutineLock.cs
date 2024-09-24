using System;
using System.Collections.Generic;
using Fantasy.Pool;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy.Async
{
    /// <summary>
    /// 协程锁专用的对象池
    /// </summary>
    public sealed class CoroutineLockPool : PoolCore<CoroutineLock>
    {
        /// <summary>
        /// 协程锁专用的对象池的构造函数
        /// </summary>
        public CoroutineLockPool() : base(2000) { }
    }
    
    /// <summary>
    /// 协程锁
    /// </summary>
    public sealed class CoroutineLock : IPool, IDisposable
    {
        private Scene _scene;
        private CoroutineLockComponent _coroutineLockComponent;
        private readonly Dictionary<long, CoroutineLockQueue> _queue = new Dictionary<long, CoroutineLockQueue>();
        /// <summary>
        /// 表示是否是对象池中创建的
        /// </summary>
        private bool _isPool;
        /// <summary>
        /// 协程锁的类型
        /// </summary>
        public long CoroutineLockType { get; private set; }

        internal void Initialize(CoroutineLockComponent coroutineLockComponent, ref long coroutineLockType)
        {
            _scene = coroutineLockComponent.Scene;
            CoroutineLockType = coroutineLockType;
            _coroutineLockComponent = coroutineLockComponent;
        }
        /// <summary>
        /// 销毁协程锁，如果调用了该方法，所有使用当前协程锁等待的逻辑会按照顺序释放锁。
        /// </summary>
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
        /// <summary>
        /// 等待上一个任务完成
        /// </summary>
        /// <param name="coroutineLockQueueKey">需要等待的Id</param>
        /// <param name="tag">用于查询协程锁的标记，可不传入，只有在超时的时候排查是哪个锁超时时使用</param>
        /// <param name="timeOut">等待多久会超时，当到达设定的时候会把当前锁给按照超时处理</param>
        /// <returns></returns>
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
        /// <summary>
        /// 按照先入先出的顺序，释放最早的一个协程锁
        /// </summary>
        /// <param name="coroutineLockQueueKey"></param>
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

        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }
    }
}