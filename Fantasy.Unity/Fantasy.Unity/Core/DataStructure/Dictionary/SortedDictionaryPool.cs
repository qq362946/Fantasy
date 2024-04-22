using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 提供一个可以使用对象池管理的排序字典类。
    /// </summary>
    /// <typeparam name="TM"></typeparam>
    /// <typeparam name="TN"></typeparam>
    public sealed class SortedDictionaryPool<TM, TN> : SortedDictionary<TM, TN>, IDisposable, IPool where TM : notnull
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
            MultiThreadPool.Return(this);
        }
        
        /// <summary>
        /// 创建一个新的 <see cref="SortedDictionaryPool{TM, TN}"/> 实例。
        /// </summary>
        /// <returns>新创建的实例。</returns>
        public static SortedDictionaryPool<TM, TN> Create()
        {
            var dictionary = MultiThreadPool.Rent<SortedDictionaryPool<TM, TN>>();
            dictionary._isDispose = false;
            dictionary.IsPool = true;
            return dictionary;
        }
    }
}