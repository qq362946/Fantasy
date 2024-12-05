using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.DataStructure.Dictionary;
using Fantasy.Entitas;
using Fantasy.InnerMessage;
using Fantasy.Network;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Network.Interface
{
    /// <summary>
    /// 用于存储消息处理器的信息，包括类型和对象实例。
    /// </summary>
    /// <typeparam name="T">消息处理器的类型</typeparam>
    internal sealed class HandlerInfo<T>
    {
        /// <summary>
        /// 获取或设置消息处理器对象。
        /// </summary>
        public T Obj;
        /// <summary>
        /// 获取或设置消息处理器的类型。
        /// </summary>
        public Type Type;
    }
    
    /// <summary>
    /// 网络消息分发组件。
    /// </summary>
    public sealed class MessageDispatcherComponent : Entity, IAssembly
    {
        public long AssemblyIdentity { get; set; }
        private readonly Dictionary<Type, Type> _responseTypes = new Dictionary<Type, Type>();
        private readonly DoubleMapDictionary<uint, Type> _networkProtocols = new DoubleMapDictionary<uint, Type>();
        private readonly Dictionary<Type, IMessageHandler> _messageHandlers = new Dictionary<Type, IMessageHandler>();
        private readonly OneToManyList<long, Type> _assemblyResponseTypes = new OneToManyList<long, Type>();
        private readonly OneToManyList<long, uint> _assemblyNetworkProtocols = new OneToManyList<long, uint>();
        private readonly OneToManyList<long, HandlerInfo<IMessageHandler>> _assemblyMessageHandlers = new OneToManyList<long, HandlerInfo<IMessageHandler>>();
#if FANTASY_UNITY

        private readonly Dictionary<Type, IMessageDelegateHandler> _messageDelegateHandlers = new Dictionary<Type, IMessageDelegateHandler>();
#endif
#if FANTASY_NET
        private readonly Dictionary<long, int> _customRouteMap = new Dictionary<long, int>();
        private readonly OneToManyList<long, long> _assemblyCustomRouteMap = new OneToManyList<long, long>();
        private readonly Dictionary<Type, IRouteMessageHandler> _routeMessageHandlers = new Dictionary<Type, IRouteMessageHandler>();
        private readonly OneToManyList<long, HandlerInfo<IRouteMessageHandler>> _assemblyRouteMessageHandlers = new OneToManyList<long, HandlerInfo<IRouteMessageHandler>>();
#endif
        private CoroutineLock _receiveRouteMessageLock;
        
        #region Initialize

        internal async FTask<MessageDispatcherComponent> Initialize()
        {
            _receiveRouteMessageLock = Scene.CoroutineLockComponent.Create(GetType().TypeHandle.Value.ToInt64());
            await AssemblySystem.Register(this);
            return this;
        }

        public async FTask Load(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                LoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        private void LoadInner(long assemblyIdentity)
        {
            // 遍历所有实现了IMessage接口的类型，获取OpCode并添加到_networkProtocols字典中
            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(IMessage)))
            {
                var obj = (IMessage) Activator.CreateInstance(type);
                var opCode = obj.OpCode();
                
                _networkProtocols.Add(opCode, type);

                var responseType = type.GetProperty("ResponseType");

                // 如果类型具有ResponseType属性，将其添加到_responseTypes字典中
                if (responseType != null)
                {
                    _responseTypes.Add(type, responseType.PropertyType);
                    _assemblyResponseTypes.Add(assemblyIdentity, type);
                }

                _assemblyNetworkProtocols.Add(assemblyIdentity, opCode);
            }
            
            // 遍历所有实现了IMessageHandler接口的类型，创建实例并添加到_messageHandlers字典中
            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(IMessageHandler)))
            {
                var obj = (IMessageHandler) Activator.CreateInstance(type);

                if (obj == null)
                {
                    throw new Exception($"message handle {type.Name} is null");
                }

                var key = obj.Type();
                _messageHandlers.Add(key, obj);
                _assemblyMessageHandlers.Add(assemblyIdentity, new HandlerInfo<IMessageHandler>()
                {
                    Obj = obj, Type = key
                });
            }
            
            // 如果编译符号FANTASY_NET存在，遍历所有实现了IRouteMessageHandler接口的类型，创建实例并添加到_routeMessageHandlers字典中
#if FANTASY_NET
            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(IRouteMessageHandler)))
            {
                var obj = (IRouteMessageHandler) Activator.CreateInstance(type);

                if (obj == null)
                {
                    throw new Exception($"message handle {type.Name} is null");
                }

                var key = obj.Type();
                _routeMessageHandlers.Add(key, obj);
                _assemblyRouteMessageHandlers.Add(assemblyIdentity, new HandlerInfo<IRouteMessageHandler>()
                {
                    Obj = obj, Type = key
                });
            }

            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(ICustomRoute)))
            {
                var obj = (ICustomRoute) Activator.CreateInstance(type);
                
                if (obj == null)
                {
                    throw new Exception($"message handle {type.Name} is null");
                }

                var opCode = obj.OpCode();
                _customRouteMap[opCode] = obj.RouteType;
                _assemblyCustomRouteMap.Add(assemblyIdentity, opCode);
            }
