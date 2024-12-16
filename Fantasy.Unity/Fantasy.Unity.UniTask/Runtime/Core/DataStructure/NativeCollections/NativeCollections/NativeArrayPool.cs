#if UNITY_2021_3_OR_NEWER || GODOT
using System;
using System.Threading;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET5_0_OR_GREATER
#endif

#pragma warning disable CA2208
#pragma warning disable CS8632

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     NativeMemoryPool
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeArrayPool<T> : IDisposable, IEquatable<NativeArrayPool<T>> where T : unmanaged
    {
        /// <summary>
        ///     Buckets
        /// </summary>
        private readonly NativeArrayPoolBucket* _buckets;

        /// <summary>
        ///     Length
        /// </summary>
        private readonly int _length;

        /// <summary>
        ///     Size
        /// </summary>
        private readonly int _size;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="size">Size</param>
        /// <param name="maxLength">Max length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArrayPool(int size, int maxLength)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "MustBePositive");
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "MustBeNonNegative");
            if (maxLength > 1073741824)
                maxLength = 1073741824;
            else if (maxLength < 16)
                maxLength = 16;
            var length = SelectBucketIndex(maxLength) + 1;
            var buckets = (NativeArrayPoolBucket*)NativeMemoryAllocator.Alloc((uint)(length * sizeof(NativeArrayPoolBucket)));
            for (var i = 0; i < length; ++i)
                buckets[i].Initialize(size, 16 << i);
            _buckets = buckets;
            _length = length;
            _size = size;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _buckets != null;

        /// <summary>
        ///     Size
        /// </summary>
        public int Size => _size;

        /// <summary>
        ///     Max length
        /// </summary>
        public int MaxLength => 16 << (_length - 1);

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeArrayPool<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeArrayPool<T> nativeArrayPool && nativeArrayPool == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_buckets;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeArrayPool<{typeof(T).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeArrayPool<T> left, NativeArrayPool<T> right) => left._buckets == right._buckets;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeArrayPool<T> left, NativeArrayPool<T> right) => left._buckets != right._buckets;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_buckets == null)
                return;
            for (var i = 0; i < _length; ++i)
                _buckets[i].Dispose();
            NativeMemoryAllocator.Free(_buckets);
        }

        /// <summary>
        ///     Rent buffer
        /// </summary>
        /// <param name="minimumLength">Minimum buffer length</param>
        /// <returns>Buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> Rent(int minimumLength)
        {
            if (minimumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength), minimumLength, "MustBeNonNegative");
            var index = SelectBucketIndex(minimumLength);
            if (index < _length)
                return _buckets[index].Rent();
            throw new ArgumentOutOfRangeException(nameof(minimumLength), minimumLength, "BiggerThanCollection");
        }

        /// <summary>
        ///     Rent buffer
        /// </summary>
        /// <param name="minimumLength">Minimum buffer length</param>
        /// <param name="array">Buffer</param>
        /// <returns>Rented</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRent(int minimumLength, out NativeArray<T> array)
        {
            if (minimumLength < 0)
            {
                array = default;
                return false;
            }

            var index = SelectBucketIndex(minimumLength);
            if (index < _length)
            {
                array = _buckets[index].Rent();
                return true;
            }

            array = default;
            return false;
        }

        /// <summary>
        ///     Return buffer
        /// </summary>
        /// <param name="array">Buffer</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(in NativeArray<T> array)
        {
            var length = array.Length;
            if (length < 16 || (length & (length - 1)) != 0)
                throw new ArgumentException("BufferNotFromPool", nameof(array));
            var bucket = SelectBucketIndex(length);
            if (bucket >= _length)
                throw new ArgumentException("BufferNotFromPool", nameof(array));
            _buckets[bucket].Return(array.Array);
        }

        /// <summary>
        ///     Try return buffer
        /// </summary>
        /// <param name="array">Buffer</param>
        /// <returns>Returned</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReturn(in NativeArray<T> array)
        {
            var length = array.Length;
            if (length < 16 || (length & (length - 1)) != 0)
                return false;
            var bucket = SelectBucketIndex(length);
            if (bucket >= _length)
                return false;
            _buckets[bucket].Return(array.Array);
            return true;
        }

        /// <summary>
        ///     Return buffer
        /// </summary>
        /// <param name="length">Length</param>
        /// <param name="array">Buffer</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(int length, T* array)
        {
            if (length < 16 || (length & (length - 1)) != 0)
                throw new ArgumentException("BufferNotFromPool", nameof(array));
            var bucket = SelectBucketIndex(length);
            if (bucket >= _length)
                throw new ArgumentException("BufferNotFromPool", nameof(array));
            _buckets[bucket].Return(array);
        }

        /// <summary>
        ///     Try return buffer
        /// </summary>
        /// <param name="length">Length</param>
        /// <param name="array">Buffer</param>
        /// <returns>Returned</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReturn(int length, T* array)
        {
            if (length < 16 || (length & (length - 1)) != 0)
                return false;
            var bucket = SelectBucketIndex(length);
            if (bucket >= _length)
                return false;
            _buckets[bucket].Return(array);
            return true;
        }

        /// <summary>
        ///     Select bucket index
        /// </summary>
        /// <param name="bufferSize">Buffer size</param>
        /// <returns>Bucket index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SelectBucketIndex(int bufferSize) => BitOperationsHelpers.Log2(((uint)bufferSize - 1) | 15) - 3;

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeArrayPool<T> Empty => new();

        /// <summary>
        ///     NativeArrayPool bucket
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeArrayPoolBucket : IDisposable
        {
            /// <summary>
            ///     Size
            /// </summary>
            private int _size;

            /// <summary>
            ///     Length
            /// </summary>
            private int _length;

            /// <summary>
            ///     Buffers
            /// </summary>
            private T** _array;

            /// <summary>
            ///     Index
            /// </summary>
            private int _index;

            /// <summary>
            ///     Memory pool
            /// </summary>
            private NativeMemoryPool _memoryPool;

            /// <summary>
            ///     State lock
            /// </summary>
            private SpinLock _lock;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="size">Size</param>
            /// <param name="length">Length</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Initialize(int size, int length)
            {
                _size = size;
                _length = length;
                _array = (T**)NativeMemoryAllocator.AllocZeroed((uint)(size * sizeof(T*)));
                _index = 0;
                _memoryPool = new NativeMemoryPool(size, length * sizeof(T), 0);
                _lock = new SpinLock();
            }

            /// <summary>
            ///     Dispose
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                NativeMemoryAllocator.Free(_array);
                _memoryPool.Dispose();
            }

            /// <summary>
            ///     Rent buffer
            /// </summary>
            /// <returns>Buffer</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public NativeArray<T> Rent()
            {
                T* ptr = null;
                var lockTaken = false;
                try
                {
                    _lock.Enter(ref lockTaken);
                    if (_index < _size)
                    {
                        ptr = _array[_index];
                        _array[_index++] = null;
                    }

                    if (ptr == null)
                        ptr = (T*)_memoryPool.Rent();
                }
                finally
                {
                    if (lockTaken)
                        _lock.Exit(false);
                }

                return new NativeArray<T>(ptr, _length);
            }

            /// <summary>
            ///     Return buffer
            /// </summary>
            /// <param name="ptr">Pointer</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Return(T* ptr)
            {
                var lockTaken = false;
                try
                {
                    _lock.Enter(ref lockTaken);
                    if (_index != 0)
                        _array[--_index] = ptr;
                    else
                        _memoryPool.Return(ptr);
                }
                finally
                {
                    if (lockTaken)
                        _lock.Exit(false);
                }
            }
        }
    }
}