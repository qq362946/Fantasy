using System;
using System.Collections.Concurrent;
using System.Threading;
using Fantasy.Core.Network;

namespace Fantasy
{
    public sealed class ThreadSynchronizationContext : SynchronizationContext
    {
        public readonly int ThreadId;
        private Action _actionHandler;
        private readonly ConcurrentQueue<Action> _queue = new();
        public static ThreadSynchronizationContext Main { get; } = new(Environment.CurrentManagedThreadId);

        public ThreadSynchronizationContext(int threadId)
        {
            ThreadId = threadId;
        }

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

        public override void Post(SendOrPostCallback callback, object state)
        {
            Post(() => callback(state));
        }

        public void Post(Action action)
        {
            _queue.Enqueue(action);
        }
    }
}