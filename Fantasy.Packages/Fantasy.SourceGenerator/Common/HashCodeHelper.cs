using System;

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
        /// 使用 MurmurHash3 算法生成一个 long 的值
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns>64位哈希值</returns>
        public static long ComputeHash64(string str)
        {
            const ulong seed = 0xc58f1a7bc58f1a7bUL; // 64-bit seed
            var hash = seed;
            var c1 = 0x87c37b91114253d5UL;
            var c2 = 0x4cf5ad432745937fUL;

            for (var i = 0; i < str.Length; i++)
            {
                var k1 = (ulong)str[i];
                k1 *= c1;
                k1 = (k1 << 31) | (k1 >> (64 - 31));
                k1 *= c2;

                hash ^= k1;
                hash = (hash << 27) | (hash >> (64 - 27));
                hash = hash * 5 + 0x52dce729;
            }

            hash ^= (ulong)str.Length;
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccdUL;
            hash ^= hash >> 33;
            hash *= 0xc4ceb9fe1a85ec53UL;
            hash ^= hash >> 33;
            return (long)hash;
        }
    }
}
