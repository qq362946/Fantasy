using ProtoBuf;
using Unity.Mathematics;
using System.Collections.Generic;
using Fantasy.Core.Network;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class C2R_GetZoneList : AProto, IRequest
	{
		[ProtoIgnore]
		public R2C_GetZoneList ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2R_GetZoneList; }
	}
	[ProtoContract]
	public partial class R2C_GetZoneList : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.R2C_GetZoneList; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public List<uint> ZoneId = new List<uint>();
		[ProtoMember(2)]
		public List<string> ZoneName = new List<string>();
		[ProtoMember(3)]
		public List<int> ZoneState = new List<int>();
	}
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
		public string AuthName { get; set; }
		[ProtoMember(2)]
		public string Pw { get; set; }
		[ProtoMember(3)]
		public string Pw2 { get; set; }
		[ProtoMember(4)]
		public uint ZoneId { get; set; }
		[ProtoMember(5)]
		public string Version { get; set; }
	}
	[ProtoContract]
	public partial class R2C_RegisterResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.R2C_RegisterResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
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
		public string AuthName { get; set; }
		[ProtoMember(2)]
		public string Pw { get; set; }
		[ProtoMember(3)]
		public string Version { get; set; }
	}
	[ProtoContract]
	public partial class R2C_LoginResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.R2C_LoginResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Message { get; set; }
		[ProtoMember(2)]
		public string GateAddress { get; set; }
		[ProtoMember(3)]
		public int GatePort { get; set; }
		[ProtoMember(4)]
		public long Key { get; set; }
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
		public string AuthName { get; set; }
		[ProtoMember(2)]
		public long Key { get; set; }
	}
	[ProtoContract]
	public partial class G2C_LoginGateResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_LoginGateResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  创建角色 消息请求
	/// </summary>
	[ProtoContract]
	public partial class C2G_RoleCreateRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_RoleCreateResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_RoleCreateRequest; }
		[ProtoMember(1)]
		public int UnitConfigId { get; set; }
		[ProtoMember(2)]
		public int Sex { get; set; }
		[ProtoMember(3)]
		public string NickName { get; set; }
		[ProtoMember(4)]
		public string Class { get; set; }
	}
	/// <summary>
	///  创建角色 消息响应
	/// </summary>
	[ProtoContract]
	public partial class G2C_RoleCreateResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_RoleCreateResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public RoleInfo RoleInfo { get; set; }
	}
	/// <summary>
	///  删除角色 消息请求
	/// </summary>
	[ProtoContract]
	public partial class C2G_RoleDeleteRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_RoleDeleteResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_RoleDeleteRequest; }
		[ProtoMember(1)]
		public long RoleId { get; set; }
	}
	/// <summary>
	///  删除角色 消息响应
	/// </summary>
	[ProtoContract]
	public partial class G2C_RoleDeleteResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_RoleDeleteResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  获取角色列列表 消息请求
	/// </summary>
	[ProtoContract]
	public partial class C2G_RoleListRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public G2C_RoleListResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_RoleListRequest; }
	}
	/// <summary>
	///  角色列表信息 消息响应
	/// </summary>
	[ProtoContract]
	public partial class G2C_RoleListResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.G2C_RoleListResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
		[ProtoMember(1)]
		public List<RoleInfo> Items = new List<RoleInfo>();
	}
	/// <summary>
	///  角色列表子项信息
	/// </summary>
	[ProtoContract]
	public partial class RoleInfo : AProto
	{
		[ProtoMember(1)]
		public int UnitConfigId { get; set; }
		[ProtoMember(3)]
		public long AccountId { get; set; }
		///<summary>
		/// 角色ID
		///</summary>
		[ProtoMember(4)]
		public long RoleId { get; set; }
		///<summary>
		/// 性别
		///</summary>
		[ProtoMember(5)]
		public int Sex { get; set; }
		///<summary>
		/// 昵称
		///</summary>
		[ProtoMember(6)]
		public string NickName { get; set; }
		///<summary>
		/// 年龄
		///</summary>
		[ProtoMember(7)]
		public int Level { get; set; }
		[ProtoMember(8)]
		public long Experience { get; set; }
		[ProtoMember(9)]
		public string Class { get; set; }
		[ProtoMember(10)]
		public int LastMap { get; set; }
		///<summary>
		/// repeated ItemInfo Equipments = 11;
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
		public int MapNum { get; set; }
		[ProtoMember(2)]
		public long RoleId { get; set; }
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
	///  强制断开连接通知
	/// </summary>
	[ProtoContract]
	public partial class G2C_ForceDisconnected : AProto, IMessage
	{
		public uint OpCode() { return OuterOpcode.G2C_ForceDisconnected; }
		[ProtoMember(1)]
		public string Message { get; set; }
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
	[ProtoContract]
	public partial class M2C_NoticeUnitAdd : AProto, IAddressableRouteMessage
	{
		public uint OpCode() { return OuterOpcode.M2C_NoticeUnitAdd; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public List<RoleInfo> RoleInfos = new List<RoleInfo>();
	}
	[ProtoContract]
	public partial class M2C_NoticeUnitRemove : AProto, IAddressableRouteMessage
	{
		public uint OpCode() { return OuterOpcode.M2C_NoticeUnitRemove; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public long UnitId { get; set; }
	}
}
