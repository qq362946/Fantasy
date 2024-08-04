using ProtoBuf;
using System.Collections.Generic;
using Fantasy;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class I_G2A_TestMessage : AProto, IRouteMessage
	{
		public uint OpCode() { return InnerOpcode.I_G2A_TestMessage; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[ProtoMember(1)]
		public string Name { get; set; }
	}
	[ProtoContract]
	public partial class I_G2A_TestRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public I_A2G_TestResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.I_G2A_TestRequest; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
		[ProtoMember(1)]
		public string Name { get; set; }
	}
	[ProtoContract]
	public partial class I_A2G_TestResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.I_A2G_TestResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Name { get; set; }
	}
	[ProtoContract]
	public partial class I_G2A_PingRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public I_A2G_PingResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.I_G2A_PingRequest; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
	}
	[ProtoContract]
	public partial class I_A2G_PingResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.I_A2G_PingResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
	}
}
