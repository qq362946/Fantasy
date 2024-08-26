using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif

#pragma warning disable CA2208
#pragma warning disable CS8632

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberHidesStaticFromOuterClass

namespace NativeCollections
{
    /// <summary>
    ///     Native array segment
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeArraySegment<T> : IDisposable, IEquatable<NativeArraySegment<T>> where T : unmanaged
    {
        /// <summary>
        ///     Array
        /// </summary>
        private readonly T* _array;

        /// <summary>
        ///     Offset
        /// </summary>
        private readonly int _offset;

        /// <summary>
        ///     Count
        /// </summary>
        private readonly int _count;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="count">Count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArraySegment(T* array, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "MustBeNonNegative");
            _array = array;
            _offset = 0;
            _count = count;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArraySegment(T* array, int offset, int count)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "MustBeNonNegative");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "MustBeNonNegative");
            _array = array;
            _offset = offset;
            _count = count;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArraySegment(NativeArray<T> array)
        {
            _array = array.Array;
            _offset = 0;
            _count = array.Length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArraySegment(NativeArray<T> array, int offset, int count)
        {
            _array = array.Array;
            _offset = offset;
            _count = count;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _array != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_offset + index];
        }

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_offset + index];
        }

        /// <summary>
        ///     Array
        /// </summary>
        public T* Array => _array;

        /// <summary>
        ///     Offset
        /// </summary>
        public int Offset => _offset;

        /// <summary>
        ///     Count
        /// </summary>
        public int Count => _count;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeArraySegment<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeArraySegment<T> nativeArraySegment && nativeArraySegment == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_array;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeArraySegment<{typeof(T).Name}>[{_offset}, {_count}]";

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(NativeArraySegment<T> nativeArraySegment) => nativeArraySegment.AsSpan();

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(NativeArraySegment<T> nativeArraySegment) => nativeArraySegment.AsReadOnlySpan();

        /// <summary>
        ///     As native array
        /// </summary>
        /// <param name="nativeArraySegment">Native array segment</param>
        /// <returns>NativeArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArray<T>(NativeArraySegment<T> nativeArraySegment) => new(nativeArraySegment._array, nativeArraySegment._offset + nativeArraySegment._count);

        /// <summary>
        ///     As native array segment
        /// </summary>
        /// <param name="nativeArray">Native array</param>
        /// <returns>NativeArraySegment</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArraySegment<T>(NativeArray<T> nativeArray) => new(nativeArray);

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeArraySegment<T> left, NativeArraySegment<T> right) => left._offset == right._offset && left._count == right._count && left._array == right._array;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeArraySegment<T> left, NativeArraySegment<T> right) => left._offset != right._offset || left._count != right._count || left._array != right._array;

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
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref *(_array + _offset), _count);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int count) => MemoryMarshal.CreateSpan(ref *(_array + _offset), count);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="count">Count</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int count) => MemoryMarshal.CreateSpan(ref *(_array + _offset + start), count);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref *(_array + _offset), _count);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan(int count) => MemoryMarshal.CreateReadOnlySpan(ref *(_array + _offset), count);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="count">Count</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan(int start, int count) => MemoryMarshal.CreateReadOnlySpan(ref *(_array + _offset + start), count);

        /// <summary>
        ///     Slice
        /// </summary>
        /// <param name="start">Start</param>
        /// <returns>NativeArraySegment</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArraySegment<T> Slice(int start) => new(_array, _offset + start, _count - start);

        /// <summary>
        ///     Slice
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="count">Count</param>
        /// <returns>NativeArraySegment</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArraySegment<T> Slice(int start, int count) => new(_array, _offset + start, count);

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeArraySegment<T> Empty => new();

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
            ///     NativeArraySegment
            /// </summary>
            private readonly NativeArraySegment<T> _nativeArraySegment;

            /// <summary>
            ///     Index
            /// </summary>
            private int _index;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeArraySegment">NativeArraySegment</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(NativeArraySegment<T> nativeArraySegment)
            {
                _nativeArraySegment = nativeArraySegment;
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
                if (index < _nativeArraySegment._count)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            /// <summary>
            ///     Current
            /// </summary>
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _nativeArraySegment[_index];
            }
        }
    }
}