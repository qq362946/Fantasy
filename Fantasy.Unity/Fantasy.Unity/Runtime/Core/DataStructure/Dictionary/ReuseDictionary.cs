using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 提供一个可以重用的字典类，支持使用对象池管理。
    /// </summary>
    /// <typeparam name="TM">字典中键的类型。</typeparam>
    /// <typeparam name="TN">字典中值的类型。</typeparam>
    public sealed class ReuseDictionary<TM, TN> : Dictionary<TM, TN>, IDisposable, IPool where TM : notnull
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
        private bool _isDispose;

        /// <summary>
        /// 创建一个新的 <see cref="ReuseDictionary{TM, TN}"/> 实例。
        /// </summary>
        /// <returns>新创建的实例。</returns>
        public static ReuseDictionary<TM, TN> Create()
        {
#if FANTASY_WEBGL
            var entityDictionary = Pool<ReuseDictionary<TM, TN>>.Rent();
#else
            var entityDictionary = MultiThreadPool.Rent<ReuseDictionary<TM, TN>>();
#endif
            entityDictionary._isDispose = false;
            entityDictionary.IsPool = true;
            return entityDictionary;
        }

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
            Pool<ReuseDictionary<TM, TN>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }
    }
}