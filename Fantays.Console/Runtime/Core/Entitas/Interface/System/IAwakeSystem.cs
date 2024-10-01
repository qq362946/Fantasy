using System;

namespace Fantasy.Entitas.Interface
{
    internal interface IAwakeSystem : IEntitiesSystem { }
    /// <summary>
    /// 实体的Awake事件的抽象接口
    /// </summary>
    /// <typeparam name="T">实体的泛型类型</typeparam>
    public abstract class AwakeSystem<T> : IAwakeSystem where T : Entity
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
        protected abstract void Awake(T self);
        /// <summary>
        /// 框架内部调用的触发Awake的方法。
        /// </summary>
        /// <param name="self">触发事件的实体实例</param>
        public void Invoke(Entity self)
        {
            Awake((T) self);
        }
    }
    
    /// <summary>
    /// 实体的Awake事件的抽象接口
    /// </summary>
    /// <typeparam name="T">实体的泛型类型</typeparam>
    /// <typeparam name="T1">需要传递的参数泛型类型</typeparam>
    public abstract class AwakeSystem<T, T1> : IAwakeSystem where T : Entity where T1 : struct
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
        /// <param name="ages">传递的参数</param>
        protected abstract void Awake(T self, T1 ages);
        /// <summary>
        /// 框架内部调用的触发Awake的方法。
        /// </summary>
        /// <param name="self">触发事件的实体实例</param>
        /// <param name="ages">传递的参数</param>
        public void Invoke(Entity self, T1 ages)
        {
            Awake((T)self, ages);
        }
        /// <summary>
        /// 该方法不可使用
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Invoke(Entity entity)
        {
            throw new NotImplementedException("This method is not implemented for AwakeSystem<T, T1>");
        }
    }
}