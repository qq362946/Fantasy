using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Fantasy
{
    /// <summary>
    /// 线程安全的静态通用对象池。
    /// </summary>
    public static class MultiThreadPool
    {
        private static readonly Func<Type, MultiThreadPoolQueue> Func = type => new MultiThreadPoolQueue(type, 2000);
        private static readonly ConcurrentDictionary<Type, MultiThreadPoolQueue> ObjectPools = new ConcurrentDictionary<Type, MultiThreadPoolQueue>();
        
        /// <summary>
        /// 从对象池中获取一个对象实例。如果池为空，则创建一个新的对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Rent<T>() where T : class
        {
            return Rent(typeof(T)) as T;
        }
        
        /// <summary>
        /// 从对象池中获取一个对象实例。如果池为空，则创建一个新的对象。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Rent(Type type)
        {
            return typeof(IPool).IsAssignableFrom(type) ? GetPool(type).Rent() : Activator.CreateInstance(type);
        }

        /// <summary>
        /// 将对象实例返回到对象池中。
        /// </summary>
        /// <param name="obj"></param>
        public static void Return(object obj)
        {
            if (obj is IPool iPool)
            {
                if (iPool.IsPool)
                {
                    return;
                }

                iPool.IsPool = true;
            }
        
            var type = obj.GetType();
            var pool = GetPool(type);
            pool.Return(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MultiThreadPoolQueue GetPool(Type type)
        {
            return ObjectPools.GetOrAdd(type, Func);
        }
    }
}