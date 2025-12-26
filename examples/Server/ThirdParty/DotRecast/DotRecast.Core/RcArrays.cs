using System;
using System.Runtime.CompilerServices;

namespace DotRecast.Core
{
    public static class RcArrays
    {
        // Type Safe Copy
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] sourceArray, long sourceIndex, T[] destinationArray, long destinationIndex, long length)
        {
            Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        }

        // Type Safe Copy
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] sourceArray, T[] destinationArray, long length)
        {
            Array.Copy(sourceArray, destinationArray, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyOf<T>(T[] source, int startIdx, int length)
        {
            var slice = source.AsSpan(startIdx, length);
            return slice.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyOf<T>(T[] source, long length)
        {
            return CopyOf(source, 0, (int)length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[][] Create2D<T>(int len1, int len2)
        {
            var temp = new T[len1][];

            for (int i = 0; i < len1; ++i)
            {
                temp[i] = new T[len2];
            }

            return temp;
        }
    }
}