using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 协程锁组件
    /// </summary>
    public class CoroutineLockComponent : Entity
    {
        private long _lockId;

        /// <summary>
        /// 获取一个锁ID
        /// </summary>
        public long LockId => ++_lockId;

        private readonly Dictionary<long, CoroutineLockQueueType> _coroutineLockQueueTypes = new ();

        /// <summary>
        /// Dispose
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            _lockId = 0;
            base.Dispose();
        }

        /// <summary>
        /// 创建一个新的协程锁
        /// </summary>
        /// <param name="coroutineLockType"></param>
        /// <returns></returns>
        public CoroutineLockQueueType Create(long coroutineLockType)
        {
            if (_coroutineLockQueueTypes.TryGetValue(coroutineLockType, out var coroutineLockQueueType))
            {
                return coroutineLockQueueType;
            }
            
            coroutineLockQueueType = CoroutineLockQueueType.Create(this, coroutineLockType);
            _coroutineLockQueueTypes.Add(coroutineLockType, coroutineLockQueueType);
            return coroutineLockQueueType;
        }

        /// <summary>
        /// 请求一个协程锁,使用这个方法要注意coroutineLockType的值，不要重复。
        /// 如果保证不了请先使用CoroutineLockComponent.Create创建一个新的协程锁。然后再用Lock传递进去。
        /// </summary>
        /// <param name="coroutineLockType">锁类型</param>
        /// <param name="coroutineLockQueueKey">锁队列Id</param>
        /// <param name="tag">当某些锁超时，需要一个标记来方便排查问题，正常的情况下这个默认为null就可以。</param>
        /// <param name="time">设置锁的超时时间，让超过设置的时间会触发超时，保证锁不会因为某一个锁一直不解锁导致卡住的问题。</param>
        /// <returns>
        /// 返回的WaitCoroutineLock通过Dispose来解除这个锁、建议用using来保住这个锁。
        /// 也可以返回的WaitCoroutineLock通过CoroutineLockComponent.UnLock来解除这个锁。
        /// </returns>
        public async FTask<WaitCoroutineLock> Lock(int coroutineLockType, long coroutineLockQueueKey, string tag = null, int time = 30000)
        {
            if (_coroutineLockQueueTypes.TryGetValue(coroutineLockType, out var coroutineLockQueueType))
            {
                return await coroutineLockQueueType.Lock(coroutineLockQueueKey, tag, time);
            }

            coroutineLockQueueType = CoroutineLockQueueType.Create(this, coroutineLockType);
            _coroutineLockQueueTypes.Add(coroutineLockType, coroutineLockQueueType);
            return await coroutineLockQueueType.Lock(coroutineLockQueueKey, tag, time);
        }

        /// <summary>
        /// 解除一个协程锁。
        /// </summary>
        /// <param name="coroutineLockQueue">锁队列</param>
        public void UnLock(CoroutineLockQueue coroutineLockQueue)
        {
            UnLock(coroutineLockQueue.CoroutineLockType, coroutineLockQueue.CoroutineLockQueueKey);
        }

        /// <summary>
        /// 解除一个协程锁。
        /// </summary>
        /// <param name="coroutineLockType"></param>
        /// <param name="coroutineLockQueueKey"></param>
        public void UnLock(long coroutineLockType, long coroutineLockQueueKey)
        {
            if (IsDisposed)
            {
                return;
            }

            // 放到下一帧执行释放锁、如果不这样、会导致逻辑的顺序不正常

            Scene.ThreadSynchronizationContext.Post(() =>
            {
                if (IsDisposed)
                {
                    return;
                }
                
                if (!_coroutineLockQueueTypes.TryGetValue(coroutineLockType, out var coroutineLockQueueType))
                {
                    return;
                }

                if (!coroutineLockQueueType.Release(coroutineLockQueueKey))
                {
                    return;
                }
                
                _coroutineLockQueueTypes.Remove(coroutineLockType);
            });
        }
    }
}