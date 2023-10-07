using System;
using System.Collections.Concurrent;

namespace Fantasy
{
    /// <summary>
    /// 线程安全的静态通用对象池。
    /// </summary>
    /// <typeparam name="T">要存储在对象池中的对象类型。</typeparam>
    public static class ConcurrentPool<T>
    {
        private static readonly ConcurrentQueue<T> PoolQueue = new ConcurrentQueue<T>();

        /// <summary>
        /// 获取对象池中的对象数量。
        /// </summary>
        public static int Count => PoolQueue.Count;

        /// <summary>
        /// 从对象池中获取一个对象实例。如果池为空，则创建一个新的对象。
        /// </summary>
        /// <returns>获取的对象实例。</returns>
        public static T Rent()
        {
            return PoolQueue.TryDequeue(out var t) ? t : Activator.CreateInstance<T>();
        }

        /// <summary>
        /// 从对象池中获取一个对象实例。如果池为空，则使用提供的生成器函数创建一个新的对象。
        /// </summary>
        /// <param name="generator">用于生成新对象的函数。</param>
        /// <returns>获取的对象实例。</returns>
        public static T Rent(Func<T> generator)
        {
            return PoolQueue.TryDequeue(out var t) ? t : generator();
        }

        /// <summary>
        /// 将对象实例返回到对象池中。
        /// </summary>
        /// <param name="t">要返回的对象实例。</param>
        public static void Return(T t)
        {
            if (t == null)
            {
                return;
            }
    
            PoolQueue.Enqueue(t);
        }

        /// <summary>
        /// 将对象实例返回到对象池中，并使用提供的重置函数对对象进行重置操作。
        /// </summary>
        /// <param name="t">要返回的对象实例。</param>
        /// <param name="reset">用于重置对象的函数。</param>
        public static void Return(T t, Action<T> reset)
        {
            if (t == null)
            {
                return;
            }

            reset(t);
            PoolQueue.Enqueue(t);
        }
    }
}