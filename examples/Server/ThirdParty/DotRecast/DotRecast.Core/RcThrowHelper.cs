using System;
using System.Runtime.CompilerServices;
using DotRecast.Core.Collections;

namespace DotRecast.Core
{
    public static class RcThrowHelper
    {
        public static void ThrowExceptionIfIndexOutOfRange(int index, int size)
        {
            if (0 > index || index >= size)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range - size({size})");
            }
        }

        public static void ThrowArgumentOutOfRangeException(string argument)
        {
            throw new ArgumentOutOfRangeException(argument);
        }

        public static void ThrowNullReferenceException(string argument)
        {
            throw new NullReferenceException(argument);
        }
        
        public static void ThrowException(string message)
        {
            throw new Exception(message);
        }

        public static void StackOverflow()
        {
            var array_128_512_1 = new RcFixedArray128<RcFixedArray512<float>>(); // 128 * 512 = 65536
            var array_128_512_2 = new RcFixedArray128<RcFixedArray512<float>>(); // 128 * 512 = 65536

            var array_32_512_1 = new RcFixedArray32<RcFixedArray512<float>>(); // 32 * 512 = 16384

            var array_16_512_1 = new RcFixedArray16<RcFixedArray512<float>>(); // 16 * 512 = 8192

            var array_8_512_1 = new RcFixedArray8<RcFixedArray512<float>>(); // 8 * 512 = 4196
            var array_4_256_1 = new RcFixedArray4<RcFixedArray256<float>>(); // 4 * 256 = 1024
            var array_4_64_1 = new RcFixedArray4<RcFixedArray64<float>>(); // 4 * 64 = 256

            //
            var array_2_8_1 = new RcFixedArray2<RcFixedArray8<float>>(); // 2 * 8 = 16
            var array_2_4_1 = new RcFixedArray2<RcFixedArray2<float>>(); // 2 * 2 = 4

            float f1 = 0.0f; // 1
            //float f2 = 0.0f; // my system stack overflow!
            
            RcDebug.UnusedRef(ref array_128_512_1);
            RcDebug.UnusedRef(ref array_128_512_2);
            RcDebug.UnusedRef(ref array_32_512_1);
            RcDebug.UnusedRef(ref array_16_512_1);
            RcDebug.UnusedRef(ref array_8_512_1);
            RcDebug.UnusedRef(ref array_4_256_1);
            RcDebug.UnusedRef(ref array_4_64_1);
            RcDebug.UnusedRef(ref array_2_8_1);
            RcDebug.UnusedRef(ref array_2_4_1);
            RcDebug.UnusedRef(ref f1);
        }

    }
}