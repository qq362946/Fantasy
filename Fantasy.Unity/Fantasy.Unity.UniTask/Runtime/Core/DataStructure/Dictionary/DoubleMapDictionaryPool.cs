using System;
using System.Collections.Generic;
using Fantasy.Pool;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.DataStructure.Dictionary
{
    /// <summary>
    /// 提供一个双向映射字典对象池类，用于双向键值对映射。
    /// </summary>
    /// <typeparam name="TKey">字典中键的类型。</typeparam>
    /// <typeparam name="TValue">字典中值的类型。</typeparam>
    public class DoubleMapDictionaryPool<TKey, TValue> : DoubleMapDictionary<TKey, TValue>, IDisposable, IPool where TKey : notnull where TValue : notnull
    {
        private bool _isPool;
        private bool _isDispose;

        /// <summary>
        /// 创建一个新的 <see cref="DoubleMapDictionaryPool{TKey, TValue}"/> 实例。
        /// </summary>
        /// <returns>新创建的实例。</returns>
        public static DoubleMapDictionaryPool<TKey, TValue> Create()
        {
#if FANTASY_WEBGL
            var a = Pool<DoubleMapDictionaryPool<TKey, TValue>>.Rent();
#else
            var a = MultiThreadPool.Rent<DoubleMapDictionaryPool<TKey, TValue>>();
#endif
            a._isDispose = false;
            a._isPool = true;
            return a;
        }

        /// <summary>
        /// 释放实例占用的资源。
        /// </summary>
        public void Dispose()
        {
            if (_isDispose)
            {
                return;
            }

            _isDispose = true;
            Clear();
#if FANTASY_WEBGL
            Pool<DoubleMapDictionaryPool<TKey, TValue>>.Return(this);
#else
            MultiThreadPool.Return(this);
#endif
        }

        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }
    }

    /// <summary>
    /// 可以实现双向映射的字典类，用于将键和值进行双向映射。
    /// </summary>
    /// <typeparam name="TK">键的类型，不能为 null。</typeparam>
    /// <typeparam name="TV">值的类型，不能为 null。</typeparam>
    public class DoubleMapDictionary<TK, TV> where TK : notnull where TV : notnull
    {
        private readonly Dictionary<TK, TV> _kv = new Dictionary<TK, TV>();
        private readonly Dictionary<TV, TK> _vk = new Dictionary<TV, TK>();

        /// <summary>
        /// 创建一个新的空的 <see cref="DoubleMapDictionary{TK, TV}"/> 实例。
        /// </summary>
        public DoubleMapDictionary() { }

        /// <summary>
        /// 创建一个新的具有指定初始容量的 <see cref="DoubleMapDictionary{TK, TV}"/> 实例。
        /// </summary>
        /// <param name="capacity">初始容量。</param>
        public DoubleMapDictionary(int capacity)
        {
            _kv = new Dictionary<TK, TV>(capacity);
            _vk = new Dictionary<TV, TK>(capacity);
        }

        /// <summary>
        /// 获取包含字典中所有键的列表。
        /// </summary>
        public List<TK> Keys => new List<TK>(_kv.Keys);

        /// <summary>
        /// 获取包含字典中所有值的列表。
        /// </summary>
        public List<TV> Values => new List<TV>(_vk.Keys);

        /// <summary>
        /// 对字典中的每个键值对执行指定的操作。
        /// </summary>
        /// <param name="action">要执行的操作。</param>
        public void ForEach(Action<TK, TV> action)
        {
            if (action == null)
            {
                return;
            }

            var keys = _kv.Keys;
            foreach (var key in keys)
            {
                action(key, _kv[key]);
            }
        }

        /// <summary>
        /// 将指定的键值对添加到字典中。
        /// </summary>
        /// <param name="key">要添加的键。</param>
        /// <param name="value">要添加的值。</param>
        public void Add(TK key, TV value)
        {
            if (key == null || value == null || _kv.ContainsKey(key) || _vk.ContainsKey(value))
            {
                return;
            }

            _kv.Add(key, value);
            _vk.Add(value, key);
        }

        /// <summary>
        /// 根据指定的键获取相应的值。
        /// </summary>
        /// <param name="key">要查找值的键。</param>
        /// <returns>与指定键关联的值，如果找不到键，则返回默认值。</returns>
        public TV GetValueByKey(TK key)
        {
            if (key != null && _kv.ContainsKey(key))
            {
                return _kv[key];
            }

            return default;
        }

        /// <summary>
        /// 尝试根据指定的键获取相应的值。
        /// </summary>
        /// <param name="key">要查找值的键。</param>
        /// <param name="value">如果找到，则为与指定键关联的值；否则为值的默认值。</param>
        /// <returns>如果找到键，则为 true；否则为 false。</returns>
        public bool TryGetValueByKey(TK key, out TV value)
        {
            var result = key != null && _kv.ContainsKey(key);

            value = result ? _kv[key] : default;

            return result;
        }

        /// <summary>
        /// 根据指定的值获取相应的键。
        /// </summary>
        /// <param name="value">要查找键的值。</param>
        /// <returns>与指定值关联的键，如果找不到值，则返回默认键。</returns>
        public TK GetKeyByValue(TV value)
        {
            if (value != null && _vk.ContainsKey(value))
            {
                return _vk[value];
            }

            return default;
        }

        /// <summary>
        /// 尝试根据指定的值获取相应的键。
        /// </summary>
        /// <param name="value">要查找键的值。</param>
        /// <param name="key">如果找到，则为与指定值关联的键；否则为键的默认值。</param>
        /// <returns>如果找到值，则为 true；否则为 false。</returns>
        public bool TryGetKeyByValue(TV value, out TK key)
        {
            var result = value != null && _vk.ContainsKey(value);

            key = result ? _vk[value] : default;

            return result;
        }

        /// <summary>
        /// 根据指定的键移除键值对。
        /// </summary>
        /// <param name="key">要移除的键。</param>
        public void RemoveByKey(TK key)
        {
            if (key == null)
            {
                return;
            }

            if (!_kv.TryGetValue(key, out var value))
            {
                return;
            }

            _kv.Remove(key);
            _vk.Remove(value);
        }

        /// <summary>
        /// 根据指定的值移除键值对。
        /// </summary>
        /// <param name="value">要移除的值。</param>
        public void RemoveByValue(TV value)
        {
            if (value == null)
            {
                return;
            }

            if (!_vk.TryGetValue(value, out var key))
            {
                return;
            }

            _kv.Remove(key);
            _vk.Remove(value);
        }

        /// <summary>
        /// 清空字典中的所有键值对。
        /// </summary>
        public void Clear()
        {
            _kv.Clear();
            _vk.Clear();
        }

        /// <summary>
        /// 判断字典是否包含指定的键。
        /// </summary>
        /// <param name="key">要检查的键。</param>
        /// <returns>如果字典包含指定的键，则为 true；否则为 false。</returns>
        public bool ContainsKey(TK key)
        {
            return key != null && _kv.ContainsKey(key);
        }

        /// <summary>
        /// 判断字典是否包含指定的值。
        /// </summary>
        /// <param name="value">要检查的值。</param>
        /// <returns>如果字典包含指定的值，则为 true；否则为 false。</returns>
        public bool ContainsValue(TV value)
        {
            return value != null && _vk.ContainsKey(value);
        }

        /// <summary>
        /// 判断字典是否包含指定的键值对。
        /// </summary>
        /// <param name="key">要检查的键。</param>
        /// <param name="value">要检查的值。</param>
        /// <returns>如果字典包含指定的键值对，则为 true；否则为 false。</returns>
        public bool Contains(TK key, TV value)
        {
            if (key == null || value == null)
            {
                return false;
            }

            return _kv.ContainsKey(key) && _vk.ContainsKey(value);
        }
    }
}