using ProtoBuf;
using System.Collections.Generic;
using Fantasy;
#pragma warning disable CS8618

namespace Fantasy
{	
	[ProtoContract]
	public partial class TestServerRequest : AProto, IRequest
	{
		[ProtoIgnore]
		public TestServerResponse ResponseType { get; set; }
		public uint OpCode() { return InnerOpcode.TestServerRequest; }
		[ProtoMember(1)]
		public int Id { get; set; } = 1;
	}
	[ProtoContract]
	public partial class TestServerResponse : AProto, IResponse
	{
		public uint OpCode() { return InnerOpcode.TestServerResponse; }
		[ProtoMember(91, IsRequired = true)]
		public uint ErrorCode { get; set; }
	}
	[ProtoContract]
	public partial class TestServerMessage : AProto, IMessage
	{
		public uint OpCode() { return InnerOpcode.TestServerMessage; }
	}
}
