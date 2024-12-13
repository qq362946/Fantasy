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
    ///     Native memory array
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeMemoryArray<T> : IDisposable, IEquatable<NativeMemoryArray<T>> where T : unmanaged
    {
        /// <summary>
        ///     Array
        /// </summary>
        private readonly T* _array;

        /// <summary>
        ///     Length
        /// </summary>
        private readonly int _length;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMemoryArray(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _array = (T*)NativeMemoryAllocator.Alloc((uint)(length * sizeof(T)));
            _length = length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="length">Length</param>
        /// <param name="zeroed">Zeroed</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMemoryArray(int length, bool zeroed)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _array = zeroed ? (T*)NativeMemoryAllocator.AllocZeroed((uint)(length * sizeof(T))) : (T*)NativeMemoryAllocator.Alloc((uint)(length * sizeof(T)));
            _length = length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMemoryArray(T* array, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _array = array;
            _length = length;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _array != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => _length == 0;

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public T* this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array + index;
        }

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public T* this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array + index;
        }

        /// <summary>
        ///     Array
        /// </summary>
        public T* Array => _array;

        /// <summary>
        ///     Length
        /// </summary>
        public int Length => _length;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeMemoryArray<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeMemoryArray<T> nativeMemoryArray && nativeMemoryArray == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_array;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeMemoryArray<{typeof(T).Name}>[{_length}]";

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(NativeMemoryArray<T> nativeMemoryArray) => nativeMemoryArray.AsSpan();

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(NativeMemoryArray<T> nativeMemoryArray) => nativeMemoryArray.AsReadOnlySpan();

        /// <summary>
        ///     As native array
        /// </summary>
        /// <param name="nativeMemoryArray">Native memory array</param>
        /// <returns>NativeArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArray<T>(NativeMemoryArray<T> nativeMemoryArray) => new(nativeMemoryArray._array, nativeMemoryArray._length);

        /// <summary>
        ///     As native memory array
        /// </summary>
        /// <param name="nativeArray">Native array</param>
        /// <returns>NativeMemoryArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeMemoryArray<T>(NativeArray<T> nativeArray) => new(nativeArray.Array, nativeArray.Length);

        /// <summary>
        ///     As native array segment
        /// </summary>
        /// <param name="nativeMemoryArray">Native memory array</param>
        /// <returns>NativeArraySegment</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArraySegment<T>(NativeMemoryArray<T> nativeMemoryArray) => new(nativeMemoryArray._array, nativeMemoryArray._length);

        /// <summary>
        ///     As native memory array
        /// </summary>
        /// <param name="nativeArraySegment">Native array segment</param>
        /// <returns>NativeMemoryArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeMemoryArray<T>(NativeArraySegment<T> nativeArraySegment) => new(nativeArraySegment.Array, nativeArraySegment.Offset + nativeArraySegment.Count);

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeMemoryArray<T> left, NativeMemoryArray<T> right) => left._length == right._length && left._array == right._array;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeMemoryArray<T> left, NativeMemoryArray<T> right) => left._length != right._length || left._array != right._array;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_array == null)
                return;
            NativeMemoryAllocator.Free(_array);
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Unsafe.InitBlockUnaligned(_array, 0, (uint)(_length * sizeof(T)));

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref *_array, _length);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int length) => MemoryMarshal.CreateSpan(ref *_array, length);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="length">Length</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int length) => MemoryMarshal.CreateSpan(ref *(_array + start), length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref *_array, _length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan(int length) => MemoryMarshal.CreateReadOnlySpan(ref *_array, length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="length">Length</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan(int start, int length) => MemoryMarshal.CreateReadOnlySpan(ref *(_array + start), length);

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeMemoryArray<T> Empty => new();

        /// <summary>
        ///     Get enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        ///     Enumerator
        /// </summary>
        public ref struct Enumerator
        {
            /// <summary>
            ///     NativeMemoryArray
            /// </summary>
            private readonly NativeMemoryArray<T> _nativeMemoryArray;

            /// <summary>
            ///     Index
            /// </summary>
            private int _index;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeMemoryArray">NativeMemoryArray</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(NativeMemoryArray<T> nativeMemoryArray)
            {
                _nativeMemoryArray = nativeMemoryArray;
                _index = -1;
            }

            /// <summary>
            ///     Move next
            /// </summary>
            /// <returns>Moved</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                var index = _index + 1;
                if (index < _nativeMemoryArray._length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            /// <summary>
            ///     Current
            /// </summary>
            public T* Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _nativeMemoryArray[_index];
            }
        }
    }
}