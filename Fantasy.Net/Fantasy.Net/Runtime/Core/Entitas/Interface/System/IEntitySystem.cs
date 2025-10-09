using System;
using Fantasy.Async;

namespace Fantasy.Entitas.Interface
{
    /// <summary>
    /// ECS事件系统的核心接口，任何事件都是要继承这个接口
    /// </summary>
    public interface IEntitySystem
    {
        /// <summary>
        /// 实体的运行时类型,如果从泛型System的实例中调用,这里会是一个运行时闭合泛型,形如 GenericEntity`1 [int]
        /// </summary>
        /// <returns></returns>
        Type EntityType();
        /// <summary>
        /// 框架内部调用的触发事件方法
        /// </summary>
        /// <param name="entity"></param>
        void Invoke(Entity entity);
    }
}