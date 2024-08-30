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
    ///     Native queue
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeQueue<T> : IDisposable, IEquatable<NativeQueue<T>> where T : unmanaged
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeQueueHandle
        {
            /// <summary>
            ///     Array
            /// </summary>
            public T* Array;

            /// <summary>
            ///     Length
            /// </summary>
            public int Length;

            /// <summary>
            ///     Head
            /// </summary>
            public int Head;

            /// <summary>
            ///     Tail
            /// </summary>
            public int Tail;

            /// <summary>
            ///     Size
            /// </summary>
            public int Size;

            /// <summary>
            ///     Version
            /// </summary>
            public int Version;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeQueueHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="capacity">Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeQueue(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            if (capacity < 4)
                capacity = 4;
            _handle = (NativeQueueHandle*)NativeMemoryAllocator.Alloc(sizeof(NativeQueueHandle));
            _handle->Array = (T*)NativeMemoryAllocator.Alloc(capacity * sizeof(T));
            _handle->Length = capacity;
            _handle->Head = 0;
            _handle->Tail = 0;
            _handle->Size = 0;
            _handle->Version = 0;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => _handle->Size == 0;

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _handle->Array[index];
        }

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _handle->Array[index];
        }

        /// <summary>
        ///     Count
        /// </summary>
        public int Count => _handle->Size;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeQueue<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeQueue<T> nativeQueue && nativeQueue == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeQueue<{typeof(T).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeQueue<T> left, NativeQueue<T> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeQueue<T> left, NativeQueue<T> right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_handle == null)
                return;
            NativeMemoryAllocator.Free(_handle->Array);
            NativeMemoryAllocator.Free(_handle);
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _handle->Size = 0;
            _handle->Head = 0;
            _handle->Tail = 0;
            _handle->Version++;
        }

        /// <summary>
        ///     Enqueue
        /// </summary>
        /// <param name="item">Item</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(in T item)
        {
            if (_handle->Size == _handle->Length)
                Grow(_handle->Size + 1);
            _handle->Array[_handle->Tail] = item;
            MoveNext(ref _handle->Tail);
            _handle->Size++;
            _handle->Version++;
        }

        /// <summary>
        ///     Try enqueue
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Enqueued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(in T item)
        {
            if (_handle->Size != _handle->Length)
            {
                _handle->Array[_handle->Tail] = item;
                MoveNext(ref _handle->Tail);
                _handle->Size++;
                _handle->Version++;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Dequeue
        /// </summary>
        /// <returns>Item</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (_handle->Size == 0)
                throw new InvalidOperationException("EmptyQueue");
            var removed = _handle->Array[_handle->Head];
            MoveNext(ref _handle->Head);
            _handle->Size--;
            _handle->Version++;
            return removed;
        }

        /// <summary>
        ///     Try dequeue
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Dequeued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T result)
        {
            if (_handle->Size == 0)
            {
                result = default;
                return false;
            }

            result = _handle->Array[_handle->Head];
            MoveNext(ref _handle->Head);
            _handle->Size--;
            _handle->Version++;
            return true;
        }

        /// <summary>
        ///     Peek
        /// </summary>
        /// <returns>Item</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek() => _handle->Size == 0 ? throw new InvalidOperationException("EmptyQueue") : _handle->Array[_handle->Head];

        /// <summary>
        ///     Try peek
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Peeked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T result)
        {
            if (_handle->Size == 0)
            {
                result = default;
                return false;
            }

            result = _handle->Array[_handle->Head];
            return true;
        }

        /// <summary>
        ///     Ensure capacity
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            if (_handle->Length < capacity)
                Grow(capacity);
            return _handle->Length;
        }

        /// <summary>
        ///     Trim excess
        /// </summary>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TrimExcess()
        {
            var threshold = (int)(_handle->Length * 0.9);
            if (_handle->Size < threshold)
                SetCapacity(_handle->Size);
            return _handle->Length;
        }

        /// <summary>
        ///     Set capacity
        /// </summary>
        /// <param name="capacity">Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCapacity(int capacity)
        {
            var newArray = (T*)NativeMemoryAllocator.Alloc(capacity * sizeof(T));
            if (_handle->Size > 0)
            {
                if (_handle->Head < _handle->Tail)
                {
                    Unsafe.CopyBlockUnaligned(newArray, _handle->Array + _handle->Head, (uint)(_handle->Size * sizeof(T)));
                }
                else
                {
                    Unsafe.CopyBlockUnaligned(newArray, _handle->Array + _handle->Head, (uint)((_handle->Length - _handle->Head) * sizeof(T)));
                    Unsafe.CopyBlockUnaligned(newArray + _handle->Length - _handle->Head, _handle->Array, (uint)(_handle->Tail * sizeof(T)));
                }
            }

            NativeMemoryAllocator.Free(_handle->Array);
            _handle->Array = newArray;
            _handle->Length = capacity;
            _handle->Head = 0;
            _handle->Tail = _handle->Size == capacity ? 0 : _handle->Size;
            _handle->Version++;
        }

        /// <summary>
        ///     Grow
        /// </summary>
        /// <param name="capacity">Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(int capacity)
        {
            var newCapacity = 2 * _handle->Length;
            if ((uint)newCapacity > 2147483591)
                newCapacity = 2147483591;
            var expected = _handle->Length + 4;
            newCapacity = newCapacity > expected ? newCapacity : expected;
            if (newCapacity < capacity)
                newCapacity = capacity;
            SetCapacity(newCapacity);
        }

        /// <summary>
        ///     Move next
        /// </summary>
        /// <param name="index">Index</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveNext(ref int index)
        {
            var tmp = index + 1;
            if (tmp == _handle->Length)
                tmp = 0;
            index = tmp;
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeQueue<T> Empty => new();

        /// <summary>
        ///     Get enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        ///     Enumerator
        /// </summary>
        public struct Enumerator
        {
            /// <summary>
            ///     NativeQueue
            /// </summary>
            private readonly NativeQueue<T> _nativeQueue;

            /// <summary>
            ///     Version
            /// </summary>
            private readonly int _version;

            /// <summary>
            ///     Index
            /// </summary>
            private int _index;

            /// <summary>
            ///     Current
            /// </summary>
            private T _currentElement;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeQueue">NativeQueue</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(in NativeQueue<T> nativeQueue)
            {
                _nativeQueue = nativeQueue;
                _version = nativeQueue._handle->Version;
                _index = -1;
                _currentElement = default;
            }

            /// <summary>
            ///     Move next
            /// </summary>
            /// <returns>Moved</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_version != _nativeQueue._handle->Version)
                    throw new InvalidOperationException("EnumFailedVersion");
                if (_index == -2)
                    return false;
                _index++;
                if (_index == _nativeQueue._handle->Size)
                {
                    _index = -2;
                    _currentElement = default;
                    return false;
                }

                var array = _nativeQueue._handle->Array;
                var capacity = (uint)_nativeQueue._handle->Length;
                var arrayIndex = (uint)(_nativeQueue._handle->Head + _index);
                if (arrayIndex >= capacity)
                    arrayIndex -= capacity;
                _currentElement = array[arrayIndex];
                return true;
            }

            /// <summary>
            ///     Current
            /// </summary>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _index < 0 ? throw new InvalidOperationException(_index == -1 ? "EnumNotStarted" : "EnumEnded") : _currentElement;
            }
        }
    }
}