#if FANTASY_NET
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using mimalloc;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#endif

namespace Fantasy.LowLevel
{
    public static unsafe class FantasyMemory
    {
        private static delegate* managed<nuint, void*> _alloc;
        private static delegate* managed<nuint, void*> _allocZeroed;
        private static delegate* managed<void*, void> _free;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize()
        {
            // KCP 使用 FantasyMemory
            kcp.KCP.ikcp_allocator(&Alloc, &Free);

            try
            {
                _ = MiMalloc.mi_version();
            }
            catch
            {
                Log.Info("mimalloc的二进制文件丢失");
                return;
            }

            try
            {
                var ptr = MiMalloc.mi_malloc(MiMalloc.MI_SMALL_SIZE_MAX);
                MiMalloc.mi_free(ptr);
            }
            catch
            {
                Log.Info("mimalloc权限不足,\r\n可能的问题:\r\n1. 禁止了虚拟内存分配 -> 允许虚拟内存分配;\r\n2. 没有硬盘读写权限 -> 管理员获取所有权限");
                return;
            }

            Custom(&MiAlloc, &MiAllocZeroed, &MiFree);

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void* MiAlloc(nuint size) => MiMalloc.mi_malloc(size);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void* MiAllocZeroed(nuint size)
            {
                var ptr = MiAlloc(size);
                Unsafe.InitBlockUnaligned(ptr, 0, (uint)size);
                return ptr;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void MiFree(void* ptr) => MiMalloc.mi_free(ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Custom(delegate*<nuint, void*> alloc, delegate*<nuint, void*> allocZeroed, delegate*<void*, void> free)
        {
            _alloc = alloc;
            _allocZeroed = allocZeroed;
            _free = free;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint Align(nuint size) => AlignUp(size, (nuint)sizeof(nint));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint AlignUp(nuint size, nuint alignment) => (size + (alignment - 1)) & ~(alignment - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint AlignDown(nuint size, nuint alignment) => size - (size & (alignment - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Alloc(nuint byteCount)
        {
            if (_alloc != null)
                return _alloc(byteCount);
#if NET6_0_OR_GREATER
            return NativeMemory.Alloc(byteCount);
#else
            return (void*)Marshal.AllocHGlobal((nint)byteCount);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocZeroed(nuint byteCount)
        {
            if (_allocZeroed != null)
                return _allocZeroed(byteCount);
            void* ptr;
            if (_alloc != null)
            {
                ptr = _alloc(byteCount);
                Unsafe.InitBlockUnaligned(ptr, 0, (uint)byteCount);
                return ptr;
            }
#if NET6_0_OR_GREATER
            return NativeMemory.AllocZeroed(byteCount, 1);
#else
            ptr = (void*)Marshal.AllocHGlobal((nint)byteCount);
            Unsafe.InitBlockUnaligned(ptr, 0, (uint)byteCount);
            return ptr;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* ptr)
        {
            if (_free != null)
            {
                _free(ptr);
                return;
            }
#if NET6_0_OR_GREATER
            NativeMemory.Free(ptr);
#else
            Marshal.FreeHGlobal((nint)ptr);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(void* destination, void* source, nuint byteCount) => Unsafe.CopyBlockUnaligned(destination, source, (uint)byteCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Move(void* destination, void* source, nuint byteCount) => Buffer.MemoryCopy(source, destination, byteCount, byteCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(void* startAddress, byte value, nuint byteCount) => Unsafe.InitBlockUnaligned(startAddress, value, (uint)byteCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Compare(void* left, void* right, nuint byteCount)
        {
            ref var first = ref *(byte*)left;
            ref var second = ref *(byte*)right;
            if (byteCount >= (nuint)sizeof(nuint))
            {
                if (!Unsafe.AreSame(ref first, ref second))
                {
#if NET7_0_OR_GREATER
                    if (Vector128.IsHardwareAccelerated)
                    {
#if NET8_0_OR_GREATER
                        if (Vector512.IsHardwareAccelerated && byteCount >= (nuint)Vector512<byte>.Count)
                        {
                            nuint offset = 0;
                            var lengthToExamine = byteCount - (nuint)Vector512<byte>.Count;
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
                        if (Vector256.IsHardwareAccelerated && byteCount >= (nuint)Vector256<byte>.Count)
                        {
                            nuint offset = 0;
                            var lengthToExamine = byteCount - (nuint)Vector256<byte>.Count;
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
                        if (byteCount >= (nuint)Vector128<byte>.Count)
                        {
                            nuint offset = 0;
                            var lengthToExamine = byteCount - (nuint)Vector128<byte>.Count;
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
                    if (sizeof(nint) == 8 && Vector128.IsHardwareAccelerated)
                    {
                        var offset = byteCount - (nuint)sizeof(nuint);
                        var differentBits = Unsafe.ReadUnaligned<nuint>(ref first) - Unsafe.ReadUnaligned<nuint>(ref second);
                        differentBits |= Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref first, offset)) - Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref second, offset));
                        return differentBits == 0;
                    }
                    else
#endif
                    {
                        nuint offset = 0;
                        var lengthToExamine = byteCount - (nuint)sizeof(nuint);
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

            if (byteCount < sizeof(uint) || sizeof(nint) != 8)
            {
                uint differentBits = 0;
                var offset = byteCount & 2;
                if (offset != 0)
                {
                    differentBits = Unsafe.ReadUnaligned<ushort>(ref first);
                    differentBits -= Unsafe.ReadUnaligned<ushort>(ref second);
                }

                if ((byteCount & 1) != 0)
#if NET7_0_OR_GREATER
                    differentBits |= Unsafe.AddByteOffset(ref first, offset) - (uint)Unsafe.AddByteOffset(ref second, offset);
#else
                    differentBits |= Unsafe.AddByteOffset(ref first, (nint)offset) - (uint)Unsafe.AddByteOffset(ref second, (nint)offset);
#endif
                return differentBits == 0;
            }
            else
            {
                var offset = byteCount - sizeof(uint);
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
#endif
