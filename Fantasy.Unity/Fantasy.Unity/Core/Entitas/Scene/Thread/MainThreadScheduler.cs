using System.Collections.Concurrent;
using System.Threading;

namespace Fantasy
{
    internal sealed class MainThreadScheduler : IThreadScheduler
    {
        private readonly ConcurrentQueue<long> _queue = new ConcurrentQueue<long>();

        public MainThreadScheduler()
        {
            SynchronizationContext.SetSynchronizationContext(ThreadSynchronizationContext.Main);
        }
        
        public void Dispose()
        {
            _queue.Clear();
        }

        public void Add(long sceneSchedulerId)
        {
            _queue.Enqueue(sceneSchedulerId);
        }

        public void Update()
        {
            ThreadSynchronizationContext.Main.Update();
            var count = _queue.Count;

            while (count-- > 0)
            {
                if(!_queue.TryDequeue(out var sceneSchedulerId))
                {
                    continue;
                }

                if (!Scene.Scenes.TryGetValue(sceneSchedulerId, out var sceneScheduler))
                {
                    continue;
                }

                if (sceneScheduler.IsDisposed)
                {
                    continue;
                }
                
                sceneScheduler.Update();
                _queue.Enqueue(sceneSchedulerId);
            }
        }
    }
}