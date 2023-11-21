using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

#pragma warning disable CS8603
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 网络会话的基类，用于管理网络通信。
    /// </summary>
    public class Session : Entity, INotSupportedPool, ISupportedMultiEntity
    {
        private uint _rpcId;
        /// <summary>
        /// 获取网络会话的唯一标识符。
        /// </summary>
        public long NetworkId { get; private set; }
        /// <summary>
        /// 获取通道的标识符。
        /// </summary>
        public uint ChannelId { get; private set; }
        /// <summary>
        /// 连接目标的终结点信息
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }
        /// <summary>
        /// 获取最后一次接收数据的时间。
        /// </summary>
        public long LastReceiveTime { get; private set; }
        /// <summary>
        /// 获取用于网络消息调度的实例。
        /// </summary>
        public ANetworkMessageScheduler NetworkMessageScheduler { get; private set;}
        /// <summary>
        /// 存储所有会话的字典。
        /// </summary>
        public static readonly Dictionary<long, Session> Sessions = new ();
        /// <summary>
        /// 存储请求回调的字典。
        /// </summary>
        public readonly Dictionary<long, FTask<IResponse>> RequestCallback = new();

        /// <summary>
        /// 创建一个会话并添加到会话字典中。
        /// </summary>
        /// <param name="networkMessageScheduler">用于网络消息调度的实例。</param>
        /// <param name="channel">与会话关联的通道。</param>
        /// <param name="networkTarget">网络目标。</param>
        public static void Create(ANetworkMessageScheduler networkMessageScheduler, ANetworkChannel channel, NetworkTarget networkTarget)
        {
#if FANTASY_DEVELOP
            // 检查是否在主线程中调用
            if (ThreadSynchronizationContext.Main.ThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new NotSupportedException("Session Create not in MainThread");
            }
#endif
            // 创建会话实例
            var session = Entity.Create<Session>(channel.Scene);
            session.ChannelId = channel.Id;
            session.NetworkId = channel.NetworkId;
            session.RemoteEndPoint = channel.RemoteEndPoint as IPEndPoint;
            session.NetworkMessageScheduler = networkMessageScheduler;
            // 关联事件处理
            channel.OnDispose += session.Dispose;
            channel.OnReceiveMemoryStream += session.OnReceive;
#if FANTASY_NET
            // 在外部网络目标下，添加会话空闲检查组件
            if (networkTarget == NetworkTarget.Outer)
            {
                var interval = Define.SessionIdleCheckerInterval;
                var timeOut = Define.SessionIdleCheckerTimeout;
                session.AddComponent<SessionIdleCheckerComponent>().Start(interval, timeOut);
            }
#endif
            // 将会话添加到会话字典中
            Sessions.Add(session.Id, session);
        }

        /// <summary>
        /// 创建一个与客户端网络相关的会话并添加到会话字典中。
        /// </summary>
        /// <param name="network">与会话关联的客户端网络。</param>
        /// <param name="remoteEndPoint">终结点信息</param>
        /// <returns>创建的会话实例。</returns>
        public static Session Create(AClientNetwork network, IPEndPoint remoteEndPoint)
        {
#if FANTASY_DEVELOP
            // 检查是否在主线程中调用
            if (ThreadSynchronizationContext.Main.ThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new NotSupportedException("Session Create not in MainThread");
            }
#endif
            // 创建会话实例
            var session = Entity.Create<Session>(network.Scene);
            session.ChannelId = network.ChannelId;
            session.NetworkId = network.Id;
            session.RemoteEndPoint = remoteEndPoint;
            session.NetworkMessageScheduler = network.NetworkMessageScheduler;
            // 关联事件处理
            network.OnDispose += session.Dispose;
            network.OnChangeChannelId += session.OnChangeChannelId;
            network.OnReceiveMemoryStream += session.OnReceive;
            // 将会话添加到会话字典中
            Sessions.Add(session.Id, session);
            return session;
        }
