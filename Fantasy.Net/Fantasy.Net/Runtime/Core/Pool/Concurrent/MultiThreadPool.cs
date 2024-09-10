#if !FANTASY_WEBGL
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Pool
{
    /// <summary>
    /// 线程安全的静态通用对象池。
    /// </summary>
    internal static class MultiThreadPool
    {
        private static readonly ConcurrentDictionary<Type, MultiThreadPoolQueue> ObjectPools = new ConcurrentDictionary<Type, MultiThreadPoolQueue>();

        public static T Rent<T>() where T : IPool, new()
        {
            return ObjectPools.GetOrAdd(typeof(T), t => new MultiThreadPoolQueue(2000, () => new T())).Rent<T>();
        }

        public static IPool Rent(Type type)
        {
            return ObjectPools.GetOrAdd(type, t => new MultiThreadPoolQueue(2000, CreateInstance.CreateIPool(type))).Rent();
        }

        public static void Return<T>(T obj) where T : IPool, new()
        {
            if (!obj.IsPool())
            {
                return;
            }

            ObjectPools.GetOrAdd(typeof(T), t => new MultiThreadPoolQueue(2000, () => new T())).Return(obj);
        }
    }
}
#endif