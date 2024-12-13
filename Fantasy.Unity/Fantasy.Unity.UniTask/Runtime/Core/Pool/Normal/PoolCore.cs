using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fantasy.DataStructure.Collection;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Pool
{
    /// <summary>
    /// 对象池抽象接口，用于创建和管理可重复使用的对象实例。
    /// </summary>
    public abstract class PoolCore : IDisposable
    {
        private int _poolCount;
        private readonly int _maxCapacity;
        /// <summary>
        /// 池子里可用的数量
        /// </summary>
        public int Count => _poolQueue.Count;
        private readonly OneToManyQueue<Type, IPool> _poolQueue = new OneToManyQueue<Type, IPool>();
        private readonly Dictionary<Type, Func<IPool>> _typeCheckCache = new Dictionary<Type, Func<IPool>>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxCapacity">初始的容量</param>
        protected PoolCore(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// 租借
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Rent<T>() where T : IPool, new()
        {
            if (!_poolQueue.TryDequeue(typeof(T), out var queue))
            {
                return new T();
            }
            
            queue.SetIsPool(true);
            _poolCount--;
            return (T)queue;
        }

        /// <summary>
        /// 租借
        /// </summary>
        /// <param name="type">租借的类型</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public IPool Rent(Type type)
        {
            if (!_poolQueue.TryDequeue(type, out var queue))
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
                instance.SetIsPool(true);
                return instance;
            }
            
            queue.SetIsPool(true);
            _poolCount--;
            return queue;
        }

        /// <summary>
        /// 返还
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        public void Return(Type type, IPool obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!obj.IsPool())
            {
                return;
            }

            if (_poolCount >= _maxCapacity)
            {
                return;
            }

            _poolCount++;
            obj.SetIsPool(false);
            _poolQueue.Enqueue(type, obj);
        }

        /// <summary>
        /// 销毁方法
        /// </summary>
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
        /// <summary>
        /// 池子里可用的数量
        /// </summary>
        public int Count => _poolQueue.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxCapacity">初始的容量</param>
        protected PoolCore(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// 租借
        /// </summary>
        /// <returns></returns>
        public virtual T Rent()
        {
            if (_poolQueue.Count == 0)
            {
                return new T();
            }

            var dequeue = _poolQueue.Dequeue();
            dequeue.SetIsPool(true);
            _poolCount--;
            return dequeue;
        }
        
        /// <summary>
        /// 返还
        /// </summary>
        /// <param name="item"></param>
        public virtual void Return(T item)
        {
            if (item == null)
            {
                return;
            }
            
            if (!item.IsPool())
            {
                return;
            }

            if (_poolCount >= _maxCapacity)
            {
                return;
            }

            _poolCount++;
            item.SetIsPool(false);
            _poolQueue.Enqueue(item);
        }
        
        /// <summary>
        /// 销毁方法
        /// </summary>
        public virtual void Dispose()
        {
            _poolCount = 0;
            _poolQueue.Clear();
        }
    }
}