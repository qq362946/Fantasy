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
    /// 线程安全的对象池。(基于<see cref="Stack{T}"/>的实现, 比基于<see cref="Queue{T}"/>的实现<see cref="MultiThreadPoolQueue"/>略快)。
    /// 缓存友好, 且能更好地在开发时暴露池对象未清理干净的问题, 但是注意: 不具备公平性(池中对象不会被平等地利用)。
    /// </summary>
    internal class MultiThreadPoolStack
    {
        // 当前池中对象计数
        private int _poolCount;

        // 池子的最大容量
        private readonly int _maxCapacity;

        // 用于创建新实例的委托
        private readonly Func<IPool> _createInstance;

        // 线程安全的栈，用于存储空闲对象
        private readonly ConcurrentStack<IPool> _poolStack = new ConcurrentStack<IPool>();

        // 私有构造函数，防止无参数实例化
        private MultiThreadPoolStack() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxCapacity">池子的最大容量</param>
        /// <param name="createInstance">创建 IPool 实例的委托</param>

        public MultiThreadPoolStack(int maxCapacity, Func<IPool> createInstance)
        {
            _maxCapacity = maxCapacity;
            _createInstance = createInstance;
        }

        /// <summary>
        /// 从池中租用指定类型 T 的对象。如果池中没有可用对象，则创建一个新对象。
        /// 此方法使用了泛型约束 new() 和 Activator.CreateInstance()
        /// </summary>
        public T Rent<T>() where T : IPool, new()
        {
            // 尝试从栈中弹出一个对象
            if (!_poolStack.TryPop(out var t))
            {
                // 如果栈为空，则创建一个新实例

                var pool = new T();
                pool.SetIsPool(true);
                return pool;
            }

            // 从池中成功取出一个对象
            t.SetIsPool(true);
            // 减少池子中当前对象的计数
            Interlocked.Decrement(ref _poolCount);
            return (T)t;
        }

        /// <summary>
        /// 从池中租用 IPool 类型的对象（使用传入的创建委托）。如果池中没有可用对象，则通过委托创建。
        /// </summary>
        public IPool Rent()
        {
            // 尝试从栈中弹出一个对象

            if (!_poolStack.TryPop(out var t))
            {
                // 如果栈为空，则通过委托创建一个新实例
                var instance = _createInstance();
                instance.SetIsPool(true);
                return instance;
            }

            // 从池中成功取出一个对象
            t.SetIsPool(true);
            // 减少池子中当前对象的计数
            Interlocked.Decrement(ref _poolCount);
            return t;
        }

        /// <summary>
        /// 将对象归还给池子。
        /// </summary>
        /// <param name="obj">要归还的对象。</param>
        public void Return(IPool obj)
        {
            // 检查对象是否来自池子（或者是否是已经被标记归还过但被多线程意外再次归还的情况）
            if (!obj.IsPool())
            {
                return;
            }

            // 归还前，先将对象的 IsPool 状态设置为 false
            obj.SetIsPool(false);

            // 尝试增加池中对象计数
            if (Interlocked.Increment(ref _poolCount) <= _maxCapacity)
            {
                // 如果计数没有超过最大容量，则将对象压入栈中
                _poolStack.Push(obj);
                return;
            }

            Interlocked.Decrement(ref _poolCount);
        }
    }
}
#endif