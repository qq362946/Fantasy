using System;
using System.Collections.Generic;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.DataStructure.Dictionary;
using Fantasy.Entitas;
using Fantasy.InnerMessage;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable InvertIf
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
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
        
        private Func<Session, uint, uint, object, bool>? _lastHitMessageHandler;

        private UInt32FrozenDictionary<Type> _opCodeDictionary;
        private UInt32FrozenDictionary<Type> _responseTypeDictionary;
        private readonly List<IMessageHandlerResolver> _messageHandlerResolver = new List<IMessageHandlerResolver>();
#if FANTASY_NET
        private UInt32FrozenDictionary<int> _customRouteDictionary;
        private Func<Session, Entity, uint, uint, object, FTask<bool>>? _lastHitRouteMessageHandler;
        private readonly List<IMessageHandlerResolver> _routeMessageHandlerResolver = new List<IMessageHandlerResolver>();
#endif
        
        #region Comparer
        
        private class MessageHandlerResolverComparer : IComparer<IMessageHandlerResolver>
        {
            public int Compare(IMessageHandlerResolver? x, IMessageHandlerResolver? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                return y.GetMessageHandlerCount().CompareTo(x.GetMessageHandlerCount());
            }
        }
#if FANTASY_NET     
        private class RouteMessageHandlerResolverComparer : IComparer<IMessageHandlerResolver>
        {
            public int Compare(IMessageHandlerResolver? x, IMessageHandlerResolver? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                return y.GetAddressMessageHandlerCount().CompareTo(x.GetAddressMessageHandlerCount());
            }
        }
#endif
        #endregion

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            _assemblyManifests.Clear();
            _receiveRouteMessageLock.Dispose();
            _messageHandlerResolver.Clear();
#if FANTASY_NET
            _routeMessageHandlerResolver.Clear();
            _customRouteDictionary = null;
             _lastHitRouteMessageHandler = null;
#endif
            _opCodeDictionary = null;
            _lastHitMessageHandler = null;
            _receiveRouteMessageLock = null;
            _responseTypeDictionary = null;
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
                // 注册Handler
                var messageHandlerResolver = assemblyManifest.MessageHandlerResolver;
                var messageHandlerCount = messageHandlerResolver.GetMessageHandlerCount();
                if (messageHandlerCount > 0)
                {
                    _messageHandlerResolver.Add(messageHandlerResolver);
                    _messageHandlerResolver.Sort(new MessageHandlerResolverComparer());
                }
                // 注册OpCode
                var opCodeResolver = assemblyManifest.OpCodeRegistrar;
                var opCodeCount = opCodeResolver.GetOpCodeCount();
                if (opCodeCount > 0)
                {
                    var opCodes = new uint[opCodeCount + InnerNetworkProtocolRegistrar.OpCodeCount];
                    var types = new Type[opCodeCount + InnerNetworkProtocolRegistrar.OpCodeCount];
                    opCodeResolver.FillOpCodeType(opCodes, types);
                    InnerNetworkProtocolRegistrar.FillOpCode(opCodes, types, opCodeCount);
                    _opCodeDictionary = new UInt32FrozenDictionary<Type>(opCodes, types);
                }
#if FANTASY_NET
                var customRouteTypeCount = opCodeResolver.GetCustomRouteTypeCount();
                if (customRouteTypeCount > 0)
                {
                    var opCodes = new uint[customRouteTypeCount + InnerNetworkProtocolRegistrar.CustomRouteTypeCount];
                    var routeTypes = new int[customRouteTypeCount + InnerNetworkProtocolRegistrar.CustomRouteTypeCount];
                    opCodeResolver.FillCustomRouteType(opCodes, routeTypes);
                    InnerNetworkProtocolRegistrar.FillCustomRouteType(opCodes, routeTypes, customRouteTypeCount);
                    _customRouteDictionary = new UInt32FrozenDictionary<int>(opCodes, routeTypes);
                }
                var routeMessageHandlerCount = messageHandlerResolver.GetAddressMessageHandlerCount();
                if (routeMessageHandlerCount > 0)
                {
                    _routeMessageHandlerResolver.Add(messageHandlerResolver);
                    _routeMessageHandlerResolver.Sort(new RouteMessageHandlerResolverComparer());
                }
#endif
                // 注册ResponseType
                var responseTypeRegistrar = assemblyManifest.ResponseTypeRegistrar;
                var requestCount = responseTypeRegistrar.GetRequestCount();
                if (requestCount > 0)
                {
                    var opCodes = new uint[requestCount + InnerNetworkProtocolRegistrar.GetRequestCount];
                    var types = new Type[requestCount + InnerNetworkProtocolRegistrar.GetRequestCount];
                    responseTypeRegistrar.FillResponseType(opCodes, types);
                    InnerNetworkProtocolRegistrar.FillResponseType(opCodes, types, requestCount);
                    _responseTypeDictionary = new UInt32FrozenDictionary<Type>(opCodes, types);
                }
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
            _messageHandlerResolver.Remove(assemblyManifest.MessageHandlerResolver);
            _messageHandlerResolver.Sort(new MessageHandlerResolverComparer());
