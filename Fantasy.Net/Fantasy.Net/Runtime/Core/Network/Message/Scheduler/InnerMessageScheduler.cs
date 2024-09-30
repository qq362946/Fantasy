#if FANTASY_NET
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System.Runtime.CompilerServices;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using Fantasy.PacketParser.Interface;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Scheduler
{
    /// <summary>
    /// 提供了一个机制来调度和处理内部网络消息。
    /// </summary>
    internal sealed class InnerMessageScheduler(Scene scene) : ANetworkMessageScheduler(scene)
    {
        public override void Scheduler(Session session, APackInfo packInfo)
        {
            var protocol = packInfo.OpCodeIdStruct.Protocol;
            
            switch (protocol)
            {
                case OpCodeType.InnerMessage:
                case OpCodeType.InnerRequest:
                {
                    var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);
                    
                    try
                    {
                        if (messageType == null)
                        {
                            throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }
                        
                        var message = packInfo.Deserialize(messageType);
                        MessageDispatcherComponent.MessageHandler(session, messageType, message, packInfo.RpcId, packInfo.ProtocolCode);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"ANetworkMessageScheduler OuterResponse error messageProtocolCode:{packInfo.ProtocolCode} messageType:{messageType} SessionId {session.Id} IsDispose {session.IsDisposed} {e}");
                    }
                    finally
                    {
                        packInfo.Dispose();
                    }
                    
                    return;
                }
                case OpCodeType.InnerResponse:
                case OpCodeType.InnerRouteResponse:
                case OpCodeType.InnerAddressableResponse:
                case OpCodeType.OuterAddressableResponse:
                case OpCodeType.OuterCustomRouteResponse:
                {
                    using (packInfo)
                    {
                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);
                        
                        if (messageType == null)
                        {
                            throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }
                        
                        NetworkMessagingComponent.ResponseHandler(packInfo.RpcId, (IResponse)packInfo.Deserialize(messageType));
                    }
                    
                    return;
                }
                case OpCodeType.InnerRouteMessage:
                case OpCodeType.InnerAddressableMessage:
                {
                    using (packInfo)
                    {
                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);

                        if (messageType == null)
                        {
                            throw new Exception($"InnerMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }

                        if (!Scene.TryGetEntity(packInfo.RouteId, out var entity))
                        {
                            throw new Exception($"The Entity associated with RouteId = {packInfo.RouteId} was not found! messageType = {messageType.FullName}");
                        }

                        var obj = packInfo.Deserialize(messageType);
                        Scene.MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, (IMessage)obj, packInfo.RpcId).Coroutine();
                    }

                    return;
                }
                case OpCodeType.InnerRouteRequest:
                case OpCodeType.InnerAddressableRequest:
                {
                    using (packInfo)
                    {
                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);

                        if (messageType == null)
                        {
                            throw new Exception($"InnerMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }

                        if (!Scene.TryGetEntity(packInfo.RouteId, out var entity))
                        {
                            Scene.MessageDispatcherComponent.FailRouteResponse(session, messageType, InnerErrorCode.ErrNotFoundRoute, packInfo.RpcId);
                        }

                        var obj = packInfo.Deserialize(messageType);
                        Scene.MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, (IMessage)obj, packInfo.RpcId).Coroutine();
                    }

                    return;
                }
                case OpCodeType.OuterCustomRouteRequest:
                case OpCodeType.OuterAddressableRequest:
                case OpCodeType.OuterAddressableMessage:
                case OpCodeType.OuterCustomRouteMessage:
                {
                    var entity = Scene.GetEntity(packInfo.RouteId);

                    switch (entity)
                    {
                        case null:
                        {
                            // 执行到这里有两种情况:
                            using (packInfo)
                            {
                                switch (Scene.SceneConfig.SceneTypeString)
                                {
                                    case "Gate":
                                    {
                                        // 1、当前是Gate进行，需要转发消息给客户端，但当前这个Session已经断开了。
                                        // 这种情况不需要做任何处理。
                                        return;
                                    }
                                    default:
                                    {
                                        // 2、当前是其他Scene、消息通过Gate发送到这个Scene上面，但这个Scene上面没有这个Entity。
                                        // 因为这个是Gate转发消息到这个Scene的，如果没有找到Entity要返回错误给Gate。
                                        // 出现这个情况一定要打印日志，因为出现这个问题肯定是上层逻辑导致的，不应该出现这样的问题。
                                        var packInfoRouteId = packInfo.RouteId;
                                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);
                            
                                        switch (protocol)
                                        {
                                            case OpCodeType.OuterCustomRouteRequest:
                                            case OpCodeType.OuterAddressableRequest:
                                            case OpCodeType.OuterAddressableMessage:
                                            {
                                                Scene.MessageDispatcherComponent.FailRouteResponse(session, messageType, InnerErrorCode.ErrNotFoundRoute, packInfo.RpcId);
                                                return;
                                            }
                                        }
                                        
                                        throw new Exception($"The Entity associated with RouteId = {packInfoRouteId} was not found! messageType = {messageType.FullName} protocol = {protocol}");
                                    }
                                }
                            }
                        }
                        case Session gateSession:
                        {
                            using (packInfo)
                            {
                                // 这里如果是Session只可能是Gate的Session、如果是的话、肯定是转发消息
                                gateSession.Send(packInfo.MemoryStream, packInfo.RpcId);
                            }
                            
                            return;
                        }
                        default:
                        {
                            using (packInfo)
                            {
                                var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);

                                if (messageType == null)
                                {
                                    throw new Exception($"InnerMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                                }
                            
                                var obj = packInfo.Deserialize(messageType);
                                Scene.MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, (IMessage)obj, packInfo.RpcId).Coroutine();
                            }
                            
                            return;
                        }
                    }
                }
                default:
                {
                    var infoProtocolCode = packInfo.ProtocolCode;
                    packInfo.Dispose();
                    throw new NotSupportedException($"InnerMessageScheduler Received unsupported message protocolCode:{infoProtocolCode}");
                }
            }
        }
    }
}
#endif

