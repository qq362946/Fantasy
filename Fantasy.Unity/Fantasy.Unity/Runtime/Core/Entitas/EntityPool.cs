using System.Collections.Generic;
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

namespace Fantasy
{
    public sealed class EntityPool : PoolCore
    {
        public EntityPool() : base(4096) { }
    }
    
    public sealed class EntityList<T> : List<T>, IPool where T : Entity
    {
        public bool IsPool { get; set; }
    }

    public sealed class EntityListPool<T> : PoolCore<EntityList<T>> where T : Entity
    {
        public EntityListPool() : base(4096) { }
    }
    
    public sealed class EntitySortedDictionary<TM, TN> : SortedDictionary<TM, TN>, IPool where TN : Entity
    {
        public bool IsPool { get; set; }
    }

    public sealed class EntitySortedDictionaryPool<TM, TN> : PoolCore<EntitySortedDictionary<TM, TN>> where TN : Entity
    {
        public EntitySortedDictionaryPool() : base(4096) { }
    }
}