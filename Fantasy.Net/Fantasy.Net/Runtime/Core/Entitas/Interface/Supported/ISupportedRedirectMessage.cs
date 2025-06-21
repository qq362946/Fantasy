#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if FANTASY_NET
namespace Fantasy.Entitas.Interface
{

    /// <summary>
    /// 消息分发时允许重定向转发给指定Entity
    /// </summary>
    public interface ISupportedRedirectMessage 
    {
        /// <summary>
        /// 重定向的实体类型
        /// </summary>
        public Entity RedirectEntity
        {
            get;
        }
    }
    public static class SupportedRedirectMessageChecker<T> where T : Entity
    {
        public static bool IsSupported { get; }

        static SupportedRedirectMessageChecker()
        {
            IsSupported = typeof(ISupportedRedirectMessage).IsAssignableFrom(typeof(T));
        }
    }
}
#endif
