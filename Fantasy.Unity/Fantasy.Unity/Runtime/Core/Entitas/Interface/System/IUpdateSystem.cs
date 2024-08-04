using System;

namespace Fantasy
{
    public interface IUpdateSystem : IEntitiesSystem { }
    public abstract class UpdateSystem<T> : IUpdateSystem where T : Entity
    {
        public Type EntitiesType() => typeof(T);
        protected abstract void Update(T self);
        public void Invoke(Entity self)
        {
            Update((T) self);
        }
    }
}