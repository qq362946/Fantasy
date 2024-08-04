using System;

namespace Fantasy
{
    public interface IFrameUpdateSystem : IEntitiesSystem { }
    public abstract class FrameUpdateSystem<T> : IFrameUpdateSystem where T : Entity
    {
        public Type EntitiesType() => typeof(T);
        protected abstract void FrameUpdate(T self);
        public void Invoke(Entity self)
        {
            FrameUpdate((T) self);
        }
    }
}