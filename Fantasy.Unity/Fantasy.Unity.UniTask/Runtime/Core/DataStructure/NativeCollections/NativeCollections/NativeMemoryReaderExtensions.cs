using System.Runtime.CompilerServices;
#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif

#pragma warning disable CA2208
#pragma warning disable CS8632

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     Native memory reader extensions
    /// </summary>
    public static unsafe class NativeMemoryReaderExtensions
    {
        /// <summary>
        ///     Read
        /// </summary>
        /// <param name="reader">Reader</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(this ref NativeMemoryReader reader) where T : unmanaged
        {
            if (reader.Position + sizeof(T) > reader.Length)
                throw new ArgumentOutOfRangeException(nameof(T), $"Requires size is {sizeof(T)}, but buffer length is {reader.Remaining}.");
            var obj = Unsafe.ReadUnaligned<T>(reader.Array + reader.Position);
            reader.Position += sizeof(T);
            return obj;
        }

        /// <summary>
        ///     Try read
        /// </summary>
        /// <param name="reader">Reader</param>
        /// <param name="obj">object</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Read</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRead<T>(this ref NativeMemoryReader reader, out T obj) where T : unmanaged
        {
            if (reader.Position + sizeof(T) > reader.Length)
            {
                obj = default;
                return false;
            }

            obj = Unsafe.ReadUnaligned<T>(reader.Array + reader.Position);
            reader.Position += sizeof(T);
            return true;
        }
    }
}