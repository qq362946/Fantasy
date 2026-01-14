using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace DotRecast.Core.Collections
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RcFixedArray4<T> where T : unmanaged
    {
        public const int Size = 4;

        private T _v0000;
        private T _v0001;
        private T _v0002;
        private T _v0003;

        public int Length => Size;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref AsSpan()[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ReadOnlySpan<T> source, int length)
        {
            source.Slice(0, length).CopyTo(AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _v0000, Size);
        }
    }
}