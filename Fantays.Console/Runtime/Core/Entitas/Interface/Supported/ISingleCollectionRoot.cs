#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Entitas.Interface
{
    /// <summary>
    /// Entity保存到数据库的时候会根据子组件设置分离存储特性分表存储在不同的集合表中
    /// </summary>
    public interface ISingleCollectionRoot { }
    public static class SingleCollectionRootChecker<T> where T : Entity
    {
        public static bool IsSupported { get; }

        static SingleCollectionRootChecker()
        {
            IsSupported = typeof(ISingleCollectionRoot).IsAssignableFrom(typeof(T));
        }
    }
}