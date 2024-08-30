using MessagePack;
using System.Collections.Generic;
using Fantasy;
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
	[MessagePackObject]
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
			Scene.MessagePoolComponent.Return<C2G_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_TestMessage; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
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
			Scene.MessagePoolComponent.Return<C2G_TestRequest>(this);
#endif
		}
		[IgnoreMember]
		public G2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_TestRequest; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
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
			Scene.MessagePoolComponent.Return<G2C_TestResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_TestResponse; }
		[Key(0)]
		public string Tag { get; set; }
		[Key(1)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class C2G_CreateAddressableRequest : AMessage, IRequest
	{
		public static C2G_CreateAddressableRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_CreateAddressableRequest>();
		}
		public override void Dispose()
		{
			RoomId = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2G_CreateAddressableRequest>(this);
#endif
		}
		[IgnoreMember]
		public G2C_CreateAddressableResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateAddressableRequest; }
		[Key(0)]
		public long RoomId { get; set; }
	}
	[MessagePackObject]
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
			Scene.MessagePoolComponent.Return<G2C_CreateAddressableResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateAddressableResponse; }
		[Key(0)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
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
			Scene.MessagePoolComponent.Return<C2M_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_TestMessage; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
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
			Scene.MessagePoolComponent.Return<C2M_TestRequest>(this);
#endif
		}
		[IgnoreMember]
		public M2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2M_TestRequest; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
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
			Scene.MessagePoolComponent.Return<M2C_TestResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.M2C_TestResponse; }
		[Key(0)]
		public string Tag { get; set; }
		[Key(1)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_RequestInit : AMessage, IAddressableRouteRequest
	{
		public static C2M_RequestInit Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_RequestInit>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_RequestInit>(this);
#endif
		}
		[IgnoreMember]
		public M2C_ResponseInit ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2M_RequestInit; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
	}
	[MessagePackObject]
	public partial class V3 : AMessage
	{
		public static V3 Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<V3>();
		}
		public override void Dispose()
		{
			x = default;
			y = default;
			z = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<V3>(this);
#endif
		}
		[Key(0)]
		public float x { get; set; }
		[Key(1)]
		public float y { get; set; }
		[Key(2)]
		public float z { get; set; }
	}
	[MessagePackObject]
	public partial class V4 : AMessage
	{
		public static V4 Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<V4>();
		}
		public override void Dispose()
		{
			x = default;
			y = default;
			z = default;
			w = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<V4>(this);
#endif
		}
		[Key(0)]
		public float x { get; set; }
		[Key(1)]
		public float y { get; set; }
		[Key(2)]
		public float z { get; set; }
		[Key(3)]
		public float w { get; set; }
	}
	[MessagePackObject]
	public partial class TransformData : AMessage
	{
		public static TransformData Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<TransformData>();
		}
		public override void Dispose()
		{
			position = default;
			quaternion = default;
			scale = default;
			active = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<TransformData>(this);
#endif
		}
		[Key(0)]
		public V3 position { get; set; }
		[Key(1)]
		public V4 quaternion { get; set; }
		[Key(2)]
		public V3 scale { get; set; }
		[Key(3)]
		public bool active { get; set; }
	}
	[MessagePackObject]
	public partial class InitData : AMessage
	{
		public static InitData Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<InitData>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			PrefabID = default;
			NetworkScriptsID.Clear();
			Transform = default;
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<InitData>(this);
#endif
		}
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public long PrefabID { get; set; }
		[Key(2)]
		public List<long> NetworkScriptsID = new List<long>();
		[Key(3)]
		public TransformData Transform { get; set; }
		[Key(4)]
		public long ClientID { get; set; }
	}
	[MessagePackObject]
	public partial class M2C_ResponseInit : AMessage, IAddressableRouteResponse
	{
		public static M2C_ResponseInit Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2C_ResponseInit>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			initData.Clear();
			ClientID = default;
			SortID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2C_ResponseInit>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.M2C_ResponseInit; }
		[Key(0)]
		public List<InitData> initData = new List<InitData>();
		[Key(1)]
		public long ClientID { get; set; }
		[Key(2)]
		public long SortID { get; set; }
		[Key(3)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_RequestNetworkObjectId : AMessage, IAddressableRouteRequest
	{
		public static C2M_RequestNetworkObjectId Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_RequestNetworkObjectId>();
		}
		public override void Dispose()
		{
			PrefabID = default;
			NetworkScriptsID.Clear();
			Transform = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_RequestNetworkObjectId>(this);
#endif
		}
		[IgnoreMember]
		public M2C_ResponseNetworkObjectId ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2M_RequestNetworkObjectId; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long PrefabID { get; set; }
		[Key(1)]
		public List<long> NetworkScriptsID = new List<long>();
		[Key(2)]
		public TransformData Transform { get; set; }
	}
	[MessagePackObject]
	public partial class M2C_ResponseNetworkObjectId : AMessage, IAddressableRouteResponse
	{
		public static M2C_ResponseNetworkObjectId Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2C_ResponseNetworkObjectId>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			AddressableId = default;
			Authority = default;
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2C_ResponseNetworkObjectId>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.M2C_ResponseNetworkObjectId; }
		[Key(0)]
		public long AddressableId { get; set; }
		[Key(1)]
		public bool Authority { get; set; }
		[Key(2)]
		public long ClientID { get; set; }
		[Key(3)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_SyncTransform : AMessage, IAddressableRouteMessage
	{
		public static C2M_SyncTransform Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_SyncTransform>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			Transform = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_SyncTransform>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_SyncTransform; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public TransformData Transform { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_CreateNetworkObjectId : AMessage, IMessage
	{
		public static G2C_CreateNetworkObjectId Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_CreateNetworkObjectId>();
		}
		public override void Dispose()
		{
			data = default;
			Authority = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_CreateNetworkObjectId>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateNetworkObjectId; }
		[Key(0)]
		public InitData data { get; set; }
		[Key(1)]
		public bool Authority { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_SyncTransform : AMessage, IMessage
	{
		public static G2C_SyncTransform Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_SyncTransform>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			Transform = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_SyncTransform>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_SyncTransform; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public TransformData Transform { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_DeleteNetworkObj : AMessage, IMessage
	{
		public static G2C_DeleteNetworkObj Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_DeleteNetworkObj>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_DeleteNetworkObj>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_DeleteNetworkObj; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_DeleteNetworkObj : AMessage, IAddressableRouteMessage
	{
		public static C2M_DeleteNetworkObj Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_DeleteNetworkObj>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_DeleteNetworkObj>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_DeleteNetworkObj; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_SyncSprite : AMessage, IMessage
	{
		public static G2C_SyncSprite Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_SyncSprite>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			SpriteName = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_SyncSprite>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_SyncSprite; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public string SpriteName { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_SyncSprite : AMessage, IAddressableRouteMessage
	{
		public static C2M_SyncSprite Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_SyncSprite>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			SpriteName = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_SyncSprite>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_SyncSprite; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public string SpriteName { get; set; }
	}
	[MessagePackObject]
	public partial class C2G_LobbyRequest : AMessage, IRequest
	{
		public static C2G_LobbyRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_LobbyRequest>();
		}
		public override void Dispose()
		{
			StatusCode = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2G_LobbyRequest>(this);
#endif
		}
		[IgnoreMember]
		public G2C_LobbyResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_LobbyRequest; }
		[Key(0)]
		public long StatusCode { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_LobbyResponse : AMessage, IResponse
	{
		public static G2C_LobbyResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_LobbyResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_LobbyResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_LobbyResponse; }
		[Key(0)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_LobbyFinishMessage : AMessage, IMessage
	{
		public static G2C_LobbyFinishMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_LobbyFinishMessage>();
		}
		public override void Dispose()
		{
			roomID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_LobbyFinishMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_LobbyFinishMessage; }
		[Key(0)]
		public long roomID { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_StartGame : AMessage, IMessage
	{
		public static G2C_StartGame Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_StartGame>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_StartGame>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_StartGame; }
	}
	[MessagePackObject]
	public partial class G2C_Hurt : AMessage, IMessage
	{
		public static G2C_Hurt Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_Hurt>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			Value = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_Hurt>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_Hurt; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public long Value { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_Hurt : AMessage, IAddressableRouteMessage
	{
		public static C2M_Hurt Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_Hurt>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			Value = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_Hurt>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_Hurt; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public long Value { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_GameOver : AMessage, IMessage
	{
		public static G2C_GameOver Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_GameOver>();
		}
		public override void Dispose()
		{
			WinnerClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_GameOver>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_GameOver; }
		[Key(0)]
		public long WinnerClientID { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_ExitRoom : AMessage, IAddressableRouteMessage
	{
		public static C2M_ExitRoom Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_ExitRoom>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_ExitRoom>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_ExitRoom; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
	}
	[MessagePackObject]
	public partial class G2C_ExitRoom : AMessage, IMessage
	{
		public static G2C_ExitRoom Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_ExitRoom>();
		}
		public override void Dispose()
		{
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_ExitRoom>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_ExitRoom; }
		[Key(0)]
		public long ClientID { get; set; }
	}
}
