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
    ///     Native memory writer
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe ref struct NativeMemoryWriter
    {
        /// <summary>
        ///     Array
        /// </summary>
        public readonly byte* Array;

        /// <summary>
        ///     Length
        /// </summary>
        public readonly int Length;

        /// <summary>
        ///     Position
        /// </summary>
        public int Position;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMemoryWriter(byte* array, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            Array = array;
            Length = length;
            Position = 0;
        }

        /// <summary>
        ///     Remaining
        /// </summary>
        public int Remaining => Length - Position;

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public byte* this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array + index;
        }

        /// <summary>
        ///     Get reference
        /// </summary>
        /// <param name="index">Index</param>
        public byte* this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array + index;
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeMemoryWriter other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => throw new NotSupportedException("Cannot call Equals on NativeMemoryWriter");

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => HashCode.Combine((int)(nint)Array, Length, Position);

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => "NativeMemoryWriter";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeMemoryWriter left, NativeMemoryWriter right) => left.Array == right.Array && left.Length == right.Length && left.Position == right.Position;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeMemoryWriter left, NativeMemoryWriter right) => left.Array != right.Array || left.Length != right.Length || left.Position != right.Position;

        /// <summary>
        ///     Advance
        /// </summary>
        /// <param name="count">Count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            var newPosition = Position + count;
            if (newPosition < 0 || newPosition > Length)
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot advance past the end of the buffer.");
            Position = newPosition;
        }

        /// <summary>
        ///     Try advance
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>Advanced</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdvance(int count)
        {
            var newPosition = Position + count;
            if (newPosition < 0 || newPosition > Length)
                return false;
            Position = newPosition;
            return true;
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="obj">object</param>
        /// <typeparam name="T">Type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T* obj) where T : unmanaged
        {
            if (Position + sizeof(T) > Length)
                throw new ArgumentOutOfRangeException(nameof(T), $"Requires size is {sizeof(T)}, but buffer length is {Remaining}.");
            Unsafe.CopyBlockUnaligned(Array + Position, obj, (uint)sizeof(T));
            Position += sizeof(T);
        }

        /// <summary>
        ///     Try write
        /// </summary>
        /// <param name="obj">object</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Wrote</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite<T>(T* obj) where T : unmanaged
        {
            if (Position + sizeof(T) > Length)
                return false;
            Unsafe.CopyBlockUnaligned(Array + Position, obj, (uint)sizeof(T));
            Position += sizeof(T);
            return true;
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="obj">object</param>
        /// <param name="count">Count</param>
        /// <typeparam name="T">Type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T* obj, int count) where T : unmanaged
        {
            count *= sizeof(T);
            if (Position + count > Length)
                throw new ArgumentOutOfRangeException(nameof(T), $"Requires size is {count}, but buffer length is {Remaining}.");
            Unsafe.CopyBlockUnaligned(Array + Position, obj, (uint)count);
            Position += count;
        }

        /// <summary>
        ///     Try write
        /// </summary>
        /// <param name="obj">object</param>
        /// <param name="count">Count</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Wrote</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite<T>(T* obj, int count) where T : unmanaged
        {
            count *= sizeof(T);
            if (Position + count > Length)
                return false;
            Unsafe.CopyBlockUnaligned(Array + Position, obj, (uint)count);
            Position += count;
            return true;
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="obj">object</param>
        /// <typeparam name="T">Type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(in T obj) where T : unmanaged
        {
            if (Position + sizeof(T) > Length)
                throw new ArgumentOutOfRangeException(nameof(T), $"Requires size is {sizeof(T)}, but buffer length is {Remaining}.");
            Unsafe.WriteUnaligned(Array + Position, obj);
            Position += sizeof(T);
        }

        /// <summary>
        ///     Try write
        /// </summary>
        /// <param name="obj">object</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Wrote</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite<T>(in T obj) where T : unmanaged
        {
            if (Position + sizeof(T) > Length)
                return false;
            Unsafe.WriteUnaligned(Array + Position, obj);
            Position += sizeof(T);
            return true;
        }

        /// <summary>
        ///     Write bytes
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte* buffer, int length)
        {
            if (Position + length > Length)
                throw new ArgumentOutOfRangeException(nameof(length), $"Requires size is {length}, but buffer length is {Remaining}.");
            Unsafe.CopyBlockUnaligned(Array + Position, buffer, (uint)length);
            Position += length;
        }

        /// <summary>
        ///     Try write bytes
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <returns>Wrote</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteBytes(byte* buffer, int length)
        {
            if (Position + length > Length)
                return false;
            Unsafe.CopyBlockUnaligned(Array + Position, buffer, (uint)length);
            Position += length;
            return true;
        }

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Position = 0;

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref *Array, Position);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int length) => MemoryMarshal.CreateSpan(ref *Array, length);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="length">Length</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int start, int length) => MemoryMarshal.CreateSpan(ref *(Array + start), length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref *Array, Position);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan(int length) => MemoryMarshal.CreateReadOnlySpan(ref *Array, length);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="length">Length</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan(int start, int length) => MemoryMarshal.CreateReadOnlySpan(ref *(Array + start), length);

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<byte>(NativeMemoryWriter writer) => writer.AsSpan();

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(NativeMemoryWriter writer) => writer.AsReadOnlySpan();

        /// <summary>
        ///     As native memory reader
        /// </summary>
        /// <returns>NativeMemoryReader</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeMemoryReader(NativeMemoryWriter writer) => new(writer.Array, writer.Position);

        /// <summary>
        ///     As native memory writer
        /// </summary>
        /// <returns>NativeMemoryWriter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeMemoryWriter(NativeArray<byte> writer) => new(writer.Array, writer.Length);

        /// <summary>
        ///     As native memory writer
        /// </summary>
        /// <returns>NativeMemoryWriter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeMemoryWriter(NativeMemoryArray<byte> writer) => new(writer.Array, writer.Length);

        /// <summary>
        ///     As native memory writer
        /// </summary>
        /// <returns>NativeMemoryWriter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeMemoryWriter(NativeArraySegment<byte> writer) => new(writer.Array + writer.Offset, writer.Count);

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeMemoryWriter Empty => new();
    }
}