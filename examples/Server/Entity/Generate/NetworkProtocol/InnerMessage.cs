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
	public partial class G2A_TestRequest : AMessage, IRouteRequest
	{
		public static G2A_TestRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2A_TestRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2A_TestRequest>(this);
#endif
		}
		[IgnoreMember]
		public G2A_TestResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2A_TestRequest; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
	}
	[MessagePackObject]
	public partial class G2A_TestResponse : AMessage, IRouteResponse
	{
		public static G2A_TestResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2A_TestResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2A_TestResponse>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.G2A_TestResponse; }
		[Key(0)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class G2M_RequestAddressableId : AMessage, IRouteRequest
	{
		public static G2M_RequestAddressableId Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2M_RequestAddressableId>();
		}
		public override void Dispose()
		{
			ClientID = default;
			RoomId = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2M_RequestAddressableId>(this);
#endif
		}
		[IgnoreMember]
		public M2G_ResponseAddressableId ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2M_RequestAddressableId; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[Key(0)]
		public long ClientID { get; set; }
		[Key(1)]
		public long RoomId { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_ResponseAddressableId : AMessage, IRouteResponse
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
			Scene.MessagePoolComponent.Return<M2G_ResponseAddressableId>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_ResponseAddressableId; }
		[Key(0)]
		public long AddressableId { get; set; }
		[Key(1)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_CreateNetworkObjectId : AMessage, IRouteMessage
	{
		public static M2G_CreateNetworkObjectId Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_CreateNetworkObjectId>();
		}
		public override void Dispose()
		{
			ClientID = default;
			data = default;
			Authority = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_CreateNetworkObjectId>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_CreateNetworkObjectId; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[Key(0)]
		public long ClientID { get; set; }
		[Key(1)]
		public InitData data { get; set; }
		[Key(2)]
		public bool Authority { get; set; }
	}
	[MessagePackObject]
	public partial class G2M_RemoveClient : AMessage, IRouteMessage
	{
		public static G2M_RemoveClient Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2M_RemoveClient>();
		}
		public override void Dispose()
		{
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2M_RemoveClient>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.G2M_RemoveClient; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[Key(0)]
		public long ClientID { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_SyncTransform : AMessage, IRouteMessage
	{
		public static M2G_SyncTransform Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_SyncTransform>();
		}
		public override void Dispose()
		{
			ClientID = default;
			NetworkObjectID = default;
			Transform = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_SyncTransform>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_SyncTransform; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[Key(0)]
		public long ClientID { get; set; }
		[Key(1)]
		public long NetworkObjectID { get; set; }
		[Key(2)]
		public TransformData Transform { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_DeleteNetworkObj : AMessage, IRouteMessage
	{
		public static M2G_DeleteNetworkObj Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_DeleteNetworkObj>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_DeleteNetworkObj>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_DeleteNetworkObj; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public long ClientID { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_SyncSprite : AMessage, IRouteMessage
	{
		public static M2G_SyncSprite Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_SyncSprite>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			SpriteName = default;
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_SyncSprite>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_SyncSprite; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public string SpriteName { get; set; }
		[Key(2)]
		public long ClientID { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_StartGame : AMessage, IRouteMessage
	{
		public static M2G_StartGame Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_StartGame>();
		}
		public override void Dispose()
		{
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_StartGame>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_StartGame; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[Key(0)]
		public long ClientID { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_Hurt : AMessage, IAddressableRouteMessage
	{
		public static M2G_Hurt Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_Hurt>();
		}
		public override void Dispose()
		{
			NetworkObjectID = default;
			Value = default;
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_Hurt>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_Hurt; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long NetworkObjectID { get; set; }
		[Key(1)]
		public long Value { get; set; }
		[Key(2)]
		public long ClientID { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_GameOver : AMessage, IAddressableRouteMessage
	{
		public static M2G_GameOver Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_GameOver>();
		}
		public override void Dispose()
		{
			WinnerClientID = default;
			ClientID = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_GameOver>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_GameOver; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long WinnerClientID { get; set; }
		[Key(1)]
		public long ClientID { get; set; }
	}
	[MessagePackObject]
	public partial class M2G_ExitRoom : AMessage, IAddressableRouteMessage
	{
		public static M2G_ExitRoom Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2G_ExitRoom>();
		}
		public override void Dispose()
		{
			ClientID = default;
			ExitClientId = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2G_ExitRoom>(this);
#endif
		}
		public uint OpCode() { return InnerOpcode.M2G_ExitRoom; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public long ClientID { get; set; }
		[Key(1)]
		public long ExitClientId { get; set; }
	}
}
