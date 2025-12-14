#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd
#endregion

#nullable enable
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
#pragma warning disable CS1574, CS1584, CS1581, CS1580

namespace LightProto
{
    /// <summary>
    /// Reads and decodes protocol message fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is generally used by generated code to read appropriate
    /// primitives from the stream. It effectively encapsulates the lowest
    /// levels of protocol buffer format.
    /// </para>
    /// <para>
    /// Repeated fields and map fields are not handled by this class; use <see cref="RepeatedField{T}"/>
    /// and <see cref="MapField{TKey, TValue}"/> to serialize such fields.
    /// </para>
    /// </remarks>
    [SecuritySafeCritical]
    internal sealed class CodedInputStream : IDisposable
    {
        /// <summary>
        /// Whether to leave the underlying stream open when disposing of this stream.
        /// This is always true when there's no stream.
        /// </summary>
        private readonly bool leaveOpen;

        /// <summary>
        /// Whether the buffer needs to be returned to ArrayPool
        /// </summary>
        private readonly bool needsReturnToPool;

        internal int leftSize;

        /// <summary>
        /// Buffer of data read from the stream or provided at construction time.
        /// </summary>
        private readonly byte[] buffer;

        /// <summary>
        /// The stream to read further input from, or null if the byte array buffer was provided
        /// directly on construction, with no further data available.
        /// </summary>
        private readonly Stream input;

        /// <summary>
        /// The parser state is kept separately so that other parse implementations can reuse the same
        /// parsing primitives.
        /// </summary>
        private ParserInternalState state;

        internal const int DefaultRecursionLimit = 100;
        internal const int DefaultSizeLimit = Int32.MaxValue;
        internal const int BufferSize = 4096;

        [ThreadStatic]
        private static CodedInputStream? _cachedInstance;

        #region Construction

        /// <summary>
        /// Gets a pooled <see cref="CodedInputStream"/> instance from thread-local cache.
        /// This significantly reduces GC pressure in high-throughput scenarios.
        /// </summary>
        internal static CodedInputStream GetPooled(Stream input, bool leaveOpen, int maxSize = int.MaxValue)
        {
            var instance = _cachedInstance;
            if (instance != null)
            {
                _cachedInstance = null;
                instance.Reset(input, leaveOpen, maxSize);
                return instance;
            }

            return new CodedInputStream(input, leaveOpen, maxSize);
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
        /// Creates a new <see cref="CodedInputStream"/> reading data from the given stream.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <param name="leaveOpen"><c>true</c> to leave <paramref name="input"/> open when the returned
        /// <c cref="CodedInputStream"/> is disposed; <c>false</c> to dispose of the given stream when the
        /// returned object is disposed.</param>
        /// <param name="maxSize"></param>
        public CodedInputStream(Stream input, bool leaveOpen, int maxSize = int.MaxValue)
            : this(
                ProtoPreconditions.CheckNotNull(input, "input"),
                ArrayPool<byte>.Shared.Rent(BufferSize),
                0,
                0,
                leaveOpen,
                maxSize,
                needsReturnToPool: true
            ) { }

        /// <summary>
        /// Creates a new CodedInputStream reading data from the given
        /// stream and buffer, using the default limits.
        /// </summary>
        internal CodedInputStream(
            Stream input,
            byte[] buffer,
            int bufferPos,
            int bufferSize,
            bool leaveOpen,
            int maxSize,
            bool needsReturnToPool = false
        )
        {
            this.input = input;
            this.buffer = buffer;
            this.state.bufferPos = bufferPos;
            this.state.bufferSize = bufferSize;
            this.state.sizeLimit = DefaultSizeLimit;
            this.state.recursionLimit = DefaultRecursionLimit;
            SegmentedBufferHelper.Initialize(this, out this.state.segmentedBufferHelper);
            this.leaveOpen = leaveOpen;
            this.needsReturnToPool = needsReturnToPool;
            this.state.currentLimit = int.MaxValue;
            this.leftSize = maxSize;
        }

        #endregion

        /// <summary>
        /// Resets the CodedInputStream to read from a new stream (for pooling).
        /// </summary>
        private void Reset(Stream input, bool leaveOpen, int maxSize)
        {
            // Reuse existing buffer if available, otherwise rent a new one
            byte[] buffer;
            if (this.buffer != null && this.buffer.Length >= BufferSize)
            {
                buffer = this.buffer;
            }
            else
            {
                buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            }

            // Reset all fields (mimic constructor)
            Unsafe.AsRef(in this.input) = input;
            Unsafe.AsRef(in this.buffer) = buffer;
            Unsafe.AsRef(in this.leaveOpen) = leaveOpen;
            Unsafe.AsRef(in this.needsReturnToPool) = true;
            Unsafe.AsRef(in this.leftSize) = maxSize;

            // Reset state
            this.state = default;
            this.state.bufferPos = 0;
            this.state.bufferSize = 0;
            this.state.sizeLimit = DefaultSizeLimit;
            this.state.recursionLimit = DefaultRecursionLimit;
            this.state.currentLimit = int.MaxValue;
            SegmentedBufferHelper.Initialize(this, out this.state.segmentedBufferHelper);
        }

        internal byte[] InternalBuffer => buffer;

        internal Stream InternalInputStream => input;

        internal ref ParserInternalState InternalState => ref state;

        /// <summary>
        /// Disposes of this instance, potentially closing any underlying stream.
        /// </summary>
        /// <remarks>
        /// As there is no flushing to perform here, disposing of a <see cref="CodedInputStream"/> which
        /// was constructed with the <c>leaveOpen</c> option parameter set to <c>true</c> (or one which
        /// was constructed to read from a byte array) has no effect.
        /// </remarks>
        public void Dispose()
        {
            if (!leaveOpen)
            {
                input.Dispose();
            }

            if (needsReturnToPool)
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
            }

            ReturnToPool();
        }
    }
}
