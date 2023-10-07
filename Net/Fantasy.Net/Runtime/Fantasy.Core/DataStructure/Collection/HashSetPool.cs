using System;
using System.Collections.Generic;

namespace Fantasy.DataStructure
{
    /// <summary>
    /// 可释放的哈希集合对象池。
    /// </summary>
    /// <typeparam name="T">哈希集合中元素的类型。</typeparam>
    public sealed class HashSetPool<T> : HashSet<T>, IDisposable
    {
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
            Pool<HashSetPool<T>>.Return(this);
        }

        /// <summary>
        /// 创建一个 <see cref="HashSetPool{T}"/> 哈希集合池的实例。
        /// </summary>
        /// <returns>创建的实例。</returns>
        public static HashSetPool<T> Create()
        {
            var list = Pool<HashSetPool<T>>.Rent();
            list._isDispose = false;
            return list;
        }
    }

    /// <summary>
    /// 基本哈希集合对象池，他自持有实际的哈希集合。
    /// </summary>
    /// <typeparam name="T">哈希集合中元素的类型。</typeparam>
    public sealed class HashSetBasePool<T> : IDisposable
    {
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
            return Pool<HashSetBasePool<T>>.Rent();
        }

        /// <summary>
        /// 释放实例所占用的资源，并将实例返回到对象池中，以便重用。
        /// </summary>
        public void Dispose()
        {
            Set.Clear();
            Pool<HashSetBasePool<T>>.Return(this);
        }
    }
}