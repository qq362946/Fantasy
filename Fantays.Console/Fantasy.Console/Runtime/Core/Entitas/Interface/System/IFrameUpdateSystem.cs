using System;

namespace Fantasy.Entitas.Interface
{
    internal interface IFrameUpdateSystem : IEntitiesSystem { }
    /// <summary>
    /// 帧更新时间的抽象接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FrameUpdateSystem<T> : IFrameUpdateSystem where T : Entity
    {
        /// <summary>
        /// 实体的类型
        /// </summary>
        /// <returns></returns>
        public Type EntitiesType() => typeof(T);
        /// <summary>
        /// 事件的抽象方法，需要自己实现这个方法
        /// </summary>
        /// <param name="self">触发事件的实体实例</param>
        protected abstract void FrameUpdate(T self);
        /// <summary>
        /// 框架内部调用的触发FrameUpdate的方法
        /// </summary>
        /// <param name="self"></param>
        public void Invoke(Entity self)
        {
            FrameUpdate((T) self);
        }
    }
}