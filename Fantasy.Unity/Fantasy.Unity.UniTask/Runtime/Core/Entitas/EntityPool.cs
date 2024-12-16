using System.Collections.Generic;
using Fantasy.Pool;

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

namespace Fantasy.Entitas
{
    internal sealed class EntityPool : PoolCore
    {
        public EntityPool() : base(4096) { }
    }
    
    internal sealed class EntityList<T> : List<T>, IPool where T : Entity
    {
        private bool _isPool;
        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }
    }

    internal sealed class EntityListPool<T> : PoolCore<EntityList<T>> where T : Entity
    {
        public EntityListPool() : base(4096) { }
    }
    
    internal sealed class EntitySortedDictionary<TM, TN> : SortedDictionary<TM, TN>, IPool where TN : Entity
    {
        private bool _isPool;
        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }
    }

    internal sealed class EntitySortedDictionaryPool<TM, TN> : PoolCore<EntitySortedDictionary<TM, TN>> where TN : Entity
    {
        public EntitySortedDictionaryPool() : base(4096) { }
    }
}