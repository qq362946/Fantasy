using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fantasy.SourceGenerator.Common
{
    /// <summary>
    /// HashCode算法帮助类
    /// </summary>
    public static class HashCodeHelper
    {
        /// <summary>
        /// 生成一个long的值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ComputeHash64(string str) => Hash64(MemoryMarshal.AsBytes(str.AsSpan()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Hash64(ReadOnlySpan<byte> buffer, ulong seed = 0)
        {
            ref var local1 = ref MemoryMarshal.GetReference(buffer);
            var length = buffer.Length;
            ulong num1;
            if (buffer.Length >= 32)
            {
                var num2 = seed + 6983438078262162902UL;
                var num3 = seed + 14029467366897019727UL;
                var num4 = seed;
                var num5 = seed - 11400714785074694791UL;
                for (; length >= 32; length -= 32)
                {
                    ref var local2 = ref Unsafe.AddByteOffset(ref local1, (IntPtr)(buffer.Length - length));
                    var num6 = num2 + Unsafe.ReadUnaligned<ulong>(ref local2) * 14029467366897019727UL;
                    num2 = (ulong)((((long)num6 << 31) | (long)(num6 >> 33)) * -7046029288634856825L);
                    var num7 = num3 +
                               Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref local2, new UIntPtr(8U))) *
                               14029467366897019727UL;
                    num3 = (ulong)((((long)num7 << 31) | (long)(num7 >> 33)) * -7046029288634856825L);
                    var num8 = num4 +
                               Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref local2, new UIntPtr(16U))) *
                               14029467366897019727UL;
                    num4 = (ulong)((((long)num8 << 31) | (long)(num8 >> 33)) * -7046029288634856825L);
                    var num9 = num5 +
                               Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref local2, new UIntPtr(24U))) *
                               14029467366897019727UL;
                    num5 = (ulong)((((long)num9 << 31) | (long)(num9 >> 33)) * -7046029288634856825L);
                }

                var num10 = (((long)num2 << 1) | (long)(num2 >> 63)) + (((long)num3 << 7) | (long)(num3 >> 57)) +
                            (((long)num4 << 12) | (long)(num4 >> 52)) + (((long)num5 << 18) | (long)(num5 >> 46));
                var num11 = num2 * 14029467366897019727UL;
                var num12 = (((long)num11 << 31) | (long)(num11 >> 33)) * -7046029288634856825L;
                var num13 = (num10 ^ num12) * -7046029288634856825L + -8796714831421723037L;
                var num14 = num3 * 14029467366897019727UL;
                var num15 = (((long)num14 << 31) | (long)(num14 >> 33)) * -7046029288634856825L;
                var num16 = (num13 ^ num15) * -7046029288634856825L + -8796714831421723037L;
                var num17 = num4 * 14029467366897019727UL;
                var num18 = (((long)num17 << 31) | (long)(num17 >> 33)) * -7046029288634856825L;
                var num19 = (num16 ^ num18) * -7046029288634856825L + -8796714831421723037L;
                var num20 = num5 * 14029467366897019727UL;
                var num21 = (((long)num20 << 31) | (long)(num20 >> 33)) * -7046029288634856825L;
                num1 = (ulong)((num19 ^ num21) * -7046029288634856825L + -8796714831421723037L);
            }
            else
                num1 = seed + 2870177450012600261UL;

            var num22 = num1 + (ulong)buffer.Length;
            for (; length >= 8; length -= 8)
            {
                var num23 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref local1,
                    (IntPtr)(buffer.Length - length))) * 14029467366897019727UL;
                var num24 = (ulong)((((long)num23 << 31) | (long)(num23 >> 33)) * -7046029288634856825L);
                var num25 = num22 ^ num24;
                num22 = (ulong)((((long)num25 << 27) | (long)(num25 >> 37)) * -7046029288634856825L +
                                -8796714831421723037L);
            }

            if (length >= 4)
            {
                ulong num26 =
                    Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref local1, (IntPtr)(buffer.Length - length)));
                var num27 = num22 ^ (num26 * 11400714785074694791UL);
                num22 = (ulong)((((long)num27 << 23) | (long)(num27 >> 41)) * -4417276706812531889L +
                                1609587929392839161L);
                length -= 4;
            }

            for (var byteOffset = 0; byteOffset < length; ++byteOffset)
            {
                ulong num28 =
                    Unsafe.AddByteOffset(ref Unsafe.AddByteOffset(ref local1, (IntPtr)(buffer.Length - length)),
                        (IntPtr)byteOffset);
                var num29 = num22 ^ (num28 * 2870177450012600261UL);
                num22 = (ulong)((((long)num29 << 11) | (long)(num29 >> 53)) * -7046029288634856825L);
            }

            var num30 = (long)num22;
            var num31 = (num30 ^ num30 >>> 33) * -4417276706812531889L;
            var num32 = (num31 ^ num31 >>> 29) * 1609587929392839161L;
            return num32 ^ num32 >>> 32;
        }
    }
}
