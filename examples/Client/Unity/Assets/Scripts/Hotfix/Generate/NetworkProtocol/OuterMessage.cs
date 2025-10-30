using ProtoBuf;
using System;

using System.Collections.Generic;
using Fantasy;
using Fantasy.Network.Interface;
using Fantasy.Serialize;
#pragma warning disable CS8618

namespace Fantasy
{
	[ProtoContract]
	public partial class C2G_TestMessage : AMessage, IMessage
	{
		public static C2G_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_TestMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class C2G_TestRequest : AMessage, IRequest
	{
		public static C2G_TestRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_TestRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_TestRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_TestRequest; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class G2C_TestResponse : AMessage, IResponse
	{
		public static G2C_TestResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_TestResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_TestResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_TestResponse; }
		[ProtoMember(1)]
		public string Tag { get; set; }
		[ProtoMember(2)]
		public uint ErrorCode { get; set; }
	}
	[ProtoContract]
	public partial class C2G_TestRequestPushMessage : AMessage, IMessage
	{
		public static C2G_TestRequestPushMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_TestRequestPushMessage>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_TestRequestPushMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_TestRequestPushMessage; }
	}
	/// <summary>
	///  Gate服务器推送一个消息给客户端
	/// </summary>
	[ProtoContract]
	public partial class G2C_PushMessage : AMessage, IMessage
	{
		public static G2C_PushMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_PushMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_PushMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_PushMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class C2G_CreateAddressableRequest : AMessage, IRequest
	{
		public static C2G_CreateAddressableRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_CreateAddressableRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_CreateAddressableRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_CreateAddressableResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateAddressableRequest; }
	}
	[ProtoContract]
	public partial class G2C_CreateAddressableResponse : AMessage, IResponse
	{
		public static G2C_CreateAddressableResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_CreateAddressableResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_CreateAddressableResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateAddressableResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	[ProtoContract]
	public partial class C2M_TestMessage : AMessage, IAddressableRouteMessage
	{
		public static C2M_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2M_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_TestMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class C2M_TestRequest : AMessage, IAddressableRouteRequest
	{
		public static C2M_TestRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_TestRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2M_TestRequest>(this);
#endif
		}
		[ProtoIgnore]
		public M2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2M_TestRequest; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class M2C_TestResponse : AMessage, IAddressableRouteResponse
	{
		public static M2C_TestResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2C_TestResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<M2C_TestResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.M2C_TestResponse; }
		[ProtoMember(1)]
		public string Tag { get; set; }
		[ProtoMember(2)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  通知Gate服务器创建一个Chat的Route连接
	/// </summary>
	[ProtoContract]
	public partial class C2G_CreateChatRouteRequest : AMessage, IRequest
	{
		public static C2G_CreateChatRouteRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_CreateChatRouteRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_CreateChatRouteRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_CreateChatRouteResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateChatRouteRequest; }
	}
	[ProtoContract]
	public partial class G2C_CreateChatRouteResponse : AMessage, IResponse
	{
		public static G2C_CreateChatRouteResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_CreateChatRouteResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_CreateChatRouteResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateChatRouteResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  发送一个Route消息给Chat
	/// </summary>
	[ProtoContract]
	public partial class C2Chat_TestMessage : AMessage, ICustomRouteMessage
	{
		public static C2Chat_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Chat_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2Chat_TestMessage; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RouteType.ChatRoute;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  发送一个RPCRoute消息给Chat
	/// </summary>
	[ProtoContract]
	public partial class C2Chat_TestMessageRequest : AMessage, ICustomRouteRequest
	{
		public static C2Chat_TestMessageRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestMessageRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Chat_TestMessageRequest>(this);
#endif
		}
		[ProtoIgnore]
		public Chat2C_TestMessageResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2Chat_TestMessageRequest; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RouteType.ChatRoute;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class Chat2C_TestMessageResponse : AMessage, ICustomRouteResponse
	{
		public static Chat2C_TestMessageResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Chat2C_TestMessageResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Chat2C_TestMessageResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.Chat2C_TestMessageResponse; }
		[ProtoMember(1)]
		public string Tag { get; set; }
		[ProtoMember(2)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  发送一个RPC消息给Map，让Map里的Entity转移到另外一个Map上
	/// </summary>
	[ProtoContract]
	public partial class C2M_MoveToMapRequest : AMessage, IAddressableRouteRequest
	{
		public static C2M_MoveToMapRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_MoveToMapRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2M_MoveToMapRequest>(this);
#endif
		}
		[ProtoIgnore]
		public M2C_MoveToMapResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2M_MoveToMapRequest; }
	}
	[ProtoContract]
	public partial class M2C_MoveToMapResponse : AMessage, IAddressableRouteResponse
	{
		public static M2C_MoveToMapResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2C_MoveToMapResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<M2C_MoveToMapResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.M2C_MoveToMapResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  发送一个消息给Gate，让Gate发送一个Addressable消息给MAP
	/// </summary>
	[ProtoContract]
	public partial class C2G_SendAddressableToMap : AMessage, IMessage
	{
		public static C2G_SendAddressableToMap Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_SendAddressableToMap>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_SendAddressableToMap>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_SendAddressableToMap; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  发送一个消息给Chat，让Chat服务器主动推送一个RouteMessage消息给客户端
	/// </summary>
	[ProtoContract]
	public partial class C2Chat_TestRequestPushMessage : AMessage, ICustomRouteMessage
	{
		public static C2Chat_TestRequestPushMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestRequestPushMessage>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Chat_TestRequestPushMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2Chat_TestRequestPushMessage; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RouteType.ChatRoute;
	}
	/// <summary>
	///  Chat服务器主动推送一个消息给客户端
	/// </summary>
	[ProtoContract]
	public partial class Chat2C_PushMessage : AMessage, ICustomRouteMessage
	{
		public static Chat2C_PushMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Chat2C_PushMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Chat2C_PushMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.Chat2C_PushMessage; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RouteType.ChatRoute;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  客户端发送给Gate服务器通知map服务器创建一个SubScene
	/// </summary>
	[ProtoContract]
	public partial class C2G_CreateSubSceneRequest : AMessage, IRequest
	{
		public static C2G_CreateSubSceneRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_CreateSubSceneRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_CreateSubSceneRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_CreateSubSceneResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateSubSceneRequest; }
	}
	[ProtoContract]
	public partial class G2C_CreateSubSceneResponse : AMessage, IResponse
	{
		public static G2C_CreateSubSceneResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_CreateSubSceneResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_CreateSubSceneResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateSubSceneResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  客户端通知Gate服务器给SubScene发送一个消息
	/// </summary>
	[ProtoContract]
	public partial class C2G_SendToSubSceneMessage : AMessage, IMessage
	{
		public static C2G_SendToSubSceneMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_SendToSubSceneMessage>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_SendToSubSceneMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_SendToSubSceneMessage; }
	}
	/// <summary>
	///  客户端通知Gate服务器创建一个SubScene的Address消息
	/// </summary>
	[ProtoContract]
	public partial class C2G_CreateSubSceneAddressableRequest : AMessage, IRequest
	{
		public static C2G_CreateSubSceneAddressableRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_CreateSubSceneAddressableRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_CreateSubSceneAddressableRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_CreateSubSceneAddressableResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateSubSceneAddressableRequest; }
	}
	[ProtoContract]
	public partial class G2C_CreateSubSceneAddressableResponse : AMessage, IResponse
	{
		public static G2C_CreateSubSceneAddressableResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_CreateSubSceneAddressableResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_CreateSubSceneAddressableResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateSubSceneAddressableResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  客户端向SubScene发送一个测试消息
	/// </summary>
	[ProtoContract]
	public partial class C2SubScene_TestMessage : AMessage, IAddressableRouteMessage
	{
		public static C2SubScene_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2SubScene_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2SubScene_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2SubScene_TestMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  客户端向SubScene发送一个销毁测试消息
	/// </summary>
	[ProtoContract]
	public partial class C2SubScene_TestDisposeMessage : AMessage, IAddressableRouteMessage
	{
		public static C2SubScene_TestDisposeMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2SubScene_TestDisposeMessage>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2SubScene_TestDisposeMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2SubScene_TestDisposeMessage; }
	}
	/// <summary>
	///  客户端向服务器发送连接消息（Roaming）
	/// </summary>
	[ProtoContract]
	public partial class C2G_ConnectRoamingRequest : AMessage, IRequest
	{
		public static C2G_ConnectRoamingRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_ConnectRoamingRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_ConnectRoamingRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_ConnectRoamingResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_ConnectRoamingRequest; }
	}
	[ProtoContract]
	public partial class G2C_ConnectRoamingResponse : AMessage, IResponse
	{
		public static G2C_ConnectRoamingResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_ConnectRoamingResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_ConnectRoamingResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_ConnectRoamingResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  测试一个Chat漫游普通消息
	/// </summary>
	[ProtoContract]
	public partial class C2Chat_TestRoamingMessage : AMessage, IRoamingMessage
	{
		public static C2Chat_TestRoamingMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestRoamingMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Chat_TestRoamingMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2Chat_TestRoamingMessage; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.ChatRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  测试一个Map漫游普通消息
	/// </summary>
	[ProtoContract]
	public partial class C2Map_TestRoamingMessage : AMessage, IRoamingMessage
	{
		public static C2Map_TestRoamingMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Map_TestRoamingMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Map_TestRoamingMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2Map_TestRoamingMessage; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.MapRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  测试一个Chat漫游RPC消息
	/// </summary>
	[ProtoContract]
	public partial class C2Chat_TestRPCRoamingRequest : AMessage, IRoamingRequest
	{
		public static C2Chat_TestRPCRoamingRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestRPCRoamingRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Chat_TestRPCRoamingRequest>(this);
#endif
		}
		[ProtoIgnore]
		public Chat2C_TestRPCRoamingResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2Chat_TestRPCRoamingRequest; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.ChatRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class Chat2C_TestRPCRoamingResponse : AMessage, IRoamingResponse
	{
		public static Chat2C_TestRPCRoamingResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Chat2C_TestRPCRoamingResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Chat2C_TestRPCRoamingResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.Chat2C_TestRPCRoamingResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  客户端发送一个漫游消息给Map通知Map主动推送一个消息给客户端
	/// </summary>
	[ProtoContract]
	public partial class C2Map_PushMessageToClient : AMessage, IRoamingMessage
	{
		public static C2Map_PushMessageToClient Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Map_PushMessageToClient>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Map_PushMessageToClient>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2Map_PushMessageToClient; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.MapRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  漫游端发送一个消息给客户端
	/// </summary>
	[ProtoContract]
	public partial class Map2C_PushMessageToClient : AMessage, IRoamingMessage
	{
		public static Map2C_PushMessageToClient Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Map2C_PushMessageToClient>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Map2C_PushMessageToClient>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.Map2C_PushMessageToClient; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.MapRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  测试传送漫游的触发协议
	/// </summary>
	[ProtoContract]
	public partial class C2Map_TestTransferRequest : AMessage, IRoamingRequest
	{
		public static C2Map_TestTransferRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Map_TestTransferRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Map_TestTransferRequest>(this);
#endif
		}
		[ProtoIgnore]
		public Map2C_TestTransferResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2Map_TestTransferRequest; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.MapRoamingType;
	}
	[ProtoContract]
	public partial class Map2C_TestTransferResponse : AMessage, IRoamingResponse
	{
		public static Map2C_TestTransferResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Map2C_TestTransferResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Map2C_TestTransferResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.Map2C_TestTransferResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  测试一个Chat发送到Map之间漫游协议
	/// </summary>
	[ProtoContract]
	public partial class C2Chat_TestSendMapMessage : AMessage, IRoamingMessage
	{
		public static C2Chat_TestSendMapMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestSendMapMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2Chat_TestSendMapMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2Chat_TestSendMapMessage; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.ChatRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  通知Gate服务器发送一个Route消息给Map的漫游终端
	/// </summary>
	[ProtoContract]
	public partial class C2G_TestRouteToRoaming : AMessage, IMessage
	{
		public static C2G_TestRouteToRoaming Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_TestRouteToRoaming>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_TestRouteToRoaming>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_TestRouteToRoaming; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  通知Gate服务器发送一个漫游消息给Map的漫游终端
	/// </summary>
	[ProtoContract]
	public partial class C2G_TestRoamingToRoaming : AMessage, IMessage
	{
		public static C2G_TestRoamingToRoaming Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_TestRoamingToRoaming>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_TestRoamingToRoaming>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_TestRoamingToRoaming; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  通知Gate服务器发送一个内网消息通知Map服务器向Gate服务器注册一个领域事件
	/// </summary>
	[ProtoContract]
	public partial class C2G_SubscribeSphereEventRequest : AMessage, IRequest
	{
		public static C2G_SubscribeSphereEventRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_SubscribeSphereEventRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_SubscribeSphereEventRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_SubscribeSphereEventResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_SubscribeSphereEventRequest; }
	}
	[ProtoContract]
	public partial class G2C_SubscribeSphereEventResponse : AMessage, IResponse
	{
		public static G2C_SubscribeSphereEventResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_SubscribeSphereEventResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_SubscribeSphereEventResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_SubscribeSphereEventResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  通知Gate发送一个订阅领域事件
	/// </summary>
	[ProtoContract]
	public partial class C2G_PublishSphereEventRequest : AMessage, IRequest
	{
		public static C2G_PublishSphereEventRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_PublishSphereEventRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_PublishSphereEventRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_PublishSphereEventResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_PublishSphereEventRequest; }
	}
	[ProtoContract]
	public partial class G2C_PublishSphereEventResponse : AMessage, IResponse
	{
		public static G2C_PublishSphereEventResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_PublishSphereEventResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_PublishSphereEventResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_PublishSphereEventResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  通知Gate取消一个订阅领域事件
	/// </summary>
	[ProtoContract]
	public partial class C2G_UnsubscribeSphereEventRequest : AMessage, IRequest
	{
		public static C2G_UnsubscribeSphereEventRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_UnsubscribeSphereEventRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<C2G_UnsubscribeSphereEventRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2C_UnsubscribeSphereEventResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_UnsubscribeSphereEventRequest; }
	}
	[ProtoContract]
	public partial class G2C_UnsubscribeSphereEventResponse : AMessage, IResponse
	{
		public static G2C_UnsubscribeSphereEventResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_UnsubscribeSphereEventResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2C_UnsubscribeSphereEventResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_UnsubscribeSphereEventResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
}

