#if !FANTASY_WEBGL
using System;
using System.Collections.Concurrent;
using System.Threading;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Pool
{
    /// <summary>
    /// 线程安全的对象池。
    /// </summary>
    internal class MultiThreadPoolQueue
    {
        private int _poolCount;
        private readonly int _maxCapacity;
        private readonly Func<IPool> _createInstance;
        private readonly ConcurrentQueue<IPool> _poolQueue = new ConcurrentQueue<IPool>();
        private MultiThreadPoolQueue() { }

        public MultiThreadPoolQueue(int maxCapacity, Func<IPool> createInstance)
        {
            _maxCapacity = maxCapacity;
            _createInstance = createInstance;
        }

        public T Rent<T>() where T : IPool, new()
        {
            if (!_poolQueue.TryDequeue(out var t))
            {
                var pool = new T();
                pool.SetIsPool(true);
                return pool;
            }
            
            t.SetIsPool(true);
            Interlocked.Decrement(ref _poolCount);
            return (T)t;
        }

        public IPool Rent()
        {
            if (!_poolQueue.TryDequeue(out var t))
            {
                var instance = _createInstance();
                instance.SetIsPool(true);
                return instance;
            }
            
            t.SetIsPool(true);
            Interlocked.Decrement(ref _poolCount);
            return t;
        }
        
        public void Return(IPool obj)
        {
            if (!obj.IsPool())
            {
                return;
            }
            
            obj.SetIsPool(false);
            
            if (Interlocked.Increment(ref _poolCount) <= _maxCapacity)
            {
                _poolQueue.Enqueue(obj);
                return;
            }
            
            Interlocked.Decrement(ref _poolCount);
        }
    }
}
#endif