using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Fantasy.Async
{
    public readonly struct SceneThreadAwaitable : ICriticalNotifyCompletion
    {
        private readonly ThreadSynchronizationContext _context;

        public SceneThreadAwaitable(ThreadSynchronizationContext context)
        {
            _context = context;
        }

        public bool IsCompleted =>
            _context == null ||
            ReferenceEquals(SynchronizationContext.Current, _context);

        public SceneThreadAwaitable GetAwaiter() => this;
        public void GetResult() { }

        public void OnCompleted(Action continuation) =>
            _context.Post(continuation);

        public void UnsafeOnCompleted(Action continuation) =>
            _context.Post(continuation);
    }
}