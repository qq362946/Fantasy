using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 可释放的哈希集合对象池。
    /// </summary>
    /// <typeparam name="T">哈希集合中元素的类型。</typeparam>
    public sealed class HashSetPool<T> : HashSet<T>, IDisposable, IPool
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
        private bool _isDispose;

        /// <summary>
        /// 释放实例所占用的资源，并将实例返回到对象池中，以便重用。
        /// </summary>
        public void Dispose()
        {
            if (_isDispose)
            {
                return;
            }

            _isDispose = true;
            Clear();
#if FANTASY_WEBGL
            Pool<HashSetPool<T>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }

        /// <summary>
        /// 创建一个 <see cref="HashSetPool{T}"/> 哈希集合池的实例。
        /// </summary>
        /// <returns>创建的实例。</returns>
        public static HashSetPool<T> Create()
        {
#if FANTASY_WEBGL
            var list = Pool<HashSetPool<T>>.Rent();
            list._isDispose = false;
            list.IsPool = true;
            return list;
#else
            var list = MultiThreadPool.Rent<HashSetPool<T>>();
            list._isDispose = false;
            list.IsPool = true;
            return list;
#endif
        }
    }

    /// <summary>
    /// 基本哈希集合对象池，他自持有实际的哈希集合。
    /// </summary>
    /// <typeparam name="T">哈希集合中元素的类型。</typeparam>
    public sealed class HashSetBasePool<T> : IDisposable, IPool
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
        
        /// <summary>
        /// 存储实际的哈希集合
        /// </summary>
        public HashSet<T> Set = new HashSet<T>();

        /// <summary>
        /// 创建一个 <see cref="HashSetBasePool{T}"/> 基本哈希集合对象池的实例。
        /// </summary>
        /// <returns>创建的实例。</returns>
        public static HashSetBasePool<T> Create()
        {
#if FANTASY_WEBGL
            var hashSetBasePool = Pool<HashSetBasePool<T>>.Rent();
            hashSetBasePool.IsPool = true;
            return hashSetBasePool;
#else
            var hashSetBasePool = MultiThreadPool.Rent<HashSetBasePool<T>>();
            hashSetBasePool.IsPool = true;
            return hashSetBasePool;
#endif
        }

        /// <summary>
        /// 释放实例所占用的资源，并将实例返回到对象池中，以便重用。
        /// </summary>
        public void Dispose()
        {
            Set.Clear();
#if FANTASY_WEBGL
            Pool<HashSetBasePool<T>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }
    }
}