using System;
using System.Collections.Generic;
using Fantasy.Pool;
using MemoryPack;

namespace Fantasy.Entitas
{
    [MemoryPackable(GenerateType.NoGenerate)]
    public sealed partial class EntityTreeCollection : SortedDictionary<long, Entity>, IPool, IDisposable
    {
        private bool _isPool;
        
        public static EntityTreeCollection Create(bool isPool)
        {
            var collection = isPool
                ? Pool<EntityTreeCollection>.Rent()
                : new EntityTreeCollection();

            collection._isPool = isPool;
            return collection;
        }

        public bool IsPool()
        {
            return _isPool;
        }

        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }

        public void Dispose()
        {
            if (!_isPool)
            {
                return;
            }

            _isPool = false;
            this.Clear();
            Pool<EntityTreeCollection>.Return(this);
        }
    }
}