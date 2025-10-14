using System.Runtime.CompilerServices;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using System.Collections.Generic;
#pragma warning disable CS8618

namespace Fantasy
{
	public static class NetworkProtocolHelper
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestMessage(this Session session, C2G_TestMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestMessage(this Session session, string tag)
		{
			using var message = Fantasy.C2G_TestMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_TestResponse> C2G_TestRequest(this Session session, C2G_TestRequest request)
		{
			return (G2C_TestResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_TestResponse> C2G_TestRequest(this Session session, string tag)
		{
			using var request = Fantasy.C2G_TestRequest.Create(session.Scene);
			request.Tag = tag;
			return (G2C_TestResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRequestPushMessage(this Session session, C2G_TestRequestPushMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRequestPushMessage(this Session session)
		{
			using var message = Fantasy.C2G_TestRequestPushMessage.Create(session.Scene);
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_PushMessage(this Session session, G2C_PushMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_PushMessage(this Session session, string tag)
		{
			using var message = Fantasy.G2C_PushMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateAddressableResponse> C2G_CreateAddressableRequest(this Session session, C2G_CreateAddressableRequest request)
		{
			return (G2C_CreateAddressableResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateAddressableResponse> C2G_CreateAddressableRequest(this Session session)
		{
			using var request = Fantasy.C2G_CreateAddressableRequest.Create(session.Scene);
			return (G2C_CreateAddressableResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2M_TestMessage(this Session session, C2M_TestMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2M_TestMessage(this Session session, string tag)
		{
			using var message = Fantasy.C2M_TestMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_TestResponse> C2M_TestRequest(this Session session, C2M_TestRequest request)
		{
			return (M2C_TestResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_TestResponse> C2M_TestRequest(this Session session, string tag)
		{
			using var request = Fantasy.C2M_TestRequest.Create(session.Scene);
			request.Tag = tag;
			return (M2C_TestResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateChatRouteResponse> C2G_CreateChatRouteRequest(this Session session, C2G_CreateChatRouteRequest request)
		{
			return (G2C_CreateChatRouteResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateChatRouteResponse> C2G_CreateChatRouteRequest(this Session session)
		{
			using var request = Fantasy.C2G_CreateChatRouteRequest.Create(session.Scene);
			return (G2C_CreateChatRouteResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestMessage(this Session session, C2Chat_TestMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestMessage(this Session session, string tag)
		{
			using var message = Fantasy.C2Chat_TestMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestMessageResponse> C2Chat_TestMessageRequest(this Session session, C2Chat_TestMessageRequest request)
		{
			return (Chat2C_TestMessageResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestMessageResponse> C2Chat_TestMessageRequest(this Session session, string tag)
		{
			using var request = Fantasy.C2Chat_TestMessageRequest.Create(session.Scene);
			request.Tag = tag;
			return (Chat2C_TestMessageResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_MoveToMapResponse> C2M_MoveToMapRequest(this Session session, C2M_MoveToMapRequest request)
		{
			return (M2C_MoveToMapResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_MoveToMapResponse> C2M_MoveToMapRequest(this Session session)
		{
			using var request = Fantasy.C2M_MoveToMapRequest.Create(session.Scene);
			return (M2C_MoveToMapResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendAddressableToMap(this Session session, C2G_SendAddressableToMap message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendAddressableToMap(this Session session, string tag)
		{
			using var message = Fantasy.C2G_SendAddressableToMap.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRequestPushMessage(this Session session, C2Chat_TestRequestPushMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRequestPushMessage(this Session session)
		{
			using var message = Fantasy.C2Chat_TestRequestPushMessage.Create(session.Scene);
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Chat2C_PushMessage(this Session session, Chat2C_PushMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Chat2C_PushMessage(this Session session, string tag)
		{
			using var message = Fantasy.Chat2C_PushMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneResponse> C2G_CreateSubSceneRequest(this Session session, C2G_CreateSubSceneRequest request)
		{
			return (G2C_CreateSubSceneResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneResponse> C2G_CreateSubSceneRequest(this Session session)
		{
			using var request = Fantasy.C2G_CreateSubSceneRequest.Create(session.Scene);
			return (G2C_CreateSubSceneResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendToSubSceneMessage(this Session session, C2G_SendToSubSceneMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendToSubSceneMessage(this Session session)
		{
			using var message = Fantasy.C2G_SendToSubSceneMessage.Create(session.Scene);
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneAddressableResponse> C2G_CreateSubSceneAddressableRequest(this Session session, C2G_CreateSubSceneAddressableRequest request)
		{
			return (G2C_CreateSubSceneAddressableResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneAddressableResponse> C2G_CreateSubSceneAddressableRequest(this Session session)
		{
			using var request = Fantasy.C2G_CreateSubSceneAddressableRequest.Create(session.Scene);
			return (G2C_CreateSubSceneAddressableResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestMessage(this Session session, C2SubScene_TestMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestMessage(this Session session, string tag)
		{
			using var message = Fantasy.C2SubScene_TestMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestDisposeMessage(this Session session, C2SubScene_TestDisposeMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestDisposeMessage(this Session session)
		{
			using var message = Fantasy.C2SubScene_TestDisposeMessage.Create(session.Scene);
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_ConnectRoamingResponse> C2G_ConnectRoamingRequest(this Session session, C2G_ConnectRoamingRequest request)
		{
			return (G2C_ConnectRoamingResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_ConnectRoamingResponse> C2G_ConnectRoamingRequest(this Session session)
		{
			using var request = Fantasy.C2G_ConnectRoamingRequest.Create(session.Scene);
			return (G2C_ConnectRoamingResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRoamingMessage(this Session session, C2Chat_TestRoamingMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRoamingMessage(this Session session, string tag)
		{
			using var message = Fantasy.C2Chat_TestRoamingMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_TestRoamingMessage(this Session session, C2Map_TestRoamingMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_TestRoamingMessage(this Session session, string tag)
		{
			using var message = Fantasy.C2Map_TestRoamingMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestRPCRoamingResponse> C2Chat_TestRPCRoamingRequest(this Session session, C2Chat_TestRPCRoamingRequest request)
		{
			return (Chat2C_TestRPCRoamingResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestRPCRoamingResponse> C2Chat_TestRPCRoamingRequest(this Session session, string tag)
		{
			using var request = Fantasy.C2Chat_TestRPCRoamingRequest.Create(session.Scene);
			request.Tag = tag;
			return (Chat2C_TestRPCRoamingResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_PushMessageToClient(this Session session, C2Map_PushMessageToClient message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_PushMessageToClient(this Session session, string tag)
		{
			using var message = Fantasy.C2Map_PushMessageToClient.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Map2C_PushMessageToClient(this Session session, Map2C_PushMessageToClient message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Map2C_PushMessageToClient(this Session session, string tag)
		{
			using var message = Fantasy.Map2C_PushMessageToClient.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Map2C_TestTransferResponse> C2Map_TestTransferRequest(this Session session, C2Map_TestTransferRequest request)
		{
			return (Map2C_TestTransferResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Map2C_TestTransferResponse> C2Map_TestTransferRequest(this Session session)
		{
			using var request = Fantasy.C2Map_TestTransferRequest.Create(session.Scene);
			return (Map2C_TestTransferResponse)await session.Call(request);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestSendMapMessage(this Session session, C2Chat_TestSendMapMessage message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestSendMapMessage(this Session session, string tag)
		{
			using var message = Fantasy.C2Chat_TestSendMapMessage.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRouteToRoaming(this Session session, C2G_TestRouteToRoaming message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRouteToRoaming(this Session session, string tag)
		{
			using var message = Fantasy.C2G_TestRouteToRoaming.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRoamingToRoaming(this Session session, C2G_TestRoamingToRoaming message)
		{
			session.Send(message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRoamingToRoaming(this Session session, string tag)
		{
			using var message = Fantasy.C2G_TestRoamingToRoaming.Create(session.Scene);
			message.Tag = tag;
			session.Send(message);
		}

	}
}
