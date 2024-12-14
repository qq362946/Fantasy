#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Entitas.Interface
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
