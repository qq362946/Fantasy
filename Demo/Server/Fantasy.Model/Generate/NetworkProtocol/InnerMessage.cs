using ProtoBuf;
using Unity.Mathematics;
using System.Collections.Generic;
using Fantasy.Core.Network;
#pragma warning disable CS8618

namespace Fantasy
{	
	/// <summary>
	///  Gate服务器登录到Map服务器
	/// </summary>
	[ProtoContract]
	public partial class I_G2MLoginRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public I_M2GLoginResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.I_G2MLoginRequest; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
		[ProtoMember(1)]
		public long AccountId { get; set; }
	}
	[ProtoContract]
	public partial class I_M2GLoginResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.I_M2GLoginResponse; }
		[ProtoMember(91, IsRequired = true)]
		public int ErrorCode { get; set; }
		[ProtoMember(1)]
		public long AddressRouteId { get; set; }
	}
	/// <summary>
	///  Gate服务器登录到Chat服务器
	/// </summary>
	[ProtoContract]
	public partial class I_G2ChatLoginRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public I_M2ChatLoginResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.I_G2ChatLoginRequest; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
		[ProtoMember(1)]
		public long AccountId { get; set; }
	}
	[ProtoContract]
	public partial class I_M2ChatLoginResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.I_M2ChatLoginResponse; }
		[ProtoMember(91, IsRequired = true)]
		public int ErrorCode { get; set; }
		[ProtoMember(1)]
		public long AddressRouteId { get; set; }
	}
}
