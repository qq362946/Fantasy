using System.Collections.Generic;
using MessagePack;
// ReSharper disable CheckNamespace
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy
{
    [MessagePackObject]
    public class IntDictionaryConfig
    {
        [Key(0)] 
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