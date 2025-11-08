using System;
using System.Collections.Generic;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable CheckNamespace

namespace Fantasy.Pool
{
    /// <summary>
    /// 静态的对象池系统，不支持多线程。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Pool<T> where T : IPool, new()
    {
        private static readonly Queue<T> PoolQueue = new Queue<T>();
        /// <summary>
        /// 池子里可用的数量
        /// </summary>
        public static int Count => PoolQueue.Count;
        
        /// <summary>
        /// 租借
        /// </summary>
        /// <returns></returns>
        public static T Rent() 
        {
            return PoolQueue.Count == 0 ? new T() : PoolQueue.Dequeue();
        }
        
        /// <summary>
        /// 租借
        /// </summary>
        /// <param name="generator">如果池子里没有，会先执行这个委托。</param>
        /// <returns></returns>
        public static T Rent(Func<T> generator)
        {
            return PoolQueue.Count == 0 ? generator() : PoolQueue.Dequeue();
        }
        
        /// <summary>
        /// 返还
        /// </summary>
        /// <param name="t"></param>
        public static void Return(T t)
        {
            if (t == null)
            {
                return;
            }
    
            PoolQueue.Enqueue(t);
        }
        
        /// <summary>
        /// 返还
        /// </summary>
        /// <param name="t">返还的东西</param>
        /// <param name="reset">返还后执行的委托</param>
        public static void Return(T t, Action<T> reset)
        {
            if (t == null)
            {
                return;
            }

            reset(t);
            PoolQueue.Enqueue(t);
        }
        
        /// <summary>
        /// 清空池子
        /// </summary>
        public static void Clear()
        {
            PoolQueue.Clear();
        }
    }
}