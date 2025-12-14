using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;
using System.Runtime;
using System.Reflection;
#if FANTASY_UNITY
using UnityEngine;
#endif

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
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
namespace Fantasy
{
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestEmptyMessage : AMessage, IMessage
    {
        public static C2G_TestEmptyMessage Create()
        {
            return MessageObjectPool<C2G_TestEmptyMessage>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_TestEmptyMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestEmptyMessage; } 
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_TestMessage : AMessage, IMessage
    {
        public static C2G_TestMessage Create()
        {
            return MessageObjectPool<C2G_TestMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2G_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRequest : AMessage, IRequest
    {
        public static C2G_TestRequest Create()
        {
            return MessageObjectPool<C2G_TestRequest>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2G_TestRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRequest; } 
        [ProtoIgnore]
        public G2C_TestResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_TestResponse : AMessage, IResponse
    {
        public static G2C_TestResponse Create()
        {
            return MessageObjectPool<G2C_TestResponse>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<G2C_TestResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_TestResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRequestPushMessage : AMessage, IMessage
    {
        public static C2G_TestRequestPushMessage Create()
        {
            return MessageObjectPool<C2G_TestRequestPushMessage>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_TestRequestPushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRequestPushMessage; } 
    }

    /// <summary>
    /// Gate服务器推送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_PushMessage : AMessage, IMessage
    {
        public static G2C_PushMessage Create()
        {
            return MessageObjectPool<G2C_PushMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<G2C_PushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PushMessage; } 
        /// <summary>
        /// 标记
        /// </summary>
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateAddressableRequest : AMessage, IRequest
    {
        public static C2G_CreateAddressableRequest Create()
        {
            return MessageObjectPool<C2G_CreateAddressableRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_CreateAddressableRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateAddressableRequest; } 
        [ProtoIgnore]
        public G2C_CreateAddressableResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateAddressableResponse : AMessage, IResponse
    {
        public static G2C_CreateAddressableResponse Create()
        {
            return MessageObjectPool<G2C_CreateAddressableResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_CreateAddressableResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateAddressableResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2M_TestMessage : AMessage, IAddressableMessage
    {
        public static C2M_TestMessage Create()
        {
            return MessageObjectPool<C2M_TestMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2M_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2M_TestRequest : AMessage, IAddressableRequest
    {
        public static C2M_TestRequest Create()
        {
            return MessageObjectPool<C2M_TestRequest>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2M_TestRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_TestRequest; } 
        [ProtoIgnore]
        public M2C_TestResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class M2C_TestResponse : AMessage, IAddressableResponse
    {
        public static M2C_TestResponse Create()
        {
            return MessageObjectPool<M2C_TestResponse>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<M2C_TestResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_TestResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 通知Gate服务器创建一个Chat的Route连接
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateChatRouteRequest : AMessage, IRequest
    {
        public static C2G_CreateChatRouteRequest Create()
        {
            return MessageObjectPool<C2G_CreateChatRouteRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_CreateChatRouteRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateChatRouteRequest; } 
        [ProtoIgnore]
        public G2C_CreateChatRouteResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateChatRouteResponse : AMessage, IResponse
    {
        public static G2C_CreateChatRouteResponse Create()
        {
            return MessageObjectPool<G2C_CreateChatRouteResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_CreateChatRouteResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateChatRouteResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 发送一个Route消息给Chat
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestMessage : AMessage, ICustomRouteMessage
    {
        public static C2Chat_TestMessage Create()
        {
            return MessageObjectPool<C2Chat_TestMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2Chat_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 发送一个RPCRoute消息给Chat
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestMessageRequest : AMessage, ICustomRouteRequest
    {
        public static C2Chat_TestMessageRequest Create()
        {
            return MessageObjectPool<C2Chat_TestMessageRequest>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2Chat_TestMessageRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestMessageRequest; } 
        [ProtoIgnore]
        public Chat2C_TestMessageResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class Chat2C_TestMessageResponse : AMessage, ICustomRouteResponse
    {
        public static Chat2C_TestMessageResponse Create()
        {
            return MessageObjectPool<Chat2C_TestMessageResponse>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<Chat2C_TestMessageResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Chat2C_TestMessageResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 发送一个RPC消息给Map，让Map里的Entity转移到另外一个Map上
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2M_MoveToMapRequest : AMessage, IAddressableRequest
    {
        public static C2M_MoveToMapRequest Create()
        {
            return MessageObjectPool<C2M_MoveToMapRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2M_MoveToMapRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_MoveToMapRequest; } 
        [ProtoIgnore]
        public M2C_MoveToMapResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class M2C_MoveToMapResponse : AMessage, IAddressableResponse
    {
        public static M2C_MoveToMapResponse Create()
        {
            return MessageObjectPool<M2C_MoveToMapResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<M2C_MoveToMapResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_MoveToMapResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 发送一个消息给Gate，让Gate发送一个Addressable消息给MAP
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_SendAddressableToMap : AMessage, IMessage
    {
        public static C2G_SendAddressableToMap Create()
        {
            return MessageObjectPool<C2G_SendAddressableToMap>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2G_SendAddressableToMap>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_SendAddressableToMap; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 发送一个消息给Chat，让Chat服务器主动推送一个RouteMessage消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestRequestPushMessage : AMessage, ICustomRouteMessage
    {
        public static C2Chat_TestRequestPushMessage Create()
        {
            return MessageObjectPool<C2Chat_TestRequestPushMessage>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2Chat_TestRequestPushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestRequestPushMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
    }

    /// <summary>
    /// Chat服务器主动推送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Chat2C_PushMessage : AMessage, ICustomRouteMessage
    {
        public static Chat2C_PushMessage Create()
        {
            return MessageObjectPool<Chat2C_PushMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<Chat2C_PushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Chat2C_PushMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 客户端发送给Gate服务器通知map服务器创建一个SubScene
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateSubSceneRequest : AMessage, IRequest
    {
        public static C2G_CreateSubSceneRequest Create()
        {
            return MessageObjectPool<C2G_CreateSubSceneRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_CreateSubSceneRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateSubSceneRequest; } 
        [ProtoIgnore]
        public G2C_CreateSubSceneResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateSubSceneResponse : AMessage, IResponse
    {
        public static G2C_CreateSubSceneResponse Create()
        {
            return MessageObjectPool<G2C_CreateSubSceneResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_CreateSubSceneResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateSubSceneResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 客户端通知Gate服务器给SubScene发送一个消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_SendToSubSceneMessage : AMessage, IMessage
    {
        public static C2G_SendToSubSceneMessage Create()
        {
            return MessageObjectPool<C2G_SendToSubSceneMessage>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_SendToSubSceneMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_SendToSubSceneMessage; } 
    }

    /// <summary>
    /// 客户端通知Gate服务器创建一个SubScene的Address消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateSubSceneAddressableRequest : AMessage, IRequest
    {
        public static C2G_CreateSubSceneAddressableRequest Create()
        {
            return MessageObjectPool<C2G_CreateSubSceneAddressableRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_CreateSubSceneAddressableRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateSubSceneAddressableRequest; } 
        [ProtoIgnore]
        public G2C_CreateSubSceneAddressableResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateSubSceneAddressableResponse : AMessage, IResponse
    {
        public static G2C_CreateSubSceneAddressableResponse Create()
        {
            return MessageObjectPool<G2C_CreateSubSceneAddressableResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_CreateSubSceneAddressableResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateSubSceneAddressableResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 客户端向SubScene发送一个测试消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2SubScene_TestMessage : AMessage, IAddressableMessage
    {
        public static C2SubScene_TestMessage Create()
        {
            return MessageObjectPool<C2SubScene_TestMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2SubScene_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2SubScene_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 客户端向SubScene发送一个销毁测试消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2SubScene_TestDisposeMessage : AMessage, IAddressableMessage
    {
        public static C2SubScene_TestDisposeMessage Create()
        {
            return MessageObjectPool<C2SubScene_TestDisposeMessage>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2SubScene_TestDisposeMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2SubScene_TestDisposeMessage; } 
    }

    /// <summary>
    /// 客户端向服务器发送连接消息（Roaming）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_ConnectRoamingRequest : AMessage, IRequest
    {
        public static C2G_ConnectRoamingRequest Create()
        {
            return MessageObjectPool<C2G_ConnectRoamingRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_ConnectRoamingRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ConnectRoamingRequest; } 
        [ProtoIgnore]
        public G2C_ConnectRoamingResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ConnectRoamingResponse : AMessage, IResponse
    {
        public static G2C_ConnectRoamingResponse Create()
        {
            return MessageObjectPool<G2C_ConnectRoamingResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_ConnectRoamingResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ConnectRoamingResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 测试一个Chat漫游普通消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestRoamingMessage : AMessage, IRoamingMessage
    {
        public static C2Chat_TestRoamingMessage Create()
        {
            return MessageObjectPool<C2Chat_TestRoamingMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2Chat_TestRoamingMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestRoamingMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.ChatRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 测试一个Map漫游普通消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Map_TestRoamingMessage : AMessage, IRoamingMessage
    {
        public static C2Map_TestRoamingMessage Create()
        {
            return MessageObjectPool<C2Map_TestRoamingMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2Map_TestRoamingMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Map_TestRoamingMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 测试一个Chat漫游RPC消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestRPCRoamingRequest : AMessage, IRoamingRequest
    {
        public static C2Chat_TestRPCRoamingRequest Create()
        {
            return MessageObjectPool<C2Chat_TestRPCRoamingRequest>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2Chat_TestRPCRoamingRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestRPCRoamingRequest; } 
        [ProtoIgnore]
        public Chat2C_TestRPCRoamingResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.ChatRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class Chat2C_TestRPCRoamingResponse : AMessage, IRoamingResponse
    {
        public static Chat2C_TestRPCRoamingResponse Create()
        {
            return MessageObjectPool<Chat2C_TestRPCRoamingResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<Chat2C_TestRPCRoamingResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Chat2C_TestRPCRoamingResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 客户端发送一个漫游消息给Map通知Map主动推送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Map_PushMessageToClient : AMessage, IRoamingMessage
    {
        public static C2Map_PushMessageToClient Create()
        {
            return MessageObjectPool<C2Map_PushMessageToClient>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2Map_PushMessageToClient>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Map_PushMessageToClient; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 漫游端发送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Map2C_PushMessageToClient : AMessage, IRoamingMessage
    {
        public static Map2C_PushMessageToClient Create()
        {
            return MessageObjectPool<Map2C_PushMessageToClient>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<Map2C_PushMessageToClient>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Map2C_PushMessageToClient; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 测试传送漫游的触发协议
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Map_TestTransferRequest : AMessage, IRoamingRequest
    {
        public static C2Map_TestTransferRequest Create()
        {
            return MessageObjectPool<C2Map_TestTransferRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2Map_TestTransferRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Map_TestTransferRequest; } 
        [ProtoIgnore]
        public Map2C_TestTransferResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
    }

    [Serializable]
    [ProtoContract]
    public partial class Map2C_TestTransferResponse : AMessage, IRoamingResponse
    {
        public static Map2C_TestTransferResponse Create()
        {
            return MessageObjectPool<Map2C_TestTransferResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<Map2C_TestTransferResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Map2C_TestTransferResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 测试一个Chat发送到Map之间漫游协议
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestSendMapMessage : AMessage, IRoamingMessage
    {
        public static C2Chat_TestSendMapMessage Create()
        {
            return MessageObjectPool<C2Chat_TestSendMapMessage>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2Chat_TestSendMapMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestSendMapMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.ChatRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 通知Gate服务器发送一个Route消息给Map的漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRouteToRoaming : AMessage, IMessage
    {
        public static C2G_TestRouteToRoaming Create()
        {
            return MessageObjectPool<C2G_TestRouteToRoaming>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2G_TestRouteToRoaming>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRouteToRoaming; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 通知Gate服务器发送一个漫游消息给Map的漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRoamingToRoaming : AMessage, IMessage
    {
        public static C2G_TestRoamingToRoaming Create()
        {
            return MessageObjectPool<C2G_TestRoamingToRoaming>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2G_TestRoamingToRoaming>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRoamingToRoaming; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

    /// <summary>
    /// 通知Gate服务器发送一个内网消息通知Map服务器向Gate服务器注册一个领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_SubscribeSphereEventRequest : AMessage, IRequest
    {
        public static C2G_SubscribeSphereEventRequest Create()
        {
            return MessageObjectPool<C2G_SubscribeSphereEventRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_SubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_SubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_SubscribeSphereEventResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_SubscribeSphereEventResponse : AMessage, IResponse
    {
        public static G2C_SubscribeSphereEventResponse Create()
        {
            return MessageObjectPool<G2C_SubscribeSphereEventResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_SubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_SubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 通知Gate发送一个订阅领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_PublishSphereEventRequest : AMessage, IRequest
    {
        public static C2G_PublishSphereEventRequest Create()
        {
            return MessageObjectPool<C2G_PublishSphereEventRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_PublishSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_PublishSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_PublishSphereEventResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_PublishSphereEventResponse : AMessage, IResponse
    {
        public static G2C_PublishSphereEventResponse Create()
        {
            return MessageObjectPool<G2C_PublishSphereEventResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_PublishSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PublishSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 通知Gate取消一个订阅领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_UnsubscribeSphereEventRequest : AMessage, IRequest
    {
        public static C2G_UnsubscribeSphereEventRequest Create()
        {
            return MessageObjectPool<C2G_UnsubscribeSphereEventRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_UnsubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_UnsubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_UnsubscribeSphereEventResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_UnsubscribeSphereEventResponse : AMessage, IResponse
    {
        public static G2C_UnsubscribeSphereEventResponse Create()
        {
            return MessageObjectPool<G2C_UnsubscribeSphereEventResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_UnsubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_UnsubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

    /// <summary>
    /// 通知Map取消一个订阅领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_MapUnsubscribeSphereEventRequest : AMessage, IRequest
    {
        public static C2G_MapUnsubscribeSphereEventRequest Create()
        {
            return MessageObjectPool<C2G_MapUnsubscribeSphereEventRequest>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_MapUnsubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_MapUnsubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_MapUnsubscribeSphereEventResponse ResponseType { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_MapUnsubscribeSphereEventResponse : AMessage, IResponse
    {
        public static G2C_MapUnsubscribeSphereEventResponse Create()
        {
            return MessageObjectPool<G2C_MapUnsubscribeSphereEventResponse>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<G2C_MapUnsubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_MapUnsubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }

#if Godot_Platform
    [Obsolete("带标签消息测试, 非实用")]
    [Serializable]
    [ProtoContract]
    public partial class C2G_带标签的消息 : AMessage, IMessage
    {
        public static C2G_带标签的消息 Create()
        {
            return MessageObjectPool<C2G_带标签的消息>.Rent();
        }

        public void Dispose()
        {
            MessageObjectPool<C2G_带标签的消息>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_带标签的消息; } 
    }

#if Godot_Platform
    [我的某个标签]
#endif
    [Serializable]
    [ProtoContract]
    public partial class C2M_带标签且条件编译的消息 : AMessage, IMessage
    {
        public static C2M_带标签且条件编译的消息 Create()
        {
            return MessageObjectPool<C2M_带标签且条件编译的消息>.Rent();
        }

        public void Dispose()
        {
            Tag = default;
            MessageObjectPool<C2M_带标签且条件编译的消息>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_带标签且条件编译的消息; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }

#endif
}