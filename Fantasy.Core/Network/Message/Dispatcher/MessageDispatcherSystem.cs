using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fantasy.DataStructure;
using Fantasy.Helper;
using Type = System.Type;


// ReSharper disable PossibleNullReferenceException

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 用于存储消息处理器的信息，包括类型和对象实例。
    /// </summary>
    /// <typeparam name="T">消息处理器的类型</typeparam>
    public sealed class HandlerInfo<T>
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
    /// 消息分发系统，负责管理消息和消息处理器之间的关系。
    /// </summary>
    public sealed class MessageDispatcherSystem : Singleton<MessageDispatcherSystem>
    {
        // 存储消息类型与响应类型之间的映射关系
        private readonly Dictionary<Type, Type> _responseTypes = new Dictionary<Type, Type>();
        // 存储协议码与消息类型之间的双向映射关系
        private readonly DoubleMapDictionary<uint, Type> _networkProtocols = new DoubleMapDictionary<uint, Type>();
        // 存储消息类型与消息处理器之间的映射关系
        private readonly Dictionary<Type, IMessageHandler> _messageHandlers = new Dictionary<Type, IMessageHandler>();

        // 存储程序集名与响应类型之间的一对多关系
        private readonly OneToManyList<int, Type> _assemblyResponseTypes = new OneToManyList<int, Type>();
        // 存储程序集名与协议码之间的一对多关系
        private readonly OneToManyList<int, uint> _assemblyNetworkProtocols = new OneToManyList<int, uint>();
        // 存储程序集名与消息处理器信息之间的一对多关系
        private readonly OneToManyList<int, HandlerInfo<IMessageHandler>> _assemblyMessageHandlers = new OneToManyList<int, HandlerInfo<IMessageHandler>>();

#if FANTASY_NET
        // 存储消息类型与路由消息处理器之间的映射关系
        private readonly Dictionary<Type, IRouteMessageHandler> _routeMessageHandlers = new Dictionary<Type, IRouteMessageHandler>();
        // 存储程序集名与路由消息处理器信息之间的一对多关系
        private readonly OneToManyList<int, HandlerInfo<IRouteMessageHandler>> _assemblyRouteMessageHandlers= new OneToManyList<int, HandlerInfo<IRouteMessageHandler>>();
#endif
        // 用于同步接收路由消息的锁
        private static readonly CoroutineLockQueueType ReceiveRouteMessageLock = new CoroutineLockQueueType("ReceiveRouteMessageLock");

        /// <summary>
        /// 在加载程序集时，用于解析并存储消息处理相关的信息。
        /// </summary>
        /// <param name="assemblyName">要加载的程序集名称</param>
        protected override void OnLoad(int assemblyName)
        {
            // 遍历所有实现了IMessage接口的类型，获取OpCode并添加到_networkProtocols字典中
            foreach (var type in AssemblyManager.ForEach(assemblyName, typeof(IMessage)))
            {
                var obj = (IMessage) Activator.CreateInstance(type);
                var opCode = obj.OpCode();
                
                _networkProtocols.Add(opCode, type);

                var responseType = type.GetProperty("ResponseType");

                // 如果类型具有ResponseType属性，将其添加到_responseTypes字典中
                if (responseType != null)
                {
                    _responseTypes.Add(type, responseType.PropertyType);
                    _assemblyResponseTypes.Add(assemblyName, type);
                }

                _assemblyNetworkProtocols.Add(assemblyName, opCode);
            }

            // 遍历所有实现了IMessageHandler接口的类型，创建实例并添加到_messageHandlers字典中
            foreach (var type in AssemblyManager.ForEach(assemblyName, typeof(IMessageHandler)))
            {
                var obj = (IMessageHandler) Activator.CreateInstance(type);

                if (obj == null)
                {
                    throw new Exception($"message handle {type.Name} is null");
                }

                var key = obj.Type();
                _messageHandlers.Add(key, obj);
                _assemblyMessageHandlers.Add(assemblyName, new HandlerInfo<IMessageHandler>()
                {
                    Obj = obj, Type = key
                });
            }

            // 如果编译符号FANTASY_NET存在，遍历所有实现了IRouteMessageHandler接口的类型，创建实例并添加到_routeMessageHandlers字典中
#if FANTASY_NET
            foreach (var type in AssemblyManager.ForEach(assemblyName, typeof(IRouteMessageHandler)))
            {
                var obj = (IRouteMessageHandler) Activator.CreateInstance(type);

                if (obj == null)
                {
                    throw new Exception($"message handle {type.Name} is null");
                }

                var key = obj.Type();
                _routeMessageHandlers.Add(key, obj);
                _assemblyRouteMessageHandlers.Add(assemblyName, new HandlerInfo<IRouteMessageHandler>()
                {
                    Obj = obj, Type = key
                });
            }
#endif
        }

        /// <summary>
        /// 在卸载程序集时，清理相关的消息处理信息。
        /// </summary>
        /// <param name="assemblyName">要卸载的程序集名称</param>
        protected override void OnUnLoad(int assemblyName)
        {
            // 移除程序集对应的ResponseType类型和OpCode信息
            if (_assemblyResponseTypes.TryGetValue(assemblyName, out var removeResponseTypes))
            {
                foreach (var removeResponseType in removeResponseTypes)
                {
                    _responseTypes.Remove(removeResponseType);
                }
                
                _assemblyResponseTypes.RemoveByKey(assemblyName);
            }

            if (_assemblyNetworkProtocols.TryGetValue(assemblyName, out var removeNetworkProtocols))
            {
                foreach (var removeNetworkProtocol in removeNetworkProtocols)
                {
                    _networkProtocols.RemoveByKey(removeNetworkProtocol);
                }

                _assemblyNetworkProtocols.RemoveByKey(assemblyName);
            }

            // 移除程序集对应的消息处理器信息
            if (_assemblyMessageHandlers.TryGetValue(assemblyName, out var removeMessageHandlers))
            {
                foreach (var removeMessageHandler in removeMessageHandlers)
                {
                    _messageHandlers.Remove(removeMessageHandler.Type);
                }
               
                _assemblyMessageHandlers.Remove(assemblyName);
            }

            // 如果编译符号FANTASY_NET存在，移除程序集对应的路由消息处理器信息
#if FANTASY_NET
            if (_assemblyRouteMessageHandlers.TryGetValue(assemblyName, out var removeRouteMessageHandlers))
            {
                foreach (var removeRouteMessageHandler in removeRouteMessageHandlers)
                {
                    _routeMessageHandlers.Remove(removeRouteMessageHandler.Type);
                }
                
                _assemblyRouteMessageHandlers.Remove(assemblyName);
            }
#endif
        }

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
                    FailResponse(session, request, CoreErrorCode.Error_NotFindEntity, rpcId);
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
            using (await ReceiveRouteMessageLock.Lock(runtimeId))
            {
                if (sessionRuntimeId != session.RuntimeId)
                {
                    return;
                }
                
                if (runtimeId != entity.RuntimeId)
                {
                    if (message is IRouteRequest request)
                    {
                        FailResponse(session, request, CoreErrorCode.Error_NotFindEntity, rpcId);
                    }
                
                    return;
                }
                
                await routeMessageHandler.Handle(session, entity, rpcId, message);
            }
        }
