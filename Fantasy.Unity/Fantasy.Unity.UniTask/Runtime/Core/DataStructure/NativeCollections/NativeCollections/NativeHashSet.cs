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
    ///     Native hashSet
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeHashSet<T> : IDisposable, IEquatable<NativeHashSet<T>> where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeHashSetHandle
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
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeHashSetHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="capacity">Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeHashSet(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            if (capacity < 4)
                capacity = 4;
            _handle = (NativeHashSetHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeHashSetHandle));
            _handle->Count = 0;
            _handle->FreeCount = 0;
            _handle->Version = 0;
            Initialize(capacity);
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
        ///     Count
        /// </summary>
        public int Count => _handle->Count - _handle->FreeCount;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeHashSet<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeHashSet<T> nativeHashSet && nativeHashSet == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeHashSet<{typeof(T).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeHashSet<T> left, NativeHashSet<T> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeHashSet<T> left, NativeHashSet<T> right) => left._handle != right._handle;

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
                Unsafe.InitBlockUnaligned(_handle->Buckets, 0, (uint)(_handle->BucketsLength * sizeof(int)));
                _handle->Count = 0;
                _handle->FreeList = -1;
                _handle->FreeCount = 0;
                Unsafe.InitBlockUnaligned(_handle->Entries, 0, (uint)(count * sizeof(Entry)));
            }
        }

        /// <summary>
        ///     Add
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Added</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T item)
        {
            uint collisionCount = 0;
            var hashCode = item.GetHashCode();
            ref var bucket = ref GetBucketRef(hashCode);
            var i = bucket - 1;
            while (i >= 0)
            {
                ref var entry = ref _handle->Entries[i];
                if (entry.HashCode == hashCode && entry.Value.Equals(item))
                    return false;
                i = entry.Next;
                collisionCount++;
                if (collisionCount > (uint)_handle->EntriesLength)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
            }

            int index;
            if (_handle->FreeCount > 0)
            {
                index = _handle->FreeList;
                _handle->FreeCount--;
                _handle->FreeList = -3 - _handle->Entries[_handle->FreeList].Next;
            }
            else
            {
                var count = _handle->Count;
                if (count == _handle->EntriesLength)
                {
                    Resize();
                    bucket = ref GetBucketRef(hashCode);
                }

                index = count;
                _handle->Count = count + 1;
            }

            ref var newEntry = ref _handle->Entries[index];
            newEntry.HashCode = hashCode;
            newEntry.Next = bucket - 1;
            newEntry.Value = item;
            bucket = index + 1;
            _handle->Version++;
            return true;
        }

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Removed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in T item)
        {
            uint collisionCount = 0;
            var last = -1;
            var hashCode = item.GetHashCode();
            ref var bucket = ref GetBucketRef(hashCode);
            var i = bucket - 1;
            while (i >= 0)
            {
                ref var entry = ref _handle->Entries[i];
                if (entry.HashCode == hashCode && entry.Value.Equals(item))
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
        ///     Contains
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Contains</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in T item) => FindItemIndex(item) >= 0;

        /// <summary>
        ///     Try to get the actual value
        /// </summary>
        /// <param name="equalValue">Equal value</param>
        /// <param name="actualValue">Actual value</param>
        /// <returns>Got</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(in T equalValue, out T actualValue)
        {
            var index = FindItemIndex(equalValue);
            if (index >= 0)
            {
                actualValue = _handle->Entries[index].Value;
                return true;
            }

            actualValue = default;
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
            var newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize);
            return newSize;
        }

        /// <summary>
        ///     Trim excess
        /// </summary>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TrimExcess()
        {
            var capacity = _handle->Count - _handle->FreeCount;
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
            var count = 0;
            for (var i = 0; i < oldCount; ++i)
            {
                var hashCode = oldEntries[i].HashCode;
                if (oldEntries[i].Next >= -1)
                {
                    ref var entry = ref newEntries[count];
                    entry = oldEntries[i];
                    ref var bucket = ref GetBucketRef(hashCode);
                    entry.Next = bucket - 1;
                    bucket = count + 1;
                    count++;
                }
            }

            NativeMemoryAllocator.Free(oldEntries);
            _handle->Count = capacity;
            _handle->FreeCount = 0;
            return newSize;
        }

        /// <summary>
        ///     Initialize
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Initialize(int capacity)
        {
            var size = HashHelpers.GetPrime(capacity);
            _handle->FreeList = -1;
            _handle->Buckets = (int*)NativeMemoryAllocator.AllocZeroed((uint)(size * sizeof(int)));
            _handle->Entries = (Entry*)NativeMemoryAllocator.AllocZeroed((uint)(size * sizeof(Entry)));
            _handle->BucketsLength = size;
            _handle->EntriesLength = size;
            _handle->FastModMultiplier = IntPtr.Size == 8 ? HashHelpers.GetFastModMultiplier((uint)size) : 0;
            return size;
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
            var entries = (Entry*)NativeMemoryAllocator.AllocZeroed((uint)(newSize * sizeof(Entry)));
            var count = _handle->Count;
            Unsafe.CopyBlockUnaligned(entries, _handle->Entries, (uint)(_handle->EntriesLength * sizeof(Entry)));
            var buckets = (int*)NativeMemoryAllocator.AllocZeroed((uint)(newSize * sizeof(int)));
            NativeMemoryAllocator.Free(_handle->Buckets);
            _handle->Buckets = buckets;
            _handle->BucketsLength = newSize;
            _handle->FastModMultiplier = IntPtr.Size == 8 ? HashHelpers.GetFastModMultiplier((uint)newSize) : 0;
            for (var i = 0; i < count; ++i)
            {
                ref var entry = ref entries[i];
                if (entry.Next >= -1)
                {
                    ref var bucket = ref GetBucketRef(entry.HashCode);
                    entry.Next = bucket - 1;
                    bucket = i + 1;
                }
            }

            NativeMemoryAllocator.Free(_handle->Entries);
            _handle->Entries = entries;
            _handle->EntriesLength = newSize;
        }

        /// <summary>
        ///     Find item index
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindItemIndex(in T item)
        {
            uint collisionCount = 0;
            var hashCode = item.GetHashCode();
            var i = GetBucketRef(hashCode) - 1;
            while (i >= 0)
            {
                ref var entry = ref _handle->Entries[i];
                if (entry.HashCode == hashCode && entry.Value.Equals(item))
                    return i;
                i = entry.Next;
                collisionCount++;
                if (collisionCount > (uint)_handle->EntriesLength)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
            }

            return -1;
        }

        /// <summary>
        ///     Get bucket ref
        /// </summary>
        /// <param name="hashCode">HashCode</param>
        /// <returns>Bucket ref</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucketRef(int hashCode) => ref IntPtr.Size == 8 ? ref _handle->Buckets[HashHelpers.FastMod((uint)hashCode, (uint)_handle->BucketsLength, _handle->FastModMultiplier)] : ref _handle->Buckets[(uint)hashCode % (uint)_handle->BucketsLength];

        /// <summary>
        ///     Entry
        /// </summary>
        private struct Entry
        {
            /// <summary>
            ///     HashCode
            /// </summary>
            public int HashCode;

            /// <summary>
            ///     Next
            /// </summary>
            public int Next;

            /// <summary>
            ///     Value
            /// </summary>
            public T Value;
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeHashSet<T> Empty => new();

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
            ///     NativeHashSet
            /// </summary>
            private readonly NativeHashSet<T> _nativeHashSet;

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
            private T _current;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="nativeHashSet">NativeHashSet</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(in NativeHashSet<T> nativeHashSet)
            {
                _nativeHashSet = nativeHashSet;
                _version = nativeHashSet._handle->Version;
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
                if (_version != _nativeHashSet._handle->Version)
                    throw new InvalidOperationException("EnumFailedVersion");
                while ((uint)_index < (uint)_nativeHashSet._handle->Count)
                {
                    ref var entry = ref _nativeHashSet._handle->Entries[_index++];
                    if (entry.Next >= -1)
                    {
                        _current = entry.Value;
                        return true;
                    }
                }

                _index = _nativeHashSet._handle->Count + 1;
                _current = default;
                return false;
            }

            /// <summary>
            ///     Current
            /// </summary>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
        }
    }
}