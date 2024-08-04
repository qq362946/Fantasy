#if FANTASY_NET
using System.Collections.Concurrent;
#pragma warning disable CS8765
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace Fantasy;

public sealed class ThreadSynchronizationContext : SynchronizationContext
{
    private Action _actionHandler;
    private readonly ConcurrentQueue<Action> _queue = new();
        
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
#endif