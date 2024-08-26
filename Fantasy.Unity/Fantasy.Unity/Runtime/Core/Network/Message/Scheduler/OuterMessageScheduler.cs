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
        protected override FTask Handler(Session session, APackInfo packInfo)
        {
            throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode}");
        }
    }
#endif
#if FANTASY_NET
    public sealed class OuterMessageScheduler(Scene scene) : ANetworkMessageScheduler(scene)
    {
        protected override async FTask Handler(Session session, APackInfo packInfo)
        {
            var packInfoProtocolCode = packInfo.ProtocolCode;

            if (packInfo.ProtocolCode >= OpCode.InnerRouteMessage)
            {
                packInfo.Dispose();
                throw new NotSupportedException($"OuterMessageScheduler Received unsupported message protocolCode:{packInfoProtocolCode}");
            }

            switch (packInfo.RouteTypeCode)
            {
                case InnerRouteType.Route:
                case InnerRouteType.BsonRoute:
                {
                    break;
                }
                case InnerRouteType.Addressable:
                {
                    var messageType = MessageDispatcherComponent.GetOpCodeType(packInfoProtocolCode);

                    if (messageType == null)
                    {
                        packInfo.Dispose();
                        throw new Exception($"OuterMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfoProtocolCode}");
                    }

                    var addressableRouteComponent = session.GetComponent<AddressableRouteComponent>();

                    if (addressableRouteComponent == null)
                    {
                        packInfo.Dispose();
                        Log.Error("OuterMessageScheduler error session does not have an AddressableRouteComponent component");
                        return;
                    }

                    switch (packInfoProtocolCode)
                    {
                        case > OpCode.OuterRouteRequest:
                        {
                            using (packInfo)
                            {
                                var rpcId = packInfo.RpcId;
                                var runtimeId = session.RunTimeId;
                                var response = await addressableRouteComponent.Call(messageType, packInfo);
                                // session可能已经断开了，所以这里需要判断
                                if (session.RunTimeId == runtimeId)
                                {
                                    session.Send(response, rpcId);
                                }
                            }
                            return;
                        }
                        case > OpCode.OuterRouteMessage:
                        {
                            using (packInfo)
                            {
                                await addressableRouteComponent.Send(messageType, packInfo);
                            }
                            return;
                        }
                    }

                    return;
                }
                case > InnerRouteType.CustomRouteType:
                {
                    var messageType = MessageDispatcherComponent.GetOpCodeType(packInfoProtocolCode);

                    if (messageType == null)
                    {
                        packInfo.Dispose();
                        throw new Exception($"OuterMessageScheduler error 可能遭受到恶意发包或没有协议定义ProtocolCode ProtocolCode：{packInfoProtocolCode}");
                    }
                    
                    var routeComponent = session.GetComponent<RouteComponent>();

                    if (routeComponent == null)
                    {
                        packInfo.Dispose();
                        Log.Error("OuterMessageScheduler CustomRouteType session does not have an routeComponent component");
                        return;
                    }

                    if (!routeComponent.TryGetRouteId(packInfo.RouteTypeCode, out var routeId))
                    {
                        packInfo.Dispose();
                        Log.Error($"OuterMessageScheduler RouteComponent cannot find RouteId with RouteTypeCode {packInfo.RouteTypeCode}");
                        return;
                    }

                    switch (packInfo.ProtocolCode)
                    {
                        case > OpCode.OuterRouteRequest:
                        {
                            using (packInfo)
                            {
                                var rpcId = packInfo.RpcId;
                                var runtimeId = session.RunTimeId;
                                var response = await NetworkMessagingComponent.CallInnerRoute(routeId, messageType, packInfo);
                                // session可能已经断开了，所以这里需要判断
                                if (session.RunTimeId == runtimeId)
                                {
                                    session.Send(response, rpcId);
                                }
                            }

                            return;
                        }
                        case > OpCode.OuterRouteMessage:
                        {
                            using (packInfo)
                            {
                                await NetworkMessagingComponent.SendInnerRoute(routeId, messageType, packInfo);
                            }
                            
                            return;
                        }
                    }

                    return;
                }
            }

            packInfo.Dispose();
            throw new NotSupportedException($"OuterMessageScheduler Received unsupported message protocolCode:{packInfoProtocolCode}");
        }
    }
#endif
}