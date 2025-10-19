using System;
using System.Collections.Generic;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.DataStructure.Dictionary;
using Fantasy.Entitas;
using Fantasy.InnerMessage;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy.Network.Interface
{
    public sealed class MessageDispatcherComponent : Entity, IAssemblyLifecycle
    {
        private CoroutineLock _receiveRouteMessageLock;
        
        private readonly HashSet<long> _assemblyManifests = new();
        private readonly Dictionary<Type, Type> _responseTypes = new Dictionary<Type, Type>();
        private readonly DoubleMapDictionary<uint, Type> _networkProtocols = new DoubleMapDictionary<uint, Type>();
        private readonly Dictionary<Type, IMessageHandler> _messageHandlers = new Dictionary<Type, IMessageHandler>();
#if FANTASY_NET
        private readonly Dictionary<long, int> _customRouteMap = new Dictionary<long, int>();
        private readonly Dictionary<Type, IRouteMessageHandler> _routeMessageHandlers = new Dictionary<Type, IRouteMessageHandler>();
#endif

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            _assemblyManifests.Clear();
            _responseTypes.Clear();
            _receiveRouteMessageLock.Dispose();
            _messageHandlers.Clear();
#if FANTASY_NET
            _customRouteMap.Clear();
            _routeMessageHandlers.Clear();
#endif
            _receiveRouteMessageLock = null;
            AssemblyLifecycle.Remove(this);
            base.Dispose();
        }

        #region AssemblyManifest

        internal async FTask<MessageDispatcherComponent> Initialize()
        {
            _receiveRouteMessageLock = Scene.CoroutineLockComponent.Create(GetType().TypeHandle.Value.ToInt64());
            await AssemblyLifecycle.Add(this);
            return this;
        }
        
        public async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            var tcs = FTask.Create(false);
            var assemblyManifestId = assemblyManifest.AssemblyManifestId;
            Scene?.ThreadSynchronizationContext.Post(() =>
            {
                if (_assemblyManifests.Contains(assemblyManifestId))
                {
                    OnUnLoadInner(assemblyManifest);
                }
#if FANTASY_NET
                assemblyManifest.MessageDispatcherRegistrar.RegisterSystems(
                    _networkProtocols,
                    _responseTypes,
                    _messageHandlers,
                    _customRouteMap,
                    _routeMessageHandlers);
#endif
#if FANTASY_UNITY
                assemblyManifest.MessageDispatcherRegistrar.RegisterSystems(
                    _networkProtocols,
                    _responseTypes,
                    _messageHandlers);
#endif
                _assemblyManifests.Add(assemblyManifestId);
                tcs.SetResult();
            });
            await tcs;
        }

        public async FTask OnUnload(AssemblyManifest assemblyManifest)
        {
            var tcs = FTask.Create(false);
            Scene?.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyManifest);
                tcs.SetResult();
            });
            await tcs;
        }

        private void OnUnLoadInner(AssemblyManifest assemblyManifest)
        {
#if FANTASY_NET
            assemblyManifest.MessageDispatcherRegistrar.UnRegisterSystems(
                _networkProtocols,
                _responseTypes,
                _messageHandlers,
                _customRouteMap,
                _routeMessageHandlers);
#endif
#if FANTASY_UNITY
            assemblyManifest.MessageDispatcherRegistrar.UnRegisterSystems(
                _networkProtocols,
                _responseTypes,
                _messageHandlers);
#endif
            _assemblyManifests.Remove(assemblyManifest.AssemblyManifestId);
        }

        #endregion

        internal void MessageHandler(Session session, Type type, object message, uint rpcId, uint protocolCode)
        {
            if (!_messageHandlers.TryGetValue(type, out var messageHandler))
            {
                Log.Warning($"Scene:{session.Scene.Id} Found Unhandled Message: {message.GetType()}");
                return;
            }
            
            // 调用消息处理器的Handle方法并启动协程执行处理逻辑
            messageHandler.Handle(session, rpcId, protocolCode, message).Coroutine();
        }
#if FANTASY_NET
        internal async FTask RouteMessageHandler(Session session, Type type, Entity entity, object message, uint rpcId)
        {
            if (!_routeMessageHandlers.TryGetValue(type, out var routeMessageHandler))
            {
                Log.Warning($"Scene:{session.Scene.Id} Found Unhandled RouteMessage: {message.GetType()}");

                if (message is IRouteRequest request)
                {
                    FailRouteResponse(session, request.GetType(), InnerErrorCode.ErrEntityNotFound, rpcId);
                }
                
                return;
            }
            
            var runtimeId = entity.RuntimeId;
            var sessionRuntimeId = session.RuntimeId;

            if (entity is Scene)
            {
                // 如果是Scene的话、就不要加锁了、如果加锁很一不小心就可能会造成死锁
                await routeMessageHandler.Handle(session, entity, rpcId, message);
                return;
            }
            
            // 使用协程锁来确保多线程安全
            using (await _receiveRouteMessageLock.Wait(runtimeId))
            {
                if (sessionRuntimeId != session.RuntimeId)
                {
                    return;
                }

                if (runtimeId != entity.RuntimeId)
                {
                    if (message is IRouteRequest request)
                    {
                        FailRouteResponse(session, request.GetType(), InnerErrorCode.ErrEntityNotFound, rpcId);
                    }
                    
                    return;
                }
                
                await routeMessageHandler.Handle(session, entity, rpcId, message);
            }
        }

        internal bool GetCustomRouteType(long protocolCode, out int routeType)
        {
            return _customRouteMap.TryGetValue(protocolCode, out routeType);
        }
#endif
        #region Response

        internal void FailRouteResponse(Session session, Type requestType, uint error, uint rpcId)
        {
            session.Send(CreateRouteResponse(requestType, error), rpcId);
        }

        internal IResponse CreateResponse(Type requestType, uint error)
        {
            IResponse response;
            
            if (_responseTypes.TryGetValue(requestType, out var responseType))
            {
                response = (IResponse) Activator.CreateInstance(responseType);
            }
            else
            {
                response = new Response();
            }
            
            response.ErrorCode = error;
            return response;
        }

        internal IRouteResponse CreateRouteResponse(Type requestType, uint error)
        {
            IRouteResponse response;

            if (_responseTypes.TryGetValue(requestType, out var responseType))
            {
                response = (IRouteResponse) Activator.CreateInstance(responseType);
            }
            else
            {
                response = new RouteResponse();
            }
            
            response.ErrorCode = error;
            return response;
        }

        #endregion

        #region OpCode

        internal uint GetOpCode(Type type)
        {
            return _networkProtocols.GetKeyByValue(type);
        }

        internal Type GetOpCodeType(uint code)
        {
            return _networkProtocols.GetValueByKey(code);
        }

        #endregion
    }
}