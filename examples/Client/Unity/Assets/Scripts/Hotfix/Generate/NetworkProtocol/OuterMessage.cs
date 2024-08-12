using ProtoBuf;
using System.Collections.Generic;
using Fantasy.Core.Network;
#pragma warning disable CS8618

namespace Fantasy
{
	[ProtoContract]
	public partial class C2G_TestMessage : AProto, IMessage
	{
		public uint OpCode() { return OuterOpcode.C2G_TestMessage; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class C2G_TestRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_TestRequest; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class G2C_TestResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_TestResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
	[ProtoContract]
	public partial class C2G_CreateAddressableRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_CreateAddressableResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateAddressableRequest; }
	}
	[ProtoContract]
	public partial class G2C_CreateAddressableResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_CreateAddressableResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
	}
	[ProtoContract]
	public partial class C2M_TestMessage : AProto, IAddressableRouteMessage
	{
		public uint OpCode() { return OuterOpcode.C2M_TestMessage; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[ProtoMember(1)]
		public string Tag { get; set; }
	}
}
