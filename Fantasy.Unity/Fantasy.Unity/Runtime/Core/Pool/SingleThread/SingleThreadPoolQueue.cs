using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 单线程对象池队列。
    /// </summary>
    public class SingleThreadPoolQueue : IDisposable
    {
        private int _poolCount;
        private readonly Type _objectType;
        private readonly int _maxCapacity;
        private readonly Queue<object> _poolQueue = new Queue<object>();

        private SingleThreadPoolQueue()
        {
            // 禁止无参数实例化
        }
        
        /// <summary>
        /// SingleThreadPoolQueue的构造函数。
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="maxCapacity"></param>
        public SingleThreadPoolQueue(Type objectType, int maxCapacity)
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
            if (!_poolQueue.TryDequeue(out var obj))
            {
                return Activator.CreateInstance(_objectType);
            }
            
            _poolCount--;
            return obj;
        }

        /// <summary>
        ///  将对象实例返回到对象池中。   
        /// </summary>
        /// <param name="obj">要回收的对象。</param>
        public void Return(object obj)
        {
            if (++_poolCount <= _maxCapacity)
            {
                _poolQueue.Enqueue(obj);
            }
        }

        /// <summary>
        /// 释放对象池。
        /// </summary>
        public void Dispose()
        {
            _poolCount = 0;
            _poolQueue.Clear();
        }
    }
}