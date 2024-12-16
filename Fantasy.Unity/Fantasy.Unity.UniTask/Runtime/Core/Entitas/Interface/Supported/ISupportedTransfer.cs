#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if FANTASY_NET
namespace Fantasy.Entitas.Interface
{
    /// <summary>
    /// Entity支持传送
    /// </summary>
    public interface ISupportedTransfer { }
    public static class SupportedTransferChecker<T> where T : Entity
    {
        public static bool IsSupported { get; }

        static SupportedTransferChecker()
        {
            IsSupported = typeof(ISupportedTransfer).IsAssignableFrom(typeof(T));
        }
    }
}
#endif