using System;
using System.IO;
// ReSharper disable UnassignedField.Global
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Fantasy
{
    public abstract class ANetworkMessageScheduler
    {
        protected readonly Scene Scene;
#if FANTASY_NET
        protected readonly Process Process;
#endif
        protected readonly MessageDispatcherComponent MessageDispatcherComponent;
        protected readonly NetworkMessagingComponent NetworkMessagingComponent;
       
        private readonly PingResponse _pingResponse = new PingResponse();

        protected ANetworkMessageScheduler(Scene scene)
        {
            Scene = scene;
#if FANTASY_NET
            Process = scene.Process;
#endif
            MessageDispatcherComponent = scene.MessageDispatcherComponent;
            NetworkMessagingComponent = scene.NetworkMessagingComponent;
        }

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

                if (packInfo.ProtocolCode == OpCode.PingRequest)
                {
                    _pingResponse.Now = TimeHelper.Now;
                    session.LastReceiveTime = _pingResponse.Now;
                    session.Send(_pingResponse, packInfo.RpcId);
                    return;
                }

                messageType = MessageDispatcherComponent.GetOpCodeType(packInfo.ProtocolCode);

                if (messageType == null)
                {
                    throw new Exception($"可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfo.ProtocolCode}");
                }

                switch (packInfo.ProtocolCode)
                {
                    case OpCode.PingResponse:
                    case >= OpCode.OuterRouteMessage:
                    {
                        disposePackInfo = false;
                        await Handler(session, messageType, packInfo);
                        return;
                    }
                    case < OpCode.OuterResponse:
                    {
                        var message = packInfo.Deserialize(messageType);
                        MessageDispatcherComponent.MessageHandler(session, messageType, message, packInfo.RpcId, packInfo.ProtocolCode);
                        return;
                    }
                    default:
                    {
                        var aResponse = (IResponse)packInfo.Deserialize(messageType);
#if FANTASY_NET
                        // 服务器之间发送消息因为走的是MessageHelper、所以接收消息的回调也应该放到MessageHelper里处理
                        
                        NetworkMessagingComponent.ResponseHandler(packInfo.RpcId, aResponse);
#else
                        // 这个一般是客户端Session.Call发送时使用的、目前这个逻辑目前只有客户端时使用
                        
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
                    packInfo.Dispose();
                }
            }
        }

        protected abstract FTask Handler(Session session, Type messageType, APackInfo packInfo);
#if FANTASY_NET
        public abstract FTask InnerScheduler(Session session, Type messageType, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, object message);
#endif
    }
}