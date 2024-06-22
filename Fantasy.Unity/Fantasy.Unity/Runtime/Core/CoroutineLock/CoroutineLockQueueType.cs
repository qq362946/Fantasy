using System;
using System.Collections.Generic;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 指定键的协程锁队列类型，用于管理不同类型的协程锁队列。
    /// </summary>
    public sealed class CoroutineLockQueueType : IDisposable
    {
        private long _coroutineLockType;
        private CoroutineLockComponent CoroutineLockComponent { get; set; }
        
        private readonly Dictionary<long, CoroutineLockQueue> _coroutineLockQueues = new Dictionary<long, CoroutineLockQueue>();
        
        public void Dispose()
        {
            var singleThreadPool = CoroutineLockComponent.Scene.Pool;
            CoroutineLockComponent = null;
            
            foreach (var (_, coroutineLockQueue) in _coroutineLockQueues)
            {
                coroutineLockQueue.Dispose();
            }
            
            _coroutineLockQueues.Clear();
            singleThreadPool.Return(this);
        }

        /// <summary>
        /// 创建一个协程锁队列类型。
        /// </summary>
        /// <param name="coroutineLockComponent">CoroutineLockComponent。</param>
        /// <param name="coroutineLockType">协程锁队列类型的名称。</param>
        public static CoroutineLockQueueType Create(CoroutineLockComponent coroutineLockComponent, long coroutineLockType)
        {
            var coroutineLockQueueType = coroutineLockComponent.Scene.Pool.Rent<CoroutineLockQueueType>();
            coroutineLockQueueType._coroutineLockType = coroutineLockType;
            coroutineLockQueueType.CoroutineLockComponent = coroutineLockComponent;
            return coroutineLockQueueType;
        }

        /// <summary>
        /// 请求协程锁，获取指定键的协程锁。
        /// </summary>
        /// <param name="coroutineLockQueueKey">协程锁队列的键。</param>
        /// <param name="tag">锁标识。</param>
        /// <param name="time">等待时间。</param>
        /// <returns>等待协程锁的任务。</returns>
        public async FTask<WaitCoroutineLock> Lock(long coroutineLockQueueKey, string tag = null, int time = 4000)
        {
            if (_coroutineLockQueues.TryGetValue(coroutineLockQueueKey, out var coroutineLockQueue))
            {
                return await coroutineLockQueue.Lock(tag, time);
            }
            
            coroutineLockQueue = CoroutineLockQueue.Create(CoroutineLockComponent, _coroutineLockType, coroutineLockQueueKey);
            _coroutineLockQueues.Add(coroutineLockQueueKey, coroutineLockQueue);
            return coroutineLockQueue.CreateWaitCoroutineLock(tag, time);
        }

        /// <summary>
        /// 从协程锁队列类型中移除指定键的协程锁队列。
        /// </summary>
        /// <param name="coroutineLockQueueKey">要解锁的协程锁队列的键。</param>
        /// <returns>如果当前锁队列已经是空的就返回True</returns>
        public bool Release(long coroutineLockQueueKey)
        {
            if (!_coroutineLockQueues.TryGetValue(coroutineLockQueueKey, out var coroutineLockQueue))
            {
                return _coroutineLockQueues.Count == 0;
            }
                
            if (coroutineLockQueue.Release())
            {
                _coroutineLockQueues.Remove(coroutineLockQueueKey);
            }

            return _coroutineLockQueues.Count == 0;
        }
    }
}