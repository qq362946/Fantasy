using System;
using System.Runtime.CompilerServices;

namespace DotRecast.Core
{
    public static class RcSpans
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ReadOnlySpan<T> src, Span<T> dst)
        {
            src.CopyTo(dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ReadOnlySpan<T> src, int srcIdx, Span<T> dst, int dstIdx, int length)
        {
            var slicedSrc = src.Slice(srcIdx, length);
            var slicedDst = dst.Slice(dstIdx);
            slicedSrc.CopyTo(slicedDst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Move<T>(Span<T> src, int srcIdx, int dstIdx, int length)
        {
            var slicedSrc = src.Slice(srcIdx, length);
            var slicedDst = src.Slice(dstIdx, length);
            slicedSrc.CopyTo(slicedDst);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(Span<T> span, T value, int start, int count)
        {
            span.Slice(start, count).Fill(value);
        }
    }
}