using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
// ReSharper disable InconsistentNaming
#pragma warning disable CS8625
#pragma warning disable CS8622
#pragma warning disable CS8618

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 表示 TCP 协议服务端网络类。
    /// </summary>
    public sealed class TCPServerNetwork : ANetwork
    {
        #region 逻辑线程

        /// <summary>
        /// 创建一个 TCP 协议服务端网络实例。
        /// </summary>
        /// <param name="scene">所属场景。</param>
        /// <param name="networkTarget">网络目标。</param>
        /// <param name="address">服务器绑定的地址和端口。</param>
        public TCPServerNetwork(Scene scene, NetworkTarget networkTarget, IPEndPoint address) : base(scene, NetworkType.Server, NetworkProtocolType.TCP, networkTarget)
        {
            _acceptAsync = new SocketAsyncEventArgs();
            NetworkThread.Instance.AddNetwork(this);
            NetworkThread.Instance.SynchronizationContext.Post(() =>
            {
                _random = new Random();
                _socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                }
            
                _socket.Bind(address);
                _socket.Listen(int.MaxValue);
                _socket.SetSocketBufferToOsLimit();
                _acceptAsync.Completed += OnCompleted;
                AcceptAsync();
            });
        }

        /// <summary>
        /// 释放<see cref="TCPServerNetwork"/>实例使用的所有资源。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            IsDisposed = true;
            
            NetworkThread.Instance.SynchronizationContext.Post(() =>
            {
                if (_socket.Connected)
                {
                    _socket.Disconnect(true);
                    _socket.Close();
                }

                _socket = null;
                _random = null;
                _acceptAsync = null;

                var channels = new List<TCPServerNetworkChannel>(_connectionChannel.Values);

                foreach (var tcpServerNetworkChannel in channels)
                {
                    tcpServerNetworkChannel.Dispose();
                }
                
                channels.Clear();
                _connectionChannel.Clear();
                base.Dispose();
            });
        }

        #endregion

        #region 网络主线程

        private Socket _socket;
        private Random _random;
        private SocketAsyncEventArgs _acceptAsync;
        private readonly Dictionary<long, TCPServerNetworkChannel> _connectionChannel = new Dictionary<long, TCPServerNetworkChannel>();

        /// <summary>
        /// 在指定通道上发送网络消息。
        /// </summary>
        /// <param name="channelId">要发送消息的通道ID。</param>
        /// <param name="rpcId">RPC（远程过程调用）的ID。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="routeId">路由ID。</param>
        /// <param name="memoryStream"><see cref="MemoryStream"/> 包含消息数据。</param>
        public override void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            // 检查指定的通道是否存在且未被释放
            if (!_connectionChannel.TryGetValue(channelId, out var channel) || channel.IsDisposed)
            {
                return;
            }

            // 打包消息并发送
            var sendMemoryStream = Pack(rpcId, routeTypeOpCode, routeId, memoryStream, null);
            channel.Send(sendMemoryStream);
        }

        /// <summary>
        /// 在指定通道上发送网络消息。
        /// </summary>
        /// <param name="channelId">要发送消息的通道ID。</param>
        /// <param name="rpcId">RPC（远程过程调用）的ID。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="routeId">路由ID。</param>
        /// <param name="message">要发送的消息对象。</param>
        public override void Send(uint channelId, uint rpcId, long routeTypeOpCode, long routeId, object message)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            // 检查指定的通道是否存在且未被释放
            if (!_connectionChannel.TryGetValue(channelId, out var channel) || channel.IsDisposed)
            {
                return;
            }

            // 打包消息并发送
            var memoryStream = Pack(rpcId, routeTypeOpCode, routeId, null, message);
            channel.Send(memoryStream);
        }

        /// <summary>
        /// 异步接受客户端连接请求。
        /// </summary>
        private void AcceptAsync()
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            _acceptAsync.AcceptSocket = null;

            if (_socket.AcceptAsync(_acceptAsync))
            {
                return;
            }

            OnAcceptComplete(_acceptAsync);
        }

        private void OnAcceptComplete(SocketAsyncEventArgs asyncEventArgs)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (_socket == null || asyncEventArgs.AcceptSocket == null)
            {
                return;
            }

            if (asyncEventArgs.SocketError != SocketError.Success)
            {
                Log.Error($"Socket Accept Error: {_acceptAsync.SocketError}");
                return;
            }

            try
            {
                var channelId = 0xC0000000 | (uint) _random.Next();

                while (_connectionChannel.ContainsKey(channelId))
                {
                    channelId = 0xC0000000 | (uint) _random.Next();
                }

                var channel = new TCPServerNetworkChannel(channelId, asyncEventArgs.AcceptSocket, this);
                
                ThreadSynchronizationContext.Main.Post(() =>
                {
                    if (channel.IsDisposed)
                    {
                        return;
                    }

                    Session.Create(NetworkMessageScheduler, channel, NetworkTarget);
                });
                
                _connectionChannel.Add(channelId, channel);
                channel.Receive();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                AcceptAsync();
            }
        }

        /// <summary>
        /// 从网络中移除指定的通道。
        /// </summary>
        /// <param name="channelId">要移除的通道的唯一标识。</param>
        public override void RemoveChannel(uint channelId)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (IsDisposed || !_connectionChannel.Remove(channelId, out var channel))
            {
                return;
            }
#if NETDEBUG
            Log.Debug($"TCPServerNetwork _connectionChannel:{_connectionChannel.Count}");
#endif
            if (channel.IsDisposed)
            {
                return;
            }

            channel.Dispose();
        }

        #endregion

        #region 网络线程（由Socket底层产生的线程）

        private void OnCompleted(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            switch (asyncEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                {
                    NetworkThread.Instance.SynchronizationContext.Post(() => OnAcceptComplete(asyncEventArgs));
                    break;
                }
                default:
                {
                    throw new Exception($"Socket Accept Error: {asyncEventArgs.LastOperation}");
                }
            }
        }

        #endregion
    }
}