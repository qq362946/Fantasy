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
        
        protected override async FTask Handler(Session session, APackInfo packInfo)
        {
            try
            {
                switch (packInfo.ProtocolCode)
                {
                    case > OpCode.InnerRouteResponse:
                    {
                        throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode}");
                    }
                    case OpCode.PingResponse:
                    case > OpCode.OuterRouteResponse:
                    {
                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);
                        if (messageType == null)
                        {
                            throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }
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
                        var messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);
                        if (messageType == null)
                        {
                            throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                        }
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
            throw new NotSupportedException($"ClientMessageScheduler Received unsupported message protocolCode:{packInfo.ProtocolCode}");
        }
    }
#endif
#if FANTASY_NET
    public sealed class ClientMessageScheduler(Scene scene) : ANetworkMessageScheduler(scene)
    {
        protected override FTask Handler(Session session, APackInfo packInfo)
        {
            throw new NotSupportedException($"ClientMessageScheduler Received unsupported message protocolCode:{packInfo.ProtocolCode}");
        }
    }
#endif
}