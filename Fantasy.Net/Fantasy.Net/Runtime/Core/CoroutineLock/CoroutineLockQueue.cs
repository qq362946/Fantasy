using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 协程锁队列，用于协程等待和释放锁的管理。
    /// </summary>
    public sealed class CoroutineLockQueue : IDisposable
    {
        /// <summary>
        /// CoroutineLockType
        /// </summary>
        public long CoroutineLockType { get; private set; }

        /// <summary>
        /// 获取锁队列的键。
        /// </summary>
        public long CoroutineLockQueueKey { get; private set; }
        /// <summary>
        /// CoroutineLockComponent
        /// </summary>
        public CoroutineLockComponent CoroutineLockComponent { get; private set; }
        private readonly Queue<WaitCoroutineLock> _waitCoroutineLocks = new Queue<WaitCoroutineLock>();

        /// <summary>
        /// 创建一个协程锁队列实例。
        /// </summary>
        /// <param name="coroutineLockType">锁的类型</param>
        /// <param name="coroutineLockQueueKey">锁队列的键。</param>
        /// <param name="coroutineLockComponent">CoroutineLockComponent。</param>
        /// <returns>创建的协程锁队列实例。</returns>
        public static CoroutineLockQueue Create(CoroutineLockComponent coroutineLockComponent, long coroutineLockType, long coroutineLockQueueKey)
        {
            var coroutineLockQueue = coroutineLockComponent.Scene.Pool.Rent<CoroutineLockQueue>();
            coroutineLockQueue.CoroutineLockType = coroutineLockType;
            coroutineLockQueue.CoroutineLockQueueKey = coroutineLockQueueKey;
            coroutineLockQueue.CoroutineLockComponent = coroutineLockComponent;
            return coroutineLockQueue;
        }

        /// <summary>
        /// 释放协程锁队列实例。
        /// </summary>
        public void Dispose()
        {
            var singleThreadPool = CoroutineLockComponent.Scene.Pool;
            
            CoroutineLockType = 0;
            CoroutineLockQueueKey = 0;
            CoroutineLockComponent = null;

            while (_waitCoroutineLocks.TryDequeue(out var waitCoroutineLock))
            {
                if (waitCoroutineLock.IsDisposed)
                {
                    continue;
                }
                
                waitCoroutineLock.SetResult();
            }
            
            singleThreadPool.Return(this);
        }

        /// <summary>
        /// 请求协程锁,获取等待协程锁的任务。
        /// </summary>
        /// <param name="tag">锁标识。</param>
        /// <param name="time">等待时间。</param>
        /// <returns>等待协程锁的任务。</returns>
        public async FTask<WaitCoroutineLock> Lock(string tag, int time)
        {
#if FANTASY_DEVELOP
            if (_waitCoroutineLocks.Count >= 100)
            {
                // 当等待队列超过100个、表示这个协程锁可能有问题、打印一个警告方便排查错误
                Log.Warning($"too much waitCoroutineLock CoroutineLockQueueType:{CoroutineLockQueueType.Name} Key:{Key} Count: {_waitCoroutineLocks.Count} ");
            }
#endif
            var waitCoroutineLock = WaitCoroutineLock.Create(this, tag, time);
            _waitCoroutineLocks.Enqueue(waitCoroutineLock);
            return await waitCoroutineLock.Tcs;
        }

        /// <summary>
        /// 创建一个新的协程锁,前提是这个锁的第一次出现
        /// </summary>
        /// <param name="tag">锁标识。</param>
        /// <param name="time">等待时间。</param>
        /// <returns>等待协程锁的任务。</returns>
        public WaitCoroutineLock CreateWaitCoroutineLock(string tag, int time)
        {
            var waitCoroutineLock = WaitCoroutineLock.Create(this, tag, time);
            _waitCoroutineLocks.Enqueue(waitCoroutineLock);
            return waitCoroutineLock;
        }

        /// <summary>
        /// 释放协程锁。
        /// </summary>
        /// <returns>是否需要移除CoroutineLockQueue</returns>
        public bool Release()
        {
            while (_waitCoroutineLocks.TryDequeue(out var waitCoroutineLock))
            {
                if (waitCoroutineLock.IsDisposed)
                {
                    // 已释放的等待锁，继续处理下一个
                    continue;
                }
                
                waitCoroutineLock.SetResult();
                break;
            }

            if (_waitCoroutineLocks.Count != 0)
            {
                return false;
            }
            
            // 如果锁队列为空，表示当前没有正在等待的所，可以通知CoroutineLockQueueType移除此锁队列。
            Dispose();
            return true;
        }
    }
}