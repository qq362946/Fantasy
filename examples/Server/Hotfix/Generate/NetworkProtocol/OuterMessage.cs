using ProtoBuf;
using System.Collections.Generic;
using Fantasy;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class C2A_TestRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public A2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2A_TestRequest; }
	}
	[ProtoContract]
	public partial class A2C_TestResponse : AProto, IResponse
	{
		public uint OpCode() { return OuterOpcode.A2C_TestResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
	}
}
