using ProtoBuf;
using Unity.Mathematics;
using System.Collections.Generic;
using Fantasy.Core.Network;
#pragma warning disable CS8618

namespace Fantasy
{
	/// <summary>
	///  登录到服务器
	/// </summary>
	[ProtoContract]
	public partial class H_C2G_LoginRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public H_G2C_LoginResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.H_C2G_LoginRequest; }
		[ProtoMember(1)]
		public string UserName { get; set; }
		[ProtoMember(2)]
		public string Password { get; set; }
	}
	[ProtoContract]
	public partial class H_G2C_LoginResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.H_G2C_LoginResponse; }
		[ProtoMember(91, IsRequired = true)]
		public int ErrorCode { get; set; }
		[ProtoMember(1)]
		public string Text { get; set; }
	}
}
