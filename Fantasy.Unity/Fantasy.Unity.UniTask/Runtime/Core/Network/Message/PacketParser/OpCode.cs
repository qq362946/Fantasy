#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Network
{
    public struct OpCodeIdStruct
    {
        // OpCodeIdStruct:5 + 4 + 23 = 32
        // +---------------------------+---------------------------------+-----------------------------+
        // |  OpCodeType(5) 最多31种类型 | Protocol(4) 最多15种不同的网络协议 | Index(23) 最多8388607个协议  |
        // +---------------------------+---------------------------------+-----------------------------+
        public uint OpCodeProtocolType { get; private set; }
        public uint Protocol { get; private set; }
        public uint Index { get; private set; }
        
        public OpCodeIdStruct(uint opCodeProtocolType, uint protocol, uint index)
        {
            OpCodeProtocolType = opCodeProtocolType;
            Protocol = protocol;
            Index = index;
        }

        public static implicit operator uint(OpCodeIdStruct opCodeIdStruct)
        {
            var result = opCodeIdStruct.Index;
            result |= opCodeIdStruct.OpCodeProtocolType << 23;
            result |= opCodeIdStruct.Protocol << 27;
            return result;
        }

        public static implicit operator OpCodeIdStruct(uint opCodeId)
        {
            var opCodeIdStruct = new OpCodeIdStruct()
            {
                Index = opCodeId & 0x7FFFFF
            };
            opCodeId >>= 23;
            opCodeIdStruct.OpCodeProtocolType = opCodeId & 0xF;
            opCodeId >>= 4;
            opCodeIdStruct.Protocol = opCodeId & 0x1F;
            return opCodeIdStruct;
        }
    }

    public static class OpCodeProtocolType
    {
        public const uint Bson = 1; 
        public const uint ProtoBuf = 0;
    }

    public static class OpCodeType
    {
        public const uint OuterMessage = 1; 
        public const uint OuterRequest = 2;
        public const uint OuterResponse = 3;
        
        public const uint InnerMessage  = 4;
        public const uint InnerRequest  = 5;
        public const uint InnerResponse = 6;
        
        public const uint InnerRouteMessage = 7;
        public const uint InnerRouteRequest = 8;
        public const uint InnerRouteResponse = 9;
        
        public const uint OuterAddressableMessage = 10;
        public const uint OuterAddressableRequest = 11;
        public const uint OuterAddressableResponse = 12;
        
        public const uint InnerAddressableMessage = 13;
        public const uint InnerAddressableRequest = 14;
        public const uint InnerAddressableResponse = 15;
        
        public const uint OuterCustomRouteMessage = 16;
        public const uint OuterCustomRouteRequest = 17;
        public const uint OuterCustomRouteResponse = 18;
        
        public const uint OuterPingRequest = 19;
        public const uint OuterPingResponse = 20;
    }

    public static class OpCode
    {
        public static readonly uint BenchmarkMessage = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.OuterMessage, 8388607);
        public static readonly uint BenchmarkRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.OuterRequest, 8388607);
        public static readonly uint BenchmarkResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.OuterResponse, 8388607);
        public static readonly uint PingRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.OuterPingRequest, 1);
        public static readonly uint PingResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.OuterPingResponse, 1);
        public static readonly uint DefaultResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerResponse, 1);
        public static readonly uint DefaultRouteResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteResponse, 7);
        public static readonly uint AddressableAddRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteRequest, 1);
        public static readonly uint AddressableAddResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteResponse, 1);
        public static readonly uint AddressableGetRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteRequest, 2);
        public static readonly uint AddressableGetResponse = Create(OpCodeProtocolType.ProtoBuf,OpCodeType.InnerRouteResponse,2);
        public static readonly uint AddressableRemoveRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteRequest, 3);
        public static readonly uint AddressableRemoveResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteResponse, 3);
        public static readonly uint AddressableLockRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteRequest, 4);
        public static readonly uint AddressableLockResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteResponse, 4);
        public static readonly uint AddressableUnLockRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteRequest, 5);
        public static readonly uint AddressableUnLockResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteResponse, 5);
        public static readonly uint LinkEntityRequest = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteRequest, 6);
        public static readonly uint LinkEntityResponse = Create(OpCodeProtocolType.ProtoBuf, OpCodeType.InnerRouteResponse, 6);
        
        public static uint Create(uint opCodeProtocolType, uint protocol, uint index)
        {
            return new OpCodeIdStruct(opCodeProtocolType, protocol, index);
        }
    }
}