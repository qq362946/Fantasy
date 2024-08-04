using System;

namespace Fantasy
{
    public interface IDestroySystem : IEntitiesSystem { }
    public abstract class DestroySystem<T> : IDestroySystem where T : Entity
    {
        public Type EntitiesType() => typeof(T);
        protected abstract void Destroy(T self);
        public void Invoke(Entity self)
        {
            Destroy((T) self);
        }
    }
}