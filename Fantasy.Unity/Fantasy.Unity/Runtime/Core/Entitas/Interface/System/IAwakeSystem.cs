using System;

namespace Fantasy
{
    public interface IAwakeSystem : IEntitiesSystem { }
    public abstract class AwakeSystem<T> : IAwakeSystem where T : Entity
    {
        public Type EntitiesType() => typeof(T);
        protected abstract void Awake(T self);
        public void Invoke(Entity self)
        {
            Awake((T) self);
        }
    }
    
    public abstract class AwakeSystem<T, T1> : IAwakeSystem where T : Entity where T1 : struct
    {
        public Type EntitiesType() => typeof(T);
        protected abstract void Awake(T self, T1 ages);
        public void Invoke(Entity self, T1 ages)
        {
            Awake((T)self, ages);
        }
        public void Invoke(Entity entity)
        {
            throw new NotImplementedException("This method is not implemented for AwakeSystem<T, T1>");
        }
    }
}