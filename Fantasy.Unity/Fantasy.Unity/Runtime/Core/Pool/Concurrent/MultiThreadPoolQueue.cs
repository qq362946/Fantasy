using System;
using System.Collections.Concurrent;
using System.Threading;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy
{
    /// <summary>
    /// 线程安全的对象池。
    /// </summary>
    public class MultiThreadPoolQueue
    {
        private int _poolCount;
        private object _fastObject;
        private readonly Type _objectType;
        private readonly int _maxCapacity;
        private readonly ConcurrentQueue<object> _poolQueue = new ConcurrentQueue<object>();

        /// <summary>
        /// 不允许无参数实例化
        /// </summary>
        private MultiThreadPoolQueue()
        {
            
        }

        /// <summary>
        /// ConcurrentPool的构造函数
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="maxCapacity"></param>
        public MultiThreadPoolQueue(Type objectType, int maxCapacity)
        {
            _objectType = objectType;
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// 从对象池中获取一个对象实例。如果池为空，则创建一个新的对象。
        /// </summary>
        /// <returns>获取的对象实例。</returns>
        public object Rent()
        {
            var obj = _fastObject;

            if (_fastObject != null && Interlocked.CompareExchange(ref _fastObject, null, obj) == obj)
            {
                return obj;
            }
            
            if (!_poolQueue.TryDequeue(out var t))
            {
                return Activator.CreateInstance(_objectType);
            }
            Interlocked.Decrement(ref _poolCount);
            return t;
        }

        /// <summary>
        /// 将对象实例返回到对象池中。
        /// </summary>
        public void Return(object obj)
        {
            if (_fastObject == null && Interlocked.CompareExchange(ref _fastObject, obj, null) == null)
            {
                return;
            }
            
            if (Interlocked.Increment(ref _poolCount) <= _maxCapacity)
            {
                _poolQueue.Enqueue(obj);
                return;
            }
                
            Interlocked.Decrement(ref _poolCount);
        }
    }
}