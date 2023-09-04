using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Fantasy.DataStructure;
using Fantasy.Helper;
using kcp2k;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

// ReSharper disable InconsistentNaming

namespace Fantasy.Core.Network
{
    /// <summary>
    /// KCP 服务器网络通道类，负责管理基于 KCP 协议的网络通信。
    /// </summary>
    public sealed class KCPServerNetworkChannel : ANetworkChannel
    {
        #region 网络主线程
        /// <summary>
        /// 最大发送窗口大小，用于控制流量。
        /// </summary>
        private int _maxSndWnd;
        /// <summary>
        /// 原始发送缓冲区，用于构建发送的数据包。
        /// </summary>
        private byte[] _rawSendBuffer;
        /// <summary>
        /// 连接创建时间（只读），用于记录连接建立的时间戳。
        /// </summary>
        public readonly uint CreateTime;
        /// <summary>
        /// 套接字，用于网络通信。
        /// </summary>
        private readonly Socket _socket;
        /// <summary>
        /// 更新函数，用于更新网络通道状态。
        /// </summary>
        private Action<uint, uint> _addToUpdate;
        /// <summary>
        /// 内存池，用于管理内存分配和回收。
        /// </summary>
        private MemoryPool<byte> _memoryPool;
        /// <summary>
        /// KCP协议实例，用于处理可靠的数据传输。
        /// </summary>
        public Kcp Kcp { get; private set; }
        /// <summary>
        /// 当网络通道被释放时触发的事件。
        /// </summary>
        public override event Action OnDispose;
        /// <summary>
        /// 当接收到内存流数据包时触发的事件。
        /// </summary>
        public override event Action<APackInfo> OnReceiveMemoryStream;

        /// <summary>
        /// 构造函数，创建 KCP 服务器网络通道实例。
        /// </summary>
        /// <param name="scene">所属场景</param>
        /// <param name="id">通道 ID</param>
        /// <param name="networkId">网络 ID</param>
        /// <param name="remoteEndPoint">远程终端点</param>
        /// <param name="socket">套接字</param>
        /// <param name="createTime">创建时间</param>
        public KCPServerNetworkChannel(Scene scene, uint id, long networkId, EndPoint remoteEndPoint, Socket socket, uint createTime) : base(scene, id, networkId)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            _socket = socket;
            CreateTime = createTime;
            RemoteEndPoint = remoteEndPoint;
            _memoryPool = MemoryPool<byte>.Shared;
        }

        /// <summary>
        /// 释放资源并断开连接。
        /// </summary>
        public override void Dispose()
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (IsDisposed)
            {
                return;
            }

            Kcp = null;
            var buff = new byte[5];
            buff.WriteTo(0, (byte) KcpHeader.Disconnect);
            buff.WriteTo(1, Id);
            _socket.SendTo(buff, 5, SocketFlags.None, RemoteEndPoint);
#if NETDEBUG
            Log.Debug($"KCPServerNetworkChannel ConnectionPtrChannel:{KCPServerNetwork.ConnectionPtrChannel.Count}");
#endif
            _maxSndWnd = 0;
            _addToUpdate = null;
            _memoryPool.Dispose();
            _memoryPool = null;
            ThreadSynchronizationContext.Main.Post(OnDispose);
            base.Dispose();
        }

        /// <summary>
        /// 连接到客户端，并设置 KCP 参数。
        /// </summary>
        /// <param name="kcp">KCP 实例</param>
        /// <param name="addToUpdate">添加到更新的方法</param>
        /// <param name="maxSndWnd">最大发送窗口大小</param>
        /// <param name="networkTarget">网络目标</param>
        /// <param name="networkMessageScheduler">网络消息调度器</param>
        public void Connect(Kcp kcp, Action<uint, uint> addToUpdate, int maxSndWnd, NetworkTarget networkTarget, ANetworkMessageScheduler networkMessageScheduler)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            Kcp = kcp;
            _maxSndWnd = maxSndWnd;
            _addToUpdate = addToUpdate;
            _rawSendBuffer = new byte[ushort.MaxValue];
            PacketParser = APacketParser.CreatePacketParser(networkTarget);
            
            ThreadSynchronizationContext.Main.Post(() =>
            {
                if (IsDisposed)
                {
                    return;
                }

                Session.Create(networkMessageScheduler, this, networkTarget);
            });
        }

        /// <summary>
        /// 发送数据到客户端。
        /// </summary>
        /// <param name="memoryStream">内存流</param>
        public void Send(MemoryStream memoryStream)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (IsDisposed)
            {
                return;
            }

            // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
            
            var waitSendSize = Kcp.WaitSnd;

            if (waitSendSize > _maxSndWnd)
            {
                Log.Warning($"ERR_KcpWaitSendSizeTooLarge {waitSendSize} > {_maxSndWnd}");
                Dispose();
                return;
            }
            // 发送消息
            Kcp.Send(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            // 因为memoryStream对象池出来的、所以需要手动回收下
            memoryStream.Dispose();
            _addToUpdate(0, Id);
        }

        /// <summary>
        /// 接收数据并进行处理。
        /// </summary>
        public void Receive()
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (IsDisposed)
            {
                return;
            }

            for (;;)
            {
                try
                {
                    // 获得一个完整消息的长度
                    
                    var peekSize = Kcp.PeekSize();
                    
                    // 如果没有接收的消息那就跳出当前循环。
                    
                    if (peekSize < 0)
                    {
                        return;
                    }
                    
                    // 如果为0，表示当前消息发生错误。提示下、一般情况不会发生的
                    if (peekSize == 0)
                    {
                        throw new Exception("SocketError.NetworkReset");
                    }
                    
                    var receiveMemoryOwner = _memoryPool.Rent(Packet.OuterPacketMaxLength);
                    var receiveCount = Kcp.Receive(receiveMemoryOwner.Memory, peekSize);

                    // 如果接收的长度跟peekSize不一样，不需要处理，因为消息肯定有问题的(虽然不可能出现)。

                    if (receiveCount != peekSize)
                    {
                        Log.Error($"receiveCount != peekSize receiveCount:{receiveCount} peekSize:{peekSize}");
                        break;
                    }

                    if (!PacketParser.UnPack(receiveMemoryOwner,out var packInfo))
                    {
                        break;
                    }

                    ThreadSynchronizationContext.Main.Post(() =>
                    {
                        if (IsDisposed)
                        {
                            return;
                        }

                        // ReSharper disable once PossibleNullReferenceException
                        OnReceiveMemoryStream(packInfo);
                    });
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 将数据发送到网络。
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="count">发送的字节数</param>
        public void Output(byte[] bytes, int count)
        {
#if FANTASY_DEVELOP
            if (NetworkThread.Instance.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("not in NetworkThread!");
                return;
            }
#endif
            if (IsDisposed)
            {
                return;
            }
            
            try
            {
                if (count == 0)
                {
                    throw new Exception("KcpOutput count 0");
                }

                _rawSendBuffer.WriteTo(0, (byte) KcpHeader.ReceiveData);
                _rawSendBuffer.WriteTo(1, Id);
                Buffer.BlockCopy(bytes, 0, _rawSendBuffer, 5, count);
                _socket.SendTo(_rawSendBuffer, 0, count + 5, SocketFlags.None, RemoteEndPoint);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #endregion
    }
}