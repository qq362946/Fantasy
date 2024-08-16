using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy
{
    /// <summary>
    /// 对象池核心类，用于创建和管理可重复使用的对象实例。
    /// </summary>
    public abstract class PoolCore : IDisposable
    {
        private int _poolCount;
        private readonly int _maxCapacity;
        public int Count => _poolQueue.Count;
        private readonly OneToManyQueue<Type, IPool> _poolQueue = new OneToManyQueue<Type, IPool>();
        private readonly Dictionary<Type, Func<IPool>> _typeCheckCache = new Dictionary<Type, Func<IPool>>();

        protected PoolCore(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        public T Rent<T>() where T : IPool, new()
        {
            if (!_poolQueue.TryGetValue(typeof(T), out var queue))
            {
                return new T();
            }

            var dequeue = queue.Dequeue();
            dequeue.IsPool = true;
            _poolCount--;
            return (T)dequeue;
        }

        public IPool Rent(Type type)
        {
            if (!_poolQueue.TryGetValue(type, out var queue))
            {
                if (!_typeCheckCache.TryGetValue(type, out var createInstance))
                {
                    if (!typeof(IPool).IsAssignableFrom(type))
                    {
                        throw new NotSupportedException($"{this.GetType().FullName} Type:{type.FullName} must inherit from IPool");
                    }
                    else
                    {
                        createInstance = CreateInstance.CreateIPool(type);
                        _typeCheckCache[type] = createInstance;
                    }
                }
                
                var instance = createInstance();
                instance.IsPool = true;
                return instance;
            }

            var dequeue = queue.Dequeue();
            dequeue.IsPool = true;
            _poolCount--;
            return dequeue;
        }

        public void Return(Type type, IPool obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!obj.IsPool)
            {
                return;
            }

            if (_poolCount >= _maxCapacity)
            {
                return;
            }

            _poolCount++;
            obj.IsPool = false;
            _poolQueue.Enqueue(type, obj);
        }

        public virtual void Dispose()
        {
            _poolCount = 0;
            _poolQueue.Clear();
            _typeCheckCache.Clear();
        }
    }

    /// <summary>
    /// 泛型对象池核心类，用于创建和管理可重复使用的对象实例。
    /// </summary>
    /// <typeparam name="T">要池化的对象类型</typeparam>
    public abstract class PoolCore<T> where T : IPool, new()
    {
        private int _poolCount;
        private readonly int _maxCapacity;
        private readonly Queue<T> _poolQueue = new Queue<T>();
        public int Count => _poolQueue.Count;

        protected PoolCore(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        public virtual T Rent()
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
        
        public virtual void Return(T item)
        {
            if (item == null)
            {
                return;
            }
            
            if (!item.IsPool)
            {
                return;
            }

            if (_poolCount >= _maxCapacity)
            {
                return;
            }

            _poolCount++;
            item.IsPool = false;
            _poolQueue.Enqueue(item);
        }
        
        public virtual void Dispose()
        {
            _poolCount = 0;
            _poolQueue.Clear();
        }
    }
}