using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DotRecast.Core
{
    public static class RcDebug
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            Debug.Assert(condition);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnusedRef<T>(ref T _)
        {
            // ..
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unused<T>(T _)
        {
            // ..
        }
    }
}