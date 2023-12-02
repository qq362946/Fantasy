using ProtoBuf;
using Unity.Mathematics;
using System.Collections.Generic;
using Fantasy;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class S2S_ConnectRequest : AProto, IRequest
	{
		public uint OpCode() { return InnerOpcode.S2S_ConnectRequest; }
		[ProtoMember(1)]
		public int Key { get; set; }
	}
	[ProtoContract]
	public partial class S2S_ConnectResponse : AProto, IResponse
	{
		public uint OpCode() { return InnerOpcode.S2S_ConnectResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public int Key { get; set; }
	}
	[ProtoContract]
	public partial class R2G_GetLoginKeyRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public G2R_GetLoginKeyResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.R2G_GetLoginKeyRequest; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
		[ProtoMember(1)]
		public string AuthName { get; set; }
		[ProtoMember(2)]
		public long AccountId { get; set; }
	}
	[ProtoContract]
	public partial class G2R_GetLoginKeyResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.G2R_GetLoginKeyResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public long Key { get; set; }
	}
	/// <summary>
	///  进入地图
	/// </summary>
	[ProtoContract]
	public partial class G2M_CreateUnitRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public M2G_CreateUnitResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.G2M_CreateUnitRequest; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
		[ProtoMember(1)]
		public string Message { get; set; }
		[ProtoMember(2)]
		public long PlayerId { get; set; }
		[ProtoMember(3)]
		public long SessionRuntimeId { get; set; }
		[ProtoMember(4)]
		public long GateRouteId { get; set; }
		[ProtoMember(5)]
		public RoleInfo RoleInfo { get; set; }
	}
	[ProtoContract]
	public partial class M2G_CreateUnitResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return InnerOpcode.M2G_CreateUnitResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
		[ProtoMember(2)]
		public long AddressableId { get; set; }
		///<summary>
		/// Unit Unit = 3;
		///</summary>
	}
	[ProtoContract]
	public partial class G2M_Return2MapMsg : AProto, IAddressableRouteMessage
	{
		public uint OpCode() { return InnerOpcode.G2M_Return2MapMsg; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public int MapNum { get; set; }
		[ProtoMember(2)]
		public RoleInfo RoleInfo { get; set; }
	}
	[ProtoContract]
	public partial class M2G_QuitMapMsg : AProto, IRouteMessage
	{
		public uint OpCode() { return InnerOpcode.M2G_QuitMapMsg; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
		[ProtoMember(1)]
		public long AccountId { get; set; }
		[ProtoMember(2)]
		public int MapNum { get; set; }
		[ProtoMember(3)]
		public bool QuitGame { get; set; }
	}
	[ProtoContract]
	public partial class S2Mgr_ServerStartComplete : AProto, IRouteMessage
	{
		public uint OpCode() { return InnerOpcode.S2Mgr_ServerStartComplete; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
	}
	[ProtoContract]
	public partial class Mgr2R_MachineStartFinished : AProto, IRouteMessage
	{
		public uint OpCode() { return InnerOpcode.Mgr2R_MachineStartFinished; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
	}
	[ProtoContract]
	public partial class AccountMsg : AProto
	{
		[ProtoMember(1)]
		public string AuthName { get; set; }
		[ProtoMember(2)]
		public long AccountId { get; set; }
		[ProtoMember(3)]
		public string Pw { get; set; }
		[ProtoMember(4)]
		public long LastLoginTime { get; set; }
		[ProtoMember(5)]
		public string LastLoginIp { get; set; }
		[ProtoMember(6)]
		public long RegisterTime { get; set; }
		[ProtoMember(7)]
		public string RegisterIp { get; set; }
	}
}