#if FANTASY_NET
            _routeMessageHandlerResolver.Remove(assemblyManifest.MessageHandlerResolver);
            _routeMessageHandlerResolver.Sort(new RouteMessageHandlerResolverComparer());
#endif
            _assemblyManifests.Remove(assemblyManifest.AssemblyManifestId);
        }

        #endregion

        internal void MessageHandler(Session session, Type type, object message, uint rpcId, uint protocolCode)
        {
            if (_lastHitMessageHandler != null &&
                _lastHitMessageHandler(session, rpcId, protocolCode, message))
            {
                return;
            }
            
            for (var i = 0; i < _messageHandlerResolver.Count; i++)
            {
                var resolver = _messageHandlerResolver[i];
                if (resolver.MessageHandler(session, rpcId, protocolCode, message))
                {
                    _lastHitMessageHandler = resolver.MessageHandler;
                    return;
                }
            }
            
            Log.Warning($"Scene:{session.Scene.Id} Found Unhandled Message: {type}");
        }
#if FANTASY_NET
        private async FTask<bool> InnerAddressMessageHandler(Session session, Entity entity, uint rpcId, uint protocolCode, object message)
        {
            if (_lastHitRouteMessageHandler != null &&
                await _lastHitRouteMessageHandler(session, entity, rpcId, protocolCode, message))
            {
                return true;
            }

            for (var i = 0; i < _routeMessageHandlerResolver.Count; i++)
            {
                var resolver = _routeMessageHandlerResolver[i];
                if (await resolver.AddressMessageHandler(session, entity, rpcId, protocolCode, message))
                {
                    _lastHitRouteMessageHandler = resolver.AddressMessageHandler;
                    return true;
                }
            }

            return false;
        }

        internal async FTask AddressMessageHandler(Session session, Type type, Entity entity, object message, uint rpcId, uint protocolCode)
        {
            if (entity is Scene)
            {
                // 如果是Scene的话、就不要加锁了、如果加锁很一不小心就可能会造成死锁
                if (!await InnerAddressMessageHandler(session, entity, rpcId, protocolCode, message))
                {
                    Log.Warning($"Scene:{session.Scene.Id} Found Unhandled AddressMessage: {type}");
                }
                return;
            }
            
            // 使用协程锁来确保消息的顺序
            var runtimeId = entity.RuntimeId;
            var sessionRuntimeId = session.RuntimeId;
            using (await _receiveRouteMessageLock.Wait(runtimeId))
            {
                if (sessionRuntimeId != session.RuntimeId)
                {
                    return;
                }

                if (runtimeId != entity.RuntimeId)
                {
                    if (message is IAddressRequest request)
                    {
                        FailRouteResponse(session, request.OpCode(), InnerErrorCode.ErrEntityNotFound, rpcId);
                    }
                    
                    return;
                }

                if (!await InnerAddressMessageHandler(session, entity, rpcId, protocolCode, message))
                {
                    Log.Warning($"Scene:{session.Scene.Id} Found Unhandled AddressMessage: {message.GetType()}");
                }
            }
        }
#endif
        #region Response

        internal void FailRouteResponse(Session session, uint requestOpCode, uint error, uint rpcId)
        {
            session.Send(CreateRouteResponse(requestOpCode, error, out var responseType), responseType, rpcId);
        }

        internal IResponse CreateResponse(uint requestOpCode, uint error)
        {
            if (_responseTypeDictionary.TryGetValue(requestOpCode, out var responseType))
            {
                var findResponse = (IResponse)Activator.CreateInstance(responseType);
                findResponse.ErrorCode = error;
                return findResponse;
            }

            var response = new Response();
            response.ErrorCode = error;
            return response;
        }

        private IAddressResponse CreateRouteResponse(uint requestOpCode, uint error, out Type responseType)
        {
            if (_responseTypeDictionary.TryGetValue(requestOpCode, out responseType))
            {
                var findResponse = (IAddressResponse)Activator.CreateInstance(responseType);
                findResponse.ErrorCode = error;
                return findResponse;
            }
            
            responseType = typeof(AddressResponse);
            var response = new AddressResponse();
            response.ErrorCode = error;
            return response;
        }

        #endregion

        #region OpCode

        internal Type? GetOpCodeType(uint opCode)
        {
            return _opCodeDictionary.TryGetValue(opCode, out var type) ? type : null;
        }
#if FANTASY_NET
        internal int? GetCustomRouteType(uint protocolCode)
        {
            return _customRouteDictionary.TryGetValue(protocolCode, out var type) ? type : null;
        }
#endif
        #endregion
    }
}