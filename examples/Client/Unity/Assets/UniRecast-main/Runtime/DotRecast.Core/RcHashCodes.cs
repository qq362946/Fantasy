using System.Runtime.CompilerServices;

namespace DotRecast.Core
{
    public static class RcHashCodes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }

        // From Thomas Wang, https://gist.github.com/badboy/6267743
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint WangHash(uint a)
        {
            a = (~a) + (a << 18); // a = (a << 18) - a - 1;
            a = a ^ (a >> 31);
            a = a * 21; // a = (a + (a << 2)) + (a << 4);
            a = a ^ (a >> 11);
            a = a + (a << 6);
            a = a ^ (a >> 22);
            return (uint)a;
        }
    }
}