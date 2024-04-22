using System;

namespace Fantasy
{
    /// <summary>
    /// 定义实体的唤醒系统接口。当需要在实体唤醒时执行特定的逻辑时，应实现此接口。
    /// </summary>
    public interface IAwakeSystem : IEntitiesSystem { }

    /// <summary>
    /// 表示用于实现实体唤醒逻辑的抽象基类。继承此类的子类用于处理特定类型的实体的唤醒操作。
    /// </summary>
    /// <typeparam name="T">需要处理唤醒逻辑的实体类型。</typeparam>
    public abstract class AwakeSystem<T> : IAwakeSystem where T : Entity
    {
        /// <summary>
        /// 获取需要处理唤醒逻辑的实体类型。
        /// </summary>
        /// <returns>实体类型。</returns>
        public Type EntitiesType() => typeof(T);

        /// <summary>
        /// 在实体唤醒时执行的逻辑。子类应实现此方法以处理特定实体类型的唤醒操作。
        /// </summary>
        /// <param name="self">正在唤醒的实体。</param>
        protected abstract void Awake(T self);

        /// <summary>
        /// 调用实体的唤醒逻辑。在实体唤醒时，会调用此方法来执行相应的唤醒操作。
        /// </summary>
        /// <param name="self">正在唤醒的实体。</param>
        public void Invoke(Entity self)
        {
            Awake((T) self);
        }
    }

    /// <summary>
    /// 表示用于实现实体唤醒逻辑的抽象基类。继承此类的子类用于处理特定类型的实体的唤醒操作。
    /// </summary>
    /// <typeparam name="T">需要处理唤醒逻辑的实体类型。</typeparam>
    /// <typeparam name="T1">参数的类型</typeparam>
    public abstract class AwakeSystem<T, T1> : IAwakeSystem where T : Entity where T1 : struct
    {
        /// <summary>
        /// 获取需要处理唤醒逻辑的实体类型。
        /// </summary>
        /// <returns>实体类型。</returns>
        public Type EntitiesType() => typeof(T);

        /// <summary>
        /// 在实体唤醒时执行的逻辑。子类应实现此方法以处理特定实体类型的唤醒操作。
        /// </summary>
        /// <param name="self">正在唤醒的实体。</param>
        /// <param name="ages">参数</param>
        protected abstract void Awake(T self, T1 ages);

        /// <summary>
        /// 调用实体的唤醒逻辑。在实体唤醒时，会调用此方法来执行相应的唤醒操作。
        /// </summary>
        /// <param name="self">正在唤醒的实体。</param>
        /// <param name="ages">参数</param>
        public void Invoke(Entity self, T1 ages)
        {
            Awake((T)self, ages);
        }
        
        /// <summary>
        /// 调用实体的唤醒逻辑。在实体唤醒时，会调用此方法来执行相应的唤醒操作。
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Invoke(Entity entity)
        {
            throw new NotImplementedException();
        }
    }
}