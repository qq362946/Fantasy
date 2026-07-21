using System;
using System.Collections.Generic;
using Fantasy.Pool;
using MemoryPack;

namespace Fantasy.Entitas
{
    [MemoryPackable(GenerateType.NoGenerate)]
    public sealed partial class EntityMultiCollection : SortedDictionary<long, Entity>, IPool, IDisposable
    {
        private bool _isPool;
        
        public static EntityMultiCollection Create(bool isPool)
        {
            var collection = isPool
                ? Pool<EntityMultiCollection>.Rent()
                : new EntityMultiCollection();

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
            Pool<EntityMultiCollection>.Return(this);
        }
    }
}