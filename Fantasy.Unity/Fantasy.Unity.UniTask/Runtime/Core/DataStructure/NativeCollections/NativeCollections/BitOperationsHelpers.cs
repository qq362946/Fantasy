using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Numerics;
using System.Runtime.Intrinsics;
#else
using System;
#endif

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     BitOperations helpers
    /// </summary>
    internal static class BitOperationsHelpers
    {
#if !NET7_0_OR_GREATER
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
#if NET7_0_OR_GREATER
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

        /// <summary>
        ///     And
        /// </summary>
        /// <param name="destination">Destination</param>
        /// <param name="source">Source</param>
        /// <param name="count">Count</param>
        public static void And(Span<int> destination, Span<int> source, uint count)
        {
            switch (count)
            {
                case 7:
                    destination[6] &= source[6];
                    goto case 6;
                case 6:
                    destination[5] &= source[5];
                    goto case 5;
                case 5:
                    destination[4] &= source[4];
                    goto case 4;
                case 4:
                    destination[3] &= source[3];
                    goto case 3;
                case 3:
                    destination[2] &= source[2];
                    goto case 2;
                case 2:
                    destination[1] &= source[1];
                    goto case 1;
                case 1:
                    destination[0] &= source[0];
                    return;
                case 0:
                    return;
            }

            ref var left = ref MemoryMarshal.GetReference(destination);
            ref var right = ref MemoryMarshal.GetReference(source);
#if NET7_0_OR_GREATER
            uint i = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                var n = count - 7;
                for (; i < n; i += 8)
                {
                    var result = Vector256.LoadUnsafe(ref left, i) & Vector256.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                var n = count - 3;
                for (; i < n; i += 4)
                {
                    var result = Vector128.LoadUnsafe(ref left, i) & Vector128.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }

            for (; i < count; ++i)
                Unsafe.Add(ref left, i) &= Unsafe.Add(ref right, i);
#else
            var i = 0;
            for (; i < count; ++i)
                Unsafe.Add(ref left, i) &= Unsafe.Add(ref right, i);
#endif
        }

        /// <summary>
        ///     Or
        /// </summary>
        /// <param name="destination">Destination</param>
        /// <param name="source">Source</param>
        /// <param name="count">Count</param>
        public static void Or(Span<int> destination, Span<int> source, uint count)
        {
            switch (count)
            {
                case 7:
                    destination[6] |= source[6];
                    goto case 6;
                case 6:
                    destination[5] |= source[5];
                    goto case 5;
                case 5:
                    destination[4] |= source[4];
                    goto case 4;
                case 4:
                    destination[3] |= source[3];
                    goto case 3;
                case 3:
                    destination[2] |= source[2];
                    goto case 2;
                case 2:
                    destination[1] |= source[1];
                    goto case 1;
                case 1:
                    destination[0] |= source[0];
                    return;
                case 0:
                    return;
            }

            ref var left = ref MemoryMarshal.GetReference(destination);
            ref var right = ref MemoryMarshal.GetReference(source);
#if NET7_0_OR_GREATER
            uint i = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                var n = count - 7;
                for (; i < n; i += 8)
                {
                    var result = Vector256.LoadUnsafe(ref left, i) | Vector256.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                var n = count - 3;
                for (; i < n; i += 4)
                {
                    var result = Vector128.LoadUnsafe(ref left, i) | Vector128.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }

            for (; i < count; ++i)
                Unsafe.Add(ref left, i) |= Unsafe.Add(ref right, i);
#else
            var i = 0;
            for (; i < count; ++i)
                Unsafe.Add(ref left, i) |= Unsafe.Add(ref right, i);
#endif
        }

        /// <summary>
        ///     Xor
        /// </summary>
        /// <param name="destination">Destination</param>
        /// <param name="source">Source</param>
        /// <param name="count">Count</param>
        public static void Xor(Span<int> destination, Span<int> source, uint count)
        {
            switch (count)
            {
                case 7:
                    destination[6] ^= source[6];
                    goto case 6;
                case 6:
                    destination[5] ^= source[5];
                    goto case 5;
                case 5:
                    destination[4] ^= source[4];
                    goto case 4;
                case 4:
                    destination[3] ^= source[3];
                    goto case 3;
                case 3:
                    destination[2] ^= source[2];
                    goto case 2;
                case 2:
                    destination[1] ^= source[1];
                    goto case 1;
                case 1:
                    destination[0] ^= source[0];
                    return;
                case 0:
                    return;
            }

            ref var left = ref MemoryMarshal.GetReference(destination);
            ref var right = ref MemoryMarshal.GetReference(source);
#if NET7_0_OR_GREATER
            uint i = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                var n = count - 7;
                for (; i < n; i += 8)
                {
                    var result = Vector256.LoadUnsafe(ref left, i) ^ Vector256.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                var n = count - 3;
                for (; i < n; i += 4)
                {
                    var result = Vector128.LoadUnsafe(ref left, i) ^ Vector128.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }

            for (; i < count; ++i)
                Unsafe.Add(ref left, i) ^= Unsafe.Add(ref right, i);
#else
            var i = 0;
            for (; i < count; ++i)
                Unsafe.Add(ref left, i) ^= Unsafe.Add(ref right, i);
#endif
        }

        /// <summary>
        ///     Not
        /// </summary>
        /// <param name="destination">Destination</param>
        /// <param name="count">Count</param>
        public static void Not(Span<int> destination, uint count)
        {
            switch (count)
            {
                case 7:
                    destination[6] = ~destination[6];
                    goto case 6;
                case 6:
                    destination[5] = ~destination[5];
                    goto case 5;
                case 5:
                    destination[4] = ~destination[4];
                    goto case 4;
                case 4:
                    destination[3] = ~destination[3];
                    goto case 3;
                case 3:
                    destination[2] = ~destination[2];
                    goto case 2;
                case 2:
                    destination[1] = ~destination[1];
                    goto case 1;
                case 1:
                    destination[0] = ~destination[0];
                    return;
                case 0:
                    return;
            }

            ref var value = ref MemoryMarshal.GetReference(destination);
#if NET7_0_OR_GREATER
            uint i = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                var n = count - 7;
                for (; i < n; i += 8)
                {
                    var result = ~Vector256.LoadUnsafe(ref value, i);
                    result.StoreUnsafe(ref value, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                var n = count - 3;
                for (; i < n; i += 4)
                {
                    var result = ~Vector128.LoadUnsafe(ref value, i);
                    result.StoreUnsafe(ref value, i);
                }
            }

            for (; i < count; ++i)
                Unsafe.Add(ref value, i) = ~ Unsafe.Add(ref value, i);
#else
            var i = 0;
            for (; i < count; ++i)
                Unsafe.Add(ref value, i) = ~ Unsafe.Add(ref value, i);
#endif
        }
    }
}