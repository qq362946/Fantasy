using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
using System.Collections.Generic;
#endif

#pragma warning disable CA2208
#pragma warning disable CS8632

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     Native sortedList
    /// </summary>
    /// <typeparam name="TKey">Type</typeparam>
    /// <typeparam name="TValue">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeSortedList<TKey, TValue> where TKey : unmanaged, IComparable<TKey> where TValue : unmanaged
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSortedListHandle
        {
            /// <summary>
            ///     Keys
            /// </summary>
            public TKey* Buckets;

            /// <summary>
            ///     Values
            /// </summary>
            public TValue* Entries;

            /// <summary>
            ///     Size
            /// </summary>
            public int Size;

            /// <summary>
            ///     Version
            /// </summary>
            public int Version;

            /// <summary>
            ///     Capacity
            /// </summary>
            public int Capacity;

            /// <summary>
            ///     Keys
            /// </summary>
            public KeyCollection Keys;

            /// <summary>
            ///     Values
            /// </summary>
            public ValueCollection Values;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeSortedListHandle* _handle;

        /// <summary>
        ///     Keys
        /// </summary>
        public KeyCollection Keys => _handle->Keys;

        /// <summary>
        ///     Values
        /// </summary>
        public ValueCollection Values => _handle->Values;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="capacity">Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSortedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            if (capacity < 4)
                capacity = 4;
            _handle = (NativeSortedListHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeSortedListHandle));
            _handle->Buckets = (TKey*)NativeMemoryAllocator.Alloc((uint)(capacity * sizeof(TKey)));
            _handle->Entries = (TValue*)NativeMemoryAllocator.Alloc((uint)(capacity * sizeof(TValue)));
            _handle->Size = 0;
            _handle->Version = 0;
            _handle->Capacity = capacity;
            _handle->Keys = new KeyCollection(this);
            _handle->Values = new ValueCollection(this);
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
        ///     Get or set value
        /// </summary>
        /// <param name="key">Key</param>
        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var index = BinarySearch(_handle->Buckets, _handle->Size, key);
                return index >= 0 ? _handle->Entries[index] : throw new KeyNotFoundException(key.ToString());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var index = BinarySearch(_handle->Buckets, _handle->Size, key);
                if (index >= 0)
                {
                    _handle->Entries[index] = value;
                    ++_handle->Version;
                }
                else
                {
                    Insert(~index, key, value);
                }
            }
        }

        /// <summary>
        ///     Count
        /// </summary>
        public int Count => _handle->Size;

        /// <summary>
        ///     Capacity
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->Capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value < _handle->Size)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "SmallCapacity");
                if (value != _handle->Capacity)
                {
                    if (value > 0)
                    {
                        var keys = (TKey*)NativeMemoryAllocator.Alloc((uint)(value * sizeof(TKey)));
                        var values = (TValue*)NativeMemoryAllocator.Alloc((uint)(value * sizeof(TValue)));
                        if (_handle->Size > 0)
                        {
                            Unsafe.CopyBlockUnaligned(keys, _handle->Buckets, (uint)(_handle->Size * sizeof(TKey)));
                            Unsafe.CopyBlockUnaligned(values, _handle->Entries, (uint)(_handle->Size * sizeof(TValue)));
                        }

                        NativeMemoryAllocator.Free(_handle->Buckets);
                        NativeMemoryAllocator.Free(_handle->Entries);
                        _handle->Buckets = keys;
                        _handle->Entries = values;
                    }
                    else
                    {
                        NativeMemoryAllocator.Free(_handle->Buckets);
                        NativeMemoryAllocator.Free(_handle->Entries);
                        _handle->Buckets = (TKey*)NativeMemoryAllocator.Alloc(0);
                        _handle->Entries = (TValue*)NativeMemoryAllocator.Alloc(0);
                    }

                    _handle->Capacity = value;
                }
            }
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeSortedList<TKey, TValue> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeSortedList<TKey, TValue> nativeSortedList && nativeSortedList == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeSortedList<{typeof(TKey).Name}, {typeof(TValue).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeSortedList<TKey, TValue> left, NativeSortedList<TKey, TValue> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeSortedList<TKey, TValue> left, NativeSortedList<TKey, TValue> right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_handle == null)
                return;
            NativeMemoryAllocator.Free(_handle->Buckets);
            NativeMemoryAllocator.Free(_handle->Entries);
            NativeMemoryAllocator.Free(_handle);
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            ++_handle->Version;
            _handle->Size = 0;
        }

        /// <summary>
        ///     Add
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in TKey key, in TValue value)
        {
            var num = BinarySearch(_handle->Buckets, _handle->Size, key);
            if (num >= 0)
                throw new ArgumentException($"AddingDuplicate, {key}", nameof(key));
            Insert(~num, key, value);
        }

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Removed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in TKey key)
        {
            var index = BinarySearch(_handle->Buckets, _handle->Size, key);
            if (index >= 0)
            {
                --_handle->Size;
                if (index < _handle->Size)
                {
                    Unsafe.CopyBlockUnaligned(_handle->Buckets + index, _handle->Buckets + index + 1, (uint)((_handle->Size - index) * sizeof(TKey)));
                    Unsafe.CopyBlockUnaligned(_handle->Entries + index, _handle->Entries + index + 1, (uint)((_handle->Size - index) * sizeof(TValue)));
                }

                ++_handle->Version;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Removed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in TKey key, out TValue value)
        {
            var index = BinarySearch(_handle->Buckets, _handle->Size, key);
            if (index >= 0)
            {
                value = _handle->Entries[index];
                --_handle->Size;
                if (index < _handle->Size)
                {
                    Unsafe.CopyBlockUnaligned(_handle->Buckets + index, _handle->Buckets + index + 1, (uint)((_handle->Size - index) * sizeof(TKey)));
                    Unsafe.CopyBlockUnaligned(_handle->Entries + index, _handle->Entries + index + 1, (uint)((_handle->Size - index) * sizeof(TValue)));
                }

                ++_handle->Version;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        ///     Contains key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Contains key</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(in TKey key) => BinarySearch(_handle->Buckets, _handle->Size, key) >= 0;

        /// <summary>
        ///     Try to get the value
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Got</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(in TKey key, out TValue value)
        {
            var index = BinarySearch(_handle->Buckets, _handle->Size, key);
            if (index >= 0)
            {
                value = _handle->Entries[index];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        ///     Ensure capacity
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int capacity)
        {
            if (_handle->Capacity < capacity)
            {
                var newCapacity = 2 * _handle->Capacity;
                if ((uint)newCapacity > 2147483591)
                    newCapacity = 2147483591;
                var expected = _handle->Capacity + 4;
                newCapacity = newCapacity > expected ? newCapacity : expected;
                if (newCapacity < capacity)
                    newCapacity = capacity;
                Capacity = newCapacity;
            }
        }

        /// <summary>
        ///     Trim excess
        /// </summary>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TrimExcess()
        {
            var threshold = (int)(_handle->Capacity * 0.9);
            if (_handle->Size < threshold)
                Capacity = _handle->Size;
            return _handle->Capacity;
        }

        /// <summary>
        ///     Insert
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Insert(int index, in TKey key, in TValue value)
        {
            if (_handle->Size == _handle->Capacity)
                EnsureCapacity(_handle->Size + 1);
            if (index < _handle->Size)
            {
                Unsafe.CopyBlockUnaligned(_handle->Buckets + index + 1, _handle->Buckets + index, (uint)((_handle->Size - index) * sizeof(TKey)));
                Unsafe.CopyBlockUnaligned(_handle->Entries + index + 1, _handle->Entries + index, (uint)((_handle->Size - index) * sizeof(TValue)));
            }

            _handle->Buckets[index] = key;
            _handle->Entries[index] = value;
            ++_handle->Size;
            ++_handle->Version;
        }

        /// <summary>
        ///     Binary search
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="length">Length</param>
        /// <param name="comparable">Comparable</param>
        /// <returns>Index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BinarySearch(TKey* start, int length, in TKey comparable)
        {
            var low = 0;
            var high = length - 1;
            while (low <= high)
            {
                var i = (int)(((uint)high + (uint)low) >> 1);
                var c = comparable.CompareTo(*(start + i));
                if (c == 0)
                    return i;
                if (c > 0)
                    low = i + 1;
                else
                    high = i - 1;
            }

            return ~low;
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeSortedList<TKey, TValue> Empty => new();

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
            ///     NativeSortedList
            /// </summary>
            private readonly NativeSortedList<TKey, TValue> _nativeSortedList;

            /// <summary>
            ///     Current
            /// </summary>
            private KeyValuePair<TKey, TValue> _current;

            /// <summary>
            ///     Index
            /// </summary>
            private int _index;

            /// <summary>
            ///     Version
            /// </summary>
            private readonly int _version;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeSortedList">NativeSortedList</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(NativeSortedList<TKey, TValue> nativeSortedList)
            {
                _nativeSortedList = nativeSortedList;
                _current = default;
                _index = 0;
                _version = _nativeSortedList._handle->Version;
            }

            /// <summary>
            ///     Move next
            /// </summary>
            /// <returns>Moved</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_version != _nativeSortedList._handle->Version)
                    throw new InvalidOperationException("EnumFailedVersion");
                if ((uint)_index < (uint)_nativeSortedList._handle->Size)
                {
                    _current = new KeyValuePair<TKey, TValue>(_nativeSortedList._handle->Buckets[_index], _nativeSortedList._handle->Entries[_index]);
                    ++_index;
                    return true;
                }

                _index = _nativeSortedList._handle->Size + 1;
                return false;
            }

            /// <summary>
            ///     Current
            /// </summary>
            public KeyValuePair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
        }

        /// <summary>
        ///     Key collection
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct KeyCollection
        {
            /// <summary>
            ///     NativeSortedList
            /// </summary>
            private readonly NativeSortedList<TKey, TValue> _nativeSortedList;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeSortedList">NativeSortedList</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal KeyCollection(NativeSortedList<TKey, TValue> nativeSortedList) => _nativeSortedList = nativeSortedList;

            /// <summary>
            ///     Get enumerator
            /// </summary>
            /// <returns>Enumerator</returns>
            public Enumerator GetEnumerator() => new(_nativeSortedList);

            /// <summary>
            ///     Enumerator
            /// </summary>
            public struct Enumerator
            {
                /// <summary>
                ///     NativeSortedList
                /// </summary>
                private readonly NativeSortedList<TKey, TValue> _nativeSortedList;

                /// <summary>
                ///     Current
                /// </summary>
                private TKey _current;

                /// <summary>
                ///     Index
                /// </summary>
                private int _index;

                /// <summary>
                ///     Version
                /// </summary>
                private readonly int _version;

                /// <summary>
                ///     Structure
                /// </summary>
                /// <param name="nativeSortedList">NativeSortedList</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                internal Enumerator(NativeSortedList<TKey, TValue> nativeSortedList)
                {
                    _nativeSortedList = nativeSortedList;
                    _current = default;
                    _index = 0;
                    _version = _nativeSortedList._handle->Version;
                }

                /// <summary>
                ///     Move next
                /// </summary>
                /// <returns>Moved</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    if (_version != _nativeSortedList._handle->Version)
                        throw new InvalidOperationException("EnumFailedVersion");
                    if ((uint)_index < (uint)_nativeSortedList._handle->Size)
                    {
                        _current = _nativeSortedList._handle->Buckets[_index];
                        ++_index;
                        return true;
                    }

                    _index = _nativeSortedList._handle->Size + 1;
                    return false;
                }

                /// <summary>
                ///     Current
                /// </summary>
                public TKey Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _current;
                }
            }
        }

        /// <summary>
        ///     Value collection
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ValueCollection
        {
            /// <summary>
            ///     NativeSortedList
            /// </summary>
            private readonly NativeSortedList<TKey, TValue> _nativeSortedList;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeSortedList">NativeSortedList</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ValueCollection(NativeSortedList<TKey, TValue> nativeSortedList) => _nativeSortedList = nativeSortedList;

            /// <summary>
            ///     Get enumerator
            /// </summary>
            /// <returns>Enumerator</returns>
            public Enumerator GetEnumerator() => new(_nativeSortedList);

            /// <summary>
            ///     Enumerator
            /// </summary>
            public struct Enumerator
            {
                /// <summary>
                ///     NativeSortedList
                /// </summary>
                private readonly NativeSortedList<TKey, TValue> _nativeSortedList;

                /// <summary>
                ///     Current
                /// </summary>
                private TValue _current;

                /// <summary>
                ///     Index
                /// </summary>
                private int _index;

                /// <summary>
                ///     Version
                /// </summary>
                private readonly int _version;

                /// <summary>
                ///     Structure
                /// </summary>
                /// <param name="nativeSortedList">NativeSortedList</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                internal Enumerator(NativeSortedList<TKey, TValue> nativeSortedList)
                {
                    _nativeSortedList = nativeSortedList;
                    _current = default;
                    _index = 0;
                    _version = _nativeSortedList._handle->Version;
                }

                /// <summary>
                ///     Move next
                /// </summary>
                /// <returns>Moved</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    if (_version != _nativeSortedList._handle->Version)
                        throw new InvalidOperationException("EnumFailedVersion");
                    if ((uint)_index < (uint)_nativeSortedList._handle->Size)
                    {
                        _current = _nativeSortedList._handle->Entries[_index];
                        ++_index;
                        return true;
                    }

                    _index = _nativeSortedList._handle->Size + 1;
                    return false;
                }

                /// <summary>
                ///     Current
                /// </summary>
                public TValue Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _current;
                }
            }
        }
    }
}