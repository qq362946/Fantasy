using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace DotRecast.Core.Collections
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RcFixedArray128<T> where T : unmanaged
    {
        public const int Size = 128;

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
        private T _v0064;
        private T _v0065;
        private T _v0066;
        private T _v0067;
        private T _v0068;
        private T _v0069;
        private T _v0070;
        private T _v0071;
        private T _v0072;
        private T _v0073;
        private T _v0074;
        private T _v0075;
        private T _v0076;
        private T _v0077;
        private T _v0078;
        private T _v0079;
        private T _v0080;
        private T _v0081;
        private T _v0082;
        private T _v0083;
        private T _v0084;
        private T _v0085;
        private T _v0086;
        private T _v0087;
        private T _v0088;
        private T _v0089;
        private T _v0090;
        private T _v0091;
        private T _v0092;
        private T _v0093;
        private T _v0094;
        private T _v0095;
        private T _v0096;
        private T _v0097;
        private T _v0098;
        private T _v0099;
        private T _v0100;
        private T _v0101;
        private T _v0102;
        private T _v0103;
        private T _v0104;
        private T _v0105;
        private T _v0106;
        private T _v0107;
        private T _v0108;
        private T _v0109;
        private T _v0110;
        private T _v0111;
        private T _v0112;
        private T _v0113;
        private T _v0114;
        private T _v0115;
        private T _v0116;
        private T _v0117;
        private T _v0118;
        private T _v0119;
        private T _v0120;
        private T _v0121;
        private T _v0122;
        private T _v0123;
        private T _v0124;
        private T _v0125;
        private T _v0126;
        private T _v0127;

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