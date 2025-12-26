using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace DotRecast.Core.Collections
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RcFixedArray64<T> where T : unmanaged
    {
        public const int Size = 64;

        private T _v0000;
        private T _v0001;
        private T _v0002;
        private T _v0003;
        private T _v0004;
        private T _v0005;
        private T _v0006;
        private T _v0007;
        private T _v0008;
        private T _v0009;
        private T _v0010;
        private T _v0011;
        private T _v0012;
        private T _v0013;
        private T _v0014;
        private T _v0015;
        private T _v0016;
        private T _v0017;
        private T _v0018;
        private T _v0019;
        private T _v0020;
        private T _v0021;
        private T _v0022;
        private T _v0023;
        private T _v0024;
        private T _v0025;
        private T _v0026;
        private T _v0027;
        private T _v0028;
        private T _v0029;
        private T _v0030;
        private T _v0031;
        private T _v0032;
        private T _v0033;
        private T _v0034;
        private T _v0035;
        private T _v0036;
        private T _v0037;
        private T _v0038;
        private T _v0039;
        private T _v0040;
        private T _v0041;
        private T _v0042;
        private T _v0043;
        private T _v0044;
        private T _v0045;
        private T _v0046;
        private T _v0047;
        private T _v0048;
        private T _v0049;
        private T _v0050;
        private T _v0051;
        private T _v0052;
        private T _v0053;
        private T _v0054;
        private T _v0055;
        private T _v0056;
        private T _v0057;
        private T _v0058;
        private T _v0059;
        private T _v0060;
        private T _v0061;
        private T _v0062;
        private T _v0063;

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