// 以下指令用于禁止 ReSharper 警告，以处理可为空引用类型的情况。
// 在此情况下，可以安全地禁用警告，因为我们处理的是非可为空类型。
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
namespace Fantasy
{
    /// <summary>
    /// 实体引用只读结构，用作对 Entity 实例的引用。
    /// </summary>
    /// <typeparam name="T">Entity 的类型。</typeparam>
    public struct EntityReference<T> where T : Entity
    {
        private T _entity;
        private readonly long _runTimeId;

        // 从 Entity 实例创建 EntityReference 的私有构造函数。
        private EntityReference(T t)
        {
            if (t == null)
            {
                _entity = null;
                _runTimeId = 0;
                return;
            }
            
            _entity = t;
            _runTimeId = t.RuntimeId;
        }

        /// <summary>
        /// 隐式地将 Entity 实例转换为 EntityReference。
        /// </summary>
        /// <param name="t">要转换的 Entity 实例。</param>
        /// <returns>引用同一 Entity 的 EntityReference 实例。</returns>
        public static implicit operator EntityReference<T>(T t)
        {
            return new EntityReference<T>(t);
        }

        /// <summary>
        /// 隐式地将 EntityReference 转换回原始的 Entity 类型。
        /// </summary>
        /// <param name="v">要转换的 EntityReference。</param>
        /// <returns>
        /// 如果运行时 ID 匹配，则返回原始的 Entity 实例，如果不匹配则返回 null，
        /// 或者如果引用为 null，则返回 null。
        /// </returns>
        public static implicit operator T(EntityReference<T> v)
        {
            if (v._entity == null)
            {
                return null;
            }

            if (v._entity.RuntimeId != v._runTimeId)
            {
                v._entity = null;
            }

            return v._entity;
        }
    }
}