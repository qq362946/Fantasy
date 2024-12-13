using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
using System.Threading;
#endif

#pragma warning disable CA2208
#pragma warning disable CS8632

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     Native concurrentQueue
    ///     (Slower than ConcurrentQueue, disable Enumerator, try peek either)
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeConcurrentQueue<T> : IDisposable, IEquatable<NativeConcurrentQueue<T>> where T : unmanaged
    {
        /// <summary>
        ///     Handle
        /// </summary>
        private void* _handle;

        /// <summary>
        ///     Not arm64
        /// </summary>
        private NativeConcurrentQueueNotArm64<T>* NotArm64Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (NativeConcurrentQueueNotArm64<T>*)_handle;
        }

        /// <summary>
        ///     Arm64
        /// </summary>
        private NativeConcurrentQueueArm64<T>* Arm64Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (NativeConcurrentQueueArm64<T>*)_handle;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="size">Size</param>
        /// <param name="maxFreeSlabs">Max free slabs</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeConcurrentQueue(int size, int maxFreeSlabs)
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            {
                var segmentPool = new NativeMemoryPool(size, sizeof(NativeConcurrentQueueSegmentNotArm64<T>) + NativeConcurrentQueueSegmentNotArm64<T>.LENGTH * sizeof(NativeConcurrentQueueSegmentNotArm64<T>.Slot), maxFreeSlabs);
                _handle = NativeMemoryAllocator.Alloc((uint)sizeof(NativeConcurrentQueueNotArm64<T>));
                NotArm64Handle->Initialize(segmentPool);
            }
            else
            {
                var segmentPool = new NativeMemoryPool(size, sizeof(NativeConcurrentQueueSegmentArm64<T>) + NativeConcurrentQueueSegmentArm64<T>.LENGTH * sizeof(NativeConcurrentQueueSegmentArm64<T>.Slot), maxFreeSlabs);
                _handle = NativeMemoryAllocator.Alloc((uint)sizeof(NativeConcurrentQueueArm64<T>));
                Arm64Handle->Initialize(segmentPool);
            }
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     IsEmpty
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RuntimeInformation.ProcessArchitecture != Architecture.Arm64 ? NotArm64Handle->IsEmpty : Arm64Handle->IsEmpty;
        }

        /// <summary>
        ///     Count
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RuntimeInformation.ProcessArchitecture != Architecture.Arm64 ? NotArm64Handle->Count : Arm64Handle->Count;
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeConcurrentQueue<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeConcurrentQueue<T> nativeConcurrentQueue && nativeConcurrentQueue == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeConcurrentQueue<{typeof(T).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeConcurrentQueue<T> left, NativeConcurrentQueue<T> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeConcurrentQueue<T> left, NativeConcurrentQueue<T> right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_handle == null)
                return;
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
                NotArm64Handle->Dispose();
            else
                Arm64Handle->Dispose();
            NativeMemoryAllocator.Free(_handle);
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
                NotArm64Handle->Clear();
            else
                Arm64Handle->Clear();
        }

        /// <summary>
        ///     Enqueue
        /// </summary>
        /// <param name="item">Item</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(in T item)
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
                NotArm64Handle->Enqueue(item);
            else
                Arm64Handle->Enqueue(item);
        }

        /// <summary>
        ///     Try dequeue
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Dequeued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T result) => RuntimeInformation.ProcessArchitecture != Architecture.Arm64 ? NotArm64Handle->TryDequeue(out result) : Arm64Handle->TryDequeue(out result);

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeConcurrentQueue<T> Empty => new();
    }

    /// <summary>
    ///     Native concurrentQueue
    ///     (Slower than ConcurrentQueue, disable Enumerator, try peek either)
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeConcurrentQueueNotArm64<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        ///     Cross segment lock
        /// </summary>
        private NativeMonitorLock _crossSegmentLock;

        /// <summary>
        ///     Segment pool
        /// </summary>
        private NativeMemoryPool _segmentPool;

        /// <summary>
        ///     Tail
        /// </summary>
        private volatile NativeConcurrentQueueSegmentNotArm64<T>* _tail;

        /// <summary>
        ///     Head
        /// </summary>
        private volatile NativeConcurrentQueueSegmentNotArm64<T>* _head;

        /// <summary>
        ///     IsEmpty
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var segment = _head;
                while (true)
                {
                    var next = Volatile.Read(ref segment->NextSegment);
                    if (segment->TryPeek())
                        return false;
                    if (next != IntPtr.Zero)
                        segment = (NativeConcurrentQueueSegmentNotArm64<T>*)next;
                    else if (Volatile.Read(ref segment->NextSegment) == IntPtr.Zero)
                        break;
                }

                return true;
            }
        }

        /// <summary>
        ///     Count
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var spinCount = 0;
                while (true)
                {
                    var head = _head;
                    var tail = _tail;
                    var headHead = Volatile.Read(ref head->HeadAndTail.Head);
                    var headTail = Volatile.Read(ref head->HeadAndTail.Tail);
                    if (head == tail)
                    {
                        if (head == _head && tail == _tail && headHead == Volatile.Read(ref head->HeadAndTail.Head) && headTail == Volatile.Read(ref head->HeadAndTail.Tail))
                            return GetCount(head, headHead, headTail);
                    }
                    else if ((NativeConcurrentQueueSegmentNotArm64<T>*)head->NextSegment == tail)
                    {
                        var tailHead = Volatile.Read(ref tail->HeadAndTail.Head);
                        var tailTail = Volatile.Read(ref tail->HeadAndTail.Tail);
                        if (head == _head && tail == _tail && headHead == Volatile.Read(ref head->HeadAndTail.Head) && headTail == Volatile.Read(ref head->HeadAndTail.Tail) && tailHead == Volatile.Read(ref tail->HeadAndTail.Head) && tailTail == Volatile.Read(ref tail->HeadAndTail.Tail))
                            return GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);
                    }
                    else
                    {
                        _crossSegmentLock.Enter();
                        try
                        {
                            if (head == _head && tail == _tail)
                            {
                                var tailHead = Volatile.Read(ref tail->HeadAndTail.Head);
                                var tailTail = Volatile.Read(ref tail->HeadAndTail.Tail);
                                if (headHead == Volatile.Read(ref head->HeadAndTail.Head) && headTail == Volatile.Read(ref head->HeadAndTail.Tail) && tailHead == Volatile.Read(ref tail->HeadAndTail.Head) && tailTail == Volatile.Read(ref tail->HeadAndTail.Tail))
                                {
                                    var count = GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);
                                    for (var s = (NativeConcurrentQueueSegmentNotArm64<T>*)head->NextSegment; s != tail; s = (NativeConcurrentQueueSegmentNotArm64<T>*)s->NextSegment)
                                        count += s->HeadAndTail.Tail - NativeConcurrentQueueSegmentNotArm64<T>.FREEZE_OFFSET;
                                    return count;
                                }
                            }
                        }
                        finally
                        {
                            _crossSegmentLock.Exit();
                        }
                    }

                    if ((spinCount >= 10 && (spinCount - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                    {
                        var yieldsSoFar = spinCount >= 10 ? (spinCount - 10) / 2 : spinCount;
                        if (yieldsSoFar % 5 == 4)
                            Thread.Sleep(0);
                        else
                            Thread.Yield();
                    }
                    else
                    {
                        var iterations = Environment.ProcessorCount / 2;
                        if (spinCount <= 30 && 1 << spinCount < iterations)
                            iterations = 1 << spinCount;
                        Thread.SpinWait(iterations);
                    }

                    spinCount = spinCount == int.MaxValue ? 10 : spinCount + 1;
                }
            }
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="segmentPool">Segment pool</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(NativeMemoryPool segmentPool)
        {
            _crossSegmentLock = new NativeMonitorLock(new object());
            _segmentPool = segmentPool;
            var segment = (NativeConcurrentQueueSegmentNotArm64<T>*)_segmentPool.Rent();
            var array = (byte*)segment + sizeof(NativeConcurrentQueueSegmentNotArm64<T>);
            segment->Initialize((NativeConcurrentQueueSegmentNotArm64<T>.Slot*)array);
            _tail = _head = segment;
        }

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _crossSegmentLock.Dispose();
            _segmentPool.Dispose();
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _crossSegmentLock.Enter();
            try
            {
                _tail->EnsureFrozenForEnqueues();
                var node = _head;
                while (node != null)
                {
                    var temp = node;
                    node = (NativeConcurrentQueueSegmentNotArm64<T>*)node->NextSegment;
                    _segmentPool.Return(temp);
                }

                var segment = (NativeConcurrentQueueSegmentNotArm64<T>*)_segmentPool.Rent();
                var array = (byte*)segment + sizeof(NativeConcurrentQueueSegmentNotArm64<T>);
                segment->Initialize((NativeConcurrentQueueSegmentNotArm64<T>.Slot*)array);
                _tail = _head = segment;
            }
            finally
            {
                _crossSegmentLock.Exit();
            }
        }

        /// <summary>
        ///     Enqueue
        /// </summary>
        /// <param name="item">Item</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(in T item)
        {
            if (!_tail->TryEnqueue(item))
            {
                while (true)
                {
                    var tail = _tail;
                    if (tail->TryEnqueue(item))
                        return;
                    _crossSegmentLock.Enter();
                    try
                    {
                        if (tail == _tail)
                        {
                            tail->EnsureFrozenForEnqueues();
                            var newTail = (NativeConcurrentQueueSegmentNotArm64<T>*)_segmentPool.Rent();
                            var array = (byte*)newTail + sizeof(NativeConcurrentQueueSegmentNotArm64<T>);
                            newTail->Initialize((NativeConcurrentQueueSegmentNotArm64<T>.Slot*)array);
                            tail->NextSegment = (nint)newTail;
                            _tail = newTail;
                        }
                    }
                    finally
                    {
                        _crossSegmentLock.Exit();
                    }
                }
            }
        }

        /// <summary>
        ///     Try dequeue
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Dequeued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T result)
        {
            var head = _head;
            if (head->TryDequeue(out result))
                return true;
            if (head->NextSegment == IntPtr.Zero)
            {
                result = default;
                return false;
            }

            while (true)
            {
                head = _head;
                if (head->TryDequeue(out result))
                    return true;
                if (head->NextSegment == IntPtr.Zero)
                {
                    result = default;
                    return false;
                }

                if (head->TryDequeue(out result))
                    return true;
                _crossSegmentLock.Enter();
                try
                {
                    if (head == _head)
                    {
                        _head = (NativeConcurrentQueueSegmentNotArm64<T>*)head->NextSegment;
                        _segmentPool.Return(head);
                    }
                }
                finally
                {
                    _crossSegmentLock.Exit();
                }
            }
        }

        /// <summary>
        ///     Get count
        /// </summary>
        /// <param name="segment">Segment</param>
        /// <param name="head">Head</param>
        /// <param name="tail">Tail</param>
        /// <returns>Count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCount(NativeConcurrentQueueSegmentNotArm64<T>* segment, int head, int tail)
        {
            if (head != tail && head != tail - NativeConcurrentQueueSegmentNotArm64<T>.FREEZE_OFFSET)
            {
                head &= NativeConcurrentQueueSegmentNotArm64<T>.SLOTS_MASK;
                tail &= NativeConcurrentQueueSegmentNotArm64<T>.SLOTS_MASK;
                return head < tail ? tail - head : NativeConcurrentQueueSegmentNotArm64<T>.LENGTH - head + tail;
            }

            return 0;
        }

        /// <summary>
        ///     Get count
        /// </summary>
        /// <param name="head">Head</param>
        /// <param name="headHead">Head head</param>
        /// <param name="tail">Tail</param>
        /// <param name="tailTail">Tail tail</param>
        /// <returns>Count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetCount(NativeConcurrentQueueSegmentNotArm64<T>* head, int headHead, NativeConcurrentQueueSegmentNotArm64<T>* tail, int tailTail)
        {
            long count = 0;
            var headTail = (head == tail ? tailTail : Volatile.Read(ref head->HeadAndTail.Tail)) - NativeConcurrentQueueSegmentNotArm64<T>.FREEZE_OFFSET;
            if (headHead < headTail)
            {
                headHead &= NativeConcurrentQueueSegmentNotArm64<T>.SLOTS_MASK;
                headTail &= NativeConcurrentQueueSegmentNotArm64<T>.SLOTS_MASK;
                count += headHead < headTail ? headTail - headHead : NativeConcurrentQueueSegmentNotArm64<T>.LENGTH - headHead + headTail;
            }

            if (head != tail)
            {
                for (var s = (NativeConcurrentQueueSegmentNotArm64<T>*)head->NextSegment; s != tail; s = (NativeConcurrentQueueSegmentNotArm64<T>*)s->NextSegment)
                    count += s->HeadAndTail.Tail - NativeConcurrentQueueSegmentNotArm64<T>.FREEZE_OFFSET;
                count += tailTail - NativeConcurrentQueueSegmentNotArm64<T>.FREEZE_OFFSET;
            }

            return count;
        }
    }

    /// <summary>
    ///     Native concurrentQueue segment
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeConcurrentQueueSegmentNotArm64<T> where T : unmanaged
    {
        /// <summary>
        ///     Slots
        /// </summary>
        public Slot* Slots;

        /// <summary>
        ///     Length
        /// </summary>
        public const int LENGTH = 1024;

        /// <summary>
        ///     Slots mask
        /// </summary>
        public const int SLOTS_MASK = LENGTH - 1;

        /// <summary>
        ///     Head and tail
        /// </summary>
        public NativeConcurrentQueuePaddedHeadAndTailNotArm64 HeadAndTail;

        /// <summary>
        ///     Frozen for enqueues
        /// </summary>
        public bool FrozenForEnqueues;

        /// <summary>
        ///     Next segment
        /// </summary>
        public nint NextSegment;

        /// <summary>
        ///     Initialize
        /// </summary>
        /// <param name="slots">Slots</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(Slot* slots)
        {
            Slots = slots;
            for (var i = 0; i < LENGTH; ++i)
                Slots[i].SequenceNumber = i;
            HeadAndTail = new NativeConcurrentQueuePaddedHeadAndTailNotArm64();
            FrozenForEnqueues = false;
            NextSegment = IntPtr.Zero;
        }

        /// <summary>
        ///     Freeze offset
        /// </summary>
        public const int FREEZE_OFFSET = LENGTH * 2;

        /// <summary>
        ///     Ensure frozen for enqueues
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureFrozenForEnqueues()
        {
            if (!FrozenForEnqueues)
            {
                FrozenForEnqueues = true;
                Interlocked.Add(ref HeadAndTail.Tail, FREEZE_OFFSET);
            }
        }

        /// <summary>
        ///     Try dequeue
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Dequeued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T result)
        {
            var slots = Slots;
            var count = 0;
            while (true)
            {
                var currentHead = Volatile.Read(ref HeadAndTail.Head);
                var slotsIndex = currentHead & SLOTS_MASK;
                var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
                var diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                {
                    if (Interlocked.CompareExchange(ref HeadAndTail.Head, currentHead + 1, currentHead) == currentHead)
                    {
                        result = slots[slotsIndex].Item;
                        Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + LENGTH);
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    var frozen = FrozenForEnqueues;
                    var currentTail = Volatile.Read(ref HeadAndTail.Tail);
                    if (currentTail - currentHead <= 0 || (frozen && currentTail - FREEZE_OFFSET - currentHead <= 0))
                    {
                        result = default;
                        return false;
                    }

                    if ((count >= 10 && (count - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                    {
                        var yieldsSoFar = count >= 10 ? (count - 10) / 2 : count;
                        if (yieldsSoFar % 5 == 4)
                            Thread.Sleep(0);
                        else
                            Thread.Yield();
                    }
                    else
                    {
                        var iterations = Environment.ProcessorCount / 2;
                        if (count <= 30 && 1 << count < iterations)
                            iterations = 1 << count;
                        Thread.SpinWait(iterations);
                    }

                    count = count == int.MaxValue ? 10 : count + 1;
                }
            }
        }

        /// <summary>
        ///     Try peek
        /// </summary>
        /// <returns>Peeked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek()
        {
            var slots = Slots;
            var count = 0;
            while (true)
            {
                var currentHead = Volatile.Read(ref HeadAndTail.Head);
                var slotsIndex = currentHead & SLOTS_MASK;
                var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
                var diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                    return true;
                if (diff < 0)
                {
                    var frozen = FrozenForEnqueues;
                    var currentTail = Volatile.Read(ref HeadAndTail.Tail);
                    if (currentTail - currentHead <= 0 || (frozen && currentTail - FREEZE_OFFSET - currentHead <= 0))
                        return false;
                    if ((count >= 10 && (count - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                    {
                        var yieldsSoFar = count >= 10 ? (count - 10) / 2 : count;
                        if (yieldsSoFar % 5 == 4)
                            Thread.Sleep(0);
                        else
                            Thread.Yield();
                    }
                    else
                    {
                        var iterations = Environment.ProcessorCount / 2;
                        if (count <= 30 && 1 << count < iterations)
                            iterations = 1 << count;
                        Thread.SpinWait(iterations);
                    }

                    count = count == int.MaxValue ? 10 : count + 1;
                }
            }
        }

        /// <summary>
        ///     Try enqueue
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Enqueued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(in T item)
        {
            var slots = Slots;
            while (true)
            {
                var currentTail = Volatile.Read(ref HeadAndTail.Tail);
                var slotsIndex = currentTail & SLOTS_MASK;
                var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
                var diff = sequenceNumber - currentTail;
                if (diff == 0)
                {
                    if (Interlocked.CompareExchange(ref HeadAndTail.Tail, currentTail + 1, currentTail) == currentTail)
                    {
                        slots[slotsIndex].Item = item;
                        Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentTail + 1);
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Slot
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Slot
        {
            /// <summary>
            ///     Item
            /// </summary>
            public T Item;

            /// <summary>
            ///     Sequence number
            /// </summary>
            public int SequenceNumber;
        }
    }

    /// <summary>
    ///     NativeConcurrentQueue padded head and tail
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 3 * CACHE_LINE_SIZE)]
    internal struct NativeConcurrentQueuePaddedHeadAndTailNotArm64
    {
        /// <summary>
        ///     Head
        /// </summary>
        [FieldOffset(1 * CACHE_LINE_SIZE)] public int Head;

        /// <summary>
        ///     Tail
        /// </summary>
        [FieldOffset(2 * CACHE_LINE_SIZE)] public int Tail;

        /// <summary>
        ///     Catch line size
        /// </summary>
        public const int CACHE_LINE_SIZE = 64;
    }

    /// <summary>
    ///     Native concurrentQueue
    ///     (Slower than ConcurrentQueue, disable Enumerator, try peek either)
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeConcurrentQueueArm64<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        ///     Cross segment lock
        /// </summary>
        private NativeMonitorLock _crossSegmentLock;

        /// <summary>
        ///     Segment pool
        /// </summary>
        private NativeMemoryPool _segmentPool;

        /// <summary>
        ///     Tail
        /// </summary>
        private volatile NativeConcurrentQueueSegmentArm64<T>* _tail;

        /// <summary>
        ///     Head
        /// </summary>
        private volatile NativeConcurrentQueueSegmentArm64<T>* _head;

        /// <summary>
        ///     IsEmpty
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var segment = _head;
                while (true)
                {
                    var next = Volatile.Read(ref segment->NextSegment);
                    if (segment->TryPeek())
                        return false;
                    if (next != IntPtr.Zero)
                        segment = (NativeConcurrentQueueSegmentArm64<T>*)next;
                    else if (Volatile.Read(ref segment->NextSegment) == IntPtr.Zero)
                        break;
                }

                return true;
            }
        }

        /// <summary>
        ///     Count
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var spinCount = 0;
                while (true)
                {
                    var head = _head;
                    var tail = _tail;
                    var headHead = Volatile.Read(ref head->HeadAndTail.Head);
                    var headTail = Volatile.Read(ref head->HeadAndTail.Tail);
                    if (head == tail)
                    {
                        if (head == _head && tail == _tail && headHead == Volatile.Read(ref head->HeadAndTail.Head) && headTail == Volatile.Read(ref head->HeadAndTail.Tail))
                            return GetCount(head, headHead, headTail);
                    }
                    else if ((NativeConcurrentQueueSegmentArm64<T>*)head->NextSegment == tail)
                    {
                        var tailHead = Volatile.Read(ref tail->HeadAndTail.Head);
                        var tailTail = Volatile.Read(ref tail->HeadAndTail.Tail);
                        if (head == _head && tail == _tail && headHead == Volatile.Read(ref head->HeadAndTail.Head) && headTail == Volatile.Read(ref head->HeadAndTail.Tail) && tailHead == Volatile.Read(ref tail->HeadAndTail.Head) && tailTail == Volatile.Read(ref tail->HeadAndTail.Tail))
                            return GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);
                    }
                    else
                    {
                        _crossSegmentLock.Enter();
                        try
                        {
                            if (head == _head && tail == _tail)
                            {
                                var tailHead = Volatile.Read(ref tail->HeadAndTail.Head);
                                var tailTail = Volatile.Read(ref tail->HeadAndTail.Tail);
                                if (headHead == Volatile.Read(ref head->HeadAndTail.Head) && headTail == Volatile.Read(ref head->HeadAndTail.Tail) && tailHead == Volatile.Read(ref tail->HeadAndTail.Head) && tailTail == Volatile.Read(ref tail->HeadAndTail.Tail))
                                {
                                    var count = GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);
                                    for (var s = (NativeConcurrentQueueSegmentArm64<T>*)head->NextSegment; s != tail; s = (NativeConcurrentQueueSegmentArm64<T>*)s->NextSegment)
                                        count += s->HeadAndTail.Tail - NativeConcurrentQueueSegmentArm64<T>.FREEZE_OFFSET;
                                    return count;
                                }
                            }
                        }
                        finally
                        {
                            _crossSegmentLock.Exit();
                        }
                    }

                    if ((spinCount >= 10 && (spinCount - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                    {
                        var yieldsSoFar = spinCount >= 10 ? (spinCount - 10) / 2 : spinCount;
                        if (yieldsSoFar % 5 == 4)
                            Thread.Sleep(0);
                        else
                            Thread.Yield();
                    }
                    else
                    {
                        var iterations = Environment.ProcessorCount / 2;
                        if (spinCount <= 30 && 1 << spinCount < iterations)
                            iterations = 1 << spinCount;
                        Thread.SpinWait(iterations);
                    }

                    spinCount = spinCount == int.MaxValue ? 10 : spinCount + 1;
                }
            }
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="segmentPool">Segment pool</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(NativeMemoryPool segmentPool)
        {
            _crossSegmentLock = new NativeMonitorLock(new object());
            _segmentPool = segmentPool;
            var segment = (NativeConcurrentQueueSegmentArm64<T>*)_segmentPool.Rent();
            var array = (byte*)segment + sizeof(NativeConcurrentQueueSegmentArm64<T>);
            segment->Initialize((NativeConcurrentQueueSegmentArm64<T>.Slot*)array);
            _tail = _head = segment;
        }

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _crossSegmentLock.Dispose();
            _segmentPool.Dispose();
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _crossSegmentLock.Enter();
            try
            {
                _tail->EnsureFrozenForEnqueues();
                var node = _head;
                while (node != null)
                {
                    var temp = node;
                    node = (NativeConcurrentQueueSegmentArm64<T>*)node->NextSegment;
                    _segmentPool.Return(temp);
                }

                var segment = (NativeConcurrentQueueSegmentArm64<T>*)_segmentPool.Rent();
                var array = (byte*)segment + sizeof(NativeConcurrentQueueSegmentArm64<T>);
                segment->Initialize((NativeConcurrentQueueSegmentArm64<T>.Slot*)array);
                _tail = _head = segment;
            }
            finally
            {
                _crossSegmentLock.Exit();
            }
        }

        /// <summary>
        ///     Enqueue
        /// </summary>
        /// <param name="item">Item</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(in T item)
        {
            if (!_tail->TryEnqueue(item))
            {
                while (true)
                {
                    var tail = _tail;
                    if (tail->TryEnqueue(item))
                        return;
                    _crossSegmentLock.Enter();
                    try
                    {
                        if (tail == _tail)
                        {
                            tail->EnsureFrozenForEnqueues();
                            var newTail = (NativeConcurrentQueueSegmentArm64<T>*)_segmentPool.Rent();
                            var array = (byte*)newTail + sizeof(NativeConcurrentQueueSegmentArm64<T>);
                            newTail->Initialize((NativeConcurrentQueueSegmentArm64<T>.Slot*)array);
                            tail->NextSegment = (nint)newTail;
                            _tail = newTail;
                        }
                    }
                    finally
                    {
                        _crossSegmentLock.Exit();
                    }
                }
            }
        }

        /// <summary>
        ///     Try dequeue
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Dequeued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T result)
        {
            var head = _head;
            if (head->TryDequeue(out result))
                return true;
            if (head->NextSegment == IntPtr.Zero)
            {
                result = default;
                return false;
            }

            while (true)
            {
                head = _head;
                if (head->TryDequeue(out result))
                    return true;
                if (head->NextSegment == IntPtr.Zero)
                {
                    result = default;
                    return false;
                }

                if (head->TryDequeue(out result))
                    return true;
                _crossSegmentLock.Enter();
                try
                {
                    if (head == _head)
                    {
                        _head = (NativeConcurrentQueueSegmentArm64<T>*)head->NextSegment;
                        _segmentPool.Return(head);
                    }
                }
                finally
                {
                    _crossSegmentLock.Exit();
                }
            }
        }

        /// <summary>
        ///     Get count
        /// </summary>
        /// <param name="segment">Segment</param>
        /// <param name="head">Head</param>
        /// <param name="tail">Tail</param>
        /// <returns>Count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCount(NativeConcurrentQueueSegmentArm64<T>* segment, int head, int tail)
        {
            if (head != tail && head != tail - NativeConcurrentQueueSegmentArm64<T>.FREEZE_OFFSET)
            {
                head &= NativeConcurrentQueueSegmentArm64<T>.SLOTS_MASK;
                tail &= NativeConcurrentQueueSegmentArm64<T>.SLOTS_MASK;
                return head < tail ? tail - head : NativeConcurrentQueueSegmentArm64<T>.LENGTH - head + tail;
            }

            return 0;
        }

        /// <summary>
        ///     Get count
        /// </summary>
        /// <param name="head">Head</param>
        /// <param name="headHead">Head head</param>
        /// <param name="tail">Tail</param>
        /// <param name="tailTail">Tail tail</param>
        /// <returns>Count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetCount(NativeConcurrentQueueSegmentArm64<T>* head, int headHead, NativeConcurrentQueueSegmentArm64<T>* tail, int tailTail)
        {
            long count = 0;
            var headTail = (head == tail ? tailTail : Volatile.Read(ref head->HeadAndTail.Tail)) - NativeConcurrentQueueSegmentArm64<T>.FREEZE_OFFSET;
            if (headHead < headTail)
            {
                headHead &= NativeConcurrentQueueSegmentArm64<T>.SLOTS_MASK;
                headTail &= NativeConcurrentQueueSegmentArm64<T>.SLOTS_MASK;
                count += headHead < headTail ? headTail - headHead : NativeConcurrentQueueSegmentArm64<T>.LENGTH - headHead + headTail;
            }

            if (head != tail)
            {
                for (var s = (NativeConcurrentQueueSegmentArm64<T>*)head->NextSegment; s != tail; s = (NativeConcurrentQueueSegmentArm64<T>*)s->NextSegment)
                    count += s->HeadAndTail.Tail - NativeConcurrentQueueSegmentArm64<T>.FREEZE_OFFSET;
                count += tailTail - NativeConcurrentQueueSegmentArm64<T>.FREEZE_OFFSET;
            }

            return count;
        }
    }

    /// <summary>
    ///     Native concurrentQueue segment
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeConcurrentQueueSegmentArm64<T> where T : unmanaged
    {
        /// <summary>
        ///     Slots
        /// </summary>
        public Slot* Slots;

        /// <summary>
        ///     Length
        /// </summary>
        public const int LENGTH = 1024;

        /// <summary>
        ///     Slots mask
        /// </summary>
        public const int SLOTS_MASK = LENGTH - 1;

        /// <summary>
        ///     Head and tail
        /// </summary>
        public NativeConcurrentQueuePaddedHeadAndTailArm64 HeadAndTail;

        /// <summary>
        ///     Frozen for enqueues
        /// </summary>
        public bool FrozenForEnqueues;

        /// <summary>
        ///     Next segment
        /// </summary>
        public nint NextSegment;

        /// <summary>
        ///     Initialize
        /// </summary>
        /// <param name="slots">Slots</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(Slot* slots)
        {
            Slots = slots;
            for (var i = 0; i < LENGTH; ++i)
                Slots[i].SequenceNumber = i;
            HeadAndTail = new NativeConcurrentQueuePaddedHeadAndTailArm64();
            FrozenForEnqueues = false;
            NextSegment = IntPtr.Zero;
        }

        /// <summary>
        ///     Freeze offset
        /// </summary>
        public const int FREEZE_OFFSET = LENGTH * 2;

        /// <summary>
        ///     Ensure frozen for enqueues
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureFrozenForEnqueues()
        {
            if (!FrozenForEnqueues)
            {
                FrozenForEnqueues = true;
                Interlocked.Add(ref HeadAndTail.Tail, FREEZE_OFFSET);
            }
        }

        /// <summary>
        ///     Try dequeue
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Dequeued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T result)
        {
            var slots = Slots;
            var count = 0;
            while (true)
            {
                var currentHead = Volatile.Read(ref HeadAndTail.Head);
                var slotsIndex = currentHead & SLOTS_MASK;
                var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
                var diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                {
                    if (Interlocked.CompareExchange(ref HeadAndTail.Head, currentHead + 1, currentHead) == currentHead)
                    {
                        result = slots[slotsIndex].Item;
                        Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + LENGTH);
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    var frozen = FrozenForEnqueues;
                    var currentTail = Volatile.Read(ref HeadAndTail.Tail);
                    if (currentTail - currentHead <= 0 || (frozen && currentTail - FREEZE_OFFSET - currentHead <= 0))
                    {
                        result = default;
                        return false;
                    }

                    if ((count >= 10 && (count - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                    {
                        var yieldsSoFar = count >= 10 ? (count - 10) / 2 : count;
                        if (yieldsSoFar % 5 == 4)
                            Thread.Sleep(0);
                        else
                            Thread.Yield();
                    }
                    else
                    {
                        var iterations = Environment.ProcessorCount / 2;
                        if (count <= 30 && 1 << count < iterations)
                            iterations = 1 << count;
                        Thread.SpinWait(iterations);
                    }

                    count = count == int.MaxValue ? 10 : count + 1;
                }
            }
        }

        /// <summary>
        ///     Try peek
        /// </summary>
        /// <returns>Peeked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek()
        {
            var slots = Slots;
            var count = 0;
            while (true)
            {
                var currentHead = Volatile.Read(ref HeadAndTail.Head);
                var slotsIndex = currentHead & SLOTS_MASK;
                var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
                var diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                    return true;
                if (diff < 0)
                {
                    var frozen = FrozenForEnqueues;
                    var currentTail = Volatile.Read(ref HeadAndTail.Tail);
                    if (currentTail - currentHead <= 0 || (frozen && currentTail - FREEZE_OFFSET - currentHead <= 0))
                        return false;
                    if ((count >= 10 && (count - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                    {
                        var yieldsSoFar = count >= 10 ? (count - 10) / 2 : count;
                        if (yieldsSoFar % 5 == 4)
                            Thread.Sleep(0);
                        else
                            Thread.Yield();
                    }
                    else
                    {
                        var iterations = Environment.ProcessorCount / 2;
                        if (count <= 30 && 1 << count < iterations)
                            iterations = 1 << count;
                        Thread.SpinWait(iterations);
                    }

                    count = count == int.MaxValue ? 10 : count + 1;
                }
            }
        }

        /// <summary>
        ///     Try enqueue
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Enqueued</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(in T item)
        {
            var slots = Slots;
            while (true)
            {
                var currentTail = Volatile.Read(ref HeadAndTail.Tail);
                var slotsIndex = currentTail & SLOTS_MASK;
                var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
                var diff = sequenceNumber - currentTail;
                if (diff == 0)
                {
                    if (Interlocked.CompareExchange(ref HeadAndTail.Tail, currentTail + 1, currentTail) == currentTail)
                    {
                        slots[slotsIndex].Item = item;
                        Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentTail + 1);
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Slot
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Slot
        {
            /// <summary>
            ///     Item
            /// </summary>
            public T Item;

            /// <summary>
            ///     Sequence number
            /// </summary>
            public int SequenceNumber;
        }
    }

    /// <summary>
    ///     NativeConcurrentQueue padded head and tail
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 3 * CACHE_LINE_SIZE)]
    internal struct NativeConcurrentQueuePaddedHeadAndTailArm64
    {
        /// <summary>
        ///     Head
        /// </summary>
        [FieldOffset(1 * CACHE_LINE_SIZE)] public int Head;

        /// <summary>
        ///     Tail
        /// </summary>
        [FieldOffset(2 * CACHE_LINE_SIZE)] public int Tail;

        /// <summary>
        ///     Catch line size
        /// </summary>
        public const int CACHE_LINE_SIZE = 128;
    }
}