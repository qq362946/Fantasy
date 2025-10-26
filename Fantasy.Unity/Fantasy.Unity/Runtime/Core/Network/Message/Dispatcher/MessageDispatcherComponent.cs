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

        private Func<uint, Type>? _lastHitGetOpCodeType;
        private Func<Session, uint, uint, object, bool>? _lastHitMessageHandler;
        
        private readonly List<INetworkProtocolOpCodeResolver> _opCodeResolvers  = new List<INetworkProtocolOpCodeResolver>();
        private readonly List<INetworkProtocolResponseTypeResolver> _responseTypeResolvers = new List<INetworkProtocolResponseTypeResolver>();
        private readonly List<IMessageHandlerResolver> _messageHandlerResolver = new List<IMessageHandlerResolver>();
#if FANTASY_NET
        private Func<uint, int?>? _lastHitGetCustomRouteType;
        private Func<Session, Entity, uint, uint, object, FTask<bool>>? _lastHitRouteMessageHandler;
        private readonly List<IMessageHandlerResolver> _routeMessageHandlerResolver = new List<IMessageHandlerResolver>();
        private readonly List<INetworkProtocolOpCodeResolver> _customRouteResolvers  = new List<INetworkProtocolOpCodeResolver>();
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

                return y.GetRouteMessageHandlerCount().CompareTo(x.GetRouteMessageHandlerCount());
            }
        }
        private class RouteTypeResolverComparer : IComparer<INetworkProtocolOpCodeResolver>
        {
            public int Compare(INetworkProtocolOpCodeResolver? x, INetworkProtocolOpCodeResolver? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                return y.GetCustomRouteTypeCount().CompareTo(x.GetCustomRouteTypeCount());
            }
        }
#endif

        private class OpCodeResolverComparer : IComparer<INetworkProtocolOpCodeResolver>
        {
            public int Compare(INetworkProtocolOpCodeResolver? x, INetworkProtocolOpCodeResolver? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                return y.GetOpCodeCount().CompareTo(x.GetOpCodeCount());
            }
        }

        private class ResponseTypeResolverComparer : IComparer<INetworkProtocolResponseTypeResolver>
        {
            public int Compare(INetworkProtocolResponseTypeResolver? x, INetworkProtocolResponseTypeResolver? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                return y.GetRequestCount().CompareTo(x.GetRequestCount());
            }
        }
        
        #endregion

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            _assemblyManifests.Clear();
            _opCodeResolvers.Clear();
            _responseTypeResolvers.Clear();
            _receiveRouteMessageLock.Dispose();
            _messageHandlerResolver.Clear();
#if FANTASY_NET
            _customRouteResolvers.Clear();
            _routeMessageHandlerResolver.Clear();
            _lastHitGetCustomRouteType = null;
             _lastHitRouteMessageHandler = null;
#endif
            _lastHitGetOpCodeType = null;
            _lastHitMessageHandler = null;
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
                // 注册Handler
                var messageHandlerResolver = assemblyManifest.MessageHandlerResolver;
                var messageHandlerCount = messageHandlerResolver.GetMessageHandlerCount();
                if (messageHandlerCount > 0)
                {
                    _messageHandlerResolver.Add(messageHandlerResolver);
                    _messageHandlerResolver.Sort(new MessageHandlerResolverComparer());
                }
                // 注册OpCode
                var opCodeResolver = assemblyManifest.NetworkProtocolOpCodeResolver;
                var opCodeCount = opCodeResolver.GetOpCodeCount();
                if (opCodeCount > 0)
                {
                    _opCodeResolvers.Add(opCodeResolver);
                    _opCodeResolvers.Sort(new OpCodeResolverComparer());
                }
#if FANTASY_NET
                var routeMessageHandlerCount = messageHandlerResolver.GetRouteMessageHandlerCount();
                if (routeMessageHandlerCount > 0)
                {
                    _routeMessageHandlerResolver.Add(messageHandlerResolver);
                    _routeMessageHandlerResolver.Sort(new RouteMessageHandlerResolverComparer());
                }
                // 注册CustomRouteType
                var customRouteTypeCount = opCodeResolver.GetCustomRouteTypeCount();
                if (customRouteTypeCount > 0)
                {
                    _customRouteResolvers.Add(opCodeResolver);
                    _customRouteResolvers.Sort(new RouteTypeResolverComparer());
                }
