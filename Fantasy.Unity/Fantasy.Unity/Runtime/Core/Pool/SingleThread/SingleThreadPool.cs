using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fantasy
{
    /// <summary>
    /// 单线程对象池。
    /// </summary>
    public class SingleThreadPool : IDisposable
    {
        private readonly Dictionary<Type, SingleThreadPoolQueue> _objectPools = new Dictionary<Type, SingleThreadPoolQueue>();
        
        /// <summary>
        /// 从对象池中获取一个对象实例。如果池为空，则创建一个新的对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Rent<T>() where T : class
        {
            return Rent(typeof(T)) as T;
        }
        
        /// <summary>
        /// 从对象池中获取一个对象实例。如果池为空，则创建一个新的对象。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Rent(Type type)
        {
            return GetPool(type).Rent();
        }
        
        /// <summary>
        /// 将对象实例返回到对象池中。
        /// </summary>
        /// <param name="obj"></param>
        public void Return(object obj)
        {
            var type = obj.GetType();
            var pool = GetPool(type);
            pool.Return(obj);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SingleThreadPoolQueue GetPool(Type type)
        {
            if (_objectPools.TryGetValue(type, out var singleThreadPoolQueue))
            {
                return singleThreadPoolQueue;
            }
            
            singleThreadPoolQueue = new SingleThreadPoolQueue(type, 2000);
            _objectPools.Add(type, singleThreadPoolQueue);
            return singleThreadPoolQueue;
        }

        /// <summary>
        /// 释放对象池。
        /// </summary>
        public void Dispose()
        {
            foreach (var (_, singleThreadPoolQueue) in _objectPools)
            {
                singleThreadPoolQueue.Dispose();
            }
            
            _objectPools.Clear();
        }
    }
}