using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 线程池调度器。
    /// </summary>
    public class ThreadPoolScheduler : IThreadScheduler
    {
        private bool _isDisposed;
        private readonly List<Thread> _threads;
        private readonly ConcurrentQueue<long> _queue = new ConcurrentQueue<long>();

        public ThreadPoolScheduler()
        {
            // 最大线程数、避免线程过多发生的资源抢占问题。
            // 但如果使用了SingleThreadScheduler，那么这里的线程数就算是设置了也有可能导致线程过多的问题。
            // 所以根据情况来使用不同的调度器。
            var maxThreadCount = Environment.ProcessorCount;
            _threads = new List<Thread>(maxThreadCount);

            for (var i = 0; i < maxThreadCount; ++i)
            {
                Thread thread = new(Loop);
                _threads.Add(thread);
                thread.Start();
            }
        }

        public void Add(long sceneSchedulerId)
        {
            _queue.Enqueue(sceneSchedulerId);
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        private void Loop()
        {
            while (true)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (!_queue.TryDequeue(out var sceneThreadId))
                {
                    Thread.Yield();
                    continue;
                }

                if (!Scene.Scenes.TryGetValue(sceneThreadId, out var scene) || scene.IsDisposed)
                {
                    return;
                }

                var threadSynchronizationContext = scene.ThreadSynchronizationContext;
                SynchronizationContext.SetSynchronizationContext(threadSynchronizationContext);
                threadSynchronizationContext.Update();
                scene.Update();
                SynchronizationContext.SetSynchronizationContext(null);
                _queue.Enqueue(sceneThreadId);
                Thread.Yield();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            
            foreach (var thread in _threads.ToArray())
            {
                thread.Join();
            }
            
            _threads.Clear();
        }
    }
}