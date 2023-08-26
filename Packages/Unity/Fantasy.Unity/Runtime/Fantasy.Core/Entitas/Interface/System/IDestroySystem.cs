using System;

namespace Fantasy
{
    /// <summary>
    /// 定义实体销毁系统接口。当需要在实体销毁时执行特定的逻辑时，应实现此接口。
    /// </summary>
    public interface IDestroySystem : IEntitiesSystem { }

    /// <summary>
    /// 表示用于实现实体销毁逻辑的抽象基类。继承此类的子类用于处理特定类型的实体的销毁操作。
    /// </summary>
    /// <typeparam name="T">需要处理销毁逻辑的实体类型。</typeparam>
    public abstract class DestroySystem<T> : IDestroySystem where T : Entity
    {
        /// <summary>
        /// 获取需要处理销毁逻辑的实体类型。
        /// </summary>
        /// <returns>实体类型。</returns>
        public Type EntitiesType() => typeof(T);

        /// <summary>
        /// 在实体销毁时执行的逻辑。子类应实现此方法以处理特定实体类型的销毁操作。
        /// </summary>
        /// <param name="self">正在销毁的实体。</param>
        protected abstract void Destroy(T self);

        /// <summary>
        /// 调用实体的销毁逻辑。在实体销毁时，会调用此方法来执行相应的销毁操作。
        /// </summary>
        /// <param name="self">正在销毁的实体。</param>
        public void Invoke(Entity self)
        {
            Destroy((T) self);
        }
    }
}