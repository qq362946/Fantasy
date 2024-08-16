using System.Collections.Generic;
using MessagePack;
// ReSharper disable CheckNamespace
// ReSharper disable CollectionNeverUpdated.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603 // Possible null reference return.
namespace Fantasy
{
    [MessagePackObject]
    public sealed class StringDictionaryConfig
    {
        [Key(0)]
        public Dictionary<int, string> Dic;
        public string this[int key] => GetValue(key);
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