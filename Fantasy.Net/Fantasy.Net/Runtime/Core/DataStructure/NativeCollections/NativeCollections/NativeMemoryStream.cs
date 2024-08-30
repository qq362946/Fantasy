using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
using System.IO;
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
    ///     Native memory stream
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeMemoryStream : IDisposable, IEquatable<NativeMemoryStream>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMemoryStreamHandle
        {
            /// <summary>
            ///     Array
            /// </summary>
            public byte* Array;

            /// <summary>
            ///     Position
            /// </summary>
            public int Position;

            /// <summary>
            ///     Length
            /// </summary>
            public int Length;

            /// <summary>
            ///     Capacity
            /// </summary>
            public int Capacity;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeMemoryStreamHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="capacity">Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMemoryStream(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");
            if (capacity < 4)
                capacity = 4;
            _handle = (NativeMemoryStreamHandle*)NativeMemoryAllocator.Alloc(sizeof(NativeMemoryStreamHandle));
            _handle->Array = (byte*)NativeMemoryAllocator.Alloc(capacity);
            _handle->Position = 0;
            _handle->Length = 0;
            _handle->Capacity = capacity;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => _handle->Length == 0;

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _handle->Array[index];
        }

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public ref byte this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _handle->Array[index];
        }

        /// <summary>
        ///     Can read
        /// </summary>
        public bool CanRead => IsCreated;

        /// <summary>
        ///     Can seek
        /// </summary>
        public bool CanSeek => IsCreated;

        /// <summary>
        ///     Can write
        /// </summary>
        public bool CanWrite => IsCreated;

        /// <summary>
        ///     Length
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureNotClosed();
                return _handle->Length;
            }
        }

        /// <summary>
        ///     Position
        /// </summary>
        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureNotClosed();
                return _handle->Position;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Position), value, "MustBeNonNegative");
                EnsureNotClosed();
                _handle->Position = value;
            }
        }

        /// <summary>
        ///     Capacity
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureNotClosed();
                return _handle->Capacity;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                EnsureNotClosed();
                if (value < _handle->Length)
                    throw new ArgumentOutOfRangeException(nameof(Capacity), value, "SmallCapacity");
                if (value != _handle->Capacity)
                {
                    if (value > 0)
                    {
                        var newBuffer = (byte*)NativeMemoryAllocator.Alloc(value);
                        if (_handle->Length > 0)
                            Unsafe.CopyBlock(newBuffer, _handle->Array, (uint)_handle->Length);
                        NativeMemoryAllocator.Free(_handle->Array);
                        _handle->Array = newBuffer;
                    }
                    else
                    {
                        NativeMemoryAllocator.Free(_handle->Array);
                        _handle->Array = (byte*)NativeMemoryAllocator.Alloc(0);
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
        public bool Equals(NativeMemoryStream other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeMemoryStream nativeMemoryStream && nativeMemoryStream == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NativeMemoryStream<{_handle->Length}>";

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<byte>(NativeMemoryStream nativeList) => nativeList.AsSpan();

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(NativeMemoryStream nativeList) => nativeList.AsReadOnlySpan();

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeMemoryStream left, NativeMemoryStream right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeMemoryStream left, NativeMemoryStream right) => left._handle != right._handle;

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
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref *_handle->Array, _handle->Length);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int length) => MemoryMarshal.CreateSpan(ref *_handle->Array, length);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="length">Length</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int start, int length) => MemoryMarshal.CreateSpan(ref *(_handle->Array + start), length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref *_handle->Array, _handle->Length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan(int length) => MemoryMarshal.CreateReadOnlySpan(ref *_handle->Array, length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="length">Length</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan(int start, int length) => MemoryMarshal.CreateReadOnlySpan(ref *(_handle->Array + start), length);

        /// <summary>
        ///     Get buffer
        /// </summary>
        /// <returns>Buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* GetBuffer() => _handle->Array;

        /// <summary>
        ///     Seek
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="loc">Seek origin</param>
        /// <returns>Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Seek(int offset, SeekOrigin loc)
        {
            if (offset > 2147483647)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "StreamLength");
            EnsureNotClosed();
            switch (loc)
            {
                case SeekOrigin.Begin:
                {
                    if (offset < 0)
                        throw new IOException("IO_SeekBeforeBegin");
                    _handle->Position = offset;
                    break;
                }
                case SeekOrigin.Current:
                {
                    var tempPosition = unchecked(_handle->Position + offset);
                    if (tempPosition < 0)
                        throw new IOException("IO_SeekBeforeBegin");
                    _handle->Position = tempPosition;
                    break;
                }
                case SeekOrigin.End:
                {
                    var tempPosition = unchecked(_handle->Length + offset);
                    if (tempPosition < 0)
                        throw new IOException("IO_SeekBeforeBegin");
                    _handle->Position = tempPosition;
                    break;
                }
                default:
                    throw new ArgumentException("InvalidSeekOrigin");
            }

            return _handle->Position;
        }

        /// <summary>
        ///     Set length
        /// </summary>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLength(int length)
        {
            if (length < 0 || length > 2147483647)
                throw new ArgumentOutOfRangeException(nameof(length), length, "StreamLength");
            EnsureNotClosed();
            var allocatedNewArray = EnsureCapacity(length);
            if (!allocatedNewArray && length > _handle->Length)
                Unsafe.InitBlock(_handle->Array + _handle->Length, 0, (uint)(length - _handle->Length));
            _handle->Length = length;
            if (_handle->Position > length)
                _handle->Position = length;
        }

        /// <summary>
        ///     Read
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        /// <returns>Bytes</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(byte* buffer, int offset, int count)
        {
            EnsureNotClosed();
            var n = _handle->Length - _handle->Position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;
            if (n <= 8)
            {
                var byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _handle->Array[_handle->Position + byteCount];
            }
            else
            {
                Unsafe.CopyBlock(buffer + offset, _handle->Array + _handle->Position, (uint)n);
            }

            _handle->Position += n;
            return n;
        }

        /// <summary>
        ///     Read
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <returns>Bytes</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(Span<byte> buffer)
        {
            EnsureNotClosed();
            var size = _handle->Length - _handle->Position;
            var n = size < buffer.Length ? size : buffer.Length;
            if (n <= 0)
                return 0;
            Unsafe.CopyBlock(ref buffer[0], ref *(_handle->Array + _handle->Position), (uint)n);
            _handle->Position += n;
            return n;
        }

        /// <summary>
        ///     Read
        /// </summary>
        /// <returns>Byte</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadByte()
        {
            EnsureNotClosed();
            return _handle->Position >= _handle->Length ? -1 : _handle->Array[_handle->Position++];
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte* buffer, int offset, int count)
        {
            EnsureNotClosed();
            var i = _handle->Position + count;
            if (i < 0)
                throw new IOException("IO_StreamTooLong");
            if (i > _handle->Length)
            {
                var mustZero = _handle->Position > _handle->Length;
                if (i > _handle->Capacity)
                {
                    var allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                        mustZero = false;
                }

                if (mustZero)
                    Unsafe.InitBlock(_handle->Array + _handle->Length, 0, (uint)(i - _handle->Length));
                _handle->Length = i;
            }

            if (count <= 8 && buffer != _handle->Array)
            {
                var byteCount = count;
                while (--byteCount >= 0)
                    _handle->Array[_handle->Position + byteCount] = buffer[offset + byteCount];
            }
            else
            {
                Unsafe.CopyBlock(_handle->Array + _handle->Position, buffer + offset, (uint)count);
            }

            _handle->Position = i;
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="buffer">Buffer</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureNotClosed();
            var i = _handle->Position + buffer.Length;
            if (i < 0)
                throw new IOException("IO_StreamTooLong");
            if (i > _handle->Length)
            {
                var mustZero = _handle->Position > _handle->Length;
                if (i > _handle->Capacity)
                {
                    var allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                        mustZero = false;
                }

                if (mustZero)
                    Unsafe.InitBlock(_handle->Array + _handle->Length, 0, (uint)(i - _handle->Length));
                _handle->Length = i;
            }

            Unsafe.CopyBlock(ref *(_handle->Array + _handle->Position), ref MemoryMarshal.GetReference(buffer), (uint)buffer.Length);
            _handle->Position = i;
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="value">Byte</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            EnsureNotClosed();
            if (_handle->Position >= _handle->Length)
            {
                var newLength = _handle->Position + 1;
                var mustZero = _handle->Position > _handle->Length;
                if (newLength >= _handle->Capacity)
                {
                    var allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray)
                        mustZero = false;
                }

                if (mustZero)
                    Unsafe.InitBlock(_handle->Array + _handle->Length, 0, (uint)(_handle->Position - _handle->Length));
                _handle->Length = newLength;
            }

            _handle->Array[_handle->Position++] = value;
        }

        /// <summary>
        ///     Ensure capacity
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <returns>Ensured</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new IOException("IO_StreamTooLong");
            if (capacity > _handle->Capacity)
            {
                var newCapacity = capacity > 256 ? capacity : 256;
                if (newCapacity < _handle->Capacity * 2)
                    newCapacity = _handle->Capacity * 2;
                if ((uint)(_handle->Capacity * 2) > 2147483591)
                    newCapacity = capacity > 2147483591 ? capacity : 2147483591;
                Capacity = newCapacity;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Ensure not closed
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotClosed()
        {
            if (_handle == null)
                throw new ObjectDisposedException("StreamClosed");
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeMemoryStream Empty => new();
    }
}