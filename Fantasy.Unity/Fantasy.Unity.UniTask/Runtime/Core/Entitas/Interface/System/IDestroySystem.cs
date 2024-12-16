using System;
using Fantasy.Async;

namespace Fantasy.Entitas.Interface
{
    internal interface IDestroySystem : IEntitiesSystem { }
    /// <summary>
    /// 实体销毁事件的抽象接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DestroySystem<T> : IDestroySystem where T : Entity
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
        protected abstract void Destroy(T self);
        /// <summary>
        /// 框架内部调用的触发Destroy的方法
        /// </summary>
        /// <param name="self"></param>
        public void Invoke(Entity self)
        {
            Destroy((T) self);
        }
    }
}