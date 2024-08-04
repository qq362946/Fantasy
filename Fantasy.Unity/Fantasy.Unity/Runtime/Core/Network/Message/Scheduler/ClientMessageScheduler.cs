using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 提供了一个用于客户端网络消息调度和处理的抽象基类。
    /// </summary>
#if FANTASY_UNITY
    public sealed class ClientMessageScheduler : ANetworkMessageScheduler
    {
        public ClientMessageScheduler(Scene scene) : base(scene) { }
        
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
                switch (packInfo.ProtocolCode)
                {
                    case > OpCode.InnerRouteResponse:
                    {
                        throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
                    }
                    case OpCode.PingResponse:
                    case > OpCode.OuterRouteResponse:
                    {
                        // 这个一般是客户端Session.Call发送时使用的、目前这个逻辑只有Unity客户端时使用
                        var aResponse = (IResponse)packInfo.Deserialize(messageType);

                        if (!session.RequestCallback.Remove(packInfo.RpcId, out var action))
                        {
                            Log.Error($"not found rpc {packInfo.RpcId}, response message: {aResponse.GetType().Name}");
                            return;
                        }

                        action.SetResult(aResponse);
                        return;
                    }
                    case < OpCode.OuterRouteRequest:
                    {
                        var message = packInfo.Deserialize(messageType);
                        MessageDispatcherComponent.MessageHandler(session, messageType, message, packInfo.RpcId, packInfo.ProtocolCode);
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
                packInfo.Dispose();
            }

            await FTask.CompletedTask;
            throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
        }
    }
#endif
#if FANTASY_NET
    public sealed class ClientMessageScheduler : ANetworkMessageScheduler
    {
        public ClientMessageScheduler(Scene scene) : base(scene) { }
        
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

        public override FTask InnerScheduler(Session session, Type messageType, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, object message)
        {
            throw new NotImplementedException();
        }
    }
#endif
}