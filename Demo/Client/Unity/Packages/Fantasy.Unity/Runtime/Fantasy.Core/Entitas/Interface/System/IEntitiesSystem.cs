using System;

namespace Fantasy
{
    /// <summary>
    /// 定义实体系统的接口。实体系统用于管理特定类型的实体，执行与该类型实体相关的逻辑。
    /// </summary>
    public interface IEntitiesSystem
    {
        /// <summary>
        /// 获取实体系统所管理的实体类型。
        /// </summary>
        /// <returns>实体类型。</returns>
        public Type EntitiesType();

        /// <summary>
        /// 在实体系统中执行特定实体的逻辑。具体的实现应在子类中实现。
        /// </summary>
        /// <param name="entity">需要执行逻辑的实体。</param>
        void Invoke(Entity entity);
    }
}