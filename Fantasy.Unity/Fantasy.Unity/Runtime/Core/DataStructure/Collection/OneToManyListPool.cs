using System;
using System.Collections.Generic;
using System.Linq;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy
{
    /// <summary>
    /// 可回收的、一对多关系的列表池。
    /// </summary>
    /// <typeparam name="TKey">键的类型。</typeparam>
    /// <typeparam name="TValue">值的类型。</typeparam>
    public class OneToManyListPool<TKey, TValue> : OneToManyList<TKey, TValue>, IDisposable, IPool where TKey : notnull
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
        private bool _isDispose;

        /// <summary>
        /// 创建一个 <see cref="OneToManyListPool{TKey, TValue}"/> 一对多关系的列表池的实例。
        /// </summary>
        /// <returns>创建的实例。</returns>
        public static OneToManyListPool<TKey, TValue> Create()
        {
#if FANTASY_WEBGL || FANTASY_EXPORTER
            var list = Pool<OneToManyListPool<TKey, TValue>>.Rent();
#else
            var list = MultiThreadPool.Rent<OneToManyListPool<TKey, TValue>>();
#endif
            list._isDispose = false;
            list.IsPool = true;
            return list;
        }

        /// <summary>
        /// 释放当前对象所占用的资源，并将对象回收到对象池中。
        /// </summary>
        public void Dispose()
        {
            if (_isDispose)
            {
                return;
            }

            _isDispose = true;
            Clear();
#if FANTASY_WEBGL || FANTASY_EXPORTER
            Pool<OneToManyListPool<TKey, TValue>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }
    }

    /// <summary>
    /// 一对多关系的列表字典。
    /// </summary>
    /// <typeparam name="TKey">键的类型。</typeparam>
    /// <typeparam name="TValue">值的类型。</typeparam>
    public class OneToManyList<TKey, TValue> : Dictionary<TKey, List<TValue>> where TKey : notnull
    {
        private readonly int _recyclingLimit = 120;
        private static readonly List<TValue> Empty = new List<TValue>();
        private readonly Queue<List<TValue>> _queue = new Queue<List<TValue>>();

        /// <summary>
        /// 初始化一个新的 <see cref="OneToManyList{TKey, TValue}"/> 实例。
        /// </summary>
        public OneToManyList() { }

        /// <summary>
        /// 设置最大缓存数量
        /// </summary>
        /// <param name="recyclingLimit">
        /// 1:防止数据量过大、所以超过recyclingLimit的数据还是走GC.
        /// 2:设置成0不控制数量，全部缓存
        /// </param>
        public OneToManyList(int recyclingLimit)
        {
            _recyclingLimit = recyclingLimit;
        }

        /// <summary>
        /// 判断给定的键和值是否存在于列表中。
        /// </summary>
        /// <param name="key">要搜索的键。</param>
        /// <param name="value">要搜索的值。</param>
        /// <returns>如果存在则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
        public bool Contains(TKey key, TValue value)
        {
            TryGetValue(key, out var list);

            return list != null && list.Contains(value);
        }

        /// <summary>
        /// 向列表中添加指定键和值。
        /// </summary>
        /// <param name="key">要添加值的键。</param>
        /// <param name="value">要添加的值。</param>
        public void Add(TKey key, TValue value)
        {
            if (!TryGetValue(key, out var list))
            {
                list = Fetch();
                list.Add(value);
                Add(key, list);

                return;
            }

            list.Add(value);
        }

        /// <summary>
        /// 获取指定键对应的列表中的第一个值。
        /// </summary>
        /// <param name="key">要获取值的键。</param>
        /// <returns>键对应的列表中的第一个值。</returns>
        public TValue First(TKey key)
        {
            return !TryGetValue(key, out var list) ? default : list.FirstOrDefault();
        }

        /// <summary>
        /// 从列表中移除指定键和值。
        /// </summary>
        /// <param name="key">要移除值的键。</param>
        /// <param name="value">要移除的值。</param>
        /// <returns>如果成功移除则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
        public bool RemoveValue(TKey key, TValue value)
        {
            if (!TryGetValue(key, out var list))
            {
                return true;
            }

            var isRemove = list.Remove(value);

            if (list.Count == 0)
            {
                isRemove = RemoveByKey(key);
            }

            return isRemove;
        }

        /// <summary>
        /// 从列表中移除指定键及其关联的所有值。
        /// </summary>
        /// <param name="key">要移除的键。</param>
        /// <returns>如果成功移除则为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
        public bool RemoveByKey(TKey key)
        {
            if (!TryGetValue(key, out var list))
            {
                return false;
            }

            Remove(key);
            Recycle(list);
            return true;
        }

        /// <summary>
        /// 获取指定键关联的所有值的列表。
        /// </summary>
        /// <param name="key">要获取值的键。</param>
        /// <returns>键关联的所有值的列表。</returns>
        public List<TValue> GetValues(TKey key)
        {
            if (TryGetValue(key, out List<TValue> list))
            {
                return list;
            }

            return Empty;
        }

        /// <summary>
        /// 清除字典中的所有键值对，并回收相关的值集合。
        /// </summary>
        public new void Clear()
        {
            foreach (var keyValuePair in this) Recycle(keyValuePair.Value);

            base.Clear();
        }

        /// <summary>
        /// 从空闲值集合队列中获取一个值集合，如果队列为空则创建一个新的值集合。
        /// </summary>
        /// <returns>从队列中获取的值集合。</returns>
        private List<TValue> Fetch()
        {
            return _queue.Count <= 0 ? new List<TValue>() : _queue.Dequeue();
        }

        /// <summary>
        /// 回收一个不再使用的值集合到空闲值集合队列中。
        /// </summary>
        /// <param name="list">要回收的值集合。</param>
        private void Recycle(List<TValue> list)
        {
            list.Clear();

            if (_recyclingLimit != 0 && _queue.Count > _recyclingLimit) return;

            _queue.Enqueue(list);
        }
    }
}