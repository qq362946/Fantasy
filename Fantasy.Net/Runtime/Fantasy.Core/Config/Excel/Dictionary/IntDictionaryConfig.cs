using System.Collections.Generic;
using ProtoBuf;
// ReSharper disable CheckNamespace
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy
{
    /// <summary>
    /// 使用 ProtoBuf 序列化的整数字典配置类。
    /// </summary>
    [ProtoContract]
    public class IntDictionaryConfig
    {
        /// <summary>
        /// 使用 ProtoBuf 序列化的字典。
        /// </summary>
        [ProtoMember(1, IsRequired = true)] 
        public Dictionary<int, int> Dic;

        /// <summary>
        /// 获取或设置指定键的整数值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns>整数值。</returns>
        public int this[int key] => GetValue(key);

        /// <summary>
        /// 尝试获取指定键的整数值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">获取到的整数值。</param>
        /// <returns>如果成功获取到值，则返回 true，否则返回 false。</returns>
        public bool TryGetValue(int key, out int value)
        {
            value = default;

            if (!Dic.ContainsKey(key))
            {
                return false;
            }
        
            value = Dic[key];
            return true;
        }

        private int GetValue(int key)
        {
            return Dic.TryGetValue(key, out var value) ? value : 0;
        }
    }
}