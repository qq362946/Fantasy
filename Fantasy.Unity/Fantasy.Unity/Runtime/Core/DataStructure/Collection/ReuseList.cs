using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 可重用的列表，继承自 <see cref="List{T}"/> 类。该类支持通过对象池重用列表实例，以减少对象分配和释放的开销。
    /// </summary>
    /// <typeparam name="T">列表中元素的类型。</typeparam>
    public sealed class ReuseList<T> : List<T>, IDisposable, IPool
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
        private bool _isDispose;

        /// <summary>
        /// 创建一个 <see cref="ReuseList{T}"/> 可重用的列表的实例。
        /// </summary>
        /// <returns>创建的实例。</returns>
        public static ReuseList<T> Create()
        {
#if FANTASY_WEBGL
            var list = Pool<ReuseList<T>>.Rent();
#else
            var list = MultiThreadPool.Rent<ReuseList<T>>();
#endif
            list._isDispose = false;
            list.IsPool = true;
            return list;
        }

        /// <summary>
        /// 释放该实例所占用的资源，并将实例返回到对象池中，以便重用。
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
            Pool<ReuseList<T>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }
    }
}