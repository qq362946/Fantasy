// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using Fantasy.PacketParser.Interface;
using Fantasy.Scheduler;
using Fantasy.Serialize;
#if FANTASY_NET
using Fantasy.Platform.Net;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#endif
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace Fantasy.Network
{
    /// <summary>
    /// 网络会话的基类，用于管理网络通信。
    /// </summary>
    public class Session : Entity, ISupportedMultiEntity
    {
        private uint _rpcId;
        internal long LastReceiveTime;
        /// <summary>
        /// 关联的网络连接通道
        /// </summary>
        public INetworkChannel Channel { get; private set; }
        /// <summary>
        /// 当前Session的终结点信息
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }
        private ANetworkMessageScheduler NetworkMessageScheduler { get; set;}
        public readonly Dictionary<long, FTask<IResponse>> RequestCallback = new();
        /// <summary>
        /// Session的Dispose委托
        /// </summary>
        public event Action OnDispose;
#if FANTASY_NET       
        internal static Session Create(ANetworkMessageScheduler networkMessageScheduler, ANetworkServerChannel channel, NetworkTarget networkTarget)
        {
            var session = Entity.Create<Session>(channel.Scene, false, true);
            session.Channel = channel;
            session.NetworkMessageScheduler = networkMessageScheduler;
            session.RemoteEndPoint = channel.RemoteEndPoint as IPEndPoint;
            session.OnDispose = channel.Dispose;
            session.LastReceiveTime = TimeHelper.Now;
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
        internal static Session Create(AClientNetwork network, IPEndPoint remoteEndPoint)
        {
            // 创建会话实例
            var session = Entity.Create<Session>(network.Scene, false, true);
            session.Channel = network;
            session.RemoteEndPoint = remoteEndPoint;
            session.OnDispose = network.Dispose;
            session.NetworkMessageScheduler = network.NetworkMessageScheduler;
            session.LastReceiveTime = TimeHelper.Now;
            return session;
        }
#if FANTASY_NET
        internal static ProcessSession CreateInnerSession(Scene scene)
        {
            var session = Entity.Create<ProcessSession>(scene, false, false);
            session.NetworkMessageScheduler = new InnerMessageScheduler(scene);
            return session;
        }

        /// <summary>
        /// 发送一个消息，框架内部使用建议不要用这个方法。
        /// </summary>
        /// <param name="rpcId">如果是RPC消息需要传递一个RPCId</param>
        /// <param name="routeId">routeId</param>
        /// <param name="messageType">消息的类型</param>
        /// <param name="packInfo">packInfo消息包</param>
        public virtual void Send(uint rpcId, long routeId, Type messageType, APackInfo packInfo)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, routeId, packInfo.MemoryStream, null);
        }

        /// <summary>
        /// 发送一个消息，框架内部使用建议不要用这个方法。
        /// </summary>
        /// <param name="packInfo">一个ProcessPackInfo消息包</param>
        /// <param name="rpcId">如果是RPC消息需要传递一个RPCId</param>
        /// <param name="routeId">routeId</param>
        public virtual void Send(ProcessPackInfo packInfo, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            using (packInfo)
            {
                Channel.Send(rpcId, routeId, packInfo.MemoryStream, null);
            }
        }

        /// <summary>
        /// 发送一个消息
        /// </summary>
        /// <param name="memoryStream">需要发送的MemoryStreamBuffer</param>
        /// <param name="rpcId">如果是RPC消息需要传递一个RPCId</param>
        /// <param name="routeId">routeId</param>
        public virtual void Send(MemoryStreamBuffer memoryStream, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, routeId, memoryStream, null);
        }
#endif
        /// <summary>
        /// 销毁一个Session，当执行了这个方法会自动断开网络的连接。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            _rpcId = 0;
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
        /// 发送一个消息
        /// </summary>
        /// <param name="message">消息的实例</param>
        /// <param name="rpcId">如果是RPC消息需要传递一个RPCId</param>
        /// <param name="routeId">routeId</param>
        public virtual void Send(IMessage message, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }
            
            Channel.Send(rpcId, routeId, null, message);
        }
        
        /// <summary>
        /// 发送一个消息
        /// </summary>
        /// <param name="routeMessage">消息的实例，不同的是这个是发送Route消息使用的</param>
        /// <param name="rpcId">如果是RPC消息需要传递一个RPCId</param>
        /// <param name="routeId">routeId</param>
        public virtual void Send(IRouteMessage routeMessage, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, routeId, null, routeMessage);
        }
        
        /// <summary>
        /// 发送一个RPC消息
        /// </summary>
        /// <param name="request">请求Route消息的实例</param>
        /// <param name="routeId">routeId</param>
        /// <returns></returns>
        public virtual FTask<IResponse> Call(IRouteRequest request, long routeId = 0)
        {
            if (IsDisposed)
            {
                return null;
            }
            
            var requestCallback = FTask<IResponse>.Create();
            var rpcId = ++_rpcId; 
            RequestCallback.Add(rpcId, requestCallback);
            Send(request, rpcId, routeId);
            return requestCallback;
        }
        
        /// <summary>
        /// 发送一个RPC消息
        /// </summary>
        /// <param name="request">请求消息的实例</param>
        /// <param name="routeId">routeId</param>
        /// <returns></returns>
        public virtual FTask<IResponse> Call(IRequest request, long routeId = 0)
        {
            if (IsDisposed)
            {
                return null;
            }
            
            var requestCallback = FTask<IResponse>.Create();
            var rpcId = ++_rpcId; 
            RequestCallback.Add(rpcId, requestCallback);
            Send(request, rpcId, routeId);
            return requestCallback;
        }

        internal void Receive(APackInfo packInfo)
        {
            if (IsDisposed)
            {
                return;
            }

            LastReceiveTime = TimeHelper.Now;

            try
            {
                NetworkMessageScheduler.Scheduler(this, packInfo);
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
