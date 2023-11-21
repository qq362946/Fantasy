using ProtoBuf;
using Unity.Mathematics;
using System.Collections.Generic;
using Fantasy;
#pragma warning disable CS8618

namespace Fantasy
{
	/// <summary>
	///  注册账号
	/// </summary>
	[ProtoContract]
	public partial class C2R_RegisterRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public R2C_RegisterResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2R_RegisterRequest; }
		[ProtoMember(1)]
		public string UserName { get; set; }
		[ProtoMember(2)]
		public string Password { get; set; }
	}
	[ProtoContract]
	public partial class R2C_RegisterResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.R2C_RegisterResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
	}
	/// <summary>
	///  登录账号
	/// </summary>
	[ProtoContract]
	public partial class C2R_LoginRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public R2C_LoginResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2R_LoginRequest; }
		[ProtoMember(1)]
		public string UserName { get; set; }
		[ProtoMember(2)]
		public string Password { get; set; }
	}
	[ProtoContract]
	public partial class R2C_LoginResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.R2C_LoginResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
	}
	/// <summary>
	///  登录网关
	/// </summary>
	[ProtoContract]
	public partial class C2G_LoginGateRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_LoginGateResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_LoginGateRequest; }
		[ProtoMember(1)]
		public string Message { get; set; }
	}
	[ProtoContract]
	public partial class G2C_LoginGateResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_LoginGateResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
	}
	/// <summary>
	///  创建角色
	/// </summary>
	[ProtoContract]
	public partial class C2G_CreateCharacterRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_CreateCharacterResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateCharacterRequest; }
		[ProtoMember(1)]
		public string Message { get; set; }
	}
	[ProtoContract]
	public partial class G2C_CreateCharacterResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_CreateCharacterResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
		///<summary>
		/// Character Character = 2;
		///</summary>
	}
	/// <summary>
	///  进入地图
	/// </summary>
	[ProtoContract]
	public partial class C2G_EnterMapRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_EnterMapResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_EnterMapRequest; }
		[ProtoMember(1)]
		public string Message { get; set; }
		[ProtoMember(2)]
		public long CharacterId { get; set; }
	}
	[ProtoContract]
	public partial class G2C_EnterMapResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_EnterMapResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
		///<summary>
		/// repeated Character Characters = 4;
		///</summary>
	}
	/// <summary>
	///  进入地图
	/// </summary>
	[ProtoContract]
	public partial class G2M_CreateUnitRequest : AProto, IRouteRequest
	{
		[ProtoIgnore]
		public M2G_CreateUnitResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.G2M_CreateUnitRequest; }
		public long RouteTypeOpCode() { return CoreRouteType.Route; }
		[ProtoMember(1)]
		public string Message { get; set; }
		[ProtoMember(2)]
		public long PlayerId { get; set; }
		[ProtoMember(3)]
		public long SessionRuntimeId { get; set; }
	}
	[ProtoContract]
	public partial class M2G_CreateUnitResponse : AProto, IRouteResponse
	{
		public uint OpCode() { return OuterOpcode.M2G_CreateUnitResponse; }
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
	/// <summary>
	///  退出地图
	/// </summary>
	[ProtoContract]
	public partial class C2M_ExitRequest : AProto, IAddressableRouteRequest
	{
		[ProtoIgnore]
		public M2C_ExitResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2M_ExitRequest; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public string Message { get; set; }
	}
	[ProtoContract]
	public partial class M2C_ExitResponse : AProto, IAddressableRouteResponse
	{
		public uint OpCode() { return OuterOpcode.M2C_ExitResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
	}
	/// <summary>
	///  位置对象
	/// </summary>
	[ProtoContract]
	public partial class MoveInfo : AProto
	{
		[ProtoMember(1)]
		public float X { get; set; }
		[ProtoMember(2)]
		public float Y { get; set; }
		[ProtoMember(3)]
		public float Z { get; set; }
		[ProtoMember(4)]
		public float RotA { get; set; }
		[ProtoMember(5)]
		public float RotB { get; set; }
		[ProtoMember(6)]
		public float RotC { get; set; }
		[ProtoMember(7)]
		public float RotW { get; set; }
		[ProtoMember(8)]
		public long MoveEndTime { get; set; }
	}
	/// <summary>
	///  移动操作
	/// </summary>
	[ProtoContract]
	public partial class C2M_MoveMessage : AProto, IAddressableRouteMessage
	{
		public uint OpCode() { return OuterOpcode.C2M_MoveMessage; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public MoveInfo MoveInfo { get; set; }
	}
	/// <summary>
	///  核心状态同步
	/// </summary>
	[ProtoContract]
	public partial class M2C_MoveBroadcast : AProto, IAddressableRouteMessage
	{
		public uint OpCode() { return OuterOpcode.M2C_MoveBroadcast; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public List<MoveInfo> Moves = new List<MoveInfo>();
	}
}
