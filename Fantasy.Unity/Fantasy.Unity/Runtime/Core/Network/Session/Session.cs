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
        private uint _rpcId;
        internal long LastReceiveTime;
        public INetworkChannel Channel { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }
        public ANetworkMessageScheduler NetworkMessageScheduler { get; private set;}
        public readonly Dictionary<long, FTask<IResponse>> RequestCallback = new();
        public event Action OnDispose;
#if FANTASY_NET       
        public static Session Create(ANetworkMessageScheduler networkMessageScheduler, ANetworkServerChannel channel, NetworkTarget networkTarget)
        {
            var session = Entity.Create<Session>(channel.Scene, false, false);
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
        public static Session Create(AClientNetwork network, IPEndPoint remoteEndPoint)
        {
            // 创建会话实例
            var session = Entity.Create<Session>(network.Scene, false, false);
            session.Channel = network;
            session.RemoteEndPoint = remoteEndPoint;
            session.OnDispose = network.Dispose;
            session.NetworkMessageScheduler = network.NetworkMessageScheduler;
            session.LastReceiveTime = TimeHelper.Now;
            return session;
        }
#if FANTASY_NET
        public static ProcessSession CreateInnerSession(Scene scene)
        {
            var session = Entity.Create<ProcessSession>(scene, false, false);
            session.NetworkMessageScheduler = new InnerMessageScheduler(scene);
            return session;
        }

        public virtual async FTask Send(uint rpcId, long routeId, Type messageType, APackInfo packInfo)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, packInfo.RouteTypeCode, routeId, packInfo.MemoryStream, null);
            await FTask.CompletedTask;
        }

        public virtual async FTask Send(ProcessPackInfo packInfo, uint rpcId = 0, long routeTypeOpCode = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            using (packInfo)
            {
                Channel.Send(rpcId, routeTypeOpCode, routeId, packInfo.MemoryStream, null);
            }
            
            await FTask.CompletedTask;
        }

        public virtual void Send(MemoryStreamBuffer memoryStream, uint rpcId = 0, long routeTypeOpCode = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, routeTypeOpCode, routeId, memoryStream, null);
        }
#endif
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
        
        public virtual void Send(IMessage message, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }
            
            Channel.Send(rpcId, 0, routeId, null, message);
        }
        
        public virtual void Send(IRouteMessage routeMessage, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            Channel.Send(rpcId, routeMessage.RouteTypeOpCode(), routeId, null, routeMessage);
        }
        
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
