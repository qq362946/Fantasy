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
        /// 获取锁队列的键。
        /// </summary>
        public long Key { get; private set; }

        /// <summary>
        /// 获取协程锁队列的类型。
        /// </summary>
        public CoroutineLockQueueType CoroutineLockQueueType { get; private set; }
        private readonly Queue<WaitCoroutineLock> _waitCoroutineLocks = new Queue<WaitCoroutineLock>();

        /// <summary>
        /// 创建一个协程锁队列实例。
        /// </summary>
        /// <param name="key">锁队列的键。</param>
        /// <param name="time">等待时间。</param>
        /// <param name="coroutineLockQueueType">协程锁队列的类型。</param>
        /// <returns>创建的协程锁队列实例。</returns>
        public static CoroutineLockQueue Create(long key, int time, CoroutineLockQueueType coroutineLockQueueType)
        {
            var coroutineLockQueue = Pool<CoroutineLockQueue>.Rent();
            coroutineLockQueue.Key = key;
            coroutineLockQueue.CoroutineLockQueueType = coroutineLockQueueType;
            return coroutineLockQueue;
        }

        /// <summary>
        /// 释放协程锁队列实例。
        /// </summary>
        public void Dispose()
        {
            Key = 0;
            CoroutineLockQueueType = null;
            Pool<CoroutineLockQueue>.Return(this);
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
        /// 释放协程锁。
        /// </summary>
        public void Release()
        {
            if (_waitCoroutineLocks.Count == 0)
            {
                // 如果等待队列为空，从类型中移除此锁队列
                CoroutineLockQueueType.Remove(Key);
                return;
            }
            
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
        }
    }
}