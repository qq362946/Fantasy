#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Network
{
    public struct OpCodeIdStruct
    {
        // OpCodeIdStruct:5 + 4 + 23 = 32
        // +-------------------------+-------------------------------------------+-----------------------------+
        // |  protocol(5) 最多31种类型 | OpCodeProtocolType(4) 最多15种不同的网络协议 | Index(23) 最多8388607个协议  |
        // +-------------------------+-------------------------------------------+-----------------------------+
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
        public const uint ProtoBuf = 0;
        public const uint Bson = 1; 
        public const uint MemoryPack = 2;
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
        
        public const uint OuterRoamingMessage = 19;
        public const uint OuterRoamingRequest = 20;
        public const uint OuterRoamingResponse = 21;
        
        public const uint InnerRoamingMessage = 22;
        public const uint InnerRoamingRequest = 23;
        public const uint InnerRoamingResponse = 24;
        
        public const uint OuterPingRequest = 30;
        public const uint OuterPingResponse = 31;
    }

    public static class OpCode
    {
        // 格式: Index | (OpCodeProtocolType << 23) | (Protocol << 27)
        // 所有值已预先计算，使 SourceGenerator 可以在编译时获取这些常量

        public const uint BenchmarkMessage = 142606335;                             // Create(ProtoBuf=0, OuterMessage=1, 8388607)
        public const uint BenchmarkRequest = 276824063;                             // Create(ProtoBuf=0, OuterRequest=2, 8388607)
        public const uint BenchmarkResponse = 411041791;                            // Create(ProtoBuf=0, OuterResponse=3, 8388607)
        public const uint PingRequest = 4026531841;                                 // Create(ProtoBuf=0, OuterPingRequest=30, 1)
        public const uint PingResponse = 4160749569;                                // Create(ProtoBuf=0, OuterPingResponse=31, 1)
        public const uint DefaultResponse = 805306369;                              // Create(ProtoBuf=0, InnerResponse=6, 1)
        public const uint DefaultAddressResponse = 1207959559;                      // Create(ProtoBuf=0, InnerAddressResponse=9, 7)
        public const uint AddressableAddRequest = 1073741825;                       // Create(ProtoBuf=0, InnerAddressRequest=8, 1)
        public const uint AddressableAddResponse = 1207959553;                      // Create(ProtoBuf=0, InnerAddressResponse=9, 1)
        public const uint AddressableGetRequest = 1073741826;                       // Create(ProtoBuf=0, InnerAddressRequest=8, 2)
        public const uint AddressableGetResponse = 1207959554;                      // Create(ProtoBuf=0, InnerAddressResponse=9, 2)
        public const uint AddressableRemoveRequest = 1073741827;                    // Create(ProtoBuf=0, InnerAddressRequest=8, 3)
        public const uint AddressableRemoveResponse = 1207959555;                   // Create(ProtoBuf=0, InnerAddressResponse=9, 3)
        public const uint AddressableLockRequest = 1073741828;                      // Create(ProtoBuf=0, InnerAddressRequest=8, 4)
        public const uint AddressableLockResponse = 1207959556;                     // Create(ProtoBuf=0, InnerAddressResponse=9, 4)
        public const uint AddressableUnLockRequest = 1073741829;                    // Create(ProtoBuf=0, InnerAddressRequest=8, 5)
        public const uint AddressableUnLockResponse = 1207959557;                   // Create(ProtoBuf=0, InnerAddressResponse=9, 5)
        public const uint LinkRoamingRequest = 1073741830;                          // Create(ProtoBuf=0, InnerAddressRequest=8, 6)
        public const uint LinkRoamingResponse = 1207959558;                         // Create(ProtoBuf=0, InnerAddressResponse=9, 6)
        public const uint UnLinkRoamingRequest = 1073741832;                        // Create(ProtoBuf=0, InnerAddressRequest=8, 8)
        public const uint UnLinkRoamingResponse = 1207959560;                       // Create(ProtoBuf=0, InnerAddressResponse=9, 8)
        public const uint LockTerminusIdRequest = 1073741833;                       // Create(ProtoBuf=0, InnerAddressRequest=8, 9)
        public const uint LockTerminusIdResponse = 1207959561;                      // Create(ProtoBuf=0, InnerAddressResponse=9, 9)
        public const uint UnLockTerminusIdRequest = 1073741834;                     // Create(ProtoBuf=0, InnerAddressRequest=8, 10)
        public const uint UnLockTerminusIdResponse = 1207959562;                    // Create(ProtoBuf=0, InnerAddressResponse=9, 10)
        public const uint GetTerminusIdRequest = 1073741835;                        // Create(ProtoBuf=0, InnerAddressRequest=8, 11)
        public const uint GetTerminusIdResponse = 1207959563;                       // Create(ProtoBuf=0, InnerAddressResponse=9, 11)
        public const uint SetForwardSessionAddressRequest = 1073741836;             // Create(ProtoBuf=0, InnerAddressRequest=8, 12)
        public const uint SetForwardSessionAddressResponse = 1207959564;            // Create(ProtoBuf=0, InnerAddressResponse=9, 12)
        public const uint StopForwardingRequest = 1073741837;                       // Create(ProtoBuf=0, InnerAddressRequest=8, 13)
        public const uint StopForwardingResponse = 1207959565;                      // Create(ProtoBuf=0, InnerAddressResponse=9, 13)
        
        public const uint TransferTerminusRequest = 1082130433;                     // Create(Bson=1, InnerAddressRequest=8, 1)
        public const uint TransferTerminusResponse = 1216348161;                    // Create(Bson=1, InnerAddressResponse=9, 1)
        
        public const uint SubscribeSphereEventRequest = 1082130434;                 // Create(Bson=1, InnerAddressRequest=8, 2)
        public const uint SubscribeSphereEventResponse = 1216348162;                // Create(Bson=1, InnerAddressResponse=9, 2)
        public const uint UnsubscribeSphereEventRequest = 1082130435;               // Create(Bson=1, InnerAddressRequest=8, 3)
        public const uint UnsubscribeSphereEventResponse = 1216348163;              // Create(Bson=1, InnerAddressResponse=9, 3)
        public const uint RevokeRemoteSubscriberRequest = 1082130436;               // Create(Bson=1, InnerAddressRequest=8, 4)
        public const uint RevokeRemoteSubscriberResponse = 1216348164;              // Create(Bson=1, InnerAddressResponse=9, 4)
        public const uint PublishSphereEventRequest = 1082130437;                   // Create(Bson=1, InnerAddressRequest=8, 5)
        public const uint PublishSphereEventResponse = 1216348165;                  // Create(Bson=1, InnerAddressResponse=9, 5)

        /// <summary>
        /// 创建 OpCode（运行时使用）
        /// </summary>
        public static uint Create(uint opCodeProtocolType, uint protocol, uint index)
        {
            return new OpCodeIdStruct(opCodeProtocolType, protocol, index);
        }
    }
}