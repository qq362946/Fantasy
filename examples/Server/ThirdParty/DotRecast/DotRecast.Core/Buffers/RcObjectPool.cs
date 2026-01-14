using System;
using System.Collections.Generic;

namespace DotRecast.Core.Buffers
{
    // This implementation is thread unsafe
    public class RcObjectPool<T> where T : class
    {
        private readonly Queue<T> _items = new Queue<T>();
        private readonly Func<T> _createFunc;

        public RcObjectPool(Func<T> createFunc)
        {
            _createFunc = createFunc;
        }

        public T Get()
        {
            if (_items.TryDequeue(out var result))
                return result;

            return _createFunc();
        }

        public void Return(T obj)
        {
            _items.Enqueue(obj);
        }
    }
}