#if !FANTASY_WEBGL || !FANTASY_SINGLETHREAD
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8604 // Possible null reference argument.
namespace Fantasy
{
    public sealed class ThreadPoolScheduler : ISceneScheduler
    {
        private bool _isDisposed;
        private readonly List<Thread> _threads;
        private readonly ConcurrentBag<Scene> _queue = new ConcurrentBag<Scene>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        public ThreadPoolScheduler()
        {
            // 最大线程数、避免线程过多发生的资源抢占问题。
            // 但如果使用了MultiThreadScheduler，那么这里的线程数就算是设置了也有可能导致线程过多的问题。
            // 线程过多看每个线程的抢占情况，如果抢占资源占用不是很大也没什么大问题。如果过大的情况，就会有性能问题。
            // 所以根据情况来使用不同的调度器。
            var maxThreadCount = Environment.ProcessorCount;
            _threads = new List<Thread>(maxThreadCount);
            
            for (var i = 0; i < maxThreadCount; ++i)
            {
                Thread thread = new(() => Loop(_cancellationTokenSource.Token))
                {
                    IsBackground = true
                };
                _threads.Add(thread);
                thread.Start();
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _cancellationTokenSource.Cancel();
            
            foreach (var thread in _threads)
            {
                if (thread.IsAlive)
                {
                    thread.Join();
                }
            }
            
            _cancellationTokenSource.Dispose();
            _threads.Clear();
        }

        public void Add(Scene scene)
        {
            if (_isDisposed)
            {
                return;
            }
            
            _queue.Add(scene);
        }

        public void Remove(Scene scene)
        {
            if (_isDisposed)
            {
                return;
            }
            
            var newQueue = new Queue<Scene>();

            while (!_queue.IsEmpty)
            {
                if (_queue.TryTake(out var currentScene))
                {
                    if (currentScene != scene)
                    {
                        newQueue.Enqueue(currentScene);
                    }
                }
            }
            
            while (newQueue.TryDequeue(out var newScene))
            {
                _queue.Add(newScene);
            }
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        private void Loop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_queue.TryTake(out var scene))
                {
                    if (scene == null || scene.IsDisposed)
                    {
                        continue;
                    }
                    
                    var sceneThreadSynchronizationContext = scene.ThreadSynchronizationContext;
                    SynchronizationContext.SetSynchronizationContext(sceneThreadSynchronizationContext);

                    try
                    {
                        sceneThreadSynchronizationContext.Update();
                        scene.Update();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error in ThreadPoolScheduler scene: {e.Message}");
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(null);
                    }
                    
                    _queue.Add(scene);
                    Thread.Sleep(1);
                }
                else
                {
                    // 当队列为空的时候、避免无效循环消耗CPU。
                    Thread.Sleep(10);
                }
            }
        }
    }
}
#endif