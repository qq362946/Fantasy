using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy
{
    /// <summary>
    /// 提供了一个机制来调度和处理外部网络消息。
    /// </summary>
#if FANTASY_UNITY
    public sealed class OuterMessageScheduler : ANetworkMessageScheduler
    {
        public OuterMessageScheduler(Scene scene) : base(scene) { }
        
        /// <summary>
        /// 在Unity环境下，处理外部消息的方法。
        /// </summary>
        /// <param name="session">网络会话。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="packInfo">消息封包信息。</param>
        protected override FTask Handler(Session session, Type messageType, APackInfo packInfo)
        {
            throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
        }
    }
#endif
#if FANTASY_NET
    public sealed class OuterMessageScheduler : ANetworkMessageScheduler
    {
        public OuterMessageScheduler(Scene scene) : base(scene) { }
        
        /// <summary>
        /// 在FantasyNet环境下，处理外部消息的方法。
        /// </summary>
        /// <param name="session">网络会话。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="packInfo">消息封包信息。</param>
        protected override async FTask Handler(Session session, Type messageType, APackInfo packInfo)
        {
            if (packInfo.ProtocolCode >= OpCode.InnerRouteMessage)
            {
                throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
            }

            try
            {
                switch (packInfo.RouteTypeCode)
                {
                    case InnerRouteType.Route:
                    case InnerRouteType.BsonRoute:
                    {
                        break;
                    }
                    case InnerRouteType.Addressable:
                    {
                        var addressableRouteComponent = session.GetComponent<AddressableRouteComponent>();
                        
                        if (addressableRouteComponent == null)
                        {
                            Log.Error("Session does not have an AddressableRouteComponent component");
                            return;
                        }
                        
                        switch (packInfo.ProtocolCode)
                        {
                            case > OpCode.OuterRouteRequest:
                            {
                                var runtimeId = session.RunTimeId;
                                var response = await addressableRouteComponent.Call(packInfo.RouteTypeCode, messageType, packInfo.MemoryStream);
                                // session可能已经断开了，所以这里需要判断
                                if (session.RunTimeId == runtimeId)
                                {
                                    session.Send(response, packInfo.RpcId);
                                }
                                return;
                            }
                            case > OpCode.OuterRouteMessage:
                            {
                                await addressableRouteComponent.Send(packInfo.RouteTypeCode, messageType, packInfo.MemoryStream);
                                return;
                            }
                        }
                    
                        return;
                    }
                    case > InnerRouteType.CustomRouteType:
                    {
                        var routeComponent = session.GetComponent<RouteComponent>();
                        
                        if (routeComponent == null)
                        {
                            Log.Error("Session does not have an routeComponent component");
                            return;
                        }
                        
                        if (!routeComponent.TryGetRouteId(packInfo.RouteTypeCode, out var routeId))
                        {
                            Log.Error($"RouteComponent cannot find RouteId with RouteTypeCode {packInfo.RouteTypeCode}");
                            return;
                        }
                        
                        switch (packInfo.ProtocolCode)
                        {
                            case > OpCode.OuterRouteRequest:
                            {
                                var runtimeId = session.RunTimeId;
                                var response = await NetworkMessagingComponent.CallInnerRoute(routeId, packInfo.RouteTypeCode, messageType, packInfo.MemoryStream);
                                // session可能已经断开了，所以这里需要判断
                                if (session.RunTimeId == runtimeId)
                                {
                                    session.Send(response, packInfo.RpcId);
                                }
                                return;
                            }
                            case > OpCode.OuterRouteMessage:
                            {
                                NetworkMessagingComponent.SendInnerRoute(routeId, packInfo.RouteTypeCode, packInfo.MemoryStream);
                                return;
                            }
                        }
                    
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

            throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
        }

        public override FTask InnerScheduler(Session session, Type messageType, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, object message)
        {
            throw new NotImplementedException();
        }
    }
#endif
}