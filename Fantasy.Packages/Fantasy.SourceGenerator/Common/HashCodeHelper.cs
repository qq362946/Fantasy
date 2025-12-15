using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fantasy.SourceGenerator.Common
{
    /// <summary>
    /// HashCode 算法帮助类 (Source Generator 版本)
    /// 用于在编译时计算类型哈希值
    /// 注意：此实现必须与 Fantasy.Helper.HashCodeHelper.ComputeHash64 保持完全一致
    /// </summary>
    internal static class HashCodeHelper
    {
        /// <summary>
        /// 使用MurmurHash3算法生成一个long的值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ComputeHash64(string str) => ComputeHash64(MemoryMarshal.AsBytes(str.AsSpan()));

        /// <summary>
        /// 使用MurmurHash3算法生成一个long的值
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ComputeHash64(byte[] data) => ComputeHash64(data.AsSpan());

        /// <summary>
        /// 使用MurmurHash3算法生成一个long的值
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ComputeHash64(byte[] data, int index, int length) => ComputeHash64(data.AsSpan(index, length));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ComputeHash64(ReadOnlySpan<byte> data)
        {
            const ulong seed = 0xc58f1a7bc58f1a7bUL; // 64-bit seed
            var hash = seed;
            var c1 = 0x87c37b91114253d5UL;
            var c2 = 0x4cf5ad432745937fUL;

            ref var local = ref MemoryMarshal.GetReference(data);

            for (var i = 0; i < data.Length; i++)
            {
                var k1 = (ulong)Unsafe.Add(ref local, i);
                k1 *= c1;
                k1 = (k1 << 31) | (k1 >> (64 - 31));
                k1 *= c2;

                hash ^= k1;
                hash = (hash << 27) | (hash >> (64 - 27));
                hash = hash * 5 + 0x52dce729;
            }

            hash ^= (ulong)data.Length;
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccdUL;
            hash ^= hash >> 33;
            hash *= 0xc4ceb9fe1a85ec53UL;
            hash ^= hash >> 33;
            return (long)hash;
        }
    }
}