#endif
                // 注册ResponseType
                var responseTypeResolver = assemblyManifest.NetworkProtocolResponseTypeResolver;
                var requestCount = responseTypeResolver.GetRequestCount();
                if (requestCount > 0)
                {
                    _responseTypeResolvers.Add(responseTypeResolver);
                    _responseTypeResolvers.Sort(new ResponseTypeResolverComparer());
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
            _opCodeResolvers.Remove(assemblyManifest.NetworkProtocolOpCodeResolver);
            _responseTypeResolvers.Remove(assemblyManifest.NetworkProtocolResponseTypeResolver);
            _messageHandlerResolver.Sort(new MessageHandlerResolverComparer());
            _opCodeResolvers.Sort(new OpCodeResolverComparer());
            _responseTypeResolvers.Sort(new ResponseTypeResolverComparer());
#if FANTASY_NET
            _routeMessageHandlerResolver.Remove(assemblyManifest.MessageHandlerResolver);
            _customRouteResolvers.Remove(assemblyManifest.NetworkProtocolOpCodeResolver);
            _routeMessageHandlerResolver.Sort(new RouteMessageHandlerResolverComparer());
            _customRouteResolvers.Sort(new RouteTypeResolverComparer());
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
        private async FTask<bool> InnerRouteMessageHandler(Session session, Entity entity, uint rpcId, uint protocolCode, object message)
        {
            if (_lastHitRouteMessageHandler != null &&
                await _lastHitRouteMessageHandler(session, entity, rpcId, protocolCode, message))
            {
                return true;
            }

            for (var i = 0; i < _routeMessageHandlerResolver.Count; i++)
            {
                var resolver = _routeMessageHandlerResolver[i];
                if (await resolver.RouteMessageHandler(session, entity, rpcId, protocolCode, message))
                {
                    _lastHitRouteMessageHandler = resolver.RouteMessageHandler;
                    return true;
                }
            }

            return false;
        }

        internal async FTask RouteMessageHandler(Session session, Type type, Entity entity, object message, uint rpcId, uint protocolCode)
        {
            if (entity is Scene)
            {
                // 如果是Scene的话、就不要加锁了、如果加锁很一不小心就可能会造成死锁
                if (!await InnerRouteMessageHandler(session, entity, rpcId, protocolCode, message))
                {
                    Log.Warning($"Scene:{session.Scene.Id} Found Unhandled RouteMessage: {type}");
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
                    if (message is IRouteRequest request)
                    {
                        FailRouteResponse(session, request.OpCode(), InnerErrorCode.ErrEntityNotFound, rpcId);
                    }
                    
                    return;
                }

                if (!await InnerRouteMessageHandler(session, entity, rpcId, protocolCode, message))
                {
                    Log.Warning($"Scene:{session.Scene.Id} Found Unhandled RouteMessage: {message.GetType()}");
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
            IResponse response;

            for (var i = 0; i < _responseTypeResolvers.Count; i++)
            {
                var resolver = _responseTypeResolvers[i];
                var responseType = resolver.GetResponseType(requestOpCode);
                if (responseType == null)
                {
                    continue;
                }
                response = (IResponse) Activator.CreateInstance(responseType);
                response.ErrorCode = error;
                return response;
            }
            
            response = new Response();
            response.ErrorCode = error;
            return response;
        }

        private IRouteResponse CreateRouteResponse(uint requestOpCode, uint error, out Type responseType)
        {
            IRouteResponse response;

            for (var i = 0; i < _responseTypeResolvers.Count; i++)
            {
                var resolver = _responseTypeResolvers[i];
                responseType = resolver.GetResponseType(requestOpCode);
                if (responseType == null)
                {
                    continue;
                }

                response = (IRouteResponse)Activator.CreateInstance(responseType);
                response.ErrorCode = error;
                return response;
            }

            responseType = typeof(RouteResponse);
            response = new RouteResponse();
            response.ErrorCode = error;
            return response;
        }

        #endregion

        #region OpCode

        internal Type? GetOpCodeType(uint opCode)
        {
            if (_lastHitGetOpCodeType != null )
            {
                var opCodeType = _lastHitGetOpCodeType(opCode);
                if (opCodeType != null)
                {
                    return opCodeType;
                }
            }

            for (var i = 0; i < _opCodeResolvers.Count; i++)
            {
                var resolver = _opCodeResolvers[i];
                var opCodeType = resolver.GetOpCodeType(opCode);
                if (opCodeType != null)
                {
                    _lastHitGetOpCodeType = resolver.GetOpCodeType;
                    return opCodeType;
                }
            }

            return null;
        }
#if FANTASY_NET
        internal int? GetCustomRouteType(uint protocolCode)
        {
            if (_lastHitGetCustomRouteType != null)
            {
                var opCodeType = _lastHitGetCustomRouteType(protocolCode);
                if (opCodeType.HasValue)
                {
                    return opCodeType.Value;
                }
            }

            for (var i = 0; i < _customRouteResolvers.Count; i++)
            {
                var resolver = _customRouteResolvers[i];
                var opCodeType = resolver.GetCustomRouteType(protocolCode);
                if (opCodeType.HasValue)
                {
                    _lastHitGetCustomRouteType = resolver.GetCustomRouteType;
                    return opCodeType.Value;
                }
            }

            return null;
        }
#endif
        #endregion
    }
}