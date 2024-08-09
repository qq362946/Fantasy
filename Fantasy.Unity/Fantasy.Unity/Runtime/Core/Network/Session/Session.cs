// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 网络会话的基类，用于管理网络通信。
    /// </summary>
    public class Session : Entity, ISupportedMultiEntity
    {
        internal uint RpcId;
        
        /// <summary>
        /// 获取最后一次接收数据的时间。
        /// </summary>
        internal long LastReceiveTime;
        /// <summary>
        /// 获取或设置网络通道。
        /// </summary>
        public INetworkChannel Channel { get; private set; }
        /// <summary>
        /// 连接目标的终结点信息。
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }
        /// <summary>
        /// 获取用于网络消息调度的实例。
        /// </summary>
        public ANetworkMessageScheduler NetworkMessageScheduler { get; private set;}
        /// <summary>
        /// 存储请求回调的字典。
        /// </summary>
        public readonly Dictionary<long, FTask<IResponse>> RequestCallback = new();
        /// <summary>
        /// 在网络连接释放时触发的事件。
        /// </summary>
        public event Action OnDispose;
#if FANTASY_NET       
        public static Session Create(ANetworkMessageScheduler networkMessageScheduler, ANetworkServerChannel channel, NetworkTarget networkTarget)
        {
            var session = Entity.Create<Session>(channel.Scene, false, false);
            session.Channel = channel;
            session.NetworkMessageScheduler = networkMessageScheduler;
            session.RemoteEndPoint = channel.RemoteEndPoint as IPEndPoint;
            session.OnDispose = channel.Dispose;

            // 在外部网络目标下，添加会话空闲检查组件
            if (networkTarget == NetworkTarget.Outer)
            {
                var interval = ProcessDefine.SessionIdleCheckerInterval;
                var timeOut = ProcessDefine.SessionIdleCheckerTimeout;
                session.AddComponent<SessionIdleCheckerComponent>().Start(interval, timeOut);
            }
            return session;
        }
#endif
        /// <summary>
        /// 创建一个与客户端网络相关的会话并添加到会话字典中。
        /// </summary>
        /// <param name="network">与会话关联的客户端网络。</param>
        /// <param name="remoteEndPoint">终结点信息</param>
        /// <returns>创建的会话实例。</returns>
        public static Session Create(AClientNetwork network, IPEndPoint remoteEndPoint)
        {
            // 创建会话实例
            var session = Entity.Create<Session>(network.Scene, false, false);
            session.Channel = network;
            session.RemoteEndPoint = remoteEndPoint;
            session.OnDispose = network.Dispose;
            session.NetworkMessageScheduler = network.NetworkMessageScheduler;
            return session;
        }
#if FANTASY_NET
        public static ServerInnerSession CreateInnerSession(Scene scene)
        {
            var session = Entity.Create<ServerInnerSession>(scene, false, false);
            session.NetworkMessageScheduler = new InnerMessageScheduler(scene);
            return session;
        }
#endif

        /// <summary>
        /// 释放会话所持有的资源。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            RpcId = 0;
            LastReceiveTime = 0;
            Channel = null;
            RemoteEndPoint = null;
            NetworkMessageScheduler = null;
            base.Dispose();

            // 终止所有等待中的请求回调
            foreach (var requestCallback in RequestCallback.Values.ToArray())
            {
                requestCallback.SetException(new Exception($"session is dispose: {Id}"));
            }
            
            RequestCallback.Clear();
            OnDispose?.Invoke();
        }

        /// <summary>
        /// 发送消息到会话。
        /// </summary>
        /// <param name="message">要发送的消息。</param>
        /// <param name="rpcId">RPC 标识符。</param>
        /// <param name="routeId">路由标识符。</param>
        public virtual void Send(IMessage message, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }
            
            Channel.Send(rpcId, 0, routeId, null, message);
        }

        /// <summary>
        /// 发送路由消息到会话。
        /// </summary>
        /// <param name="routeMessage">要发送的路由消息。</param>
        /// <param name="rpcId">RPC 标识符。</param>
        /// <param name="routeId">路由标识符。</param>
        public virtual void Send(IRouteMessage routeMessage, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, routeMessage.RouteTypeOpCode(), routeId, null, routeMessage);
        }

        /// <summary>
        /// 发送内存流到会话。
        /// </summary>
        /// <param name="memoryStream">要发送的内存流。</param>
        /// <param name="rpcId">RPC 标识符。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="routeId">路由标识符。</param>
        public virtual void Send(MemoryStream memoryStream, uint rpcId = 0, long routeTypeOpCode = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, routeTypeOpCode, routeId, memoryStream, null);
        }

        /// <summary>
        /// 调用请求并等待响应。
        /// </summary>
        /// <param name="request"></param>
        /// <param name="routeId"></param>
        /// <returns></returns>
        public virtual FTask<IResponse> Call(IRouteRequest request, long routeId = 0)
        {
            if (IsDisposed)
            {
                return null;
            }
            
            var requestCallback = FTask<IResponse>.Create();
            var rpcId = ++RpcId; 
            RequestCallback.Add(rpcId, requestCallback);
            Send(request, rpcId, routeId);
            return requestCallback;
        }

        /// <summary>
        /// 调用请求并等待响应。
        /// </summary>
        /// <param name="request"></param>
        /// <param name="routeId"></param>
        /// <returns></returns>
        public virtual FTask<IResponse> Call(IRequest request, long routeId = 0)
        {
            if (IsDisposed)
            {
                return null;
            }
            
            var requestCallback = FTask<IResponse>.Create();
            var rpcId = ++RpcId; 
            RequestCallback.Add(rpcId, requestCallback);
            Send(request, rpcId, routeId);
            return requestCallback;
        }
        
        /// <summary>
        /// 接收到网络流数据。
        /// </summary>
        /// <param name="packInfo"></param>
        public void Receive(APackInfo packInfo)
        {
            if (IsDisposed)
            {
                return;
            }

            LastReceiveTime = TimeHelper.Now;

            try
            {
                NetworkMessageScheduler.Scheduler(this, packInfo).Coroutine();
            }
            catch (Exception e)
            {
                // 如果解析失败，只有一种可能，那就是有人恶意发包。
                // 所以这里强制关闭了当前连接。不让对方一直发包。
                Dispose();
                Log.Error(e);
            }
        }
    }
}
