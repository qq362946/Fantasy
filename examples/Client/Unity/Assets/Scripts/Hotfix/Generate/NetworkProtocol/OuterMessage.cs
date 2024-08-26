using MessagePack;
using System.Collections.Generic;
#pragma warning disable CS8618

namespace Fantasy
{
	[MessagePackObject]
	public partial class C2G_TestMessage : AMessage, IMessage
	{
		public static C2G_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2G_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2G_TestMessage; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
	public partial class C2G_TestRequest : AMessage, IRequest
	{
		public static C2G_TestRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_TestRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2G_TestRequest>(this);
#endif
		}
		[IgnoreMember]
		public G2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_TestRequest; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
	public partial class G2C_TestResponse : AMessage, IResponse
	{
		public static G2C_TestResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_TestResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_TestResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_TestResponse; }
		[Key(0)]
		public string Tag { get; set; }
		[Key(1)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class C2G_CreateAddressableRequest : AMessage, IRequest
	{
		public static C2G_CreateAddressableRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_CreateAddressableRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2G_CreateAddressableRequest>(this);
#endif
		}
		[IgnoreMember]
		public G2C_CreateAddressableResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateAddressableRequest; }
	}
	[MessagePackObject]
	public partial class G2C_CreateAddressableResponse : AMessage, IResponse
	{
		public static G2C_CreateAddressableResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_CreateAddressableResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_CreateAddressableResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateAddressableResponse; }
		[Key(0)]
		public uint ErrorCode { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_TestMessage : AMessage, IAddressableRouteMessage
	{
		public static C2M_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2M_TestMessage; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
	public partial class C2M_TestRequest : AMessage, IAddressableRouteRequest
	{
		public static C2M_TestRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2M_TestRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2M_TestRequest>(this);
#endif
		}
		[IgnoreMember]
		public M2C_TestResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2M_TestRequest; }
		public long RouteTypeOpCode() { return InnerRouteType.Addressable; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
	public partial class M2C_TestResponse : AMessage, IAddressableRouteResponse
	{
		public static M2C_TestResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<M2C_TestResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<M2C_TestResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.M2C_TestResponse; }
		[Key(0)]
		public string Tag { get; set; }
		[Key(1)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  通知Gate服务器创建一个Chat的Route连接
	/// </summary>
	[MessagePackObject]
	public partial class C2G_CreateChatRouteRequest : AMessage, IRequest
	{
		public static C2G_CreateChatRouteRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2G_CreateChatRouteRequest>();
		}
		public override void Dispose()
		{
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2G_CreateChatRouteRequest>(this);
#endif
		}
		[IgnoreMember]
		public G2C_CreateChatRouteResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2G_CreateChatRouteRequest; }
	}
	[MessagePackObject]
	public partial class G2C_CreateChatRouteResponse : AMessage, IResponse
	{
		public static G2C_CreateChatRouteResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<G2C_CreateChatRouteResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<G2C_CreateChatRouteResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.G2C_CreateChatRouteResponse; }
		[Key(0)]
		public uint ErrorCode { get; set; }
	}
	/// <summary>
	///  发送一个Route消息给Chat
	/// </summary>
	[MessagePackObject]
	public partial class C2Chat_TestMessage : AMessage, ICustomRouteMessage
	{
		public static C2Chat_TestMessage Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestMessage>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2Chat_TestMessage>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.C2Chat_TestMessage; }
		public long RouteTypeOpCode() { return (long)RouteType.ChatRoute; }
		[Key(0)]
		public string Tag { get; set; }
	}
	/// <summary>
	///  发送一个RPCRoute消息给Chat
	/// </summary>
	[MessagePackObject]
	public partial class C2Chat_TestMessageRequest : AMessage, ICustomRouteRequest
	{
		public static C2Chat_TestMessageRequest Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<C2Chat_TestMessageRequest>();
		}
		public override void Dispose()
		{
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<C2Chat_TestMessageRequest>(this);
#endif
		}
		[IgnoreMember]
		public Chat2C_TestMessageResponse ResponseType { get; set; }
		public uint OpCode() { return OuterOpcode.C2Chat_TestMessageRequest; }
		public long RouteTypeOpCode() { return (long)RouteType.ChatRoute; }
		[Key(0)]
		public string Tag { get; set; }
	}
	[MessagePackObject]
	public partial class Chat2C_TestMessageResponse : AMessage, ICustomRouteResponse
	{
		public static Chat2C_TestMessageResponse Create(Scene scene)
		{
			return scene.MessagePoolComponent.Rent<Chat2C_TestMessageResponse>();
		}
		public override void Dispose()
		{
			ErrorCode = default;
			Tag = default;
#if FANTASY_NET || FANTASY_UNITY
			Scene.MessagePoolComponent.Return<Chat2C_TestMessageResponse>(this);
#endif
		}
		public uint OpCode() { return OuterOpcode.Chat2C_TestMessageResponse; }
		[Key(0)]
		public string Tag { get; set; }
		[Key(1)]
		public uint ErrorCode { get; set; }
	}
}
