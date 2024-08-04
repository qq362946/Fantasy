using System.Collections.Generic;
#pragma warning disable CS8601 // Possible null reference assignment.

namespace Fantasy
{
    /// <summary>
    /// 提供对字典的扩展方法。
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// 尝试从字典中移除指定键，并返回相应的值。
        /// </summary>
        /// <typeparam name="T">字典中键的类型。</typeparam>
        /// <typeparam name="TV">字典中值的类型。</typeparam>
        /// <param name="self">要操作的字典实例。</param>
        /// <param name="key">要移除的键。</param>
        /// <param name="value">从字典中移除的值（如果成功移除）。</param>
        /// <returns>如果成功移除键值对，则为 true；否则为 false。</returns>
        public static bool TryRemove<T, TV>(this IDictionary<T, TV> self, T key, out TV value)
        {
            if (!self.TryGetValue(key, out value))
            {
                return false;
            }

            self.Remove(key);
            return true;
        }
    }
}