// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#if FANTASY_NET

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 提供了一个机制来调度和处理内部网络消息。
    /// </summary>
    public sealed class InnerMessageScheduler : ANetworkMessageScheduler
    {
        /// <summary>
        /// 在FantasyNet环境下，处理外部消息的方法。
        /// </summary>
        /// <param name="session">网络会话。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="packInfo">消息封包信息。</param>
        protected override async FTask Handler(Session session, Type messageType, APackInfo packInfo)
        {
            try
            {
                switch (packInfo.ProtocolCode)
                {
                    case >= Opcode.InnerBsonRouteResponse:
                    case >= Opcode.InnerRouteResponse:
                    {
                        var response = (IRouteResponse)packInfo.Deserialize(messageType);
                        MessageHelper.ResponseHandler(packInfo.RpcId, response);
                        return;
                    }
                    case >= Opcode.OuterRouteResponse:
                    {
                        // 如果Gate服务器、需要转发Addressable协议、所以这里有可能会接收到该类型协议
                        var aResponse = (IResponse)packInfo.Deserialize(messageType);
                        MessageHelper.ResponseHandler(packInfo.RpcId, aResponse);
                        return;
                    }
                    case > Opcode.InnerBsonRouteMessage:
                    {
                        var obj = packInfo.Deserialize(messageType);
                        var entity = Entity.GetEntity(packInfo.RouteId);

                        if (entity == null)
                        {
                            if (packInfo.ProtocolCode > Opcode.InnerBsonRouteRequest)
                            {
                                MessageDispatcherSystem.Instance.FailResponse(session, (IRouteRequest)obj, CoreErrorCode.ErrNotFoundRoute, packInfo.RpcId);
                            }

                            return;
                        }

                        await MessageDispatcherSystem.Instance.RouteMessageHandler(session, messageType, entity, obj, packInfo.RpcId);
                        return;
                    }
                    case > Opcode.InnerRouteMessage:
                    {
                        var obj = packInfo.Deserialize(messageType);
                        var entity = Entity.GetEntity(packInfo.RouteId);

                        if (entity == null)
                        {
                            if (packInfo.ProtocolCode > Opcode.InnerRouteRequest)
                            {
                                MessageDispatcherSystem.Instance.FailResponse(session, (IRouteRequest)obj, CoreErrorCode.ErrNotFoundRoute, packInfo.RpcId);
                            }

                            return;
                        }

                        await MessageDispatcherSystem.Instance.RouteMessageHandler(session, messageType, entity, obj, packInfo.RpcId);
                        return;
                    }
                    case > Opcode.OuterRouteMessage:
                    {
                        var entity = Entity.GetEntity(packInfo.RouteId);

                        switch (entity)
                        {
                            case null:
                            {
                                // 执行到这里是说明Session已经断开了
                                // 因为这里是其他服务器Send到外网的数据、所以不需要给发送端返回就可以
                                // var obj = packInfo.Deserialize(messageType);
                                // var response = MessageDispatcherSystem.Instance.CreateResponse((IRouteMessage)obj, CoreErrorCode.ErrNotFoundRoute);
                                // session.Send(response, packInfo.RpcId, packInfo.RouteId);
                                return;
                            }
                            case Session gateSession:
                            {
                                // 这里如果是Session只可能是Gate的Session、如果是的话、肯定是转发Address消息
                                gateSession.Send(packInfo.CreateMemoryStream(), packInfo.RpcId);
                                return;
                            }
                            default:
                            {
                                var obj = packInfo.Deserialize(messageType);
                                await MessageDispatcherSystem.Instance.RouteMessageHandler(session, messageType, entity, obj, packInfo.RpcId);
                                return;
                            }
                        }
                    }
                    default:
                    {
                        throw new NotSupportedException(
                            $"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"InnerMessageSchedulerHandler error messageProtocolCode:{packInfo.ProtocolCode} messageType:{messageType} {e}");
            }
            finally
            {
                NetworkThread.Instance.SynchronizationContext.Post(packInfo.Dispose);
            }
        }

        /// <summary>
        /// 在FantasyNet环境下，处理内部消息的方法。
        /// </summary>
        /// <param name="session">网络会话。</param>
        /// <param name="rpcId">RPC请求ID。</param>
        /// <param name="routeId">消息路由ID。</param>
        /// <param name="protocolCode">协议码。</param>
        /// <param name="routeTypeCode">路由类型码。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="message">消息对象。</param>
        protected override async FTask InnerHandler(Session session, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, Type messageType, object message)
        {
            try
            {
                switch (protocolCode)
                {
                    case >= Opcode.InnerBsonRouteResponse:
                    case >= Opcode.InnerRouteResponse:
                    {
                        MessageHelper.ResponseHandler(rpcId, (IRouteResponse)message);
                        return;
                    }
                    case >= Opcode.OuterRouteResponse:
                    {
                        // 如果Gate服务器、需要转发Addressable协议、所以这里有可能会接收到该类型协议
                        MessageHelper.ResponseHandler(rpcId, (IResponse)message);
                        return;
                    }
                    case > Opcode.InnerBsonRouteMessage:
                    {
                        var entity = Entity.GetEntity(routeId);

                        if (entity == null)
                        {
                            if (protocolCode > Opcode.InnerBsonRouteRequest)
                            {
                                MessageDispatcherSystem.Instance.FailResponse(session, (IRouteRequest)message, CoreErrorCode.ErrNotFoundRoute, rpcId);
                            }

                            return;
                        }

                        await MessageDispatcherSystem.Instance.RouteMessageHandler(session, messageType, entity, message, rpcId);
                        return;
                    }
                    case > Opcode.InnerRouteMessage:
                    {
                        var entity = Entity.GetEntity(routeId);

                        if (entity == null)
                        {
                            if (protocolCode > Opcode.InnerRouteRequest)
                            {
                                MessageDispatcherSystem.Instance.FailResponse(session, (IRouteRequest)message, CoreErrorCode.ErrNotFoundRoute, rpcId);
                            }

                            return;
                        }

                        await MessageDispatcherSystem.Instance.RouteMessageHandler(session, messageType, entity, message, rpcId);
                        return;
                    }
                    case > Opcode.OuterRouteMessage:
                    {
                        var entity = Entity.GetEntity(routeId);

                        switch (entity)
                        {
                            case null:
                            {
                                var response = MessageDispatcherSystem.Instance.CreateResponse((IRouteMessage)message, CoreErrorCode.ErrNotFoundRoute);
                                session.Send(response, rpcId, routeId);
                                return;
                            }
                            case Session gateSession:
                            {
                                // 这里如果是Session只可能是Gate的Session、如果是的话、肯定是转发Address消息
                                gateSession.Send(message, rpcId);
                                return;
                            }
                            default:
                            {
                                await MessageDispatcherSystem.Instance.RouteMessageHandler(session, messageType, entity, message, rpcId);
                                return;
                            }
                        }
                    }
                    default:
                    {
                        throw new NotSupportedException($"Received unsupported message protocolCode:{protocolCode} messageType:{messageType}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"InnerMessageSchedulerHandler error messageProtocolCode:{protocolCode} messageType:{messageType} {e}");
            }
        }
    }
}
#endif

