using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotRecast.Core.Collections
{
    public sealed class RcBinaryMinHeap<T>
    {
        private readonly List<T> _items;
        private readonly Comparison<T> _comparision;

        public int Count => _items.Count;
        public int Capacity => _items.Capacity;

        public RcBinaryMinHeap(Comparison<T> comparision)
        {
            _items = new List<T>();
            _comparision = comparision;
        }

        public RcBinaryMinHeap(int capacity, Comparison<T> comparison) : this(comparison)
        {
            if (capacity <= 0)
                throw new ArgumentException("capacity must greater than zero");

            _items = new List<T>(capacity);
            _comparision = comparison;
        }

        public void Push(T val)
        {
            _items.Add(val);
            SiftUp(_items.Count - 1);
        }

        public T Pop()
        {
            var min = Peek();
            RemoveMin();
            return min;
        }

        private void RemoveMin()
        {
            if (_items.Count == 0)
            {
                Throw();
                static void Throw() => throw new InvalidOperationException("no element to pop");
            }

            int last = _items.Count - 1;
            Swap(0, last);
            _items.RemoveAt(last);

            MinHeapify(0, last - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Top()
        {
            return _items[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            if (IsEmpty())
            {
                throw new Exception("Heap is empty.");
            }

            return _items[0];
        }


        public bool Modify(T node)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Equals(node))
                {
                    SiftUp(i);
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _items.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty()
        {
            return 0 == _items.Count;
        }

        private void SiftUp(int nodeIndex)
        {
            int parent = (nodeIndex - 1) / 2;
            while (_comparision.Invoke(_items[nodeIndex], _items[parent]) < 0)
            {
                Swap(parent, nodeIndex);
                nodeIndex = parent;
                parent = (nodeIndex - 1) / 2;
            }
        }


        private void MinHeapify(int nodeIndex, int lastIndex)
        {
            int left = (nodeIndex * 2) + 1;
            int right = left + 1;
            int smallest = nodeIndex;

            if (left <= lastIndex && _comparision.Invoke(_items[left], _items[nodeIndex]) < 0)
                smallest = left;

            if (right <= lastIndex && _comparision.Invoke(_items[right], _items[smallest]) < 0)
                smallest = right;

            if (smallest == nodeIndex)
                return;

            Swap(nodeIndex, smallest);
            MinHeapify(smallest, lastIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int x, int y)
        {
            if (x == y)
                return;

            (_items[y], _items[x]) = (_items[x], _items[y]);
        }


        public T[] ToArray()
        {
            return _items.ToArray();
        }

        public List<T> ToList()
        {
            return new List<T>(_items);
        }
    }
}