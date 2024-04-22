using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Fantasy
{
    internal class MultiThreadScheduler : IThreadScheduler
    {
        private bool _isDisposed;
        private readonly ConcurrentDictionary<long, Thread> _threads = new();
        private readonly ConcurrentQueue<long> _queue = new ConcurrentQueue<long>();
        
        public void Add(long sceneSchedulerId)
        {
            if (!Scene.Scenes.TryGetValue(sceneSchedulerId, out var scene))
            {
                Log.Error($"not found scene {sceneSchedulerId}");
                return;
            }

            var thread = new Thread(() => Loop(sceneSchedulerId, scene.ThreadSynchronizationContext));
            _threads.TryAdd(sceneSchedulerId, thread);
            thread.Start();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
        
        private void Loop(long sceneSchedulerId, ThreadSynchronizationContext synchronizationContext)
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            
            while (true)
            {
                if (_isDisposed)
                {
                    return;
                }
                
                if (!Scene.Scenes.TryGetValue(sceneSchedulerId, out var scene) || scene.IsDisposed)
                {
                    _threads.Remove(sceneSchedulerId, out _);
                    return;
                }
                
                synchronizationContext.Update();
                scene.Update();
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
            
            foreach (var (_, thread) in _threads.ToArray())
            {
                thread.Join();
            }
            
            _threads.Clear();
        }
    }
}