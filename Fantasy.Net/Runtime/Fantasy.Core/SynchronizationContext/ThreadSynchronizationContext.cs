using System;
using System.Collections.Concurrent;
using System.Threading;

#pragma warning disable CS8765
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 一个用于线程同步的上下文。
    /// </summary>
    public sealed class ThreadSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// 获取线程的唯一标识符。
        /// </summary>
        public readonly int ThreadId;
        private Action _actionHandler;
        private readonly ConcurrentQueue<Action> _queue = new();
        /// <summary>
        /// 获取主线程的同步上下文实例。
        /// </summary>
        public static ThreadSynchronizationContext Main { get; } = new(Environment.CurrentManagedThreadId);

        /// <summary>
        /// 初始化 ThreadSynchronizationContext 类的新实例。
        /// </summary>
        /// <param name="threadId">线程的唯一标识符。</param>
        public ThreadSynchronizationContext(int threadId)
        {
            ThreadId = threadId;
        }

        /// <summary>
        /// 更新同步上下文中的操作。
        /// </summary>
        public void Update()
        {
            while (_queue.TryDequeue(out _actionHandler))
            {
                try
                {
                    _actionHandler();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 将操作排队以在同步上下文中异步执行。
        /// </summary>
        /// <param name="callback">要执行的回调方法。</param>
        /// <param name="state">传递给回调方法的状态对象。</param>
        public override void Post(SendOrPostCallback callback, object state)
        {
            Post(() => callback(state));
        }

        /// <summary>
        /// 将操作排队以在同步上下文中异步执行。
        /// </summary>
        /// <param name="action">要执行的操作。</param>
        public void Post(Action action)
        {
            _queue.Enqueue(action);
        }
    }
}