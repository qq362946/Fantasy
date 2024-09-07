using System.Collections.Generic;
using ProtoBuf;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable CheckNamespace
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.ConfigTable
{
    [ProtoContract]
    public partial class IntDictionaryConfig
    {
        public Dictionary<int, int> Dic;
        public int this[int key] => GetValue(key);
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