#if FANTASY_NET
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System.Runtime.CompilerServices;

namespace Fantasy;

public static class ProcessScheduler
{
    public static async FTask Scheduler(this ProcessSession session, Type messageType, uint rpcId, long routeId, APackInfo packInfo)
    {
        await FTask.CompletedTask;
        var protocolCode = packInfo.ProtocolCode;
        
        switch (protocolCode)
        {
            case >= OpCode.InnerBsonRouteResponse:
            case >= OpCode.InnerRouteResponse:
            case >= OpCode.OuterRouteResponse: // 如果Gate服务器、需要转发Addressable协议、所以这里有可能会接收到该类型协议。
            {
                using (packInfo)
                {
                    var sessionScene = session.Scene;
                    var message = packInfo.Deserialize(messageType);
                    sessionScene.ThreadSynchronizationContext.Post(() =>
                    {
                        // 因为有可能是其他Scene线程下发送过来的、所以必须放到当前Scene进程下运行。
                        sessionScene.NetworkMessagingComponent.ResponseHandler(rpcId, (IResponse)message);
                    });
                }
                
                return;
            }
            case > OpCode.InnerBsonRouteMessage:
            {
                using (packInfo)
                {
                    var message = packInfo.Deserialize(messageType);
                    InnerRouteMessageHandler(session, messageType, packInfo.RouteId, rpcId, protocolCode, OpCode.InnerBsonRouteRequest, message);
                }
                
                return;
            }
            case > OpCode.InnerRouteMessage:
            {
                using (packInfo)
                {
                    var message = packInfo.Deserialize(messageType);
                    InnerRouteMessageHandler(session, messageType, packInfo.RouteId, rpcId, protocolCode, OpCode.InnerRouteRequest, message);
                }
                
                return;
            }
            case > OpCode.OuterRouteMessage:
            {
                var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);

                if (!Process.TryGetScene(sceneId, out var scene))
                {
                    packInfo.Dispose();
                    Log.Error($"not found scene routeId:{routeId}");
                    return;
                }

                using (packInfo)
                {
                    var message = packInfo.Deserialize(messageType);
                    
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
                }
                
                return;
            }
            default:
            {
                throw new NotSupportedException($"SessionInnerScheduler Received unsupported message protocolCode:{protocolCode} messageType:{messageType}");
            }
        }
    }
    
    public static async FTask Scheduler(this ProcessSession session, Type messageType, uint rpcId, long routeId, uint protocolCode, object message)
    {
        await FTask.CompletedTask;
        
        switch (protocolCode)
        {
            case >= OpCode.InnerBsonRouteResponse:
            case >= OpCode.InnerRouteResponse:
            case >= OpCode.OuterRouteResponse: // 如果Gate服务器、需要转发Addressable协议、所以这里有可能会接收到该类型协议。
            {
                var sessionScene = session.Scene;
                sessionScene.ThreadSynchronizationContext.Post(() =>
                {
                    // 因为有可能是其他Scene线程下发送过来的、所以必须放到当前Scene进程下运行。
                    sessionScene.NetworkMessagingComponent.ResponseHandler(rpcId, (IResponse)message);
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
                    Log.Error($"not found scene routeId:{routeId}");
                    return;
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
                throw new NotSupportedException($"SessionInnerScheduler Received unsupported message protocolCode:{protocolCode} messageType:{messageType}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InnerRouteMessageHandler(Session session, Type messageType, long routeId, uint rpcId, uint protocolCode, uint errorProtocolCode, object message)
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
#endif