using System;
using System.Collections;
using System.Collections.Generic;

namespace DotRecast.Core.Buffers
{
    // https://github.com/joaoportela/CircularBuffer-CSharp/blob/master/CircularBuffer/CircularBuffer.cs
    public class RcCyclicBuffer<T> : IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly RcCyclicBuffer<T> _cb;
            private int _index;
            private readonly int _size;

            internal Enumerator(RcCyclicBuffer<T> cb)
            {
                _cb = cb;
                _size = _cb._size;
                _index = default;
                Reset();
            }
            
            public bool MoveNext()
            {
                return ++_index < _size;
            }

            public void Reset()
            {
                _index = -1;
            }

            public T Current => _cb[_index];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // This could be used to unlock write access to collection
            }
        }
        
        private readonly T[] _buffer;

        private int _start;
        private int _end;
        private int _size;

        public RcCyclicBuffer(int capacity)
            : this(capacity, new T[] { })
        {
        }

        public RcCyclicBuffer(int capacity, T[] items)
        {
            if (capacity < 1)
            {
                throw new ArgumentException("RcCyclicBuffer cannot have negative or zero capacity.", nameof(capacity));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Length > capacity)
            {
                throw new ArgumentException("Too many items to fit RcCyclicBuffer", nameof(items));
            }

            _buffer = new T[capacity];

            Array.Copy(items, _buffer, items.Length);
            _size = items.Length;

            _start = 0;
            _end = _size == capacity ? 0 : _size;
        }

        public int Capacity => _buffer.Length;
        public bool IsFull => Size == Capacity;
        public bool IsEmpty => Size == 0;

        public int Size => _size;

        public T Front()
        {
            ThrowIfEmpty();
            return _buffer[_start];
        }

        public T Back()
        {
            ThrowIfEmpty();
            return _buffer[(_end != 0 ? _end : Capacity) - 1];
        }

        public T this[int index]
        {
            get
            {
                if (IsEmpty)
                {
                    throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
                }

                if (index >= _size)
                {
                    throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {_size}");
                }

                int actualIndex = InternalIndex(index);
                return _buffer[actualIndex];
            }
            set
            {
                if (IsEmpty)
                {
                    throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
                }

                if (index >= _size)
                {
                    throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {_size}");
                }

                int actualIndex = InternalIndex(index);
                _buffer[actualIndex] = value;
            }
        }

        public void PushBack(T item)
        {
            if (IsFull)
            {
                _buffer[_end] = item;
                Increment(ref _end);
                _start = _end;
            }
            else
            {
                _buffer[_end] = item;
                Increment(ref _end);
                ++_size;
            }
        }

        public void PushFront(T item)
        {
            if (IsFull)
            {
                Decrement(ref _start);
                _end = _start;
                _buffer[_start] = item;
            }
            else
            {
                Decrement(ref _start);
                _buffer[_start] = item;
                ++_size;
            }
        }

        public void PopBack()
        {
            ThrowIfEmpty("Cannot take elements from an empty buffer.");
            Decrement(ref _end);
            _buffer[_end] = default(T);
            --_size;
        }

        public void PopFront()
        {
            ThrowIfEmpty("Cannot take elements from an empty buffer.");
            _buffer[_start] = default(T);
            Increment(ref _start);
            --_size;
        }

        public void Clear()
        {
            // to clear we just reset everything.
            _start = 0;
            _end = 0;
            _size = 0;
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        public T[] ToArray()
        {
            T[] newArray = new T[Size];
            CopyTo(newArray);
            return newArray;
        }

        public void CopyTo(Span<T> destination)
        {
            var span1 = ArrayOne();
            span1.CopyTo(destination);
            ArrayTwo().CopyTo(destination[span1.Length..]);
        }

        private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException(message);
            }
        }

        private void Increment(ref int index)
        {
            if (++index == Capacity)
            {
                index = 0;
            }
        }

        private void Decrement(ref int index)
        {
            if (index == 0)
            {
                index = Capacity;
            }

            index--;
        }

        private int InternalIndex(int index)
        {
            return _start + (index < (Capacity - _start)
                ? index
                : index - Capacity);
        }

        internal Span<T> ArrayOne()
        {
            if (IsEmpty)
            {
                return new Span<T>(Array.Empty<T>());
            }

            if (_start < _end)
            {
                return new Span<T>(_buffer, _start, _end - _start);
            }

            return new Span<T>(_buffer, _start, _buffer.Length - _start);
        }

        internal Span<T> ArrayTwo()
        {
            if (IsEmpty)
            {
                return new Span<T>(Array.Empty<T>());
            }

            if (_start < _end)
            {
                return new Span<T>(_buffer, _end, 0);
            }

            return new Span<T>(_buffer, 0, _end);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}