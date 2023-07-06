using System.Collections.Generic;
using ProtoBuf;

namespace Fantasy.Core
{
    [ProtoContract]
    public sealed class AttrConfig
    {
        [ProtoMember(1, IsRequired = true)] public Dictionary<int, int> KV;

        public int this[int key] => GetValue(key);

        public bool TryGetValue(int key, out int value)
        {
            value = 0;

            if (!KV.ContainsKey(key))
            {
                return false;
            }
        
            value = KV[key];
            return true;
        }

        private int GetValue(int key)
        {
            return KV.ContainsKey(key) ? KV[key] : 0;
        }
    }
}