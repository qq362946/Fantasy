using System;
using System.IO;


namespace Fantasy
{
    /// <summary>
    /// 抽象网络消息调度器基类，用于处理网络消息的调度和处理逻辑。
    /// </summary>
    public abstract class ANetworkMessageScheduler
    {
        /// <summary>
        /// 用于回复Ping消息的响应实例。
        /// </summary>
        private readonly PingResponse _pingResponse = new PingResponse();

        /// <summary>
        /// 调度网络消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="packInfo">消息包信息。</param>
        /// <returns>异步任务。</returns>
        public async FTask Scheduler(Session session, APackInfo packInfo)
        {
            var disposePackInfo = true;
            Type messageType = null;

            try
            {
                if (session.IsDisposed)
                {
                    return;
                }

                if (packInfo.ProtocolCode == Opcode.PingRequest)
                {
                    _pingResponse.Now = TimeHelper.Now;
                    session.Send(_pingResponse, packInfo.RpcId);
                    return;
                }

                messageType = MessageDispatcherSystem.Instance.GetOpCodeType(packInfo.ProtocolCode);

                if (messageType == null)
                {
                    throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                }

                switch (packInfo.ProtocolCode)
                {
                    case Opcode.PingResponse:
                    case >= Opcode.OuterRouteMessage:
                    {
                        disposePackInfo = false;
                        await Handler(session, messageType, packInfo);
                        return;
                    }
                    case < Opcode.OuterResponse:
                    {
                        var message = packInfo.Deserialize(messageType);
                        MessageDispatcherSystem.Instance.MessageHandler(session, messageType, message, packInfo.RpcId, packInfo.ProtocolCode);
                        return;
                    }
                    default:
                    {
                        var aResponse = (IResponse)packInfo.Deserialize(messageType);
#if FANTASY_NET
                        // 服务器之间发送消息因为走的是MessageHelper、所以接收消息的回调也应该放到MessageHelper里处理
                        MessageHelper.ResponseHandler(packInfo.RpcId, aResponse);
#else
                        // 这个一般是客户端Session.Call发送时使用的、目前这个逻辑只有Unity客户端时使用
                        
                        if (!session.RequestCallback.TryGetValue(packInfo.RpcId, out var action))
                        {
                            Log.Error($"not found rpc {packInfo.RpcId}, response message: {aResponse.GetType().Name}");
                            return;
                        }
                        
                        session.RequestCallback.Remove(packInfo.RpcId);
                        action.SetResult(aResponse);
#endif
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"NetworkMessageScheduler error messageProtocolCode:{packInfo.ProtocolCode} messageType:{messageType} SessionId {session.Id} IsDispose {session.IsDisposed} {e}");
            }
            finally
            {
                if (disposePackInfo)
                {
                    NetworkThread.Instance.SynchronizationContext.Post(packInfo.Dispose);
                }
            }
        }

        /// <summary>
        /// 内部调度网络消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeId">路由标识。</param>
        /// <param name="protocolCode">协议代码。</param>
        /// <param name="routeTypeCode">路由类型代码。</param>
        /// <param name="message">要处理的消息对象。</param>
        /// <returns>异步任务。</returns>
        public async FTask InnerScheduler(Session session, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, object message)
        {
            var messageType = message.GetType();
            
            try
            {
                if (session.IsDisposed)
                {
                    return;
                }

                switch (protocolCode)
                {
                    case >= Opcode.OuterRouteMessage:
                    {
                        await InnerHandler(session, rpcId, routeId, protocolCode, routeTypeCode, messageType, message);
                        return;
                    }
                    case < Opcode.OuterResponse:
                    {
                        MessageDispatcherSystem.Instance.MessageHandler(session, messageType, message, rpcId, protocolCode);
                        return;
                    }
                    default:
                    {
#if FANTASY_NET
                        // 服务器之间发送消息因为走的是MessageHelper、所以接收消息的回调也应该放到MessageHelper里处理
                        MessageHelper.ResponseHandler(rpcId, (IResponse)message);
#endif
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"NetworkMessageScheduler error messageProtocolCode:{protocolCode} messageType:{messageType} SessionId {session.Id} IsDispose {session.IsDisposed} {e}");
            }
        }

        /// <summary>
        /// 处理外部网络消息的抽象方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="packInfo">消息包信息。</param>
        /// <returns>异步任务。</returns>
        protected abstract FTask Handler(Session session, Type messageType, APackInfo packInfo);
        /// <summary>
        /// 处理内部网络消息的抽象方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeId">路由标识。</param>
        /// <param name="protocolCode">协议代码。</param>
        /// <param name="routeTypeCode">路由类型代码。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="message">要处理的消息对象。</param>
        /// <returns>异步任务。</returns>
        protected abstract FTask InnerHandler(Session session, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, Type messageType, object message);
    }
}