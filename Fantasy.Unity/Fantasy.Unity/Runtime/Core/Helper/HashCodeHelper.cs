using System.Security.Cryptography;
using System.Text;
// ReSharper disable InconsistentNaming

namespace Fantasy.Helper
{
    /// <summary>
    /// HashCode算法帮助类
    /// </summary>
    public static partial class HashCodeHelper
    {
        private static readonly SHA256 Sha256Hash = SHA256.Create();
        
        /// <summary>
        /// 使用bkdr算法生成一个long的值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static unsafe long GetBKDRHashCode(string str)
        {
            ulong hash = 0;
            // 如果要修改这个种子、建议选择一个质数来做种子
            const uint seed = 13131; // 31 131 1313 13131 131313 etc..
            fixed (char* p = str)
            {
                for (var i = 0; i < str.Length; i++)
                {
                    var c = p[i];
                    var high = (byte)(c >> 8);
                    var low = (byte)(c & byte.MaxValue);
                    hash = hash * seed + high;
                    hash = hash * seed + low;
                }
            }
            return (long)hash;
        }

        /// <summary>
        /// 使用MurmurHash3算法生成一个uint的值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static unsafe uint MurmurHash3(string str)
        {
            const uint seed = 0xc58f1a7b;
            uint hash = seed;
            uint c1 = 0xcc9e2d51;
            uint c2 = 0x1b873593;

            fixed (char* p = str)
            {
                var current = p;

                for (var i = 0; i < str.Length; i++)
                {
                    var k1 = (uint)(*current);
                    k1 *= c1;
                    k1 = (k1 << 15) | (k1 >> (32 - 15));
                    k1 *= c2;

                    hash ^= k1;
                    hash = (hash << 13) | (hash >> (32 - 13));
                    hash = hash * 5 + 0xe6546b64;

                    current++;
                }
            }

            hash ^= (uint)str.Length;
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;
            return hash;
        }
        
        /// <summary>
        /// 使用MurmurHash3算法生成一个long的值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static unsafe long ComputeHash64(string str)
        {
            const ulong seed = 0xc58f1a7bc58f1a7bUL; // 64-bit seed
            var hash = seed;
            var c1 = 0x87c37b91114253d5UL;
            var c2 = 0x4cf5ad432745937fUL;

            fixed (char* p = str)
            {
                var current = p;

                for (var i = 0; i < str.Length; i++)
                {
                    var k1 = (ulong)(*current);
                    k1 *= c1;
                    k1 = (k1 << 31) | (k1 >> (64 - 31));
                    k1 *= c2;

                    hash ^= k1;
                    hash = (hash << 27) | (hash >> (64 - 27));
                    hash = hash * 5 + 0x52dce729;

                    current++;
                }
            }

            hash ^= (ulong)str.Length;
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccdUL;
            hash ^= hash >> 33;
            hash *= 0xc4ceb9fe1a85ec53UL;
            hash ^= hash >> 33;
            return (long)hash;
        }
        
        /// <summary>
        /// 根据字符串计算一个Hash值
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static int ComputeSha256HashAsInt(string rawData)
        {
            var bytes = Sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        }
    }
}