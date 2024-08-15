#if FANTASY_NET
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System.Runtime.CompilerServices;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    /// <summary>
    /// 提供了一个机制来调度和处理内部网络消息。
    /// </summary>
    public sealed class InnerMessageScheduler(Scene scene) : ANetworkMessageScheduler(scene)
    {
        protected override async FTask Handler(Session session, APackInfo packInfo)
        {
            var packInfoProtocolCode = packInfo.ProtocolCode;
            
            switch (packInfo.ProtocolCode)
            {
                case >= OpCode.InnerBsonRouteResponse:
                case >= OpCode.InnerRouteResponse:
                case >= OpCode.OuterRouteResponse: // 如果Gate服务器、需要转发Addressable协议、所以这里有可能会接收到该类型协议。
                {
                    var messageType = MessageDispatcherComponent.GetOpCodeType(packInfoProtocolCode);

                    if (messageType == null)
                    {
                        packInfo.Dispose();
                        throw new Exception($"InnerMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfoProtocolCode}");
                    }

                    using (packInfo)
                    {
                        var rpcId = packInfo.RpcId;
                        var response = (IResponse)packInfo.Deserialize(messageType);
                        NetworkMessagingComponent.ResponseHandler(rpcId, response);
                    }
                    
                    return;
                }
                case > OpCode.InnerBsonRouteMessage:
                {
                    var messageType = MessageDispatcherComponent.GetOpCodeType(packInfoProtocolCode);

                    if (messageType == null)
                    {
                        packInfo.Dispose();
                        throw new Exception($"InnerMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfoProtocolCode}");
                    }

                    using (packInfo)
                    {
                        RouteMessageHandler(session, messageType, packInfo, OpCode.InnerBsonRouteRequest);
                    }
                    
                    return;
                }
                case > OpCode.InnerRouteMessage:
                {
                    var messageType = MessageDispatcherComponent.GetOpCodeType(packInfoProtocolCode);

                    if (messageType == null)
                    {
                        packInfo.Dispose();
                        throw new Exception($"InnerMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfoProtocolCode}");
                    }

                    using (packInfo)
                    {
                        RouteMessageHandler(session, messageType, packInfo, OpCode.InnerRouteRequest);
                    }
                    
                    return;
                }
                case > OpCode.OuterRouteMessage:
                {
                    var entity = Scene.GetEntity(packInfo.RouteId);

                    switch (entity)
                    {
                        case null:
                        {
                            // 执行到这里是说明Session已经断开了
                            // 因为这里是其他服务器Send到外网的数据、所以不需要给发送端返回就可以
                            return;
                        }
                        case Session gateSession:
                        {
                            using (packInfo)
                            {
                                // 这里如果是Session只可能是Gate的Session、如果是的话、肯定是转发Address消息
                                gateSession.Send(packInfo.MemoryStream, packInfo.RpcId);
                            }
                            
                            return;
                        }
                        default:
                        {
                            var messageType = MessageDispatcherComponent.GetOpCodeType(packInfoProtocolCode);

                            if (messageType == null)
                            {
                                packInfo.Dispose();
                                throw new Exception($"InnerMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfoProtocolCode}");
                            }

                            using (packInfo)
                            {
                                var obj = packInfo.Deserialize(messageType);
                                await MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, (IMessage)obj, packInfo.RpcId);
                            }
                            
                            return;
                        }
                    }
                }
                default:
                {
                    throw new NotSupportedException($"InnerMessageScheduler Received unsupported message protocolCode:{packInfoProtocolCode}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RouteMessageHandler(Session session, Type messageType, APackInfo packInfo, uint protocolCode)
        {
            var obj = packInfo.Deserialize(messageType);
            var entity = Scene.GetEntity(packInfo.RouteId);

            if (entity == null)
            {
                if (packInfo.ProtocolCode > protocolCode)
                {
                    Scene.MessageDispatcherComponent.FailResponse(session, (IRouteRequest)obj, InnerErrorCode.ErrNotFoundRoute, packInfo.RpcId);
                }

                return;
            }

            Scene.MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, (IMessage)obj, packInfo.RpcId).Coroutine();
        }
    }
}
#endif

