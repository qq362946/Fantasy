#if NET5_0_OR_GREATER
using System.Numerics;

#else
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

// ReSharper disable RedundantExplicitArraySize

namespace NativeCollections
{
    /// <summary>
    ///     BitOperations helpers
    /// </summary>
    internal static class BitOperationsHelpers
    {
#if !NET5_0_OR_GREATER
        /// <summary>
        ///     DeBruijn sequence
        /// </summary>
        private static ReadOnlySpan<byte> Log2DeBruijn => new byte[32]
        {
            0, 9, 1, 10, 13, 21, 2, 29,
            11, 14, 16, 18, 22, 25, 3, 30,
            8, 12, 20, 28, 15, 17, 24, 7,
            19, 27, 23, 6, 26, 5, 4, 31
        };
#endif

        /// <summary>
        ///     Log2
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Log2</returns>
        public static int Log2(int value) => Log2((uint)value);

        /// <summary>
        ///     Log2
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Log2</returns>
        public static int Log2(uint value)
        {
#if NET5_0_OR_GREATER
            return BitOperations.Log2(value);
#else
            value |= 1;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(Log2DeBruijn), (nint)(int)((value * 130329821U) >> 27));
#endif
        }
    }
}