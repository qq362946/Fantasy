#if FANTASY_NET || !FANTASY_WEBGL
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes1
    {
        private byte _e0;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes1, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes1>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes2
    {
        private FixedBytes1 _e0;
        private FixedBytes1 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes2, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes2>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes4
    {
        private FixedBytes2 _e0;
        private FixedBytes2 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes4, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes4>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes8
    {
        private FixedBytes4 _e0;
        private FixedBytes4 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes8, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes8>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes16
    {
        private FixedBytes8 _e0;
        private FixedBytes8 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes16, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes16>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes32
    {
        private FixedBytes16 _e0;
        private FixedBytes16 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes32, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes32>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes64
    {
        private FixedBytes32 _e0;
        private FixedBytes32 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes64, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes64>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes128
    {
        private FixedBytes64 _e0;
        private FixedBytes64 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes128, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes128>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes256
    {
        private FixedBytes128 _e0;
        private FixedBytes128 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes256, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes256>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes512
    {
        private FixedBytes256 _e0;
        private FixedBytes256 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes512, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes512>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FixedBytes1024
    {
        private FixedBytes512 _e0;
        private FixedBytes512 _e1;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<FixedBytes1024, byte>(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<FixedBytes1024>());
    }
}
#endif