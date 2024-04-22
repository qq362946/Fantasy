using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 实体对象列表，继承自 List&lt;T&gt;，并实现 IDisposable 接口，用于创建和管理实体对象的集合。
    /// </summary>
    /// <typeparam name="T">实体对象的类型。</typeparam>
    public sealed class EntityList<T> : List<T>, IDisposable, IPool where T : IDisposable
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
        private bool _isDispose;

        /// <summary>
        /// 创建一个 <see cref="EntityList{T}"/> 实体对象列表的实例。
        /// </summary>
        /// <returns>创建的实例。</returns>
        public static EntityList<T> Create()
        {
            var list = MultiThreadPool.Rent<EntityList<T>>();
            list._isDispose = false;
            list.IsPool = true;
            return list;
        }

        /// <summary>
        /// 清空列表，并释放所有实体对象的资源。
        /// </summary>
        public new void Clear()
        {
            // 逐个释放实体对象的资源
            for (var i = 0; i < this.Count; i++)
            {
                this[i].Dispose();
            }
            // 调用基类的 Clear 方法，清空列表
            base.Clear();
        }

        /// <summary>
        /// 清空列表，但不释放实体对象的资源。
        /// </summary>
        public void ClearNotDispose()
        {
            base.Clear();
        }

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
            MultiThreadPool.Return(this);
        }
    }
}