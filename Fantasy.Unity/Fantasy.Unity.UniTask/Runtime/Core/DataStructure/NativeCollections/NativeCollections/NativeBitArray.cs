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
    ///     Native bit array
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NativeBitArray : IDisposable, IEquatable<NativeBitArray>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeBitArrayHandle
        {
            /// <summary>
            ///     Array
            /// </summary>
            public NativeArray<int> Array;

            /// <summary>
            ///     Length
            /// </summary>
            public int Length;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeBitArrayHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _handle = (NativeBitArrayHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeBitArrayHandle));
            _handle->Array = new NativeArray<int>(GetInt32ArrayLengthFromBitLength(length));
            _handle->Length = length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="length">Length</param>
        /// <param name="defaultValue">Default value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray(int length, bool defaultValue)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _handle = (NativeBitArrayHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeBitArrayHandle));
            _handle->Array = new NativeArray<int>(GetInt32ArrayLengthFromBitLength(length));
            _handle->Length = length;
            if (defaultValue)
            {
                _handle->Array.AsSpan().Fill(-1);
                Div32Rem(length, out var extraBits);
                if (extraBits > 0)
                    _handle->Array[^1] = (1 << extraBits) - 1;
            }
            else
            {
                _handle->Array.Clear();
            }
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray(int* array, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _handle = (NativeBitArrayHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeBitArrayHandle));
            _handle->Array = new NativeArray<int>(array, GetInt32ArrayLengthFromBitLength(length));
            _handle->Length = length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="length">Length</param>
        /// <param name="defaultValue">Default value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray(int* array, int length, bool defaultValue)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            _handle = (NativeBitArrayHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeBitArrayHandle));
            _handle->Array = new NativeArray<int>(array, GetInt32ArrayLengthFromBitLength(length));
            _handle->Length = length;
            if (defaultValue)
            {
                _handle->Array.AsSpan().Fill(-1);
                Div32Rem(length, out var extraBits);
                if (extraBits > 0)
                    _handle->Array[^1] = (1 << extraBits) - 1;
            }
            else
            {
                _handle->Array.Clear();
            }
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray(NativeArray<int> array, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            var intCount = GetInt32ArrayLengthFromBitLength(length);
            if (array.Length < intCount)
                throw new ArgumentOutOfRangeException(nameof(array), array.Length, $"Requires size is {intCount}, but buffer length is {array.Length}.");
            _handle = (NativeBitArrayHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeBitArrayHandle));
            _handle->Array = array;
            _handle->Length = length;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="length">Length</param>
        /// <param name="defaultValue">Default value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray(NativeArray<int> array, int length, bool defaultValue)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "MustBeNonNegative");
            var intCount = GetInt32ArrayLengthFromBitLength(length);
            if (array.Length < intCount)
                throw new ArgumentOutOfRangeException(nameof(array), array.Length, $"Requires size is {intCount}, but buffer length is {array.Length}.");
            _handle = (NativeBitArrayHandle*)NativeMemoryAllocator.Alloc((uint)sizeof(NativeBitArrayHandle));
            _handle->Array = array;
            _handle->Length = length;
            if (defaultValue)
            {
                _handle->Array.AsSpan().Fill(-1);
                Div32Rem(length, out var extraBits);
                if (extraBits > 0)
                    _handle->Array[^1] = (1 << extraBits) - 1;
            }
            else
            {
                _handle->Array.Clear();
            }
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     Array
        /// </summary>
        public NativeArray<int> Array => _handle->Array;

        /// <summary>
        ///     Length
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->Length;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MustBeNonNegative");
                var newLength = GetInt32ArrayLengthFromBitLength(value);
                if (newLength > _handle->Array.Length || newLength + 256 < _handle->Array.Length)
                {
                    var array = new NativeArray<int>(newLength);
                    Unsafe.CopyBlockUnaligned(array.Array, _handle->Array.Array, (uint)(_handle->Array.Length * sizeof(int)));
                    Unsafe.InitBlockUnaligned(array.Array + _handle->Array.Length, 0, (uint)(newLength - _handle->Array.Length));
                    _handle->Array.Dispose();
                    _handle->Array = array;
                }

                if (value > _handle->Length)
                {
                    var last = (_handle->Length - 1) >> 5;
                    Div32Rem(_handle->Length, out var bits);
                    if (bits > 0)
                        _handle->Array[last] &= (1 << bits) - 1;
                    _handle->Array.AsSpan(last + 1, newLength - last - 1).Clear();
                }

                _handle->Length = value;
            }
        }

        /// <summary>
        ///     Get or set value
        /// </summary>
        /// <param name="index">Index</param>
        public bool this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Get(index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Set(index, value);
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeBitArray other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeBitArray nativeBitArray && nativeBitArray == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => "NativeBitArray";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeBitArray left, NativeBitArray right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeBitArray left, NativeBitArray right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_handle == null)
                return;
            _handle->Array.Dispose();
            NativeMemoryAllocator.Free(_handle);
        }

        /// <summary>
        ///     Get
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            if ((uint)index >= (uint)_handle->Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "IndexMustBeLess");
            return (_handle->Array[index >> 5] & (1 << index)) != 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            if ((uint)index >= (uint)_handle->Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "IndexMustBeLess");
            var bitMask = 1 << index;
            ref var segment = ref _handle->Array[index >> 5];
            if (value)
                segment |= bitMask;
            else
                segment &= ~bitMask;
        }

        /// <summary>
        ///     Set all
        /// </summary>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAll(bool value)
        {
            var arrayLength = GetInt32ArrayLengthFromBitLength(Length);
            var span = _handle->Array.AsSpan(0, arrayLength);
            if (value)
            {
                span.Fill(-1);
                Div32Rem(_handle->Length, out var extraBits);
                if (extraBits > 0)
                    span[^1] &= (1 << extraBits) - 1;
            }
            else
            {
                span.Clear();
            }
        }

        /// <summary>
        ///     And
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>NativeBitArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray And(NativeBitArray value)
        {
            if (!value.IsCreated)
                throw new ArgumentNullException(nameof(value));
            var count = GetInt32ArrayLengthFromBitLength(Length);
            if (Length != value.Length || (uint)count > (uint)_handle->Array.Length || (uint)count > (uint)value._handle->Array.Length)
                throw new ArgumentException("ArrayLengthsDiffer");
            BitOperationsHelpers.And(_handle->Array, value._handle->Array, (uint)count);
            return this;
        }

        /// <summary>
        ///     Or
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>NativeBitArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray Or(NativeBitArray value)
        {
            if (!value.IsCreated)
                throw new ArgumentNullException(nameof(value));
            var count = GetInt32ArrayLengthFromBitLength(Length);
            if (Length != value.Length || (uint)count > (uint)_handle->Array.Length || (uint)count > (uint)value._handle->Array.Length)
                throw new ArgumentException("ArrayLengthsDiffer");
            BitOperationsHelpers.Or(_handle->Array, value._handle->Array, (uint)count);
            return this;
        }

        /// <summary>
        ///     Xor
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>NativeBitArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray Xor(NativeBitArray value)
        {
            if (!value.IsCreated)
                throw new ArgumentNullException(nameof(value));
            var count = GetInt32ArrayLengthFromBitLength(Length);
            if (Length != value.Length || (uint)count > (uint)_handle->Array.Length || (uint)count > (uint)value._handle->Array.Length)
                throw new ArgumentException("ArrayLengthsDiffer");
            BitOperationsHelpers.Xor(_handle->Array, value._handle->Array, (uint)count);
            return this;
        }

        /// <summary>
        ///     Not
        /// </summary>
        /// <returns>NativeBitArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray Not()
        {
            var count = GetInt32ArrayLengthFromBitLength(Length);
            BitOperationsHelpers.Not(_handle->Array, (uint)count);
            return this;
        }

        /// <summary>
        ///     Right shift
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>NativeBitArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray RightShift(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "MustBeNonNegative");
            if (count == 0)
                return this;
            var toIndex = 0;
            var length = GetInt32ArrayLengthFromBitLength(_handle->Length);
            if (count < _handle->Length)
            {
                var fromIndex = Div32Rem(count, out var shiftCount);
                Div32Rem(_handle->Length, out var extraBits);
                if (shiftCount == 0)
                {
                    unchecked
                    {
                        var mask = uint.MaxValue >> (32 - extraBits);
                        _handle->Array[length - 1] &= (int)mask;
                    }

                    Unsafe.CopyBlockUnaligned(_handle->Array.Array, _handle->Array.Array + fromIndex, (uint)((length - fromIndex) * sizeof(int)));
                    toIndex = length - fromIndex;
                }
                else
                {
                    var lastIndex = length - 1;
                    unchecked
                    {
                        while (fromIndex < lastIndex)
                        {
                            var right = (uint)_handle->Array[fromIndex] >> shiftCount;
                            var left = _handle->Array[++fromIndex] << (32 - shiftCount);
                            _handle->Array[toIndex++] = left | (int)right;
                        }

                        var mask = uint.MaxValue >> (32 - extraBits);
                        mask &= (uint)_handle->Array[fromIndex];
                        _handle->Array[toIndex++] = (int)(mask >> shiftCount);
                    }
                }
            }

            _handle->Array.AsSpan(toIndex, length - toIndex).Clear();
            return this;
        }

        /// <summary>
        ///     Left shift
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>NativeBitArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBitArray LeftShift(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "MustBeNonNegative");
            if (count == 0)
                return this;
            int lengthToClear;
            if (count < _handle->Length)
            {
                var lastIndex = (_handle->Length - 1) >> 5;
                lengthToClear = Div32Rem(count, out var shiftCount);
                if (shiftCount == 0)
                {
                    Unsafe.CopyBlockUnaligned(_handle->Array.Array + lengthToClear, _handle->Array.Array, (uint)((lastIndex + 1 - lengthToClear) * sizeof(int)));
                }
                else
                {
                    var fromIndex = lastIndex - lengthToClear;
                    unchecked
                    {
                        while (fromIndex > 0)
                        {
                            var left = _handle->Array[fromIndex] << shiftCount;
                            var right = (uint)_handle->Array[--fromIndex] >> (32 - shiftCount);
                            _handle->Array[lastIndex] = left | (int)right;
                            lastIndex--;
                        }

                        _handle->Array[lastIndex] = _handle->Array[fromIndex] << shiftCount;
                    }
                }
            }
            else
            {
                lengthToClear = GetInt32ArrayLengthFromBitLength(_handle->Length);
            }

            _handle->Array.AsSpan(0, lengthToClear).Clear();
            return this;
        }

        /// <summary>
        ///     Has all set
        /// </summary>
        /// <returns>All set</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAllSet()
        {
            Div32Rem(_handle->Length, out var extraBits);
            var intCount = GetInt32ArrayLengthFromBitLength(_handle->Length);
            if (extraBits != 0)
                intCount--;
#if NET8_0_OR_GREATER
            if (_handle->Array.AsSpan(0, intCount).ContainsAnyExcept(-1))
                return false;
#elif NET7_0_OR_GREATER
            if (_handle->Array.AsSpan(0, intCount).IndexOfAnyExcept(-1) >= 0)
                return false;
#else
            for (var i = 0; i < intCount; ++i)
            {
                if (_handle->Array[i] != -1)
                    return false;
            }
#endif
            if (extraBits == 0)
                return true;
            var mask = (1 << extraBits) - 1;
            return (_handle->Array[intCount] & mask) == mask;
        }

        /// <summary>
        ///     Has any set
        /// </summary>
        /// <returns>Any set</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnySet()
        {
            Div32Rem(_handle->Length, out var extraBits);
            var intCount = GetInt32ArrayLengthFromBitLength(_handle->Length);
            if (extraBits != 0)
                intCount--;
#if NET8_0_OR_GREATER
            if (_handle->Array.AsSpan(0, intCount).ContainsAnyExcept(0))
                return true;
#elif NET7_0_OR_GREATER
            if (_handle->Array.AsSpan(0, intCount).IndexOfAnyExcept(0) >= 0)
                return true;
#else
            for (var i = 0; i < intCount; ++i)
            {
                if (_handle->Array[i] != 0)
                    return true;
            }
#endif
            if (extraBits == 0)
                return false;
            return (_handle->Array[intCount] & ((1 << extraBits) - 1)) != 0;
        }

        /// <summary>
        ///     Get int32 array length from bit length
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetInt32ArrayLengthFromBitLength(int n)
        {
#if NET7_0_OR_GREATER
            return (n - 1 + (1 << 5)) >>> 5;
#else
            return (int)((uint)(n - 1 + (1 << 5)) >> 5);
#endif
        }

        /// <summary>
        ///     Divide by 32 and get remainder
        /// </summary>
        /// <param name="number">Number</param>
        /// <param name="remainder">Remainder</param>
        /// <returns>Quotient</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Div32Rem(int number, out int remainder)
        {
            var quotient = (uint)number / 32;
            remainder = number & (32 - 1);
            return (int)quotient;
        }

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeBitArray Empty => new();
    }
}