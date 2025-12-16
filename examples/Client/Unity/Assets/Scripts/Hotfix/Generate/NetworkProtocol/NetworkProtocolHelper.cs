using System.Runtime.CompilerServices;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using System.Collections.Generic;
// ReSharper disable InconsistentNaming
#pragma warning disable CS8618
namespace Fantasy
{
   public static class NetworkProtocolHelper
   {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestEmptyMessage(this Session session, C2G_TestEmptyMessage C2G_TestEmptyMessage_message)
		{
			session.Send(C2G_TestEmptyMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestEmptyMessage(this Session session)
		{
			using var message = Fantasy.C2G_TestEmptyMessage.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestMessage(this Session session, C2G_TestMessage C2G_TestMessage_message)
		{
			session.Send(C2G_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestMessage(this Session session, string tag)
		{
			using var C2G_TestMessage_message = Fantasy.C2G_TestMessage.Create();
			C2G_TestMessage_message.Tag = tag;
			session.Send(C2G_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_TestResponse> C2G_TestRequest(this Session session, C2G_TestRequest C2G_TestRequest_request)
		{
			return (G2C_TestResponse)await session.Call(C2G_TestRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_TestResponse> C2G_TestRequest(this Session session, string tag)
		{
			using var C2G_TestRequest_request = Fantasy.C2G_TestRequest.Create();
			C2G_TestRequest_request.Tag = tag;
			return (G2C_TestResponse)await session.Call(C2G_TestRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRequestPushMessage(this Session session, C2G_TestRequestPushMessage C2G_TestRequestPushMessage_message)
		{
			session.Send(C2G_TestRequestPushMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRequestPushMessage(this Session session)
		{
			using var message = Fantasy.C2G_TestRequestPushMessage.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_PushMessage(this Session session, G2C_PushMessage G2C_PushMessage_message)
		{
			session.Send(G2C_PushMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_PushMessage(this Session session, string tag)
		{
			using var G2C_PushMessage_message = Fantasy.G2C_PushMessage.Create();
			G2C_PushMessage_message.Tag = tag;
			session.Send(G2C_PushMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateAddressableResponse> C2G_CreateAddressableRequest(this Session session, C2G_CreateAddressableRequest C2G_CreateAddressableRequest_request)
		{
			return (G2C_CreateAddressableResponse)await session.Call(C2G_CreateAddressableRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateAddressableResponse> C2G_CreateAddressableRequest(this Session session)
		{
			using var C2G_CreateAddressableRequest_request = Fantasy.C2G_CreateAddressableRequest.Create();
			return (G2C_CreateAddressableResponse)await session.Call(C2G_CreateAddressableRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2M_TestMessage(this Session session, C2M_TestMessage C2M_TestMessage_message)
		{
			session.Send(C2M_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2M_TestMessage(this Session session, string tag)
		{
			using var C2M_TestMessage_message = Fantasy.C2M_TestMessage.Create();
			C2M_TestMessage_message.Tag = tag;
			session.Send(C2M_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_TestResponse> C2M_TestRequest(this Session session, C2M_TestRequest C2M_TestRequest_request)
		{
			return (M2C_TestResponse)await session.Call(C2M_TestRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_TestResponse> C2M_TestRequest(this Session session, string tag)
		{
			using var C2M_TestRequest_request = Fantasy.C2M_TestRequest.Create();
			C2M_TestRequest_request.Tag = tag;
			return (M2C_TestResponse)await session.Call(C2M_TestRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateChatRouteResponse> C2G_CreateChatRouteRequest(this Session session, C2G_CreateChatRouteRequest C2G_CreateChatRouteRequest_request)
		{
			return (G2C_CreateChatRouteResponse)await session.Call(C2G_CreateChatRouteRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateChatRouteResponse> C2G_CreateChatRouteRequest(this Session session)
		{
			using var C2G_CreateChatRouteRequest_request = Fantasy.C2G_CreateChatRouteRequest.Create();
			return (G2C_CreateChatRouteResponse)await session.Call(C2G_CreateChatRouteRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestMessage(this Session session, C2Chat_TestMessage C2Chat_TestMessage_message)
		{
			session.Send(C2Chat_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestMessage(this Session session, string tag)
		{
			using var C2Chat_TestMessage_message = Fantasy.C2Chat_TestMessage.Create();
			C2Chat_TestMessage_message.Tag = tag;
			session.Send(C2Chat_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestMessageResponse> C2Chat_TestMessageRequest(this Session session, C2Chat_TestMessageRequest C2Chat_TestMessageRequest_request)
		{
			return (Chat2C_TestMessageResponse)await session.Call(C2Chat_TestMessageRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestMessageResponse> C2Chat_TestMessageRequest(this Session session, string tag)
		{
			using var C2Chat_TestMessageRequest_request = Fantasy.C2Chat_TestMessageRequest.Create();
			C2Chat_TestMessageRequest_request.Tag = tag;
			return (Chat2C_TestMessageResponse)await session.Call(C2Chat_TestMessageRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_MoveToMapResponse> C2M_MoveToMapRequest(this Session session, C2M_MoveToMapRequest C2M_MoveToMapRequest_request)
		{
			return (M2C_MoveToMapResponse)await session.Call(C2M_MoveToMapRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<M2C_MoveToMapResponse> C2M_MoveToMapRequest(this Session session)
		{
			using var C2M_MoveToMapRequest_request = Fantasy.C2M_MoveToMapRequest.Create();
			return (M2C_MoveToMapResponse)await session.Call(C2M_MoveToMapRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendAddressableToMap(this Session session, C2G_SendAddressableToMap C2G_SendAddressableToMap_message)
		{
			session.Send(C2G_SendAddressableToMap_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendAddressableToMap(this Session session, string tag)
		{
			using var C2G_SendAddressableToMap_message = Fantasy.C2G_SendAddressableToMap.Create();
			C2G_SendAddressableToMap_message.Tag = tag;
			session.Send(C2G_SendAddressableToMap_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRequestPushMessage(this Session session, C2Chat_TestRequestPushMessage C2Chat_TestRequestPushMessage_message)
		{
			session.Send(C2Chat_TestRequestPushMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRequestPushMessage(this Session session)
		{
			using var message = Fantasy.C2Chat_TestRequestPushMessage.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Chat2C_PushMessage(this Session session, Chat2C_PushMessage Chat2C_PushMessage_message)
		{
			session.Send(Chat2C_PushMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Chat2C_PushMessage(this Session session, string tag)
		{
			using var Chat2C_PushMessage_message = Fantasy.Chat2C_PushMessage.Create();
			Chat2C_PushMessage_message.Tag = tag;
			session.Send(Chat2C_PushMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneResponse> C2G_CreateSubSceneRequest(this Session session, C2G_CreateSubSceneRequest C2G_CreateSubSceneRequest_request)
		{
			return (G2C_CreateSubSceneResponse)await session.Call(C2G_CreateSubSceneRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneResponse> C2G_CreateSubSceneRequest(this Session session)
		{
			using var C2G_CreateSubSceneRequest_request = Fantasy.C2G_CreateSubSceneRequest.Create();
			return (G2C_CreateSubSceneResponse)await session.Call(C2G_CreateSubSceneRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendToSubSceneMessage(this Session session, C2G_SendToSubSceneMessage C2G_SendToSubSceneMessage_message)
		{
			session.Send(C2G_SendToSubSceneMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_SendToSubSceneMessage(this Session session)
		{
			using var message = Fantasy.C2G_SendToSubSceneMessage.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneAddressableResponse> C2G_CreateSubSceneAddressableRequest(this Session session, C2G_CreateSubSceneAddressableRequest C2G_CreateSubSceneAddressableRequest_request)
		{
			return (G2C_CreateSubSceneAddressableResponse)await session.Call(C2G_CreateSubSceneAddressableRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateSubSceneAddressableResponse> C2G_CreateSubSceneAddressableRequest(this Session session)
		{
			using var C2G_CreateSubSceneAddressableRequest_request = Fantasy.C2G_CreateSubSceneAddressableRequest.Create();
			return (G2C_CreateSubSceneAddressableResponse)await session.Call(C2G_CreateSubSceneAddressableRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestMessage(this Session session, C2SubScene_TestMessage C2SubScene_TestMessage_message)
		{
			session.Send(C2SubScene_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestMessage(this Session session, string tag)
		{
			using var C2SubScene_TestMessage_message = Fantasy.C2SubScene_TestMessage.Create();
			C2SubScene_TestMessage_message.Tag = tag;
			session.Send(C2SubScene_TestMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestDisposeMessage(this Session session, C2SubScene_TestDisposeMessage C2SubScene_TestDisposeMessage_message)
		{
			session.Send(C2SubScene_TestDisposeMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2SubScene_TestDisposeMessage(this Session session)
		{
			using var message = Fantasy.C2SubScene_TestDisposeMessage.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_ConnectRoamingResponse> C2G_ConnectRoamingRequest(this Session session, C2G_ConnectRoamingRequest C2G_ConnectRoamingRequest_request)
		{
			return (G2C_ConnectRoamingResponse)await session.Call(C2G_ConnectRoamingRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_ConnectRoamingResponse> C2G_ConnectRoamingRequest(this Session session)
		{
			using var C2G_ConnectRoamingRequest_request = Fantasy.C2G_ConnectRoamingRequest.Create();
			return (G2C_ConnectRoamingResponse)await session.Call(C2G_ConnectRoamingRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRoamingMessage(this Session session, C2Chat_TestRoamingMessage C2Chat_TestRoamingMessage_message)
		{
			session.Send(C2Chat_TestRoamingMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestRoamingMessage(this Session session, string tag)
		{
			using var C2Chat_TestRoamingMessage_message = Fantasy.C2Chat_TestRoamingMessage.Create();
			C2Chat_TestRoamingMessage_message.Tag = tag;
			session.Send(C2Chat_TestRoamingMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_TestRoamingMessage(this Session session, C2Map_TestRoamingMessage C2Map_TestRoamingMessage_message)
		{
			session.Send(C2Map_TestRoamingMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_TestRoamingMessage(this Session session, string tag)
		{
			using var C2Map_TestRoamingMessage_message = Fantasy.C2Map_TestRoamingMessage.Create();
			C2Map_TestRoamingMessage_message.Tag = tag;
			session.Send(C2Map_TestRoamingMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestRPCRoamingResponse> C2Chat_TestRPCRoamingRequest(this Session session, C2Chat_TestRPCRoamingRequest C2Chat_TestRPCRoamingRequest_request)
		{
			return (Chat2C_TestRPCRoamingResponse)await session.Call(C2Chat_TestRPCRoamingRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Chat2C_TestRPCRoamingResponse> C2Chat_TestRPCRoamingRequest(this Session session, string tag)
		{
			using var C2Chat_TestRPCRoamingRequest_request = Fantasy.C2Chat_TestRPCRoamingRequest.Create();
			C2Chat_TestRPCRoamingRequest_request.Tag = tag;
			return (Chat2C_TestRPCRoamingResponse)await session.Call(C2Chat_TestRPCRoamingRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_PushMessageToClient(this Session session, C2Map_PushMessageToClient C2Map_PushMessageToClient_message)
		{
			session.Send(C2Map_PushMessageToClient_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Map_PushMessageToClient(this Session session, string tag)
		{
			using var C2Map_PushMessageToClient_message = Fantasy.C2Map_PushMessageToClient.Create();
			C2Map_PushMessageToClient_message.Tag = tag;
			session.Send(C2Map_PushMessageToClient_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Map2C_PushMessageToClient(this Session session, Map2C_PushMessageToClient Map2C_PushMessageToClient_message)
		{
			session.Send(Map2C_PushMessageToClient_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Map2C_PushMessageToClient(this Session session, string tag)
		{
			using var Map2C_PushMessageToClient_message = Fantasy.Map2C_PushMessageToClient.Create();
			Map2C_PushMessageToClient_message.Tag = tag;
			session.Send(Map2C_PushMessageToClient_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Map2C_TestTransferResponse> C2Map_TestTransferRequest(this Session session, C2Map_TestTransferRequest C2Map_TestTransferRequest_request)
		{
			return (Map2C_TestTransferResponse)await session.Call(C2Map_TestTransferRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<Map2C_TestTransferResponse> C2Map_TestTransferRequest(this Session session)
		{
			using var C2Map_TestTransferRequest_request = Fantasy.C2Map_TestTransferRequest.Create();
			return (Map2C_TestTransferResponse)await session.Call(C2Map_TestTransferRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestSendMapMessage(this Session session, C2Chat_TestSendMapMessage C2Chat_TestSendMapMessage_message)
		{
			session.Send(C2Chat_TestSendMapMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2Chat_TestSendMapMessage(this Session session, string tag)
		{
			using var C2Chat_TestSendMapMessage_message = Fantasy.C2Chat_TestSendMapMessage.Create();
			C2Chat_TestSendMapMessage_message.Tag = tag;
			session.Send(C2Chat_TestSendMapMessage_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRouteToRoaming(this Session session, C2G_TestRouteToRoaming C2G_TestRouteToRoaming_message)
		{
			session.Send(C2G_TestRouteToRoaming_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRouteToRoaming(this Session session, string tag)
		{
			using var C2G_TestRouteToRoaming_message = Fantasy.C2G_TestRouteToRoaming.Create();
			C2G_TestRouteToRoaming_message.Tag = tag;
			session.Send(C2G_TestRouteToRoaming_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRoamingToRoaming(this Session session, C2G_TestRoamingToRoaming C2G_TestRoamingToRoaming_message)
		{
			session.Send(C2G_TestRoamingToRoaming_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_TestRoamingToRoaming(this Session session, string tag)
		{
			using var C2G_TestRoamingToRoaming_message = Fantasy.C2G_TestRoamingToRoaming.Create();
			C2G_TestRoamingToRoaming_message.Tag = tag;
			session.Send(C2G_TestRoamingToRoaming_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_SubscribeSphereEventResponse> C2G_SubscribeSphereEventRequest(this Session session, C2G_SubscribeSphereEventRequest C2G_SubscribeSphereEventRequest_request)
		{
			return (G2C_SubscribeSphereEventResponse)await session.Call(C2G_SubscribeSphereEventRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_SubscribeSphereEventResponse> C2G_SubscribeSphereEventRequest(this Session session)
		{
			using var C2G_SubscribeSphereEventRequest_request = Fantasy.C2G_SubscribeSphereEventRequest.Create();
			return (G2C_SubscribeSphereEventResponse)await session.Call(C2G_SubscribeSphereEventRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_PublishSphereEventResponse> C2G_PublishSphereEventRequest(this Session session, C2G_PublishSphereEventRequest C2G_PublishSphereEventRequest_request)
		{
			return (G2C_PublishSphereEventResponse)await session.Call(C2G_PublishSphereEventRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_PublishSphereEventResponse> C2G_PublishSphereEventRequest(this Session session)
		{
			using var C2G_PublishSphereEventRequest_request = Fantasy.C2G_PublishSphereEventRequest.Create();
			return (G2C_PublishSphereEventResponse)await session.Call(C2G_PublishSphereEventRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_UnsubscribeSphereEventResponse> C2G_UnsubscribeSphereEventRequest(this Session session, C2G_UnsubscribeSphereEventRequest C2G_UnsubscribeSphereEventRequest_request)
		{
			return (G2C_UnsubscribeSphereEventResponse)await session.Call(C2G_UnsubscribeSphereEventRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_UnsubscribeSphereEventResponse> C2G_UnsubscribeSphereEventRequest(this Session session)
		{
			using var C2G_UnsubscribeSphereEventRequest_request = Fantasy.C2G_UnsubscribeSphereEventRequest.Create();
			return (G2C_UnsubscribeSphereEventResponse)await session.Call(C2G_UnsubscribeSphereEventRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_MapUnsubscribeSphereEventResponse> C2G_MapUnsubscribeSphereEventRequest(this Session session, C2G_MapUnsubscribeSphereEventRequest C2G_MapUnsubscribeSphereEventRequest_request)
		{
			return (G2C_MapUnsubscribeSphereEventResponse)await session.Call(C2G_MapUnsubscribeSphereEventRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_MapUnsubscribeSphereEventResponse> C2G_MapUnsubscribeSphereEventRequest(this Session session)
		{
			using var C2G_MapUnsubscribeSphereEventRequest_request = Fantasy.C2G_MapUnsubscribeSphereEventRequest.Create();
			return (G2C_MapUnsubscribeSphereEventResponse)await session.Call(C2G_MapUnsubscribeSphereEventRequest_request);
		}
#if Godot_Platform
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_带标签的消息(this Session session, C2G_带标签的消息 C2G_带标签的消息_message)
		{
			session.Send(C2G_带标签的消息_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_带标签的消息(this Session session)
		{
			using var message = Fantasy.C2G_带标签的消息.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2M_带标签且条件编译的消息(this Session session, C2M_带标签且条件编译的消息 C2M_带标签且条件编译的消息_message)
		{
			session.Send(C2M_带标签且条件编译的消息_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2M_带标签且条件编译的消息(this Session session, string tag)
		{
			using var C2M_带标签且条件编译的消息_message = Fantasy.C2M_带标签且条件编译的消息.Create();
			C2M_带标签且条件编译的消息_message.Tag = tag;
			session.Send(C2M_带标签且条件编译的消息_message);
		}
#endif

   }
}