#if FANTASY_NET
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System.Runtime.CompilerServices;
using Fantasy.IdFactory;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using Fantasy.PacketParser.Interface;
using Fantasy.Platform.Net;
using Fantasy.Serialize;

namespace Fantasy.Scheduler;

internal static class ProcessScheduler
{
    public static void Scheduler(this ProcessSession session, Type messageType, uint rpcId, long routeId, APackInfo packInfo)
    {
        switch (packInfo.OpCodeIdStruct.Protocol)
        {
            case OpCodeType.InnerResponse:
            case OpCodeType.InnerRouteResponse:
            case OpCodeType.InnerAddressableResponse:
            case OpCodeType.OuterAddressableResponse:
            case OpCodeType.OuterCustomRouteResponse:
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
            case OpCodeType.InnerRouteMessage:
            {
                using (packInfo)
                {
                    var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);
                
                    if (!Process.TryGetScene(sceneId, out var scene))
                    {
                        throw new Exception($"not found scene routeId:{routeId}");
                    }
                    
                    var message = packInfo.Deserialize(messageType);
        
                    scene.ThreadSynchronizationContext.Post(() =>
                    {
                        var entity = scene.GetEntity(routeId);
                        var sceneMessageDispatcherComponent = scene.MessageDispatcherComponent;
            
                        if (entity == null || entity.IsDisposed)
                        {
                            return;
                        }

                        sceneMessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, message, rpcId).Coroutine();
                    });
                }
                
                return;
            }
            case OpCodeType.InnerRouteRequest:
            {
                using (packInfo)
                {
                    var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);
                
                    if (!Process.TryGetScene(sceneId, out var scene))
                    {
                        throw new Exception($"not found scene routeId:{routeId}");
                    }
                    
                    var message = packInfo.Deserialize(messageType);
        
                    scene.ThreadSynchronizationContext.Post(() =>
                    {
                        var entity = scene.GetEntity(routeId);
                        var sceneMessageDispatcherComponent = scene.MessageDispatcherComponent;
            
                        if (entity == null || entity.IsDisposed)
                        {
                            sceneMessageDispatcherComponent.FailRouteResponse(session, messageType, InnerErrorCode.ErrNotFoundRoute, rpcId);
                            return;
                        }

                        sceneMessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, message, rpcId).Coroutine();
                    });
                }
                
                return;
            }
            case OpCodeType.OuterAddressableMessage:
            case OpCodeType.OuterCustomRouteMessage:
            case OpCodeType.OuterAddressableRequest:
            case OpCodeType.OuterCustomRouteRequest:
            {
                using (packInfo)
                {
                    var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);

                    if (!Process.TryGetScene(sceneId, out var scene))
                    {
                        throw new NotSupportedException($"not found scene routeId = {routeId}");
                    }
                    
                    var message = packInfo.Deserialize(messageType);
                    
                    scene.ThreadSynchronizationContext.Post(() =>
                    {
                        var entity = scene.GetEntity(routeId);

                        if (entity == null || entity.IsDisposed)
                        {
                            scene.MessageDispatcherComponent.FailRouteResponse(session, messageType, InnerErrorCode.ErrNotFoundRoute, rpcId);
                            return;
                        }
                        
                        scene.MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, message, rpcId).Coroutine();
                    });
                }
                return;
            }
            default:
            {
                var packInfoProtocolCode = packInfo.ProtocolCode;
                packInfo.Dispose();
                throw new NotSupportedException($"SessionInnerScheduler Received unsupported message protocolCode:{packInfoProtocolCode} messageType:{messageType}");
            }
        }
    }
    
    public static void Scheduler(this ProcessSession session, Type messageType, uint rpcId, long routeId, uint protocolCode, object message)
    {
        OpCodeIdStruct opCodeIdStruct = protocolCode;
        
        switch (opCodeIdStruct.Protocol)
        {
            case OpCodeType.InnerResponse:
            case OpCodeType.InnerRouteResponse:
            case OpCodeType.InnerAddressableResponse:
            case OpCodeType.OuterAddressableResponse:
            case OpCodeType.OuterCustomRouteResponse:
            {
                var sessionScene = session.Scene;
                sessionScene.ThreadSynchronizationContext.Post(() =>
                {
                    var iResponse = (IResponse)session.Deserialize(messageType, message, ref opCodeIdStruct);
                    // 因为有可能是其他Scene线程下发送过来的、所以必须放到当前Scene进程下运行。
                    sessionScene.NetworkMessagingComponent.ResponseHandler(rpcId, iResponse);
                });
                
                return;
            }
            case OpCodeType.InnerAddressableMessage:
            case OpCodeType.InnerRouteMessage:
            {
                var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);
                
                if (!Process.TryGetScene(sceneId, out var scene))
                {
                    throw new Exception($"not found scene routeId:{routeId}");
                }
        
                var messageObject = session.Deserialize(messageType, message, ref opCodeIdStruct);
                
                scene.ThreadSynchronizationContext.Post(() =>
                {
                    var entity = scene.GetEntity(routeId);
                    var sceneMessageDispatcherComponent = scene.MessageDispatcherComponent;
            
                    if (entity == null || entity.IsDisposed)
                    {
                        return;
                    }
                    
                    sceneMessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, messageObject, rpcId).Coroutine();
                });
                
                return;
            }
            case OpCodeType.InnerAddressableRequest:
            case OpCodeType.InnerRouteRequest:
            {
                var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);
                
                if (!Process.TryGetScene(sceneId, out var scene))
                {
                    throw new Exception($"not found scene routeId:{routeId}");
                }
                
                var messageObject = session.Deserialize(messageType, message, ref opCodeIdStruct);
        
                scene.ThreadSynchronizationContext.Post(() =>
                {
                    var entity = scene.GetEntity(routeId);
                    var sceneMessageDispatcherComponent = scene.MessageDispatcherComponent;
            
                    if (entity == null || entity.IsDisposed)
                    {
                        sceneMessageDispatcherComponent.FailRouteResponse(session, message.GetType(), InnerErrorCode.ErrNotFoundRoute, rpcId);
                        return;
                    }
                    
                    sceneMessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, messageObject, rpcId).Coroutine();
                });
                
                return;
            }
            case OpCodeType.OuterAddressableMessage:
            case OpCodeType.OuterCustomRouteMessage:
            {
                var sceneId = RuntimeIdFactory.GetSceneId(ref routeId);
                
                if (!Process.TryGetScene(sceneId, out var scene))
                {
                    Log.Error($"not found scene routeId:{routeId}");
                    return;
                }
                
                var messageObject = session.Deserialize(messageType, message, ref opCodeIdStruct);
                
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
                            gateSession.Send((IMessage)messageObject, rpcId);
                            return;
                        }
                        default:
                        {
                            scene.MessageDispatcherComponent.RouteMessageHandler(session, messageType, entity, messageObject, rpcId).Coroutine();
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
}
#endif