#endif
        /// <summary>
        /// 处理失败时，向会话发送失败响应消息。
        /// </summary>
        /// <param name="session">会话对象</param>
        /// <param name="iRouteRequest">路由请求对象</param>
        /// <param name="error">错误码</param>
        /// <param name="rpcId">RPC标识</param>
        public void FailResponse(Session session, IRouteRequest iRouteRequest, uint error, uint rpcId)
        {
            var response = CreateResponse(iRouteRequest, error);
            session.Send(response, rpcId);
        }

        /// <summary>
        /// 创建一个空的路由响应消息。
        /// </summary>
        /// <returns>创建的路由响应消息</returns>
        public IRouteResponse CreateRouteResponse()
        {
            return new RouteResponse();
        }

        /// <summary>
        /// 根据请求类型和错误码，创建普通响应消息。
        /// </summary>
        /// <param name="requestType">请求类型</param>
        /// <param name="error">错误码</param>
        /// <returns>创建的普通响应消息</returns>
        public IResponse CreateResponse(Type requestType, uint error)
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

        /// <summary>
        /// 根据请求对象和错误码，创建普通响应消息。
        /// </summary>
        /// <param name="iRequest">请求对象</param>
        /// <param name="error">错误码</param>
        /// <returns>创建的普通响应消息</returns>
        public IResponse CreateResponse(IRequest iRequest, uint error)
        {
            IResponse response;

            if (_responseTypes.TryGetValue(iRequest.GetType(), out var responseType))
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

        /// <summary>
        /// 根据路由请求对象和错误码，创建路由响应消息。
        /// </summary>
        /// <param name="iRouteRequest">路由请求对象</param>
        /// <param name="error">错误码</param>
        /// <returns>创建的路由响应消息</returns>
        public IRouteResponse CreateResponse(IRouteRequest iRouteRequest, uint error)
        {
            IRouteResponse response;

            if (_responseTypes.TryGetValue(iRouteRequest.GetType(), out var responseType))
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