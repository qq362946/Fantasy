using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 提供一个可以使用对象池管理的字典类。
    /// </summary>
    /// <typeparam name="TM">字典中键的类型。</typeparam>
    /// <typeparam name="TN">字典中值的类型。</typeparam>
    public sealed class DictionaryPool<TM, TN> : Dictionary<TM, TN>, IDisposable where TM : notnull
    {
        private bool _isDispose;

        /// <summary>
        /// 释放实例占用的资源。
        /// </summary>
        public void Dispose()
        {
            if (_isDispose)
            {
                return;
            }

            _isDispose = true;
            Clear();
            Pool<DictionaryPool<TM, TN>>.Return(this);
        }

        /// <summary>
        /// 创建一个新的 <see cref="DictionaryPool{TM, TN}"/> 实例。
        /// </summary>
        /// <returns>新创建的实例。</returns>
        public static DictionaryPool<TM, TN> Create()
        {
            var dictionary = Pool<DictionaryPool<TM, TN>>.Rent();
            dictionary._isDispose = false;
            return dictionary;
        }
    }
}