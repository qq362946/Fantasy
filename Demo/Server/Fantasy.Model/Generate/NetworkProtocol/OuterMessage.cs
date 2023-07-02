using ProtoBuf;
using Unity.Mathematics;
using System.Collections.Generic;
using Fantasy.Core.Network;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class H_C2L_TestMessage : AProto, IMessage
	{
		public uint OpCode() { return OuterOpcode.H_C2L_TestMessage; }
		[ProtoMember(1)]
		public int Key { get; set; }
	}
	/// <summary>
	///  注册
	/// </summary>
	[ProtoContract]
	public partial class H_C2A_RegisterAccount : AProto, IRequest
	{
		[ProtoIgnore]
		public H_A2C_RegisterAccount ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.H_C2A_RegisterAccount; }
		[ProtoMember(1)]
		public string Account { get; set; }
		[ProtoMember(2)]
		public string Pw { get; set; }
		[ProtoMember(3)]
		public string Pw2 { get; set; }
		[ProtoMember(4)]
		public string Code { get; set; }
		[ProtoMember(5)]
		public string Version { get; set; }
		[ProtoMember(6)]
		public string DeviceId { get; set; }
	}
	[ProtoContract]
	public partial class H_A2C_RegisterAccount : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.H_A2C_RegisterAccount; }
		[ProtoMember(91, IsRequired = true)]
		public int ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Account { get; set; }
	}
	/// <summary>
	///  测试代码
	/// </summary>
	[ProtoContract]
	public partial class H_C2Chat_GetTest_Req : AProto, ICustomRouteRequest
	{
		[ProtoIgnore]
		public H_Chat2C_GetTest_Res ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.H_C2Chat_GetTest_Req; }
		public long RouteTypeOpCode() { return (long)RouteType.ChatRoute; }
		[ProtoMember(1)]
		public string Name { get; set; }
	}
	[ProtoContract]
	public partial class H_Chat2C_GetTest_Res : AProto, ICustomRouteResponse
	{
		public uint OpCode() { return OuterOpcode.H_Chat2C_GetTest_Res; }
		[ProtoMember(91, IsRequired = true)]
		public int ErrorCode { get; set; }
		[ProtoMember(1)]
		public string ResultName { get; set; }
	}
	/// <summary>
	///  测试Addressable协议
	/// </summary>
	[ProtoContract]
	public partial class H_C2M_AddressableRequest : AProto, IAddressableRouteRequest
	{
		[ProtoIgnore]
		public H_M2C_AddressableResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.H_C2M_AddressableRequest; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public string Name { get; set; }
	}
	[ProtoContract]
	public partial class H_M2C_AddressableResponse : AProto, IAddressableRouteResponse
	{
		public uint OpCode() { return OuterOpcode.H_M2C_AddressableResponse; }
		[ProtoMember(91, IsRequired = true)]
		public int ErrorCode { get; set; }
		[ProtoMember(1)]
		public string ResultName { get; set; }
	}
	/// <summary>
	///  测试Addressable协议
	/// </summary>
	[ProtoContract]
	public partial class H_C2M_TestAddressable : AProto, IAddressableRouteMessage
	{
		public uint OpCode() { return OuterOpcode.H_C2M_TestAddressable; }
		public long RouteTypeOpCode() { return CoreRouteType.Addressable; }
		[ProtoMember(1)]
		public string Name { get; set; }
	}
}
