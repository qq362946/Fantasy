using System;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.PacketParser.Interface;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Scheduler
{
#if FANTASY_UNITY || FANTASY_CONSOLE
    /// <summary>
    /// 用于客户端调试的网络消息调度器，会在收到消息后打印消息 JSON。
    /// </summary>
    public sealed class DebugClientMessageScheduler : ANetworkMessageScheduler
    {
        public DebugClientMessageScheduler(Scene scene) : base(scene) { }

        public override async FTask Scheduler(Session session, APackInfo packInfo)
        {
            await FTask.CompletedTask;
            switch (packInfo.OpCodeIdStruct.Protocol)
            {
                case OpCodeType.OuterMessage:
                case OpCodeType.OuterRequest:
                case OpCodeType.OuterAddressableMessage:
                case OpCodeType.OuterAddressableRequest:
                case OpCodeType.OuterCustomRouteMessage:
                case OpCodeType.OuterCustomRouteRequest:
                case OpCodeType.OuterRoamingMessage:
                case OpCodeType.OuterRoamingRequest:
                {
                    using (packInfo)
                    {
                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);

                        if (messageType == null)
                        {
                            throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }

                        var message = packInfo.Deserialize(messageType);
                        LogReceiveMessageJson(messageType, packInfo, message);
                        MessageDispatcherComponent.MessageHandler(session, messageType, message, packInfo.RpcId, packInfo.ProtocolCode);
                    }
                    
                    return;
                }
                case OpCodeType.OuterResponse:
                case OpCodeType.OuterPingResponse:
                case OpCodeType.OuterAddressableResponse:
                case OpCodeType.OuterCustomRouteResponse:
                case OpCodeType.OuterRoamingResponse:
                {
                    using (packInfo)
                    {
                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);
                    
                        if (messageType == null)
                        {
                            throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }
                    
                        // 这个一般是客户端Session.Call发送时使用的、目前这个逻辑只有Unity客户端时使用
                    
                        var aResponse = (IResponse)packInfo.Deserialize(messageType);
                        LogReceiveMessageJson(messageType, packInfo, aResponse);

                        if (!session.RequestCallback.Remove(packInfo.RpcId, out var action))
                        {
                            Log.Error($"not found rpc {packInfo.RpcId}, response message: {aResponse.GetType().Name}");
                            return;
                        }

                        action.SetResult(aResponse);
                    }
                    
                    return;
                }
                default:
                {
                    packInfo.Dispose();
                    throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode}");
                }
            }
        }

        private static void LogReceiveMessageJson(Type messageType, APackInfo packInfo, object message)
        {
            if (packInfo.OpCodeIdStruct.Protocol == OpCodeType.OuterPingResponse)
            {
                return;
            }

            Log.Debug($"Receive Message: {messageType.Name} ProtocolCode:{packInfo.ProtocolCode} RpcId:{packInfo.RpcId} OpCodeType:{packInfo.OpCodeIdStruct.Protocol} Json:{message.ToJson()}");
        }
    }
#endif
#if FANTASY_NET
    internal sealed class DebugClientMessageScheduler(Scene scene) : ANetworkMessageScheduler(scene)
    {
        public override FTask Scheduler(Session session, APackInfo packInfo)
        {
            throw new NotSupportedException($"DebugClientMessageScheduler Received unsupported message protocolCode:{packInfo.ProtocolCode}");
        }
    }
#endif
}
