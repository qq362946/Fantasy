using System.Collections.Generic;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy
{
    /// <summary>
    /// 协程锁组件
    /// </summary>
    public class CoroutineLockComponent : Entity
    {
        private long _lockId;
        private CoroutineLockPool _coroutineLockPool;
        
        public WaitCoroutineLockPool WaitCoroutineLockPool { get; private set; }
        public CoroutineLockQueuePool CoroutineLockQueuePool { get; private set; }
        private readonly Dictionary<long, CoroutineLock> _coroutineLocks = new Dictionary<long, CoroutineLock>();

        public CoroutineLockComponent Initialize()
        {
            _coroutineLockPool = new CoroutineLockPool();
            CoroutineLockQueuePool = new CoroutineLockQueuePool();
            WaitCoroutineLockPool = new WaitCoroutineLockPool(this);
            return this;
        }
        
        public long LockId => ++_lockId;
        
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
        /// 使用这个方法创建的协程锁，需要手动释放管理CoroutineLock。
        /// 不会再CoroutineLockComponent理进行管理。
        /// </summary>
        /// <param name="coroutineLockType"></param>
        /// <returns></returns>
        public CoroutineLock Create(long coroutineLockType)
        {
            var coroutineLock = _coroutineLockPool.Rent();
            coroutineLock.Initialize(this, ref coroutineLockType);
            return coroutineLock;
        }

        /// <summary>
        /// 请求一个协程锁。
        /// 使用这个方法创建的协程锁，会自动释放CoroutineLockQueueType。
        /// </summary>
        /// <param name="coroutineLockType">锁类型</param>
        /// <param name="coroutineLockQueueKey">锁队列Id</param>
        /// <param name="tag">当某些锁超时，需要一个标记来方便排查问题，正常的情况下这个默认为null就可以。</param>
        /// <param name="time">设置锁的超时时间，让超过设置的时间会触发超时，保证锁不会因为某一个锁一直不解锁导致卡住的问题。</param>
        /// <returns>
        /// 返回的WaitCoroutineLock通过Dispose来解除这个锁、建议用using来保住这个锁。
        /// 也可以返回的WaitCoroutineLock通过CoroutineLockComponent.UnLock来解除这个锁。
        /// </returns>
        public FTask<WaitCoroutineLock> Wait(long coroutineLockType, long coroutineLockQueueKey, string tag = null, int time = 30000)
        {
            if (!_coroutineLocks.TryGetValue(coroutineLockType, out var coroutineLock))
            {
                coroutineLock = _coroutineLockPool.Rent();
                coroutineLock.Initialize(this, ref coroutineLockType);
                _coroutineLocks.Add(coroutineLockType, coroutineLock);
            }

            return coroutineLock.Wait(coroutineLockQueueKey, tag, time);
        }

        /// <summary>
        /// 解除一个协程锁。
        /// </summary>
        /// <param name="coroutineLockType"></param>
        /// <param name="coroutineLockQueueKey"></param>
        public void Release(int coroutineLockType, long coroutineLockQueueKey)
        {
            if (IsDisposed)
            {
                return;
            }
            
            if (!_coroutineLocks.TryGetValue(coroutineLockType, out var coroutineLock))
            {
                return;
            }
                
            coroutineLock.Release(coroutineLockQueueKey);
        }
    }
}