using System.Collections.Generic;
using ProtoBuf;
// ReSharper disable CheckNamespace
// ReSharper disable CollectionNeverUpdated.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603 // Possible null reference return.
namespace Fantasy
{
    /// <summary>
    /// 使用 ProtoBuf 序列化的字符串字典配置类。
    /// </summary>
    [ProtoContract]
    public sealed class StringDictionaryConfig
    {
        /// <summary>
        /// 使用 ProtoBuf 序列化的字典。
        /// </summary>
        [ProtoMember(1, IsRequired = true)] 
        public Dictionary<int, string> Dic;

        /// <summary>
        /// 获取或设置指定键的字符串值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns>字符串值。</returns>
        public string this[int key] => GetValue(key);

        /// <summary>
        /// 尝试获取指定键的字符串值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">获取到的字符串值。</param>
        /// <returns>如果成功获取到值，则返回 true，否则返回 false。</returns>
        public bool TryGetValue(int key, out string value)
        {
            value = default;

            if (!Dic.ContainsKey(key))
            {
                return false;
            }
        
            value = Dic[key];
            return true;
        }

        private string GetValue(int key)
        {
            return Dic.TryGetValue(key, out var value) ? value : null;
        }
    }
}