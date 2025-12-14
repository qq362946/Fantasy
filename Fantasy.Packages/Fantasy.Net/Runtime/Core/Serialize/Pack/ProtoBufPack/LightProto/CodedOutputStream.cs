#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd
#endregion

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace LightProto
{
    /// <summary>
    /// Encodes and writes protocol message fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is generally used by generated code to write appropriate
    /// primitives to the stream. It effectively encapsulates the lowest
    /// levels of protocol buffer format. Unlike some other implementations,
    /// this does not include combined "write tag and value" methods. Generated
    /// code knows the exact byte representations of the tags they're going to write,
    /// so there's no need to re-encode them each time. Manually-written code calling
    /// this class should just call one of the <c>WriteTag</c> overloads before each value.
    /// </para>
    /// <para>
    /// Repeated fields and map fields are not handled by this class; use <c>RepeatedField&lt;T&gt;</c>
    /// and <c>MapField&lt;TKey, TValue&gt;</c> to serialize such fields.
    /// </para>
    /// </remarks>
    [SecuritySafeCritical]
    public sealed partial class CodedOutputStream : IDisposable
    {
        /// <summary>
        /// The buffer size used by CreateInstance(Stream).
        /// </summary>
        public static readonly int DefaultBufferSize = 4096;

        /// <summary>
        /// Whether to leave the underlying stream open when disposing of this stream.
        /// This is always true when there's no stream.
        /// </summary>
        private readonly bool leaveOpen;

        /// <summary>
        /// Whether the buffer needs to be returned to ArrayPool
        /// </summary>
        private readonly bool needsReturnToPool;

        private readonly byte[] buffer;
        private WriterInternalState state;

        private readonly Stream? output;

        [ThreadStatic]
        private static CodedOutputStream? _cachedInstance;

        #region Construction

        /// <summary>
        /// Gets a pooled <see cref="CodedOutputStream"/> instance from thread-local cache.
        /// This significantly reduces GC pressure in high-throughput scenarios.
        /// </summary>
        internal static CodedOutputStream GetPooled(Stream output, bool leaveOpen)
        {
            var instance = _cachedInstance;
            if (instance != null)
            {
                _cachedInstance = null;
                instance.Reset(output, leaveOpen);
                return instance;
            }

            return new CodedOutputStream(output, leaveOpen);
        }

        /// <summary>
        /// Returns the instance to the thread-local pool for reuse.
        /// </summary>
        private void ReturnToPool()
        {
            if (_cachedInstance == null)
            {
                _cachedInstance = this;
            }
        }

        /// <summary>
        /// Creates a new CodedOutputStream that writes directly to the given
        /// byte array. If more bytes are written than fit in the array,
        /// OutOfSpaceException will be thrown.
        /// </summary>
        public CodedOutputStream(byte[] flatArray)
            : this(flatArray, 0, flatArray.Length) { }

        /// <summary>
        /// Creates a new CodedOutputStream that writes directly to the given
        /// byte array slice. If more bytes are written than fit in the array,
        /// OutOfSpaceException will be thrown.
        /// </summary>
        private CodedOutputStream(byte[] buffer, int offset, int length)
        {
            this.output = null;
            this.buffer = ProtoPreconditions.CheckNotNull(buffer, nameof(buffer));
            this.state.position = offset;
            this.state.limit = offset + length;
            WriteBufferHelper.Initialize(this, out this.state.writeBufferHelper);
            leaveOpen = true; // Simple way of avoiding trying to dispose of a null reference
        }

        private CodedOutputStream(Stream output, byte[] buffer, bool leaveOpen, bool needsReturnToPool = false)
        {
            this.output = output;
            this.buffer = buffer;
            this.state.position = 0;
            this.state.limit = buffer.Length;
            WriteBufferHelper.Initialize(this, out this.state.writeBufferHelper);
            this.leaveOpen = leaveOpen;
            this.needsReturnToPool = needsReturnToPool;
        }

        /// <summary>
        /// Creates a new CodedOutputStream which write to the given stream.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="leaveOpen">If <c>true</c>, <paramref name="output"/> is left open when the returned <c>CodedOutputStream</c> is disposed;
        /// if <c>false</c>, the provided stream is disposed as well.</param>
        public CodedOutputStream(Stream output, bool leaveOpen)
            : this(
                ProtoPreconditions.CheckNotNull(output, nameof(output)),
                ArrayPool<byte>.Shared.Rent(DefaultBufferSize),
                leaveOpen,
                needsReturnToPool: true
            ) { }

        /// <summary>
        /// Creates a new CodedOutputStream which write to the given stream and uses
        /// the specified buffer size.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="bufferSize">The size of buffer to use internally.</param>
        /// <param name="leaveOpen">If <c>true</c>, <paramref name="output"/> is left open when the returned <c>CodedOutputStream</c> is disposed;
        /// if <c>false</c>, the provided stream is disposed as well.</param>
        public CodedOutputStream(Stream output, int bufferSize, bool leaveOpen)
            : this(
                ProtoPreconditions.CheckNotNull(output, nameof(output)),
                ArrayPool<byte>.Shared.Rent(bufferSize),
                leaveOpen,
                needsReturnToPool: true
            ) { }
        #endregion

        /// <summary>
        /// Resets the CodedOutputStream to write to a new stream (for pooling).
        /// </summary>
        private void Reset(Stream output, bool leaveOpen)
        {
            // Reuse existing buffer if available, otherwise rent a new one
            byte[] buffer;
            if (this.buffer != null && this.buffer.Length >= DefaultBufferSize)
            {
                buffer = this.buffer;
            }
            else
            {
                buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
            }

            // Reset all fields
            Unsafe.AsRef(in this.output) = output;
            Unsafe.AsRef(in this.buffer) = buffer;
            Unsafe.AsRef(in this.leaveOpen) = leaveOpen;
            Unsafe.AsRef(in this.needsReturnToPool) = true;

            // Reset state
            this.state = default;
            this.state.position = 0;
            this.state.limit = buffer.Length;
            WriteBufferHelper.Initialize(this, out this.state.writeBufferHelper);
        }

        /// <summary>
        /// Indicates that a CodedOutputStream wrapping a flat byte array
        /// ran out of space.
        /// </summary>
        public sealed class OutOfSpaceException : IOException
        {
            internal OutOfSpaceException()
                : base("CodedOutputStream was writing to a flat byte array and ran out of space.")
            { }
        }

        /// <summary>
        /// Flushes any buffered data and optionally closes the underlying stream, if any.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, any underlying stream is closed by this method. To configure this behaviour,
        /// use a constructor overload with a <c>leaveOpen</c> parameter. If this instance does not
        /// have an underlying stream, this method does nothing.
        /// </para>
        /// <para>
        /// For the sake of efficiency, calling this method does not prevent future write calls - but
        /// if a later write ends up writing to a stream which has been disposed, that is likely to
        /// fail. It is recommend that you not call any other methods after this.
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            Flush();
            if (!leaveOpen)
            {
                output?.Dispose();
            }

            if (needsReturnToPool)
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
            }

            ReturnToPool();
        }

        /// <summary>
        /// Flushes any buffered data to the underlying stream (if there is one).
        /// </summary>
        public void Flush()
        {
            var span = new Span<byte>(buffer);
            WriteBufferHelper.Flush(ref span, ref state);
        }

        internal byte[] InternalBuffer => buffer;

        internal Stream? InternalOutputStream => output;

        internal ref WriterInternalState InternalState => ref state;
    }
}
