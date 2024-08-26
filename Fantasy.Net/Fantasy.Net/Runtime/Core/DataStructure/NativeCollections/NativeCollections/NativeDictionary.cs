using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
using System.Collections.Generic;
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
    ///     Native dictionary
    /// </summary>
    /// <typeparam name="TKey">Type</typeparam>
    /// <typeparam name="TValue">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeDictionary<TKey, TValue> : IDisposable, IEquatable<NativeDictionary<TKey, TValue>> where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeDictionaryHandle
        {
            /// <summary>
            ///     Buckets
            /// </summary>
            public int* Buckets;

            /// <summary>
            ///     Entries
            /// </summary>
            public Entry* Entries;

            /// <summary>
            ///     BucketsLength
            /// </summary>
            public int BucketsLength;

            /// <summary>
            ///     EntriesLength
            /// </summary>
            public int EntriesLength;

            /// <summary>
            ///     FastModMultiplier
            /// </summary>
            public ulong FastModMultiplier;

            /// <summary>
            ///     Count
            /// </summary>
            public int Count;

            /// <summary>
            ///     FreeList
            /// </summary>
            public int FreeList;

            /// <summary>
            ///     FreeCount
            /// </summary>
            public int FreeCount;

            /// <summary>
            ///     Version
            /// </summary>
            public int Version;

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
        private readonly NativeDictionaryHandle* _handle;

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
        public NativeDictionary(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            if (capacity < 4)
                capacity = 4;
            _handle = (NativeDictionaryHandle*)NativeMemoryAllocator.Alloc(sizeof(NativeDictionaryHandle));
            _handle->Count = 0;
            _handle->FreeCount = 0;
            _handle->Version = 0;
            Initialize(capacity);
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
        public bool IsEmpty => _handle->Count - _handle->FreeCount == 0;

        /// <summary>
        ///     Get or set value
        /// </summary>
        /// <param name="key">Key</param>
        public TValue this[in TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref var value = ref FindValue(key);
                if (Unsafe.AsPointer(ref Unsafe.AsRef(in value)) != null)
                    return value;
                throw new KeyNotFoundException(key.ToString());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => TryInsertOverwriteExisting(key, value);
        }

        /// <summary>
        ///     Count
        /// </summary>
        public int Count => _handle->Count - _handle->FreeCount;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeDictionary<TKey, TValue> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeDictionary<TKey, TValue> nativeDictionary && nativeDictionary == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeDictionary<TKey, TValue> left, NativeDictionary<TKey, TValue> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeDictionary<TKey, TValue> left, NativeDictionary<TKey, TValue> right) => left._handle != right._handle;

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
            var count = _handle->Count;
            if (count > 0)
            {
                Unsafe.InitBlockUnaligned(_handle->Buckets, 0, (uint)(count * sizeof(int)));
                _handle->Count = 0;
                _handle->FreeList = -1;
                _handle->FreeCount = 0;
                Unsafe.InitBlockUnaligned(_handle->Entries, 0, (uint)(count * sizeof(Entry)));
            }
        }

        /// <summary>
        ///     Add
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in TKey key, in TValue value) => TryInsertThrowOnExisting(key, value);

        /// <summary>
        ///     Try add
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Added</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(in TKey key, in TValue value) => TryInsertNone(key, value);

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Removed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in TKey key)
        {
            uint collisionCount = 0;
            var hashCode = (uint)key.GetHashCode();
            ref var bucket = ref GetBucket(hashCode);
            var last = -1;
            var i = bucket - 1;
            while (i >= 0)
            {
                ref var entry = ref _handle->Entries[i];
                if (entry.HashCode == hashCode && entry.Key.Equals(key))
                {
                    if (last < 0)
                        bucket = entry.Next + 1;
                    else
                        _handle->Entries[last].Next = entry.Next;
                    entry.Next = -3 - _handle->FreeList;
                    _handle->FreeList = i;
                    _handle->FreeCount++;
                    return true;
                }

                last = i;
                i = entry.Next;
                collisionCount++;
                if (collisionCount > (uint)_handle->EntriesLength)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
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
            uint collisionCount = 0;
            var hashCode = (uint)key.GetHashCode();
            ref var bucket = ref GetBucket(hashCode);
            var last = -1;
            var i = bucket - 1;
            while (i >= 0)
            {
                ref var entry = ref _handle->Entries[i];
                if (entry.HashCode == hashCode && entry.Key.Equals(key))
                {
                    if (last < 0)
                        bucket = entry.Next + 1;
                    else
                        _handle->Entries[last].Next = entry.Next;
                    value = entry.Value;
                    entry.Next = -3 - _handle->FreeList;
                    _handle->FreeList = i;
                    _handle->FreeCount++;
                    return true;
                }

                last = i;
                i = entry.Next;
                collisionCount++;
                if (collisionCount > (uint)_handle->EntriesLength)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
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
        public bool ContainsKey(in TKey key) => Unsafe.AsPointer(ref Unsafe.AsRef(in FindValue(key))) != null;

        /// <summary>
        ///     Try to get the value
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Got</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(in TKey key, out TValue value)
        {
            ref var valRef = ref FindValue(key);
            if (Unsafe.AsPointer(ref Unsafe.AsRef(in valRef)) != null)
            {
                value = valRef;
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
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            var currentCapacity = _handle->EntriesLength;
            if (currentCapacity >= capacity)
                return currentCapacity;
            _handle->Version++;
            var newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize);
            return newSize;
        }

        /// <summary>
        ///     Trim excess
        /// </summary>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TrimExcess() => TrimExcess(Count);

        /// <summary>
        ///     Trim excess
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TrimExcess(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            var newSize = HashHelpers.GetPrime(capacity);
            var oldEntries = _handle->Entries;
            var currentCapacity = _handle->EntriesLength;
            if (newSize >= currentCapacity)
                return currentCapacity;
            var oldCount = _handle->Count;
            _handle->Version++;
            NativeMemoryAllocator.Free(_handle->Buckets);
            Initialize(newSize);
            var newEntries = _handle->Entries;
            var newCount = 0;
            for (var i = 0; i < oldCount; i++)
            {
                var hashCode = oldEntries[i].HashCode;
                if (oldEntries[i].Next >= -1)
                {
                    ref var entry = ref newEntries[newCount];
                    entry = oldEntries[i];
                    ref var bucket = ref GetBucket(hashCode);
                    entry.Next = bucket - 1;
                    bucket = newCount + 1;
                    newCount++;
                }
            }

            NativeMemoryAllocator.Free(oldEntries);
            _handle->Count = newCount;
            _handle->FreeCount = 0;
            return newSize;
        }

        /// <summary>
        ///     Find value
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref TValue FindValue(in TKey key)
        {
            var hashCode = (uint)key.GetHashCode();
            var i = GetBucket(hashCode);
            uint collisionCount = 0;
            i--;
            do
            {
                if ((uint)i >= (uint)_handle->EntriesLength)
                    return ref Unsafe.AsRef<TValue>(null);
                ref var entry = ref _handle->Entries[i];
                if (entry.HashCode == hashCode && entry.Key.Equals(key))
                    return ref entry.Value;
                i = entry.Next;
                collisionCount++;
            } while (collisionCount <= (uint)_handle->EntriesLength);

            throw new InvalidOperationException("ConcurrentOperationsNotSupported");
        }

        /// <summary>
        ///     Initialize
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize(int capacity)
        {
            var size = HashHelpers.GetPrime(capacity);
            _handle->FreeList = -1;
            _handle->Buckets = (int*)NativeMemoryAllocator.AllocZeroed(size * sizeof(int));
            _handle->Entries = (Entry*)NativeMemoryAllocator.AllocZeroed(size * sizeof(Entry));
            _handle->BucketsLength = size;
            _handle->EntriesLength = size;
            _handle->FastModMultiplier = IntPtr.Size == 8 ? HashHelpers.GetFastModMultiplier((uint)size) : 0;
        }

        /// <summary>
        ///     Resize
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize() => Resize(HashHelpers.ExpandPrime(_handle->Count));

        /// <summary>
        ///     Resize
        /// </summary>
        /// <param name="newSize"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newSize)
        {
            var entries = (Entry*)NativeMemoryAllocator.AllocZeroed(newSize * sizeof(Entry));
            var count = _handle->Count;
            Unsafe.CopyBlockUnaligned(entries, _handle->Entries, (uint)(count * sizeof(Entry)));
            var buckets = (int*)NativeMemoryAllocator.AllocZeroed(newSize * sizeof(int));
            NativeMemoryAllocator.Free(_handle->Buckets);
            _handle->Buckets = buckets;
            _handle->BucketsLength = newSize;
            _handle->FastModMultiplier = IntPtr.Size == 8 ? HashHelpers.GetFastModMultiplier((uint)newSize) : 0;
            for (var i = 0; i < count; i++)
            {
                if (entries[i].Next >= -1)
                {
                    ref var bucket = ref GetBucket(entries[i].HashCode);
                    entries[i].Next = bucket - 1;
                    bucket = i + 1;
                }
            }

            NativeMemoryAllocator.Free(_handle->Entries);
            _handle->Entries = entries;
            _handle->EntriesLength = newSize;
        }

        /// <summary>
        ///     Insert
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryInsertOverwriteExisting(in TKey key, in TValue value)
        {
            var hashCode = (uint)key.GetHashCode();
            uint collisionCount = 0;
            ref var bucket = ref GetBucket(hashCode);
            var i = bucket - 1;
            while (true)
            {
                if ((uint)i >= (uint)_handle->EntriesLength)
                    break;
                if (_handle->Entries[i].HashCode == hashCode && _handle->Entries[i].Key.Equals(key))
                {
                    _handle->Entries[i].Value = value;
                    return;
                }

                i = _handle->Entries[i].Next;
                collisionCount++;
                if (collisionCount > (uint)_handle->EntriesLength)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
            }

            int index;
            if (_handle->FreeCount > 0)
            {
                index = _handle->FreeList;
                _handle->FreeList = -3 - _handle->Entries[_handle->FreeList].Next;
                _handle->FreeCount--;
            }
            else
            {
                var count = _handle->Count;
                if (count == _handle->EntriesLength)
                {
                    Resize();
                    bucket = ref GetBucket(hashCode);
                }

                index = count;
                _handle->Count = count + 1;
            }

            ref var entry = ref _handle->Entries[index];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1;
            entry.Key = key;
            entry.Value = value;
            bucket = index + 1;
            _handle->Version++;
        }

        /// <summary>
        ///     Insert
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryInsertThrowOnExisting(in TKey key, in TValue value)
        {
            var hashCode = (uint)key.GetHashCode();
            uint collisionCount = 0;
            ref var bucket = ref GetBucket(hashCode);
            var i = bucket - 1;
            while (true)
            {
                if ((uint)i >= (uint)_handle->EntriesLength)
                    break;
                if (_handle->Entries[i].HashCode == hashCode && _handle->Entries[i].Key.Equals(key))
                    throw new ArgumentException($"Argument_AddingDuplicateWithKey, {key}");
                i = _handle->Entries[i].Next;
                collisionCount++;
                if (collisionCount > (uint)_handle->EntriesLength)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
            }

            int index;
            if (_handle->FreeCount > 0)
            {
                index = _handle->FreeList;
                _handle->FreeList = -3 - _handle->Entries[_handle->FreeList].Next;
                _handle->FreeCount--;
            }
            else
            {
                var count = _handle->Count;
                if (count == _handle->EntriesLength)
                {
                    Resize();
                    bucket = ref GetBucket(hashCode);
                }

                index = count;
                _handle->Count = count + 1;
            }

            ref var entry = ref _handle->Entries[index];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1;
            entry.Key = key;
            entry.Value = value;
            bucket = index + 1;
            _handle->Version++;
            return true;
        }

        /// <summary>
        ///     Insert
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryInsertNone(in TKey key, in TValue value)
        {
            var hashCode = (uint)key.GetHashCode();
            uint collisionCount = 0;
            ref var bucket = ref GetBucket(hashCode);
            var i = bucket - 1;
            while (true)
            {
                if ((uint)i >= (uint)_handle->EntriesLength)
                    break;
                if (_handle->Entries[i].HashCode == hashCode && _handle->Entries[i].Key.Equals(key))
                    return false;
                i = _handle->Entries[i].Next;
                collisionCount++;
                if (collisionCount > (uint)_handle->EntriesLength)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
            }

            int index;
            if (_handle->FreeCount > 0)
            {
                index = _handle->FreeList;
                _handle->FreeList = -3 - _handle->Entries[_handle->FreeList].Next;
                _handle->FreeCount--;
            }
            else
            {
                var count = _handle->Count;
                if (count == _handle->EntriesLength)
                {
                    Resize();
                    bucket = ref GetBucket(hashCode);
                }

                index = count;
                _handle->Count = count + 1;
            }

            ref var entry = ref _handle->Entries[index];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1;
            entry.Key = key;
            entry.Value = value;
            bucket = index + 1;
            _handle->Version++;
            return true;
        }

        /// <summary>
        ///     Get bucket ref
        /// </summary>
        /// <param name="hashCode">HashCode</param>
        /// <returns>Bucket ref</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(uint hashCode) => ref IntPtr.Size == 8 ? ref _handle->Buckets[HashHelpers.FastMod(hashCode, (uint)_handle->BucketsLength, _handle->FastModMultiplier)] : ref _handle->Buckets[hashCode % _handle->BucketsLength];

        /// <summary>
        ///     Entry
        /// </summary>
        private struct Entry
        {
            /// <summary>
            ///     HashCode
            /// </summary>
            public uint HashCode;

            /// <summary>
            ///     Next
            /// </summary>
            public int Next;

            /// <summary>
            ///     Key
            /// </summary>
            public TKey Key;

            /// <summary>
            ///     Value
            /// </summary>
            public TValue Value;
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeDictionary<TKey, TValue> Empty => new();

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
            ///     NativeDictionary
            /// </summary>
            private readonly NativeDictionary<TKey, TValue> _nativeDictionary;

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
            private KeyValuePair<TKey, TValue> _current;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeDictionary">NativeDictionary</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(in NativeDictionary<TKey, TValue> nativeDictionary)
            {
                _nativeDictionary = nativeDictionary;
                _version = nativeDictionary._handle->Version;
                _index = 0;
                _current = default;
            }

            /// <summary>
            ///     Move next
            /// </summary>
            /// <returns>Moved</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_version != _nativeDictionary._handle->Version)
                    throw new InvalidOperationException("EnumFailedVersion");
                while ((uint)_index < (uint)_nativeDictionary._handle->Count)
                {
                    ref var entry = ref _nativeDictionary._handle->Entries[_index++];
                    if (entry.Next >= -1)
                    {
                        _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                        return true;
                    }
                }

                _index = _nativeDictionary._handle->Count + 1;
                _current = default;
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
            ///     NativeDictionary
            /// </summary>
            private readonly NativeDictionary<TKey, TValue> _nativeDictionary;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeDictionary">NativeDictionary</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal KeyCollection(in NativeDictionary<TKey, TValue> nativeDictionary) => _nativeDictionary = nativeDictionary;

            /// <summary>
            ///     Get enumerator
            /// </summary>
            /// <returns>Enumerator</returns>
            public Enumerator GetEnumerator() => new(_nativeDictionary);

            /// <summary>
            ///     Enumerator
            /// </summary>
            public struct Enumerator
            {
                /// <summary>
                ///     NativeDictionary
                /// </summary>
                private readonly NativeDictionary<TKey, TValue> _nativeDictionary;

                /// <summary>
                ///     Index
                /// </summary>
                private int _index;

                /// <summary>
                ///     Version
                /// </summary>
                private readonly int _version;

                /// <summary>
                ///     Current
                /// </summary>
                private TKey _currentKey;

                /// <summary>
                ///     Structure
                /// </summary>
                /// <param name="nativeDictionary">NativeDictionary</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                internal Enumerator(in NativeDictionary<TKey, TValue> nativeDictionary)
                {
                    _nativeDictionary = nativeDictionary;
                    _version = nativeDictionary._handle->Version;
                    _index = 0;
                    _currentKey = default;
                }

                /// <summary>
                ///     Move next
                /// </summary>
                /// <returns>Moved</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    if (_version != _nativeDictionary._handle->Version)
                        throw new InvalidOperationException("EnumFailedVersion");
                    while ((uint)_index < (uint)_nativeDictionary._handle->Count)
                    {
                        ref var entry = ref _nativeDictionary._handle->Entries[_index++];
                        if (entry.Next >= -1)
                        {
                            _currentKey = entry.Key;
                            return true;
                        }
                    }

                    _index = _nativeDictionary._handle->Count + 1;
                    _currentKey = default;
                    return false;
                }

                /// <summary>
                ///     Current
                /// </summary>
                public TKey Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _currentKey;
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
            ///     NativeDictionary
            /// </summary>
            private readonly NativeDictionary<TKey, TValue> _nativeDictionary;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeDictionary">NativeDictionary</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ValueCollection(in NativeDictionary<TKey, TValue> nativeDictionary) => _nativeDictionary = nativeDictionary;

            /// <summary>
            ///     Get enumerator
            /// </summary>
            /// <returns>Enumerator</returns>
            public Enumerator GetEnumerator() => new(_nativeDictionary);

            /// <summary>
            ///     Enumerator
            /// </summary>
            public struct Enumerator
            {
                /// <summary>
                ///     NativeDictionary
                /// </summary>
                private readonly NativeDictionary<TKey, TValue> _nativeDictionary;

                /// <summary>
                ///     Index
                /// </summary>
                private int _index;

                /// <summary>
                ///     Version
                /// </summary>
                private readonly int _version;

                /// <summary>
                ///     Current
                /// </summary>
                private TValue _currentValue;

                /// <summary>
                ///     Structure
                /// </summary>
                /// <param name="nativeDictionary">NativeDictionary</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                internal Enumerator(in NativeDictionary<TKey, TValue> nativeDictionary)
                {
                    _nativeDictionary = nativeDictionary;
                    _version = nativeDictionary._handle->Version;
                    _index = 0;
                    _currentValue = default;
                }

                /// <summary>
                ///     Move next
                /// </summary>
                /// <returns>Moved</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    if (_version != _nativeDictionary._handle->Version)
                        throw new InvalidOperationException("EnumFailedVersion");
                    while ((uint)_index < (uint)_nativeDictionary._handle->Count)
                    {
                        ref var entry = ref _nativeDictionary._handle->Entries[_index++];
                        if (entry.Next >= -1)
                        {
                            _currentValue = entry.Value;
                            return true;
                        }
                    }

                    _index = _nativeDictionary._handle->Count + 1;
                    _currentValue = default;
                    return false;
                }

                /// <summary>
                ///     Current
                /// </summary>
                public TValue Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _currentValue;
                }
            }
        }
    }
}