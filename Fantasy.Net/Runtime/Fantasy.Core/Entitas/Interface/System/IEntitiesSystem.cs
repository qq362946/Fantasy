using System;

namespace Fantasy
{
    public interface IEntitiesSystem
    {
        public Type EntitiesType();
        void Invoke(Entity entity);
    }
}