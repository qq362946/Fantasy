using System;

namespace Fantasy
{
    /// <summary>
    /// 表示一个用于反序列化的系统接口，扩展自 <see cref="IEntitiesSystem"/>。
    /// </summary>
    public interface IDeserializeSystem : IEntitiesSystem { }

    /// <summary>
    /// 表示一个用于反序列化特定类型实体的系统抽象类，扩展自 <see cref="IDeserializeSystem"/>。
    /// </summary>
    /// <typeparam name="T">要反序列化的 Entity 类型。</typeparam>
    public abstract class DeserializeSystem<T> : IDeserializeSystem where T : Entity
    {
        /// <summary>
        /// 获取此系统用于处理的实体类型。
        /// </summary>
        /// <returns>实体类型。</returns>
        public Type EntitiesType() => typeof(T);

        /// <summary>
        /// 在派生类中实现，用于反序列化指定的实体。
        /// </summary>
        /// <param name="self">要反序列化的实体。</param>
        protected abstract void Deserialize(T self);

        /// <summary>
        /// 调用实体的反序列化方法。
        /// </summary>
        /// <param name="self">要反序列化的实体。</param>
        public void Invoke(Entity self)
        {
            // 将传入的实体转换为泛型类型并调用反序列化方法。
            Deserialize((T) self);
        }
    }
}