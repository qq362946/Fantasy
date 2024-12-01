using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !NET6_0_OR_GREATER
using System.Security.Cryptography;
#endif

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
    ///     Native concurrentStack
    ///     (Slower than ConcurrentStack, disable Enumerator, try peek, push/pop range either)
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeConcurrentStack<T> : IDisposable, IEquatable<NativeConcurrentStack<T>> where T : unmanaged
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeConcurrentStackHandle
        {
            /// <summary>
            ///     Head
            /// </summary>
            public volatile nint Head;

            /// <summary>
            ///     Node pool
            /// </summary>
            public NativeMemoryPool NodePool;

            /// <summary>
            ///     Node pool lock
            /// </summary>
            public NativeConcurrentSpinLock NodePoolLock;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeConcurrentStackHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="size">Size</param>
        /// <param name="maxFreeSlabs">Max free slabs</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeConcurrentStack(int size, int maxFreeSlabs)
        {
            var nodePool = new NativeMemoryPool(size, sizeof(Node), maxFreeSlabs);
            _handle = (NativeConcurrentStackHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeConcurrentStackHandle));
            _handle->Head = IntPtr.Zero;
            _handle->NodePool = nodePool;
            _handle->NodePoolLock = new NativeConcurrentSpinLock(-1);
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     IsEmpty
        /// </summary>
        public bool IsEmpty => _handle->Head == IntPtr.Zero;

        /// <summary>
        ///     Count
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var count = 0;
                for (var node = (Node*)_handle->Head; node != null; node = node->Next)
                    count++;
                return count;
            }
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeConcurrentStack<T> other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeConcurrentStack<T> nativeConcurrentStack && nativeConcurrentStack == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeConcurrentStack<{typeof(T).Name}>";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeConcurrentStack<T> left, NativeConcurrentStack<T> right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeConcurrentStack<T> left, NativeConcurrentStack<T> right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_handle == null)
                return;
            _handle->NodePool.Dispose();
            _handle->NodePoolLock.Dispose();
            NativeMemoryAllocator.Free(_handle);
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _handle->NodePoolLock.Enter();
            try
            {
                var node = (Node*)_handle->Head;
                while (node != null)
                {
                    var temp = node;
                    node = node->Next;
                    _handle->NodePool.Return(temp);
                }
            }
            finally
            {
                _handle->NodePoolLock.Exit();
            }
        }

        /// <summary>
        ///     Push
        /// </summary>
        /// <param name="item">Item</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(in T item)
        {
            Node* newNode;
            _handle->NodePoolLock.Enter();
            try
            {
                newNode = (Node*)_handle->NodePool.Rent();
            }
            finally
            {
                _handle->NodePoolLock.Exit();
            }

            newNode->Value = item;
            newNode->Next = (Node*)_handle->Head;
            if (Interlocked.CompareExchange(ref _handle->Head, (nint)newNode, (nint)newNode->Next) == (nint)newNode->Next)
                return;
            var count = 0;
            do
            {
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
                newNode->Next = (Node*)_handle->Head;
            } while (Interlocked.CompareExchange(ref _handle->Head, (nint)newNode, (nint)newNode->Next) != (nint)newNode->Next);
        }

        /// <summary>
        ///     Try pop
        /// </summary>
        /// <param name="result">Item</param>
        /// <returns>Popped</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T result)
        {
            var head = (Node*)_handle->Head;
            if (head == null)
            {
                result = default;
                return false;
            }

            if (Interlocked.CompareExchange(ref _handle->Head, (nint)head->Next, (nint)head) == (nint)head)
            {
                result = head->Value;
                _handle->NodePoolLock.Enter();
                try
                {
                    _handle->NodePool.Return(head);
                }
                finally
                {
                    _handle->NodePoolLock.Exit();
                }

                return true;
            }

            var count = 0;
            var backoff = 1;
#if !NET6_0_OR_GREATER
            Span<byte> random = stackalloc byte[1];
#endif
            while (true)
            {
                head = (Node*)_handle->Head;
                if (head == null)
                {
                    result = default;
                    return false;
                }

                if (Interlocked.CompareExchange(ref _handle->Head, (nint)head->Next, (nint)head) == (nint)head)
                {
                    result = head->Value;
                    _handle->NodePoolLock.Enter();
                    try
                    {
                        _handle->NodePool.Return(head);
                    }
                    finally
                    {
                        _handle->NodePoolLock.Exit();
                    }

                    return true;
                }

                for (var i = 0; i < backoff; ++i)
                {
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

                if (count >= 10 || Environment.ProcessorCount == 1)
                {
#if NET6_0_OR_GREATER
                    backoff = Random.Shared.Next(1, 8);
#else
                    RandomNumberGenerator.Fill(random);
                    backoff = random[0] % 7 + 1;
#endif
                }
                else
                {
                    backoff *= 2;
                }
            }
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeConcurrentStack<T> Empty => new();

        /// <summary>
        ///     Node
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct Node
        {
            /// <summary>
            ///     Value
            /// </summary>
            public T Value;

            /// <summary>
            ///     Next
            /// </summary>
            public Node* Next;
        }
    }
}