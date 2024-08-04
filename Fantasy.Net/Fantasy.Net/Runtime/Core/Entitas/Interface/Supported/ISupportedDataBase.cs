#if FANTASY_NET
namespace Fantasy
{
    /// <summary>
    /// Entity支持数据库
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface ISupportedDataBase { }

    public static class SupportedDataBaseChecker<T> where T : Entity
    {
        public static bool IsSupported { get; }

        static SupportedDataBaseChecker()
        {
            IsSupported = typeof(ISupportedDataBase).IsAssignableFrom(typeof(T));
        }
    }
}
#endif
