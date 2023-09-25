using System;
using Fantasy.Helper;

namespace Fantasy.Core
{
    /// <summary>
    /// 等待协程锁超时的数据结构
    /// </summary>
    public struct CoroutineLockTimeout
    {
        /// <summary>
        /// 协程锁的唯一标识
        /// </summary>
        public long LockId;
        /// <summary>
        /// 等待的协程锁对象
        /// </summary>
        public WaitCoroutineLock WaitCoroutineLock;
    }

    /// <summary>
    /// 协程锁超时时的事件处理类，继承自 EventSystem&lt;CoroutineLockTimeout&gt;
    /// </summary>
    public sealed class OnCoroutineLockTimeout : EventSystem<CoroutineLockTimeout>
    {
        /// <summary>
        /// 处理协程锁超时时的逻辑
        /// </summary>
        /// <param name="self">协程锁超时的信息</param>
        public override void Handler(CoroutineLockTimeout self)
        {
            // 检查锁的唯一标识是否匹配
            if (self.LockId != self.WaitCoroutineLock.LockId)
            {
                return; // 不匹配则直接返回，不进行后续处理
            }

            var coroutineLockQueue = self.WaitCoroutineLock.CoroutineLockQueue;
            var coroutineLockQueueType = coroutineLockQueue.CoroutineLockQueueType;
            var key = coroutineLockQueue.Key;

            // 记录日志，指明超时的协程锁队列类型、键值和标签
            Log.Error($"coroutine lock timeout CoroutineLockQueueType:{coroutineLockQueueType.Name} Key:{key} Tag:{self.WaitCoroutineLock.Tag}");
        }
    }

    /// <summary>
    /// 等待协程锁的类，实现了 IDisposable 接口
    /// </summary>
    public sealed class WaitCoroutineLock : IDisposable
    {
        private long _timerId;
        /// <summary>
        /// 获取当前对象是否已经被释放的标识
        /// </summary>
        public bool IsDisposed => LockId == 0;
        /// <summary>
        /// 获取协程锁的标签
        /// </summary>
        public string Tag { get; private set; }
        /// <summary>
        /// 获取协程锁的唯一标识
        /// </summary>
        public long LockId { get; private set; }
        /// <summary>
        /// 获取用于等待协程锁释放的任务
        /// </summary>
        public FTask<WaitCoroutineLock> Tcs { get; private set; }

        /// <summary>
        /// 获取所属的协程锁队列
        /// </summary>
        public CoroutineLockQueue CoroutineLockQueue{ get; private set; }

        /// <summary>
        /// 创建一个等待协程锁对象
        /// </summary>
        /// <param name="coroutineLockQueue">协程锁队列</param>
        /// <param name="tag">协程锁标签</param>
        /// <param name="timeOut">超时时间（毫秒）</param>
        /// <returns>等待协程锁对象</returns>
        public static WaitCoroutineLock Create(CoroutineLockQueue coroutineLockQueue, string tag, int timeOut)
        {
            var lockId = IdFactory.NextRunTimeId();
            var waitCoroutineLock = Pool<WaitCoroutineLock>.Rent();

            waitCoroutineLock.Tag = tag;
            waitCoroutineLock.LockId = lockId;
            waitCoroutineLock.CoroutineLockQueue = coroutineLockQueue;
            waitCoroutineLock.Tcs = FTask<WaitCoroutineLock>.Create();
            waitCoroutineLock._timerId = TimerScheduler.Instance.Core.OnceTimer(timeOut, new CoroutineLockTimeout()
            {
                LockId = lockId, WaitCoroutineLock = waitCoroutineLock
            });

            return waitCoroutineLock;
        }

        /// <summary>
        /// 释放协程锁对象
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                Log.Error("WaitCoroutineLock is Dispose");
                return;
            }
            
            Release(CoroutineLockQueue);
            
            LockId = 0;
            Tcs = null;
            Tag = null;
            CoroutineLockQueue = null;
            
            if (_timerId != 0)
            {
                TimerScheduler.Instance.Core.RemoveByRef(ref _timerId);
            }
            
            Pool<WaitCoroutineLock>.Return(this);
        }

        private static void Release(CoroutineLockQueue coroutineLockQueue)
        {
            // 放到下一帧执行释放锁、如果不这样、会导致逻辑的顺序不正常
            ThreadSynchronizationContext.Main.Post(coroutineLockQueue.Release);
        }

        /// <summary>
        /// 设置等待协程锁的任务结果
        /// </summary>
        public void SetResult()
        {
            if (Tcs == null)
            {
                throw new NullReferenceException("SetResult tcs is null");
            }
            
            Tcs.SetResult(this);
        }
    }
}