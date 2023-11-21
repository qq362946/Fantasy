using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 指定键的协程锁队列类型，用于管理不同类型的协程锁队列。
    /// </summary>
    public sealed class CoroutineLockQueueType
    {
        /// <summary>
        /// 获取协程锁队列类型的名称。
        /// </summary>
        public readonly string Name;
        private readonly Dictionary<long, CoroutineLockQueue> _coroutineLockQueues = new Dictionary<long, CoroutineLockQueue>();

        // 私有构造函数，防止外部实例化
        private CoroutineLockQueueType() { }

        /// <summary>
        /// 初始化协程锁队列类型的实例。
        /// </summary>
        /// <param name="name">协程锁队列类型的名称。</param>
        public CoroutineLockQueueType(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 请求协程锁，获取指定键的协程锁。
        /// </summary>
        /// <param name="key">协程锁队列的键。</param>
        /// <param name="tag">锁标识。</param>
        /// <param name="time">等待时间。</param>
        /// <returns>等待协程锁的任务。</returns>
        public async FTask<WaitCoroutineLock> Lock(long key, string tag = null, int time = 30000)
        {
            if (_coroutineLockQueues.TryGetValue(key, out var coroutineLockQueue))
            {
                return await coroutineLockQueue.Lock(tag,time);
            }

            coroutineLockQueue = CoroutineLockQueue.Create(key, time, this);
            _coroutineLockQueues.Add(key, coroutineLockQueue);
            return WaitCoroutineLock.Create(coroutineLockQueue, tag, time);
        }

        /// <summary>
        /// 从协程锁队列类型中移除指定键的协程锁队列。
        /// </summary>
        /// <param name="key">要移除的协程锁队列的键。</param>
        public void Remove(long key)
        {
            if (_coroutineLockQueues.Remove(key, out var coroutineLockQueue))
            {
                coroutineLockQueue.Dispose();
            }
        }
    }
}