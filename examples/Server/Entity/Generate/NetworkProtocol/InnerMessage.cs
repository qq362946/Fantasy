using ProtoBuf;
using System.Collections.Generic;
using Fantasy;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class G2A_TestRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public G2A_TestResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2A_TestRequest; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
	}
	[ProtoContract]
	public partial class G2A_TestResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.G2A_TestResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
	}
	[ProtoContract]
	public partial class G2M_RequestAddressableId : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public M2G_ResponseAddressableId ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2M_RequestAddressableId; }
		public long RouteTypeOpCode() { return InnerRouteType.Route; }
	}
	[ProtoContract]
	public partial class M2G_ResponseAddressableId : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.M2G_ResponseAddressableId; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public long AddressableId { get; set; }
	}
}
