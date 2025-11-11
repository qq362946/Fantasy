using ProtoBuf;

using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Network.Interface;
using Fantasy.Serialize;
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantOverriddenMember
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618

namespace Fantasy
{
	[ProtoContract]
	public partial class G2A_TestMessage : AMessage, IAddressMessage
	{
		public static G2A_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2A_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2A_TestMessage>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.G2A_TestMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class G2A_TestRequest : AMessage, IAddressRequest
	{
		public static G2A_TestRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2A_TestRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2A_TestRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2A_TestResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2A_TestRequest; }
	}
	[ProtoContract]
	public partial class G2A_TestResponse : AMessage, IAddressResponse
	{
		public static G2A_TestResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2A_TestResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2A_TestResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.G2A_TestResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	[ProtoContract]
	public partial class G2M_RequestAddressableId : AMessage, IAddressRequest
	{
		public static G2M_RequestAddressableId Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2M_RequestAddressableId>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2M_RequestAddressableId>(this);
#endif
		}
		[ProtoIgnore]
		public M2G_ResponseAddressableId ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2M_RequestAddressableId; }
	}
	[ProtoContract]
	public partial class M2G_ResponseAddressableId : AMessage, IAddressResponse
	{
		public static M2G_ResponseAddressableId Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_ResponseAddressableId>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			AddressableId = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<M2G_ResponseAddressableId>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_ResponseAddressableId; }
		[ProtoMember(1)]
		public long AddressableId { get; set; }
		[ProtoMember(2)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  通知Chat服务器创建一个RouteId
	/// </summary>
	[ProtoContract]
	public partial class G2Chat_CreateRouteRequest : AMessage, IAddressRequest
	{
		public static G2Chat_CreateRouteRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2Chat_CreateRouteRequest>();
		}
		public override void Dispose()
		{
			GateAddress = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2Chat_CreateRouteRequest>(this);
#endif
		}
		[ProtoIgnore]
		public Chat2G_CreateRouteResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2Chat_CreateRouteRequest; }
		[ProtoMember(1)]
		public long GateAddress { get; set; }
	}
	[ProtoContract]
	public partial class Chat2G_CreateRouteResponse : AMessage, IAddressResponse
	{
		public static Chat2G_CreateRouteResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Chat2G_CreateRouteResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			ChatAddress = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Chat2G_CreateRouteResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.Chat2G_CreateRouteResponse; }
		[ProtoMember(1)]
		public long ChatAddress { get; set; }
		[ProtoMember(2)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  Map给另外一个Map发送Unit数据
	/// </summary>
	public partial class M2M_SendUnitRequest : AMessage, IAddressRequest
	{
		public static M2M_SendUnitRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2M_SendUnitRequest>();
		}
		public override void Dispose()
		{
			Unit = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<M2M_SendUnitRequest>(this);
#endif
		}
		[BsonIgnore]
		public M2M_SendUnitResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.M2M_SendUnitRequest; }
		public Unit Unit { get; set; }
	}
	public partial class M2M_SendUnitResponse : AMessage, IAddressResponse
	{
		public static M2M_SendUnitResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2M_SendUnitResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<M2M_SendUnitResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2M_SendUnitResponse; }
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  Gate发送Addressable消息给MAP
	/// </summary>
	[ProtoContract]
	public partial class G2M_SendAddressableMessage : AMessage, IAddressableMessage
	{
		public static G2M_SendAddressableMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2M_SendAddressableMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2M_SendAddressableMessage>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.G2M_SendAddressableMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class G2M_CreateSubSceneRequest : AMessage, IAddressRequest
	{
		public static G2M_CreateSubSceneRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2M_CreateSubSceneRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2M_CreateSubSceneRequest>(this);
#endif
		}
		[ProtoIgnore]
		public M2G_CreateSubSceneResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2M_CreateSubSceneRequest; }
	}
	[ProtoContract]
	public partial class M2G_CreateSubSceneResponse : AMessage, IAddressResponse
	{
		public static M2G_CreateSubSceneResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_CreateSubSceneResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			SubSceneAddress = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<M2G_CreateSubSceneResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_CreateSubSceneResponse; }
		[ProtoMember(1)]
		public long SubSceneAddress { get; set; }
		[ProtoMember(2)]
		public uint ErrorCode { get; set; }
	}
	[ProtoContract]
	public partial class G2SubScene_SentMessage : AMessage, IAddressMessage
	{
		public static G2SubScene_SentMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2SubScene_SentMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2SubScene_SentMessage>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.G2SubScene_SentMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  Gate通知SubScene创建一个Addressable消息
	/// </summary>
	[ProtoContract]
	public partial class G2SubScene_AddressableIdRequest : AMessage, IAddressRequest
	{
		public static G2SubScene_AddressableIdRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2SubScene_AddressableIdRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2SubScene_AddressableIdRequest>(this);
#endif
		}
		[ProtoIgnore]
		public SubScene2G_AddressableIdResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2SubScene_AddressableIdRequest; }
	}
	[ProtoContract]
	public partial class SubScene2G_AddressableIdResponse : AMessage, IAddressResponse
	{
		public static SubScene2G_AddressableIdResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<SubScene2G_AddressableIdResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			AddressableId = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<SubScene2G_AddressableIdResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.SubScene2G_AddressableIdResponse; }
		[ProtoMember(1)]
		public long AddressableId { get; set; }
		[ProtoMember(2)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  Chat发送一个漫游消息给Map
	/// </summary>
	[ProtoContract]
	public partial class Chat2M_TestMessage : AMessage, IRoamingMessage
	{
		public static Chat2M_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Chat2M_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Chat2M_TestMessage>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.Chat2M_TestMessage; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.MapRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  测试一个Gate服务器发送一个Route消息给某个漫游终端
	/// </summary>
	[ProtoContract]
	public partial class G2Map_TestRouteMessageRequest : AMessage, IAddressRequest
	{
		public static G2Map_TestRouteMessageRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2Map_TestRouteMessageRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2Map_TestRouteMessageRequest>(this);
#endif
		}
		[ProtoIgnore]
		public Map2G_TestRouteMessageResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2Map_TestRouteMessageRequest; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class Map2G_TestRouteMessageResponse : AMessage, IAddressResponse
	{
		public static Map2G_TestRouteMessageResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Map2G_TestRouteMessageResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Map2G_TestRouteMessageResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.Map2G_TestRouteMessageResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  测试一个Gate服务器发送一个漫游协议给某个漫游终端
	/// </summary>
	[ProtoContract]
	public partial class G2Map_TestRoamingMessageRequest : AMessage, IRoamingRequest
	{
		public static G2Map_TestRoamingMessageRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2Map_TestRoamingMessageRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2Map_TestRoamingMessageRequest>(this);
#endif
		}
		[ProtoIgnore]
		public Map2G_TestRoamingMessageResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2Map_TestRoamingMessageRequest; }
		[ProtoIgnore]
		public int RouteType => Fantasy.RoamingType.MapRoamingType;
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class Map2G_TestRoamingMessageResponse : AMessage, IRoamingResponse
	{
		public static Map2G_TestRoamingMessageResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Map2G_TestRoamingMessageResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Map2G_TestRoamingMessageResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.Map2G_TestRoamingMessageResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  Gate服务器通知Map订阅一个领域事件到Gate上
	/// </summary>
	[ProtoContract]
	public partial class G2Map_SubscribeSphereEventRequest : AMessage, IAddressRequest
	{
		public static G2Map_SubscribeSphereEventRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2Map_SubscribeSphereEventRequest>();
		}
		public override void Dispose()
		{
			GateAddress = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2Map_SubscribeSphereEventRequest>(this);
#endif
		}
		[ProtoIgnore]
		public G2Map_SubscribeSphereEventResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2Map_SubscribeSphereEventRequest; }
		[ProtoMember(1)]
		public long GateAddress { get; set; }
	}
	[ProtoContract]
	public partial class G2Map_SubscribeSphereEventResponse : AMessage, IAddressResponse
	{
		public static G2Map_SubscribeSphereEventResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2Map_SubscribeSphereEventResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2Map_SubscribeSphereEventResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.G2Map_SubscribeSphereEventResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  Gate服务器通知Map取消订阅一个领域事件到Gate上
	/// </summary>
	[ProtoContract]
	public partial class G2Map_UnsubscribeSphereEventRequest : AMessage, IAddressRequest
	{
		public static G2Map_UnsubscribeSphereEventRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2Map_UnsubscribeSphereEventRequest>();
		}
		public override void Dispose()
		{
			GateAddress = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<G2Map_UnsubscribeSphereEventRequest>(this);
#endif
		}
		[ProtoIgnore]
		public Map2G_UnsubscribeSphereEventResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2Map_UnsubscribeSphereEventRequest; }
		[ProtoMember(1)]
		public long GateAddress { get; set; }
	}
	[ProtoContract]
	public partial class Map2G_UnsubscribeSphereEventResponse : AMessage, IAddressResponse
	{
		public static Map2G_UnsubscribeSphereEventResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Map2G_UnsubscribeSphereEventResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			GetScene().MessagePoolComponent.Return<Map2G_UnsubscribeSphereEventResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.Map2G_UnsubscribeSphereEventResponse; }
		[ProtoMember(1)]
		public uint ErrorCode { get; set; }
	}
}

