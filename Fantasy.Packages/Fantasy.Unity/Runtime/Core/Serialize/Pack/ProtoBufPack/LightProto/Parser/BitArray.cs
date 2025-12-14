using System;
using System.Collections;
using System.Runtime.CompilerServices;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace LightProto.Parser
{
    [ProtoContract]
    [ProtoSurrogateFor(typeof(BitArray))]
    public partial struct BitArrayProtoParser
    {
        [ProtoMember(1, IsPacked = true)]
        internal bool[] Bits { get; set; }

        public static implicit operator BitArray(BitArrayProtoParser proxy)
        {
            return new BitArray(proxy.Bits ?? Array.Empty<bool>());
        }

        public static implicit operator BitArrayProtoParser(BitArray value)
        {
            if (value is null)
                return new BitArrayProtoParser { Bits = Array.Empty<bool>() };
            bool[] bits = new bool[value.Count];
            value.CopyTo(bits, 0);
            return new BitArrayProtoParser { Bits = bits };
        }
    }
}