#endif
        }

        public async FTask ReLoad(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                LoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        public async FTask OnUnLoad(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        private void OnUnLoadInner(long assemblyIdentity)
        {
            // 移除程序集对应的ResponseType类型和OpCode信息
            if (_assemblyResponseTypes.TryGetValue(assemblyIdentity, out var removeResponseTypes))
            {
                foreach (var removeResponseType in removeResponseTypes)
                {
                    _responseTypes.Remove(removeResponseType);
                }

                _assemblyResponseTypes.RemoveByKey(assemblyIdentity);
            }

            if (_assemblyNetworkProtocols.TryGetValue(assemblyIdentity, out var removeNetworkProtocols))
            {
                foreach (var removeNetworkProtocol in removeNetworkProtocols)
                {
                    _networkProtocols.RemoveByKey(removeNetworkProtocol);
                }

                _assemblyNetworkProtocols.RemoveByKey(assemblyIdentity);
            }

            // 移除程序集对应的消息处理器信息
            if (_assemblyMessageHandlers.TryGetValue(assemblyIdentity, out var removeMessageHandlers))
            {
                foreach (var removeMessageHandler in removeMessageHandlers)
                {
                    _messageHandlers.Remove(removeMessageHandler.Type);
                }

                _assemblyMessageHandlers.RemoveByKey(assemblyIdentity);
            }

            // 如果编译符号FANTASY_NET存在，移除程序集对应的路由消息处理器信息
#if FANTASY_NET
            if (_assemblyRouteMessageHandlers.TryGetValue(assemblyIdentity, out var removeRouteMessageHandlers))
            {
                foreach (var removeRouteMessageHandler in removeRouteMessageHandlers)
                {
                    _routeMessageHandlers.Remove(removeRouteMessageHandler.Type);
                }

                _assemblyRouteMessageHandlers.RemoveByKey(assemblyIdentity);
            }

            if (_assemblyCustomRouteMap.TryGetValue(assemblyIdentity, out var removeCustomRouteMap))
            {
                foreach (var removeCustom in removeCustomRouteMap)
                {
                    _customRouteMap.Remove(removeCustom);
                }

                _assemblyCustomRouteMap.RemoveByKey(assemblyIdentity);
            }
#endif
        }
        
#if FANTASY_UNITY       
        /// <summary>
        /// 手动注册一个消息处理器。
        /// </summary>
        /// <param name="delegate"></param>
        /// <typeparam name="T"></typeparam>
        public void RegisterHandler<T>(MessageDelegate<T> @delegate) where T : IMessage
        {
            var type = typeof(T);

            if (!_messageDelegateHandlers.TryGetValue(type, out var messageDelegate))
            {
                messageDelegate = new MessageDelegateHandler<T>();
                _messageDelegateHandlers.Add(type,messageDelegate);
            }

            messageDelegate.Register(@delegate);
        }

        /// <summary>
        /// 手动卸载一个消息处理器，必须是通过RegisterHandler方法注册的消息处理器。
        /// </summary>
        /// <param name="delegate"></param>
        /// <typeparam name="T"></typeparam>
        public void UnRegisterHandler<T>(MessageDelegate<T> @delegate) where T : IMessage
        {
            var type = typeof(T);
            
            if (!_messageDelegateHandlers.TryGetValue(type, out var messageDelegate))
            {
                return;
            }

            if (messageDelegate.UnRegister(@delegate) != 0)
            {
                return;
            }
            
            _messageDelegateHandlers.Remove(type);
        }
#endif

        #endregion
        
        /// <summary>
        /// 处理普通消息，将消息分发给相应的消息处理器。
        /// </summary>
        /// <param name="session">会话对象</param>
        /// <param name="type">消息类型</param>
        /// <param name="message">消息对象</param>
        /// <param name="rpcId">RPC标识</param>
        /// <param name="protocolCode">协议码</param>
        public void MessageHandler(Session session, Type type, object message, uint rpcId, uint protocolCode)
        {
#if FANTASY_UNITY
            if(_messageDelegateHandlers.TryGetValue(type,out var messageDelegateHandler))
            {
                messageDelegateHandler.Handle(session, message);
                return;
            }
#endif
            if (!_messageHandlers.TryGetValue(type, out var messageHandler))
            {
                Log.Warning($"Scene:{session.Scene.Id} Found Unhandled Message: {message.GetType()}");
                return;
            }
            
            // 调用消息处理器的Handle方法并启动协程执行处理逻辑
            messageHandler.Handle(session, rpcId, protocolCode, message).Coroutine();
        }
        
        // 如果编译符号FANTASY_NET存在，定义处理路由消息的方法
#if FANTASY_NET
        /// <summary>
        /// 处理路由消息，将消息分发给相应的路由消息处理器。
        /// </summary>
        /// <param name="session">会话对象</param>
        /// <param name="type">消息类型</param>
        /// <param name="entity">实体对象</param>
        /// <param name="message">消息对象</param>
        /// <param name="rpcId">RPC标识</param>
        public async FTask RouteMessageHandler(Session session, Type type, Entity entity, object message, uint rpcId)
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

        internal bool GetCustomRouteType(long protocolCode,out int routeType)
        {
            return _customRouteMap.TryGetValue(protocolCode, out routeType);
        }
#endif
        internal void FailRouteResponse(Session session, Type requestType, uint error, uint rpcId)
        {
            var response = CreateRouteResponse(requestType, error);
            session.Send(response, rpcId);
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
        
        /// <summary>
        /// 根据消息类型获取对应的OpCode。
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <returns>消息对应的OpCode</returns>
        public uint GetOpCode(Type type)
        {
            return _networkProtocols.GetKeyByValue(type);
        }

        /// <summary>
        /// 根据OpCode获取对应的消息类型。
        /// </summary>
        /// <param name="code">OpCode</param>
        /// <returns>OpCode对应的消息类型</returns>
        public Type GetOpCodeType(uint code)
        {
            return _networkProtocols.GetValueByKey(code);
        }
    }
}