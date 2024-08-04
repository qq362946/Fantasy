using System;

namespace Fantasy
{
    public interface IEntitiesSystem
    {
        Type EntitiesType();
        void Invoke(Entity entity);
    }
}