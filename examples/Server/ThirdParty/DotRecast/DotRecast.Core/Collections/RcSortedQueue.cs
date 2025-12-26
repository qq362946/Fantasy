/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
recast4j copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
DotRecast Copyright (c) 2023-2024 Choi Ikpil ikpil@naver.com

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:
1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software. If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;

namespace DotRecast.Core.Collections
{
    public class RcSortedQueue<T>
    {
        private bool _dirty;
        private readonly List<T> _items;
        private readonly Comparison<T> _comparison;

        public RcSortedQueue(Comparison<T> comp)
        {
            _items = new List<T>();
            _comparison = (x, y) => comp(x, y) * -1;
        }

        public int Count()
        {
            return _items.Count;
        }

        public bool IsEmpty()
        {
            return 0 == _items.Count;
        }

        public void Clear()
        {
            _items.Clear();
            _dirty = false;
        }

        private void Balance()
        {
            if (_dirty)
            {
                _items.Sort(_comparison); // reverse
                _dirty = false;
            }
        }

        public T Peek()
        {
            Balance();
            return _items[^1];
        }

        public T Dequeue()
        {
            var node = Peek();
            _items.RemoveAt(_items.Count - 1);
            return node;
        }

        public void Enqueue(T item)
        {
            if (null == item)
                return;

            _items.Add(item);
            _dirty = true;
        }

        public bool Remove(T item)
        {
            if (null == item)
                return false;

            //int idx = _items.BinarySearch(item, _comparer); // don't use this! Because reference types can be reused externally.
            //int idx = _items.FindLastIndex(x => item.Equals(x));
            int idx = _items.LastIndexOf(item);
            if (0 > idx)
                return false;

            _items.RemoveAt(idx);
            return true;
        }


        public List<T> ToList()
        {
            Balance();
            var temp = new List<T>(_items);
            temp.Reverse();
            return temp;
        }
    }
}