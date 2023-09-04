using System;
using System.Collections.Generic;

namespace Fantasy.DataStructure
{
    /// <summary>
    /// 可释放的列表（List）对象池。
    /// </summary>
    /// <typeparam name="T">列表中元素的类型。</typeparam>
    public sealed class ListPool<T> : List<T>, IDisposable
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
            Pool<ListPool<T>>.Return(this);
        }

        /// <summary>
        /// 使用指定的元素创建一个 <see cref="ListPool{T}"/> 列表（List）对象池的实例。
        /// </summary>
        /// <param name="args">要添加到列表的元素。</param>
        /// <returns>创建的实例。</returns>
        public static ListPool<T> Create(params T[] args)
        {
            var list = Pool<ListPool<T>>.Rent();
            list._isDispose = false;
            if (args != null) list.AddRange(args);
            return list;
        }

        /// <summary>
        /// 使用指定的列表创建一个 <see cref="ListPool{T}"/> 列表（List）对象池的实例。
        /// </summary>
        /// <param name="args">要添加到列表的元素列表。</param>
        /// <returns>创建的实例。</returns>
        public static ListPool<T> Create(List<T> args)
        {
            var list = Pool<ListPool<T>>.Rent();
            list._isDispose = false;
            if (args != null) list.AddRange(args);
            return list;
        }
    }
}