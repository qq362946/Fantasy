using System;
using Fantasy.IO;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 提供了一个机制来调度和处理外部网络消息。
    /// </summary>
#if FANTASY_UNITY
    public sealed class OuterMessageScheduler : ANetworkMessageScheduler
    {
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

        /// <summary>
        /// 在Unity环境下，处理内部消息的方法。
        /// </summary>
        /// <param name="session">网络会话。</param>
        /// <param name="rpcId">RPC请求ID。</param>
        /// <param name="routeId">消息路由ID。</param>
        /// <param name="protocolCode">协议码。</param>
        /// <param name="routeTypeCode">路由类型码。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="message">消息对象。</param>
        protected override FTask InnerHandler(Session session, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, Type messageType, object message)
        {
            throw new NotImplementedException();
        }
    }
#endif
#if FANTASY_NET
    public sealed class OuterMessageScheduler : ANetworkMessageScheduler
    {
        /// <summary>
        /// 在FantasyNet环境下，处理外部消息的方法。
        /// </summary>
        /// <param name="session">网络会话。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="packInfo">消息封包信息。</param>
        protected override async FTask Handler(Session session, Type messageType, APackInfo packInfo)
        {
            if (packInfo.ProtocolCode >= Opcode.InnerRouteMessage)
            {
                throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
            }

            try
            {
                DisposePackInfo = false;
                switch (packInfo.RouteTypeCode)
                {
                    case CoreRouteType.Route:
                    case CoreRouteType.BsonRoute:
                    {
                        break;
                    }
                    case CoreRouteType.Addressable:
                    {
                        var addressableRouteComponent = session.GetComponent<AddressableRouteComponent>();

                        if (addressableRouteComponent == null)
                        {
                            Log.Error("Session does not have an AddressableRouteComponent component");
                            return;
                        }

                        switch (packInfo.ProtocolCode)
                        {
                            case > Opcode.OuterRouteRequest:
                            {
                                var runtimeId = session.RuntimeId;
                                var response = await addressableRouteComponent.Call(packInfo.RouteTypeCode, messageType, packInfo.CreateMemoryStream());
                                // session可能已经断开了，所以这里需要判断
                                if (session.RuntimeId == runtimeId)
                                {
                                    session.Send(response, packInfo.RpcId);
                                }

                                return;
                            }
                            case > Opcode.OuterRouteMessage:
                            {
                                addressableRouteComponent.Send(packInfo.RouteTypeCode, messageType, packInfo.CreateMemoryStream());
                                return;
                            }
                        }

                        return;
                    }
                    case > CoreRouteType.CustomRouteType:
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
                            case > Opcode.OuterRouteRequest:
                            {
                                var runtimeId = session.RuntimeId;
                                var response = await MessageHelper.CallInnerRoute(session.Scene, routeId, packInfo.RouteTypeCode, messageType, packInfo.CreateMemoryStream());
                                // session可能已经断开了，所以这里需要判断
                                if (session.RuntimeId == runtimeId)
                                {
                                    session.Send(response, packInfo.RpcId);
                                }

                                return;
                            }
                            case > Opcode.OuterRouteMessage:
                            {
                                MessageHelper.SendInnerRoute(session.Scene, routeId, packInfo.RouteTypeCode, packInfo.CreateMemoryStream());
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
                NetworkThread.Instance.SynchronizationContext.Post(packInfo.Dispose);
            }

            throw new NotSupportedException($"Received unsupported message protocolCode:{packInfo.ProtocolCode} messageType:{messageType}");
        }

        /// <summary>
        /// 在FantasyNet环境下，处理内部消息的方法。
        /// </summary>
        /// <param name="session">网络会话。</param>
        /// <param name="rpcId">RPC请求ID。</param>
        /// <param name="routeId">消息路由ID。</param>
        /// <param name="protocolCode">协议码。</param>
        /// <param name="routeTypeCode">路由类型码。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="message">消息对象。</param>
        protected override FTask InnerHandler(Session session, uint rpcId, long routeId, uint protocolCode, long routeTypeCode, Type messageType, object message)
        {
            throw new NotSupportedException($"OuterMessageScheduler NotSupported InnerHandler");
        }
    }
#endif
}