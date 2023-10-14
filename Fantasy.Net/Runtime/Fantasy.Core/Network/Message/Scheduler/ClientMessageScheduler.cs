using System;

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 提供了一个用于客户端网络消息调度和处理的抽象基类。
    /// </summary>
#if FANTASY_UNITY
    
    public sealed class ClientMessageScheduler : ANetworkMessageScheduler
    {
        /// <summary>
        /// 处理客户端外部消息的方法。
        /// </summary>
        /// <param name="session">客户端会话。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="packInfo">消息包信息。</param>
        protected override async FTask Handler(Session session, Type messageType, APackInfo packInfo)
        {
            try
            {
                DisposePackInfo = false;
                switch (packInfo.ProtocolCode)
                {
                    case > Opcode.InnerRouteResponse:
                    {
                        throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
                    }
                    case Opcode.PingResponse:
                    case > Opcode.OuterRouteResponse:
                    {
                        // 这个一般是客户端Session.Call发送时使用的、目前这个逻辑只有Unity客户端时使用
                        var aResponse = (IResponse)packInfo.Deserialize(messageType);

                        if (!session.RequestCallback.TryGetValue(packInfo.RpcId, out var action))
                        {
                            Log.Error($"not found rpc {packInfo.RpcId}, response message: {aResponse.GetType().Name}");
                            return;
                        }

                        session.RequestCallback.Remove(packInfo.RpcId);
                        action.SetResult(aResponse);
                        return;
                    }
                    case < Opcode.OuterRouteRequest:
                    {
                        var message = packInfo.Deserialize(messageType);
                        MessageDispatcherSystem.Instance.MessageHandler(session, messageType, message, packInfo.RpcId, packInfo.ProtocolCode);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                return;
            }
            finally
            {
                NetworkThread.Instance.SynchronizationContext.Post(packInfo.Dispose);
            }

            await FTask.CompletedTask;
            throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
        }

        protected override FTask InnerHandler(Session session, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, Type messageType, object message)
        {
            throw new NotImplementedException();
        }
    }
#endif
#if FANTASY_NET
    public sealed class ClientMessageScheduler : ANetworkMessageScheduler
    {
        /// <summary>
        /// 处理客户端外部消息的方法。
        /// </summary>
        /// <param name="session">客户端会话。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="packInfo">消息包信息。</param>
        protected override FTask Handler(Session session, Type messageType, APackInfo packInfo)
        {
            throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
        }

        /// <summary>
        /// 处理客户端内部消息的方法。
        /// </summary>
        /// <param name="session">客户端会话。</param>
        /// <param name="rpcId">远程过程调用ID。</param>
        /// <param name="routeId">路由ID。</param>
        /// <param name="protocolCode">协议编码。</param>
        /// <param name="routeTypeCode">路由类型编码。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="message">消息实例。</param>
        protected override FTask InnerHandler(Session session, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, Type messageType, object message)
        {
            throw new NotSupportedException($"Received unsupported message protocolCode:{protocolCode} messageType:{messageType}");
        }
    }
#endif
}