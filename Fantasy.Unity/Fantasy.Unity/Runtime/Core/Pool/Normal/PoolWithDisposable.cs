using System;
using System.Collections.Generic;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy
{
    /// <summary>
    /// 静态通用对象池，用于存储实现了 IDisposable 接口的对象。
    /// </summary>
    /// <typeparam name="T">要存储在对象池中的对象类型，必须实现 IDisposable 接口。</typeparam>
    public abstract class PoolWithDisposable<T> : IDisposable where T : IPool, IDisposable, new()
    {
        private int _poolCount;
        private readonly int _maxCapacity;
        private readonly Queue<T> _poolQueue = new Queue<T>();
        public int Count => _poolQueue.Count;

        protected PoolWithDisposable(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }
        
        public T Rent()
        {
            if (_poolQueue.Count == 0)
            {
                return new T();
            }

            var dequeue = _poolQueue.Dequeue();
            dequeue.IsPool = true;
            _poolCount--;
            return dequeue;
        }
        
        public T Rent(Func<T> generator)
        {
            if (_poolQueue.Count == 0)
            {
                return generator();
            }
            
            var dequeue = _poolQueue.Dequeue();
            dequeue.IsPool = true;
            _poolCount--;
            return dequeue;
        }
        
        public void Return(T t)
        {
            if (t == null)
            {
                return;
            }

            if (!t.IsPool)
            {
                return;
            }

            if (_poolCount >= _maxCapacity)
            {
                return;
            }

            _poolCount++;
            t.IsPool = true;
            _poolQueue.Enqueue(t);
            t.Dispose();
        }
        
        public void Return(T t, Action<T> reset)
        {
            if (t == null)
            {
                return;
            }
            
            if (!t.IsPool)
            {
                reset(t);
                return;
            }
            
            if (_poolCount >= _maxCapacity)
            {
                return;
            }

            reset(t);
            _poolCount++;
            t.IsPool = false;
            _poolQueue.Enqueue(t);
            t.Dispose();
        }
        
        public virtual void Dispose()
        {
            _poolCount = 0;
            _poolQueue.Clear();
        }
    }
}