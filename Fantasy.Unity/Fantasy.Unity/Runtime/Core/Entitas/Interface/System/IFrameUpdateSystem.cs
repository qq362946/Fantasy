using System;

namespace Fantasy
{
    /// <summary>
    /// 定义实体帧更新系统的接口。实体帧更新系统用于管理特定类型的实体，在每次帧更新时执行与该类型实体相关的逻辑。
    /// </summary>
    public interface IFrameUpdateSystem : IEntitiesSystem { }
    
    /// <summary>
    /// 表示实体帧更新系统的抽象基类。继承此类可以定义特定类型实体的更新逻辑。
    /// </summary>
    /// <typeparam name="T">实体类型，必须继承自Entity。</typeparam>
    public abstract class FrameUpdateSystem<T> : IFrameUpdateSystem where T : Entity
    {
        /// <summary>
        /// 获取实体帧更新系统所管理的实体类型。
        /// </summary>
        /// <returns>实体类型。</returns>
        public Type EntitiesType() => typeof(T);

        /// <summary>
        /// 在实体帧更新系统中执行特定实体的更新逻辑。具体的实现应在子类中实现。
        /// </summary>
        /// <param name="self">需要执行帧更新逻辑的实体。</param>
        protected abstract void FrameUpdate(T self);

        /// <summary>
        /// 在实体帧更新系统中调用更新逻辑。
        /// </summary>
        /// <param name="self">需要执行帧更新逻辑的实体。</param>
        public void Invoke(Entity self)
        {
            FrameUpdate((T) self);
        }
    }
}