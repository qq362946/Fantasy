using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fantasy.Helper;
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 网络线程管理器的单例类。负责处理网络相关操作、通信以及线程管理。
    /// </summary>
    public sealed class NetworkThread : Singleton<NetworkThread>
    {
        #region 逻辑线程
        
        private Thread _netWorkThread;
        /// <summary>
        /// 线程同步上下文对象，用于在多线程环境中进行线程间的同步和通信。
        /// 在网络线程中，通过设置和使用该上下文对象，可以实现线程安全的操作和数据传递。
        /// </summary>
        public ThreadSynchronizationContext SynchronizationContext;
        private readonly ConcurrentQueue<NetAction> _actions = new ConcurrentQueue<NetAction>();

        /// <summary>
        /// 获取当前逻辑线程的线程 ID。
        /// </summary>
        public int ManagedThreadId => _netWorkThread.ManagedThreadId;

        /// <summary>
        /// 初始化网络线程管理器，创建逻辑线程并启动。
        /// </summary>
        public override async Task Initialize()
        {
            _netWorkThread = new Thread(Update);
            SynchronizationContext = new ThreadSynchronizationContext(_netWorkThread.ManagedThreadId);
            _netWorkThread.Start();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 向指定的网络通道发送消息。
        /// </summary>
        /// <param name="networkId">网络通道的 ID。</param>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型操作码。</param>
        /// <param name="entityId">实体 ID。</param>
        /// <param name="message">要发送的消息对象。</param>
        public void Send(long networkId, uint channelId, uint rpcId, long routeTypeOpCode, long entityId, object message)
        {
            if (IsDisposed)
            {
                return;
            }

            _actions.Enqueue(new NetAction(networkId, channelId, rpcId, routeTypeOpCode, entityId, NetActionType.Send, message));
        }

        /// <summary>
        /// 向指定的网络通道发送内存流。
        /// </summary>
        /// <param name="networkId">网络通道的 ID。</param>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="routeTypeOpCode">路由类型操作码。</param>
        /// <param name="routeId">路由 ID。</param>
        /// <param name="memoryStream">要发送的内存流。</param>
        public void SendStream(long networkId, uint channelId, uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream)
        {
            if (IsDisposed)
            {
                return;
            }

            _actions.Enqueue(new NetAction(networkId, channelId, rpcId, routeTypeOpCode, routeId, NetActionType.SendMemoryStream, memoryStream));
        }

        /// <summary>
        /// 移除指定网络通道。
        /// </summary>
        /// <param name="networkId">网络通道的 ID。</param>
        /// <param name="channelId">通道 ID。</param>
        public void RemoveChannel(long networkId, uint channelId)
        {
            if (IsDisposed)
            {
                return;
            }

            _actions.Enqueue(new NetAction(networkId, channelId, 0, 0, 0, NetActionType.RemoveChannel, null));
        }

        /// <summary>
        /// 释放对象所占用的资源，并执行必要的清理操作。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            // 使用网络线程同步上下文来确保在网络线程上执行释放和清理操作
            SynchronizationContext.Post(() =>
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
                // 创建一个队列来存储需要释放的网络对象
                var removeList = new Queue<ANetwork>(_networks.Values);

                // 逐个释放网络对象
                foreach (var aNetwork in removeList)
                {
                    if (aNetwork.IsDisposed)
                    {
                        continue;
                    }
                
                    aNetwork.Dispose();
                }

                // 释放所有网络操作
                foreach (var netAction in _actions)
                {
                    netAction.Dispose();
                }

                // 清空队列和字典
                _actions.Clear();
                _networks.Clear();
                removeList.Clear();
                // 清空字段引用
                _netWorkThread = null;
                SynchronizationContext = null;
                base.Dispose();
            });
        }

        #endregion

        #region 网络线程
        /// <summary>
        /// 存储已添加的网络对象的字典，使用长整型作为键。
        /// </summary>
        private readonly Dictionary<long, ANetwork> _networks = new Dictionary<long, ANetwork>();
        /// <summary>
        /// 存储实现了 INetworkUpdate 接口的网络对象的字典，用于更新操作。
        /// </summary>
        private readonly Dictionary<long, INetworkUpdate> _updates = new Dictionary<long, INetworkUpdate>();

        /// <summary>
        /// 网络线程的主循环，用于处理网络操作和更新。
        /// </summary>
        private void Update()
        {
            // 将同步上下文设置为网络线程的上下文，以确保操作在正确的线程上下文中执行。
            System.Threading.SynchronizationContext.SetSynchronizationContext(SynchronizationContext);

            while (!IsDisposed)
            {
                // 线程休眠一毫秒，减少 CPU 使用。
                Thread.Sleep(1);
                // 遍历更新字典中的网络对象，执行更新操作。
                foreach (var (_, aNetwork) in _updates)
                {
                    try
                    {
                        aNetwork.Update();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                // 执行同步上下文的更新，确保异步操作在正确的线程上下文中执行。
                SynchronizationContext.Update();

                // 处理待处理的网络操作队列。
                while (_actions.TryDequeue(out var action))
                {
                    // 根据操作中的网络 ID 查找对应的网络对象。
                    if (!_networks.TryGetValue(action.NetworkId, out var network) || network.IsDisposed)
                    {
                        continue;
                    }

                    try
                    {
                        // 根据操作类型执行相应的网络操作。
                        switch (action.NetActionType)
                        {
                            case NetActionType.Send:
                            {
                                network.Send(action.ChannelId, action.RpcId, action.RouteTypeOpCode, action.EntityId, action.Obj);
                                break;
                            }
                            case NetActionType.SendMemoryStream:
                            {
                                network.Send(action.ChannelId, action.RpcId, action.RouteTypeOpCode, action.EntityId, action.MemoryStream);
                                break;
                            }
                            case NetActionType.RemoveChannel:
                            {
                                network.RemoveChannel(action.ChannelId);
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                    finally
                    {
                        action.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// 添加网络对象到网络线程管理。
        /// </summary>
        /// <param name="aNetwork">要添加的网络对象。</param>
        public void AddNetwork(ANetwork aNetwork)
        {
            if (IsDisposed)
            {
                return;
            }

            // 使用同步上下文将操作异步传递给网络线程执行。
            SynchronizationContext.Post(() =>
            {
                if (IsDisposed || aNetwork.IsDisposed)
                {
                    return;
                }
                // 将网络对象添加到字典中。
                _networks.Add(aNetwork.Id, aNetwork);
                // 如果网络对象实现了 INetworkUpdate 接口，则将其添加到更新字典中。
                if (aNetwork is INetworkUpdate iNetworkUpdate)
                {
                    _updates.Add(aNetwork.Id, iNetworkUpdate);
                }
            });
        }

        /// <summary>
        /// 从网络线程管理中移除网络对象。
        /// </summary>
        /// <param name="networkId">要移除的网络对象的 ID。</param>
        public void RemoveNetwork(long networkId)
        {
            if (IsDisposed)
            {
                return;
            }
            // 使用同步上下文将操作异步传递给网络线程执行。
            SynchronizationContext.Post(() =>
            {
                if (IsDisposed || !_networks.Remove(networkId, out var network))
                {
                    return;
                }
                // 如果网络对象实现了 INetworkUpdate 接口，则将其从更新字典中移除。
                if (network is INetworkUpdate)
                {
                    _updates.Remove(networkId);
                }
            });
        }

        #endregion
    }
}