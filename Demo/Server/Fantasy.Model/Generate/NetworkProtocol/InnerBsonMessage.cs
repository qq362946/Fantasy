using ProtoBuf;
using Unity.Mathematics;
using System.Collections.Generic;
using Fantasy.Core.Network;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class I_G2M_TestBosnMessage_Req : AProto, IBsonRouteRequest
	{
		[ProtoIgnore]
		public I_G2M_TestBosnMessage_Res ResponseType { get; set; }
		public uint OpCode() { return InnerBsonOpcode.I_G2M_TestBosnMessage_Req; }
		public long RouteTypeOpCode() { return CoreRouteType.BsonRoute; }
		[ProtoMember(1)]
		public string Name { get; set; }
	}
	[ProtoContract]
	public partial class I_G2M_TestBosnMessage_Res : AProto, IBsonRouteResponse
	{
		public uint OpCode() { return InnerBsonOpcode.I_G2M_TestBosnMessage_Res; }
		[ProtoMember(91, IsRequired = true)]
		public int ErrorCode { get; set; }
		[ProtoMember(1)]
		public long RouteId { get; set; }
	}
}
