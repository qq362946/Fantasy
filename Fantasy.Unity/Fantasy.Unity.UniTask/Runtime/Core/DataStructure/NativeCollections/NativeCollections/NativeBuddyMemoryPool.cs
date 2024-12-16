using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif

#pragma warning disable CA2208
#pragma warning disable CS8632

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     Native buddy memory pool
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeBuddyMemoryPool : IDisposable, IEquatable<NativeBuddyMemoryPool>
    {
        /// <summary>
        ///     Min block size
        /// </summary>
        private readonly int _minBlockSize;

        /// <summary>
        ///     Max block size
        /// </summary>
        private readonly int _maxBlockSize;

        /// <summary>
        ///     Bit map
        /// </summary>
        private readonly int* _bitmap;

        /// <summary>
        ///     Memory
        /// </summary>
        private readonly byte* _memory;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="minBlockSize">Min block size</param>
        /// <param name="maxBlockSize">Max block size</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBuddyMemoryPool(int minBlockSize, int maxBlockSize)
        {
            if (minBlockSize > maxBlockSize)
                throw new ArgumentException($"{minBlockSize} cannot be greater than {maxBlockSize}.");
            if (minBlockSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(minBlockSize), minBlockSize, "MustBePositive");
            if ((minBlockSize & (minBlockSize - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(minBlockSize), minBlockSize, "MustBePowOf2");
            if ((maxBlockSize & (maxBlockSize - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(maxBlockSize), maxBlockSize, "MustBePowOf2");
            _minBlockSize = minBlockSize;
            _maxBlockSize = maxBlockSize;
            var bitmapSize = ((1 << (BitOperationsHelpers.Log2(maxBlockSize / minBlockSize) + 1)) + 31) / 32 * sizeof(int);
            var array = (byte*)NativeMemoryAllocator.Alloc((uint)(bitmapSize + maxBlockSize));
            _bitmap = (int*)array;
            _memory = array + bitmapSize;
            Unsafe.InitBlockUnaligned(array, 0, (uint)bitmapSize);
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _bitmap != null;

        /// <summary>
        ///     Min block size
        /// </summary>
        public int MinBlockSize => _minBlockSize;

        /// <summary>
        ///     Max block size
        /// </summary>
        public int MaxBlockSize => _maxBlockSize;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeBuddyMemoryPool other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeBuddyMemoryPool nativeBuddyMemoryPool && nativeBuddyMemoryPool == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_bitmap;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => "NativeBuddyMemoryPool";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeBuddyMemoryPool left, NativeBuddyMemoryPool right) => left._bitmap == right._bitmap;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeBuddyMemoryPool left, NativeBuddyMemoryPool right) => left._bitmap != right._bitmap;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_bitmap == null)
                return;
            NativeMemoryAllocator.Free(_bitmap);
        }

        /// <summary>
        ///     Get layer
        /// </summary>
        /// <param name="size">Size</param>
        /// <returns>Layer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetLayer(int size) => size <= _minBlockSize ? 0 : BitOperationsHelpers.Log2((uint)((size - 1) / _minBlockSize)) + 1;

        /// <summary>
        ///     Find free block
        /// </summary>
        /// <param name="layer">Layer</param>
        /// <returns>Free block</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindFreeBlock(int layer)
        {
            var blocksInLayer = 1 << layer;
            var offset = blocksInLayer - 1;
            for (var i = 0; i < blocksInLayer; ++i)
            {
                var index = offset + i;
                if ((_bitmap[index / 32] & (1 << (index % 32))) == 0)
                    return index;
            }

            return -1;
        }

        /// <summary>
        ///     Merge blocks
        /// </summary>
        /// <param name="layer">Layer</param>
        /// <param name="index">Index</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MergeBlocks(int layer, int index)
        {
            while (layer != 0)
            {
                var buddyIndex = index % 2 == 0 ? index + 1 : index - 1;
                var bitMask = buddyIndex % 32;
                ref var segment = ref _bitmap[buddyIndex / 32];
                if ((segment & (1 << bitMask)) != 0)
                    break;
                var parentIndex = index / 2 + ((1 << (layer - 1)) - 1);
                (*(_bitmap + index / 32)) &= ~(1 << (index % 32));
                segment &= ~(1 << bitMask);
                (*(_bitmap + parentIndex / 32)) |= 1 << (parentIndex % 32);
                --layer;
                index = parentIndex;
            }
        }

        /// <summary>
        ///     Rent buffer
        /// </summary>
        /// <returns>Buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* Rent(int size)
        {
            if (size > _maxBlockSize)
                throw new ArgumentOutOfRangeException(nameof(size), $"{size} cannot be greater than {_maxBlockSize}.");
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "MustBePositive");
            var layer = GetLayer(size);
            var blockIndex = FindFreeBlock(layer);
            if (blockIndex == -1)
                return null;
            _bitmap[blockIndex / 32] |= 1 << (blockIndex % 32);
            return _memory + (_minBlockSize << layer) * (blockIndex - ((1 << layer) - 1));
        }

        /// <summary>
        ///     Return buffer
        /// </summary>
        /// <param name="ptr">Pointer</param>
        /// <param name="size">Size</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(void* ptr, int size)
        {
            if (size > _maxBlockSize)
                throw new ArgumentOutOfRangeException(nameof(size), $"{size} cannot be greater than {_maxBlockSize}.");
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "MustBePositive");
            var layer = GetLayer(size);
            var blockIndex = (int)((byte*)ptr - _memory) / (_minBlockSize << layer) + ((1 << layer) - 1);
            _bitmap[blockIndex / 32] &= ~(1 << (blockIndex % 32));
            MergeBlocks(layer, blockIndex);
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeBuddyMemoryPool Empty => new();
    }
}