using LightProto;
using MemoryPack;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
namespace Fantasy
{
    [Serializable]
    [ProtoContract]
    public partial class G2A_TestMessage : AMessage, IAddressMessage
    {
        public static G2A_TestMessage Create()
        {
            return MessageObjectPool<G2A_TestMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<G2A_TestMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_TestRequest : AMessage, IAddressRequest
    {
        public static G2A_TestRequest Create()
        {
            return MessageObjectPool<G2A_TestRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2A_TestRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_TestRequest; } 
        [ProtoIgnore]
        public G2A_TestResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_TestResponse : AMessage, IAddressResponse
    {
        public static G2A_TestResponse Create()
        {
            return MessageObjectPool<G2A_TestResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            MessageObjectPool<G2A_TestResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_TestResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2M_RequestAddressableId : AMessage, IAddressRequest
    {
        public static G2M_RequestAddressableId Create()
        {
            return MessageObjectPool<G2M_RequestAddressableId>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2M_RequestAddressableId>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2M_RequestAddressableId; } 
        [ProtoIgnore]
        public M2G_ResponseAddressableId ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class M2G_ResponseAddressableId : AMessage, IAddressResponse
    {
        public static M2G_ResponseAddressableId Create()
        {
            return MessageObjectPool<M2G_ResponseAddressableId>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            AddressableId = default;
            MessageObjectPool<M2G_ResponseAddressableId>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2G_ResponseAddressableId; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long AddressableId { get; set; }
    }

    /// <summary>
    /// 通知Chat服务器创建一个RouteId
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Chat_CreateRouteRequest : AMessage, IAddressRequest
    {
        public static G2Chat_CreateRouteRequest Create()
        {
            return MessageObjectPool<G2Chat_CreateRouteRequest>.Rent();
        }

        public void Dispose()
        {
            GateAddress = default;
            MessageObjectPool<G2Chat_CreateRouteRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Chat_CreateRouteRequest; } 
        [ProtoIgnore]
        public Chat2G_CreateRouteResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long GateAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class Chat2G_CreateRouteResponse : AMessage, IAddressResponse
    {
        public static Chat2G_CreateRouteResponse Create()
        {
            return MessageObjectPool<Chat2G_CreateRouteResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            ChatAddress = default;
            MessageObjectPool<Chat2G_CreateRouteResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Chat2G_CreateRouteResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long ChatAddress { get; set; }
    }

    /// <summary>
    /// Map给另外一个Map发送Unit数据
    /// </summary>
    [Serializable]
    [MemoryPackable]
    public partial class M2M_SendUnitRequest : AMessage, IAddressRequest
    {
        public static M2M_SendUnitRequest Create()
        {
            return MessageObjectPool<M2M_SendUnitRequest>.Rent();
        }

        public void Dispose()
        {
            Unit = default;
            MessageObjectPool<M2M_SendUnitRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2M_SendUnitRequest; } 
        [MemoryPackIgnore]
        public M2M_SendUnitResponse ResponseType { get; set; }
        [MemoryPackOrder(1)]
        public Unit Unit { get; set; }
    }

    [Serializable]
    [MemoryPackable]
    public partial class M2M_SendUnitResponse : AMessage, IAddressResponse
    {
        public static M2M_SendUnitResponse Create()
        {
            return MessageObjectPool<M2M_SendUnitResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            MessageObjectPool<M2M_SendUnitResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2M_SendUnitResponse; } 
        [MemoryPackOrder(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// Gate发送Addressable消息给MAP
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2M_SendAddressableMessage : AMessage, IAddressableMessage
    {
        public static G2M_SendAddressableMessage Create()
        {
            return MessageObjectPool<G2M_SendAddressableMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<G2M_SendAddressableMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2M_SendAddressableMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2M_CreateSubSceneRequest : AMessage, IAddressRequest
    {
        public static G2M_CreateSubSceneRequest Create()
        {
            return MessageObjectPool<G2M_CreateSubSceneRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2M_CreateSubSceneRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2M_CreateSubSceneRequest; } 
        [ProtoIgnore]
        public M2G_CreateSubSceneResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class M2G_CreateSubSceneResponse : AMessage, IAddressResponse
    {
        public static M2G_CreateSubSceneResponse Create()
        {
            return MessageObjectPool<M2G_CreateSubSceneResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            SubSceneAddress = default;
            MessageObjectPool<M2G_CreateSubSceneResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2G_CreateSubSceneResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long SubSceneAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2SubScene_SentMessage : AMessage, IAddressMessage
    {
        public static G2SubScene_SentMessage Create()
        {
            return MessageObjectPool<G2SubScene_SentMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<G2SubScene_SentMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2SubScene_SentMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// Gate通知SubScene创建一个Addressable消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2SubScene_AddressableIdRequest : AMessage, IAddressRequest
    {
        public static G2SubScene_AddressableIdRequest Create()
        {
            return MessageObjectPool<G2SubScene_AddressableIdRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2SubScene_AddressableIdRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2SubScene_AddressableIdRequest; } 
        [ProtoIgnore]
        public SubScene2G_AddressableIdResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class SubScene2G_AddressableIdResponse : AMessage, IAddressResponse
    {
        public static SubScene2G_AddressableIdResponse Create()
        {
            return MessageObjectPool<SubScene2G_AddressableIdResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            AddressableId = default;
            MessageObjectPool<SubScene2G_AddressableIdResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.SubScene2G_AddressableIdResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long AddressableId { get; set; }
    }

    /// <summary>
    /// Chat发送一个漫游消息给Map
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Chat2M_TestMessage : AMessage, IRoamingMessage
    {
        public static Chat2M_TestMessage Create()
        {
            return MessageObjectPool<Chat2M_TestMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<Chat2M_TestMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Chat2M_TestMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 测试一个Gate服务器发送一个Route消息给某个漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_TestRouteMessageRequest : AMessage, IAddressRequest
    {
        public static G2Map_TestRouteMessageRequest Create()
        {
            return MessageObjectPool<G2Map_TestRouteMessageRequest>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<G2Map_TestRouteMessageRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_TestRouteMessageRequest; } 
        [ProtoIgnore]
        public Map2G_TestRouteMessageResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class Map2G_TestRouteMessageResponse : AMessage, IAddressResponse
    {
        public static Map2G_TestRouteMessageResponse Create()
        {
            return MessageObjectPool<Map2G_TestRouteMessageResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            MessageObjectPool<Map2G_TestRouteMessageResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Map2G_TestRouteMessageResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 测试一个Gate服务器发送一个漫游协议给某个漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_TestRoamingMessageRequest : AMessage, IRoamingRequest
    {
        public static G2Map_TestRoamingMessageRequest Create()
        {
            return MessageObjectPool<G2Map_TestRoamingMessageRequest>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<G2Map_TestRoamingMessageRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_TestRoamingMessageRequest; } 
        [ProtoIgnore]
        public Map2G_TestRoamingMessageResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class Map2G_TestRoamingMessageResponse : AMessage, IRoamingResponse
    {
        public static Map2G_TestRoamingMessageResponse Create()
        {
            return MessageObjectPool<Map2G_TestRoamingMessageResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            MessageObjectPool<Map2G_TestRoamingMessageResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Map2G_TestRoamingMessageResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// Gate服务器通知Map订阅一个领域事件到Gate上
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_SubscribeSphereEventRequest : AMessage, IAddressRequest
    {
        public static G2Map_SubscribeSphereEventRequest Create()
        {
            return MessageObjectPool<G2Map_SubscribeSphereEventRequest>.Rent();
        }

        public void Dispose()
        {
            GateAddress = default;
            MessageObjectPool<G2Map_SubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_SubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2Map_SubscribeSphereEventResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long GateAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2Map_SubscribeSphereEventResponse : AMessage, IAddressResponse
    {
        public static G2Map_SubscribeSphereEventResponse Create()
        {
            return MessageObjectPool<G2Map_SubscribeSphereEventResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            MessageObjectPool<G2Map_SubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_SubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// Gate服务器通知Map取消订阅一个领域事件到Gate上
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_UnsubscribeSphereEventRequest : AMessage, IAddressRequest
    {
        public static G2Map_UnsubscribeSphereEventRequest Create()
        {
            return MessageObjectPool<G2Map_UnsubscribeSphereEventRequest>.Rent();
        }

        public void Dispose()
        {
            GateAddress = default;
            MessageObjectPool<G2Map_UnsubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_UnsubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public Map2G_UnsubscribeSphereEventResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long GateAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class Map2G_UnsubscribeSphereEventResponse : AMessage, IAddressResponse
    {
        public static Map2G_UnsubscribeSphereEventResponse Create()
        {
            return MessageObjectPool<Map2G_UnsubscribeSphereEventResponse>.Rent();
        }

        public void Dispose()
        {
            ErrorCode = 0;
            MessageObjectPool<Map2G_UnsubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Map2G_UnsubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

}