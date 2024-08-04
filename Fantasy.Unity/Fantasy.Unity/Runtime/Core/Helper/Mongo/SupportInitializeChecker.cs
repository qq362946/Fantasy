#if FANTASY_NET
using System.ComponentModel;
namespace Fantasy;

public static class SupportInitializeChecker<T> where T : Entity
{
    public static bool IsSupported { get; }

    static SupportInitializeChecker()
    {
        IsSupported = typeof(ISupportInitialize).IsAssignableFrom(typeof(T));
    }
}
#endif