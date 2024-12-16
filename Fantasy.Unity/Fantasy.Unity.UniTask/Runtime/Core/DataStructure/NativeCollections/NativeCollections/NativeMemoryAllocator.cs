#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     Native memory allocator
    /// </summary>
    public static unsafe class NativeMemoryAllocator
    {
        /// <summary>
        ///     Alloc
        /// </summary>
        /// <param name="byteCount">Byte count</param>
        /// <returns>Memory</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Alloc(uint byteCount)
        {
#if NET6_0_OR_GREATER
            return NativeMemory.Alloc(byteCount);
#else
            return (void*)Marshal.AllocHGlobal((nint)byteCount);
#endif
        }

        /// <summary>
        ///     Alloc zeroed
        /// </summary>
        /// <param name="byteCount">Byte count</param>
        /// <returns>Memory</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocZeroed(uint byteCount)
        {
#if NET6_0_OR_GREATER
            return NativeMemory.AllocZeroed(byteCount, 1);
#else
            var ptr = (void*)Marshal.AllocHGlobal((nint)byteCount);
            Unsafe.InitBlockUnaligned(ptr, 0, byteCount);
            return ptr;
#endif
        }

        /// <summary>
        ///     Free
        /// </summary>
        /// <param name="ptr">Pointer</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* ptr)
        {
#if NET6_0_OR_GREATER
            NativeMemory.Free(ptr);
#else
            Marshal.FreeHGlobal((nint)ptr);
#endif
        }

        /// <summary>
        ///     Copy
        /// </summary>
        /// <param name="destination">Destination</param>
        /// <param name="source">Source</param>
        /// <param name="byteCount">Byte count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(void* destination, void* source, uint byteCount) => Unsafe.CopyBlockUnaligned(destination, source, byteCount);

        /// <summary>
        ///     Move
        /// </summary>
        /// <param name="destination">Destination</param>
        /// <param name="source">Source</param>
        /// <param name="byteCount">Byte count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Move(void* destination, void* source, uint byteCount) => Buffer.MemoryCopy(source, destination, byteCount, byteCount);

        /// <summary>
        ///     Set
        /// </summary>
        /// <param name="startAddress">Start address</param>
        /// <param name="value">Value</param>
        /// <param name="byteCount">Byte count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(void* startAddress, byte value, uint byteCount) => Unsafe.InitBlockUnaligned(startAddress, value, byteCount);

        /// <summary>
        ///     Compare
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <param name="byteCount">Byte count</param>
        /// <returns>Sequences equal</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Compare(void* left, void* right, uint byteCount)
        {
            ref var first = ref *(byte*)left;
            ref var second = ref *(byte*)right;
            nuint length = byteCount;
            if (length >= (nuint)sizeof(nuint))
            {
                if (!Unsafe.AreSame(ref first, ref second))
                {
#if NET7_0_OR_GREATER
                    if (Vector128.IsHardwareAccelerated)
                    {
#if NET8_0_OR_GREATER
                        if (Vector512.IsHardwareAccelerated && length >= (nuint)Vector512<byte>.Count)
                        {
                            nuint offset = 0;
                            var lengthToExamine = length - (nuint)Vector512<byte>.Count;
                            if (lengthToExamine != 0)
                            {
                                do
                                {
                                    if (Vector512.LoadUnsafe(ref first, offset) != Vector512.LoadUnsafe(ref second, offset))
                                        return false;
                                    offset += (nuint)Vector512<byte>.Count;
                                } while (lengthToExamine > offset);
                            }

                            return Vector512.LoadUnsafe(ref first, lengthToExamine) == Vector512.LoadUnsafe(ref second, lengthToExamine);
                        }
#endif
                        if (Vector256.IsHardwareAccelerated && length >= (nuint)Vector256<byte>.Count)
                        {
                            nuint offset = 0;
                            var lengthToExamine = length - (nuint)Vector256<byte>.Count;
                            if (lengthToExamine != 0)
                            {
                                do
                                {
                                    if (Vector256.LoadUnsafe(ref first, offset) != Vector256.LoadUnsafe(ref second, offset))
                                        return false;
                                    offset += (nuint)Vector256<byte>.Count;
                                } while (lengthToExamine > offset);
                            }

                            return Vector256.LoadUnsafe(ref first, lengthToExamine) == Vector256.LoadUnsafe(ref second, lengthToExamine);
                        }

                        if (length >= (nuint)Vector128<byte>.Count)
                        {
                            nuint offset = 0;
                            var lengthToExamine = length - (nuint)Vector128<byte>.Count;
                            if (lengthToExamine != 0)
                            {
                                do
                                {
                                    if (Vector128.LoadUnsafe(ref first, offset) != Vector128.LoadUnsafe(ref second, offset))
                                        return false;
                                    offset += (nuint)Vector128<byte>.Count;
                                } while (lengthToExamine > offset);
                            }

                            return Vector128.LoadUnsafe(ref first, lengthToExamine) == Vector128.LoadUnsafe(ref second, lengthToExamine);
                        }
                    }

                    if (IntPtr.Size == 8 && Vector128.IsHardwareAccelerated)
                    {
                        var offset = length - (nuint)sizeof(nuint);
                        var differentBits = Unsafe.ReadUnaligned<nuint>(ref first) - Unsafe.ReadUnaligned<nuint>(ref second);
                        differentBits |= Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref first, offset)) - Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref second, offset));
                        return differentBits == 0;
                    }
                    else
#endif
                    {
                        nuint offset = 0;
                        var lengthToExamine = length - (nuint)sizeof(nuint);
                        if (lengthToExamine > 0)
                        {
                            do
                            {
#if NET7_0_OR_GREATER
                                if (Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref first, offset)) != Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref second, offset)))
#else
                                if (Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref first, (nint)offset)) != Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref second, (nint)offset)))
#endif
                                    return false;
                                offset += (nuint)sizeof(nuint);
                            } while (lengthToExamine > offset);
                        }
#if NET7_0_OR_GREATER
                        return Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref first, lengthToExamine)) == Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref second, lengthToExamine));
#else
                        return Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref first, (nint)lengthToExamine)) == Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref second, (nint)lengthToExamine));
#endif
                    }
                }

                return true;
            }

            if (length < sizeof(uint) || IntPtr.Size != 8)
            {
                uint differentBits = 0;
                var offset = length & 2;
                if (offset != 0)
                {
                    differentBits = Unsafe.ReadUnaligned<ushort>(ref first);
                    differentBits -= Unsafe.ReadUnaligned<ushort>(ref second);
                }

                if ((length & 1) != 0)
#if NET7_0_OR_GREATER
                    differentBits |= Unsafe.AddByteOffset(ref first, offset) - (uint)Unsafe.AddByteOffset(ref second, offset);
#else
                    differentBits |= Unsafe.AddByteOffset(ref first, (nint)offset) - (uint)Unsafe.AddByteOffset(ref second, (nint)offset);
#endif
                return differentBits == 0;
            }
            else
            {
                var offset = length - sizeof(uint);
                var differentBits = Unsafe.ReadUnaligned<uint>(ref first) - Unsafe.ReadUnaligned<uint>(ref second);
#if NET7_0_OR_GREATER
                differentBits |= Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref first, offset)) - Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref second, offset));
#else
                differentBits |= Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref first, (nint)offset)) - Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref second, (nint)offset));
#endif
                return differentBits == 0;
            }
        }
    }
}