#if FANTASY_NET
        /// <summary>
        /// 创建一个与服务器网络相关的会话并添加到会话字典中。
        /// </summary>
        /// <param name="network">与会话关联的服务器网络。</param>
        /// <returns>创建的会话实例。</returns>
        public static ServerInnerSession Create(ANetwork network)
        {
#if FANTASY_DEVELOP
            // 检查是否在主线程中调用
            if (ThreadSynchronizationContext.Main.ThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new NotSupportedException("Session Create not in MainThread");
            }
#endif
            var session = Entity.Create<ServerInnerSession>(network.Scene);
            session.NetworkMessageScheduler = network.NetworkMessageScheduler;
            Sessions.Add(session.Id, session);
            return session;
        }

        /// <summary>
        /// 创建一个用于服务器内部会话的实例并添加到会话字典中。
        /// </summary>
        /// <param name="scene">关联的场景。</param>
        /// <returns>创建的会话实例。</returns>
        public static ServerInnerSession CreateServerInner(Scene scene)
        {
#if FANTASY_DEVELOP
            // 检查是否在主线程中调用
            if (ThreadSynchronizationContext.Main.ThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new NotSupportedException("Session Create not in MainThread");
            }
#endif
            var session = Entity.Create<ServerInnerSession>(scene, false);
            Sessions.Add(session.Id, session);
            return session;
        }
#endif

        /// <summary>
        /// 尝试从会话字典中获取指定标识符的会话实例。
        /// </summary>
        /// <param name="id">会话标识符。</param>
        /// <param name="session">输出参数，如果找到会话则返回会话实例，否则返回 null。</param>
        /// <returns>如果找到会话返回 true，否则返回 false。</returns>
        public static bool TryGet(long id, out Session session)
        {
            return Sessions.TryGetValue(id, out session);
        }

        /// <summary>
        /// 释放会话所持有的资源。
        /// </summary>
        public override void Dispose()
        {
#if FANTASY_DEVELOP
            // 检查是否在主线程中调用
            if (ThreadSynchronizationContext.Main.ThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new NotSupportedException("Session Create not in MainThread");
            }
#endif
            if (IsDisposed)
            {
                return;
            }
            
            var id = Id;
            var networkId = NetworkId;
            var channelId = ChannelId;

            // 终止所有等待中的请求回调
            foreach (var requestCallback in RequestCallback.Values.ToArray())
            {
                requestCallback.SetException(new Exception($"session is dispose: {Id}"));
            }

            if (networkId != 0 && channelId != 0)
            {
                // 通过网络线程在主线程上异步移除通道
                NetworkThread.Instance?.SynchronizationContext.Post(() =>
                {
                    NetworkThread.Instance?.RemoveChannel(networkId, channelId);
                });
            }

            NetworkId = 0;
            ChannelId = 0;
            base.Dispose();
            // 从会话字典中移除会话
            Sessions.Remove(id);
#if NETDEBUG
            Log.Debug($"Sessions Dispose Count:{Sessions.Count}");
#endif
        }

        /// <summary>
        /// 发送消息到会话。
        /// </summary>
        /// <param name="message">要发送的消息。</param>
        /// <param name="rpcId">RPC 标识符。</param>
        /// <param name="routeId">路由标识符。</param>
        public virtual void Send(object message, uint rpcId = 0, long routeId = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            NetworkThread.Instance.Send(NetworkId, ChannelId, rpcId, 0, routeId, message);
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

            NetworkThread.Instance.Send(NetworkId, ChannelId, rpcId, routeMessage.RouteTypeOpCode(), routeId, routeMessage);
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

            NetworkThread.Instance.SendStream(NetworkId, ChannelId, rpcId, routeTypeOpCode, routeId, memoryStream);
        }

        /// <summary>
        /// 调用请求并等待响应。
        /// </summary>
        /// <param name="request">要调用的请求。</param>
        /// <param name="routeId">路由标识符。</param>
        /// <returns>一个代表异步操作的任务，返回响应。</returns>
        public virtual FTask<IResponse> Call(IRequest request, long routeId = 0)
        {
            if (IsDisposed)
            {
                return null;
            }

            // 创建用于等待响应的任务
            var requestCallback = FTask<IResponse>.Create();
            
            unchecked
            {
                var rpcId = ++_rpcId; // 增加RPC标识符
                RequestCallback.Add(rpcId, requestCallback); // 将任务添加到回调字典中

                if (request is IRouteMessage iRouteMessage)
                {
                    // 如果请求是路由消息类型，则通过路由消息发送请求
                    Send(iRouteMessage, rpcId, routeId);
                }
                else
                {
                    // 否则通过普通消息发送请求
                    Send(request, rpcId, routeId);
                }
            }

            // 返回任务以等待响应
            return requestCallback;
        }

        private void OnReceive(APackInfo packInfo)
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

        private void OnChangeChannelId(uint channelId)
        {
            ChannelId = channelId;
        }
    }
}