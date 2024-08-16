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
			return (C2G_TestMessage)scene.MessagePoolComponent.Rent(typeof(C2G_TestMessage));
		}
		public override void Dispose()
		{
			Tag = default;
			base.Dispose();
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
			return (C2G_TestRequest)scene.MessagePoolComponent.Rent(typeof(C2G_TestRequest));
		}
		public override void Dispose()
		{
			Tag = default;
			base.Dispose();
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
			return (G2C_TestResponse)scene.MessagePoolComponent.Rent(typeof(G2C_TestResponse));
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
			base.Dispose();
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
			return (C2G_CreateAddressableRequest)scene.MessagePoolComponent.Rent(typeof(C2G_CreateAddressableRequest));
		}
		public override void Dispose()
		{
			base.Dispose();
		}
		[IgnoreMember]
		public G2C_CreateAddressableResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateAddressableRequest; }
	}
	[MessagePackObject]
	public partial class G2C_CreateAddressableResponse : AMessage, IResponse
	{
		public static G2C_CreateAddressableResponse Create(Scene scene)
		{
			return (G2C_CreateAddressableResponse)scene.MessagePoolComponent.Rent(typeof(G2C_CreateAddressableResponse));
		}
		public override void Dispose()
		{
			ErrorCode = default;
			base.Dispose();
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
			return (C2M_TestMessage)scene.MessagePoolComponent.Rent(typeof(C2M_TestMessage));
		}
		public override void Dispose()
		{
			Tag = default;
			base.Dispose();
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
			return (C2M_TestRequest)scene.MessagePoolComponent.Rent(typeof(C2M_TestRequest));
		}
		public override void Dispose()
		{
			Tag = default;
			base.Dispose();
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
			return (M2C_TestResponse)scene.MessagePoolComponent.Rent(typeof(M2C_TestResponse));
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
			base.Dispose();
		}
		public uint OpCode() { return OuterOpcode.M2C_TestResponse; }
		[Key(0)]
		public string Tag { get; set; }
		[Key(1)]
		public uint ErrorCode { get; set; }
	}
}
