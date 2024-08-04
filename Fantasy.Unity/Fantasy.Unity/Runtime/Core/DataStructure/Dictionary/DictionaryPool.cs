using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 提供一个可以使用对象池管理的字典类。
    /// </summary>
    /// <typeparam name="TM">字典中键的类型。</typeparam>
    /// <typeparam name="TN">字典中值的类型。</typeparam>
    public sealed class DictionaryPool<TM, TN> : Dictionary<TM, TN>, IDisposable, IPool where TM : notnull
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
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
#if FANTASY_WEBGL
            Pool<DictionaryPool<TM, TN>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }

        /// <summary>
        /// 创建一个新的 <see cref="DictionaryPool{TM, TN}"/> 实例。
        /// </summary>
        /// <returns>新创建的实例。</returns>
        public static DictionaryPool<TM, TN> Create()
        {
#if FANTASY_WEBGL
            var dictionary = Pool<DictionaryPool<TM, TN>>.Rent();
#else
            var dictionary = MultiThreadPool.Rent<DictionaryPool<TM, TN>>();
#endif
            dictionary._isDispose = false;
            dictionary.IsPool = true;
            return dictionary;
        }
    }
}