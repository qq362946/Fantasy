using System;
using System.Collections.Generic;
using System.Linq;
using Fantasy.DataStructure.Collection;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Pool
{
    /// <summary>
    /// 对象池抽象类，用于创建和管理可重复使用的对象实例。
    /// 使用内部 Stack 来管理每种类型的池。
    /// 比基于<see cref="Queue{T}"/>的实现<see cref="PoolCore"/>略快)。
    /// 缓存友好, 且能更好地在开发时暴露池对象未清理干净的问题, 但是不具备公平性(池中对象不会被平等地利用)。
    /// </summary>
    public abstract class PoolStack : IDisposable
    {
        private int _poolCount;
        private readonly int _maxCapacity;
        private readonly Dictionary<RuntimeTypeHandle, Stack<IPool>> _poolStacks = new();

        /// <summary>
        /// 池子里可用的数量
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (var stack in _poolStacks.Values)
                    count += stack.Count;
                return count;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxCapacity">初始的容量</param>
        protected PoolStack(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// 租借
        /// </summary>
        public T Rent<T>() where T : IPool, new()
        {
            var typeHandle = typeof(T).TypeHandle;
            if (!_poolStacks.TryGetValue(typeHandle, out var stack) || stack.Count == 0)
            {
                stack = stack ?? new Stack<IPool>();
                var instance = new T();
                instance.SetIsPool(true);
                _poolCount--;
                return instance;
            }

            var obj = stack.Pop();
            obj.SetIsPool(true);
            _poolCount--;
            return (T)obj;
        }

        /// <summary>
        /// 租借
        /// </summary>
        public IPool Rent(Scene scene, Type type)
        {
            var typeHandle = type.TypeHandle;
            if (!_poolStacks.TryGetValue(typeHandle, out var stack) || stack.Count == 0)
            {
                var instance = scene.PoolGeneratorComponent.Create(type);
                instance.SetIsPool(true);
                return instance;
            }

            var obj = stack.Pop();
            obj.SetIsPool(true);
            _poolCount--;
            return obj;
        }

        /// <summary>
        /// 返还
        /// </summary>
        public void Return(Type type, IPool obj)
        {
            if (obj == null || !obj.IsPool())
                return;

            if (_poolCount >= _maxCapacity)
                return;

            _poolCount++;
            obj.SetIsPool(false);

            var typeHandle = type.TypeHandle;
            if (!_poolStacks.TryGetValue(typeHandle, out var stack))
            {
                stack = new Stack<IPool>();
                _poolStacks[typeHandle] = stack;
            }

            stack.Push(obj);
        }

        /// <summary>
        /// 销毁方法
        /// </summary>
        public virtual void Dispose()
        {
            _poolCount = 0;
            _poolStacks.Clear();
        }
    }

    /// <summary>
    /// 泛型对象栈池(基于<see cref="Stack{T}"/>的实现, 比基于<see cref="Queue{T}"/>的实现<see cref="PoolCore"/>略快)。
    /// 缓存友好, 且能更好地在开发时暴露池对象未清理干净的问题, 但是不具备公平性(池中对象不会被平等地利用)。
    /// </summary>
    /// <typeparam name="T">要池化的对象类型</typeparam>
    public abstract class PoolStack<T> where T : IPool, new()
    {
        private int _poolCount;
        private readonly int _maxCapacity;
        private readonly Stack<T> _poolQueue = new Stack<T>();
        /// <summary>
        /// 池子里可用的数量
        /// </summary>
        public int Count => _poolQueue.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxCapacity">初始的容量</param>
        protected PoolStack(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// 租借
        /// </summary>
        /// <returns></returns>
        public virtual T Rent()
        {
            T dequeue;

            if (!_poolQueue.TryPop(out T Out))
            {
                dequeue = new T();
            }
            else
            {
                _poolCount--;
                dequeue = Out;
            }

            dequeue.SetIsPool(true);
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
            _poolQueue.Push(item);
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
