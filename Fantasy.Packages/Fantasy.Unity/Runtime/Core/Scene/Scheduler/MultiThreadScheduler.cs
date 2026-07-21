#if !FANTASY_WEBGL && !UNITY_WEBGL && !FANTASY_SINGLETHREAD
using System;
using System.Collections.Concurrent;
using System.Threading;
namespace Fantasy
{
    internal struct MultiThreadStruct : IDisposable
    {
        public readonly Thread Thread;
        public readonly CancellationTokenSource Cts;

        public MultiThreadStruct(Thread thread, CancellationTokenSource cts)
        {
            Thread = thread;
            Cts = cts;
        }

        public void Dispose()
        {
            Cts.Cancel();
            // 避免在 Scene 自己的调度线程内 Remove 时 Join 自身导致死锁。
            if (Thread.IsAlive && Thread != Thread.CurrentThread)
            {
                Thread.Join();
            }
            Cts.Dispose();
        }
    }
    
    internal sealed class MultiThreadScheduler : ISceneScheduler
    {
        private bool _isDisposed;
        private readonly ConcurrentDictionary<long, MultiThreadStruct> _threads = new ConcurrentDictionary<long, MultiThreadStruct>();
        public int ThreadCount => _threads.Count;
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            
            foreach (var (_, multiThreadStruct) in _threads.ToArray())
            {
                multiThreadStruct.Dispose();
            }
            
            _threads.Clear();
        }

        public void Add(Scene scene)
        {
            var cts = new CancellationTokenSource();
            var thread = new Thread(() => Loop(scene, cts.Token))
            {
                IsBackground = true
            };
            
            var multiThreadStruct = new MultiThreadStruct(thread, cts);
            
            if (!_threads.TryAdd(scene.RuntimeId, multiThreadStruct))
            {
                multiThreadStruct.Dispose();
                throw new InvalidOperationException(
                    $"Scene RuntimeId already exists: {scene.RuntimeId}.");
            }
            
            try
            {
                thread.Start();
            }
            catch
            {
                _threads.TryRemove(scene.RuntimeId, out _);
                multiThreadStruct.Dispose();
                throw;
            }
        }

        public void Remove(Scene scene)
        {
            if (_threads.TryRemove(scene.RuntimeId, out var multiThreadStruct))
            {
                multiThreadStruct.Dispose();
            }
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        private void Loop(Scene scene, CancellationToken cancellationToken)
        {
            var sceneThreadSynchronizationContext = scene.ThreadSynchronizationContext;
            SynchronizationContext.SetSynchronizationContext(sceneThreadSynchronizationContext);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (scene.IsDisposed)
                    {
                        Remove(scene);
                        return;
                    }

                    sceneThreadSynchronizationContext.Update();
                    
                    // 同步上下文中可能刚刚执行了 Scene.Close/Dispose。
                    // 必须重新检查，不能继续调用已经销毁的 Scene。
                    if (cancellationToken.IsCancellationRequested || scene.IsDisposed)
                    {
                        return;
                    }
                    
                    scene.Update();
                }
                catch (Exception e)
                {
                    Log.Error($"Error in MultiThreadScheduler loop: {e.Message}");
                }
                finally
                {
                    Thread.Sleep(1);
                }
            }
        }
    }
}
#endif
