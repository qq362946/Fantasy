using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif

#pragma warning disable CA2208
#pragma warning disable CS8600
#pragma warning disable CS8603
#pragma warning disable CS8632

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable SuggestBaseTypeForParameterInConstructor

namespace NativeCollections
{
    /// <summary>
    ///     Native array reference
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeArrayReference<T> : IDisposable, IEquatable<NativeArrayReference<T>>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        private GCHandle _handle;

        /// <summary>
        ///     Length
        /// </summary>
        private readonly int _length;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArrayReference(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _handle = GCHandle.Alloc(new T[length], GCHandleType.Normal);
            _length = length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="length">Length</param>
        /// <param name="type">GCHandle type</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArrayReference(int length, GCHandleType type)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _handle = GCHandle.Alloc(new T[length], type);
            _length = length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArrayReference(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), "MustBeNotNull");
            _handle = GCHandle.Alloc(array, GCHandleType.Normal);
            _length = array.Length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="type">GCHandle type</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArrayReference(T[] array, GCHandleType type)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), "MustBeNotNull");
            _handle = GCHandle.Alloc(array, type);
            _length = array.Length;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle.IsAllocated;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => _length == 0;

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Array[index];
        }

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Array[index];
        }

        /// <summary>
        ///     Array
        /// </summary>
        public T[] Array
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (T[])_handle.Target;
        }

        /// <summary>
        ///     Length
        /// </summary>
        public int Length => _length;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeArrayReference<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeArrayReference<T> nativeArrayReference && nativeArrayReference == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeArrayReference<{typeof(T).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeArrayReference<T> left, NativeArrayReference<T> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeArrayReference<T> left, NativeArrayReference<T> right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!_handle.IsAllocated)
                return;
            _handle.Free();
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeArrayReference<T> Empty => new();

        /// <summary>
        ///     Get enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Enumerator GetEnumerator() => new(Array);

        /// <summary>
        ///     Enumerator
        /// </summary>
        public ref struct Enumerator
        {
            /// <summary>
            ///     Array
            /// </summary>
            private readonly T[] _array;

            /// <summary>
            ///     Index
            /// </summary>
            private int _index;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="array">Array</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(T[] array)
            {
                _array = array;
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
                if (index < _array.Length)
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
                get => ref _array[_index];
            }
        }
    }
}