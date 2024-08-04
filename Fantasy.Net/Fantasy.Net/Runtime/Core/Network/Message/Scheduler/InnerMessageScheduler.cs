// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System.Runtime.CompilerServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if FANTASY_NET

namespace Fantasy
{
    /// <summary>
    /// 提供了一个机制来调度和处理内部网络消息。
    /// </summary>
    public sealed class InnerMessageScheduler : ANetworkMessageScheduler
    {
        public InnerMessageScheduler(Scene scene) : base(scene) { }
        
        protected override async FTask Handler(Session session, Type messageType, APackInfo packInfo)
        {
            try
            {
                switch (packInfo.ProtocolCode)
                {
                    case >= OpCode.InnerBsonRouteResponse:
                    case >= OpCode.InnerRouteResponse:
                    case >= OpCode.OuterRouteResponse: // 如果Gate服务器、需要转发Addressable协议、所以这里有可能会接收到该类型协议。
                    {
                        var rpcId = packInfo.RpcId;
                        var response = (IResponse)packInfo.Deserialize(messageType);
                        NetworkMessagingComponent.ResponseHandler(rpcId, response);
                        return;
                    }
                    case > OpCode.InnerBsonRouteMessage:
                    {
                        RouteMessageHandler(session, messageType, packInfo, OpCode.InnerBsonRouteRequest);
                        return;
                    }
                    case > OpCode.InnerRouteMessage:
                    {
                        RouteMessageHandler(session, messageType, packInfo, OpCode.InnerRouteRequest);
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
                                // 这里如果是Session只可能是Gate的Session、如果是的话、肯定是转发Address消息
                                gateSession.Send(packInfo.MemoryStream, packInfo.RpcId);
                                return;
                            }
                            default:
                            {
                                var obj = packInfo.Deserialize(messageType);
                                await MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, (IMessage)obj, packInfo.RpcId);
                                return;
                            }
                        }
                    }
                    default:
                    {
                        throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"InnerMessageSchedulerHandler error messageProtocolCode:{packInfo.ProtocolCode} messageType:{messageType} {e}");
            }
            finally
            {
                packInfo.Dispose();
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

        public override async FTask InnerScheduler(Session session, Type messageType, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, object message)
        {
            try
            {
                switch (protocolCode)
                {
                    case >= OpCode.InnerBsonRouteResponse:
                    case >= OpCode.InnerRouteResponse:
                    case >= OpCode.OuterRouteResponse: // 如果Gate服务器、需要转发Addressable协议、所以这里有可能会接收到该类型协议。
                    {
                        Scene.ThreadSynchronizationContext.Post(() =>
                        {
                            // 因为有可能是其他Scene线程下发送过来的、所以必须放到当前Scene进程下运行。
                            NetworkMessagingComponent.ResponseHandler(rpcId, (IResponse)message);
                        });
                        
                        return;
                    }
                    case > OpCode.InnerBsonRouteMessage:
                    {
                        InnerRouteMessageHandler(session, messageType, routeId, rpcId, protocolCode, OpCode.InnerBsonRouteRequest, message);
                        return;
                    }
                    case > OpCode.InnerRouteMessage:
                    {
                        InnerRouteMessageHandler(session, messageType, routeId, rpcId, protocolCode, OpCode.InnerRouteRequest, message);
                        return;
                    }
                    case > OpCode.OuterRouteMessage:
                    {
                        var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);

                        if (!Process.TryGetScene(sceneId, out var scene))
                        {
                            throw new Exception($"not found scene routeId:{routeId}");
                        }

                        scene.ThreadSynchronizationContext.Post(() =>
                        {
                            var entity = scene.GetEntity(routeId);

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
                                    // 这里如果是Session只可能是Gate的Session、如果是的话、肯定是转发Address消息
                                    gateSession.Send((IMessage)message, rpcId);
                                    return;
                                }
                                default:
                                {
                                    scene.MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, message, rpcId).Coroutine();
                                    return;
                                }
                            }
                        });

                        return;
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

            await FTask.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InnerRouteMessageHandler(Session session, Type messageType, long routeId, uint rpcId, uint protocolCode, uint errorProtocolCode, object message)
        {
            var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);

            if (!Process.TryGetScene(sceneId, out var scene))
            {
                throw new Exception($"not found scene routeId:{routeId}");
            }

            scene.ThreadSynchronizationContext.Post(() =>
            {
                var entity = scene.GetEntity(routeId);
                var sceneMessageDispatcherComponent = scene.MessageDispatcherComponent;
                
                if (entity == null || entity.IsDisposed)
                {
                    if (protocolCode > errorProtocolCode)
                    {
                        sceneMessageDispatcherComponent.FailResponse(session, (IRouteRequest)message, InnerErrorCode.ErrNotFoundRoute, rpcId);
                    }

                    return;
                }

                sceneMessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, message, rpcId).Coroutine();
            });
        }

    }
}
#endif

