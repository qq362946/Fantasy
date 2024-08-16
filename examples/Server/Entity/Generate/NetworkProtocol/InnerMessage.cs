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
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2M_RequestAddressableId>(this);
#endif
		}
		[IgnoreMember]
		public M2G_ResponseAddressableId ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2M_RequestAddressableId; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
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
}
