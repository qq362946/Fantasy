#if FANTASY_NET
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using kcp2k;

namespace Fantasy;

/// <summary>
/// KCP 服务器网络通道，用于处理服务器与客户端之间的数据通信。
/// </summary>
public class KCPServerNetworkChannel : ANetworkServerChannel
{
    /// <summary>
    /// 最大发送窗口大小，用于控制流量。
    /// </summary>
    private int _maxSndWnd;
    /// <summary>
    /// 套接字，用于网络通信。
    /// </summary>
    private readonly Socket _socket;
    /// <summary>
    /// 连接创建时间（只读），用于记录连接建立的时间戳。
    /// </summary>
    public readonly uint CreateTime;
    /// <summary>
    /// 内存池，用于管理内存分配和回收。
    /// </summary>
    private MemoryStream _receiveMemoryStream;
    /// <summary>
    /// KCPServerNetwork 实例，用于处理 KCP 服务器网络通道。
    /// </summary>
    private KCPServerNetwork _kcpServerNetwork;
    /// <summary>
    /// KCP协议实例，用于处理可靠的数据传输。
    /// </summary>
    public Kcp Kcp { get; private set; }

    /// <summary>
    /// 构造函数，创建 KCP 服务器网络通道实例。
    /// </summary>
    /// <param name="network"></param>
    /// <param name="id"></param>
    /// <param name="remoteEndPoint"></param>
    /// <param name="socket"></param>
    /// <param name="createTime"></param>
    public KCPServerNetworkChannel(KCPServerNetwork network, uint id, EndPoint remoteEndPoint, Socket socket, uint createTime) : base(network, id, remoteEndPoint)
    {
        _socket = socket;
        CreateTime = createTime;
        _kcpServerNetwork = network;
    }

    /// <summary>
    /// 释放网络通道。
    /// </summary>
    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_kcpServerNetwork.NetworkThread != Thread.CurrentThread)
        {
            _kcpServerNetwork.NetworkThreadSynchronizationContext.Post(Dispose);
            return;
        }
        
        Kcp = null;

        if (_socket.Connected)
        {
            var buff = new byte[5];
            buff.WriteTo(0, (byte)KcpHeader.Disconnect);
            buff.WriteTo(1, Id);
            _socket.SendTo(buff, 5, SocketFlags.None, RemoteEndPoint);
        }

        _maxSndWnd = 0;
        _receiveMemoryStream.Dispose();
        _receiveMemoryStream = null;
        // 移除网络通道
        _kcpServerNetwork.RemoveChannel(Id);
        // 因为当前是在网络线程中执行的，所以需要投递到Scene线程执行
        ChannelSynchronizationContext.Post(() =>
        {
            base.Dispose();
        });
    }

    /// <summary>
    /// 连接到客户端，并设置 KCP 参数。
    /// </summary>
    /// <param name="kcp">KCP 实例</param>
    /// <param name="maxSndWnd">最大发送窗口大小</param>
    public void Connect(Kcp kcp, int maxSndWnd)
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
    }
    
    /// <summary>
    /// 发送数据到客户端。
    /// </summary>
    /// <param name="memoryStream">内存流</param>
    public override void Send(MemoryStream memoryStream)
    {
        if (IsDisposed)
        {
            return;
        }
        
        // 投递到网络线程执行
        _kcpServerNetwork.NetworkThreadSynchronizationContext.Post(() =>
        {
            // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
            
            var waitSendSize = Kcp.WaitSnd;

            if (waitSendSize > _maxSndWnd)
            {
                Log.Warning($"ERR_KcpWaitSendSizeTooLarge {waitSendSize} > {_maxSndWnd}");
                Dispose();
                return;
            }
        
            // 发送消息
            Kcp.Send(memoryStream.GetBuffer(),0, (int)memoryStream.Length);
            // 因为memoryStream对象池出来的、所以需要手动回收下
            memoryStream.Dispose();
            _kcpServerNetwork.AddToUpdate(0, Id);
        });
    }

    /// <summary>
    /// 发送数据到客户端。
    /// </summary>
    /// <param name="rpcId"></param>
    /// <param name="routeTypeOpCode"></param>
    /// <param name="routeId"></param>
    /// <param name="memoryStream"></param>
    /// <param name="message"></param>
    public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
    {
        Send(PacketParser.Pack(rpcId, routeTypeOpCode, routeId, memoryStream, message));
    }

    /// <summary>
    /// 接收数据并进行处理。
    /// </summary>
    public void Receive()
    {
        if (IsDisposed)
        {
            return;
        }

        while (true)
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

                _receiveMemoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
                _receiveMemoryStream.SetLength(peekSize);
                _receiveMemoryStream.Seek(0, SeekOrigin.Begin);

                var receiveCount = Kcp.Receive(_receiveMemoryStream.GetBuffer().AsMemory(), peekSize);

                // 如果接收的长度跟peekSize不一样，不需要处理，因为消息肯定有问题的(虽然不可能出现)。

                if (receiveCount != peekSize)
                {
                    Log.Error($"receiveCount != peekSize receiveCount:{receiveCount} peekSize:{peekSize}");
                    break;
                }

                if (!PacketParser.UnPack(_receiveMemoryStream, out var packInfo))
                {
                    break;
                }
                
                // 投递到Scene所在的线程执行
                
                ChannelSynchronizationContext.Post(() =>
                {
                    Session.Receive(packInfo);
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

            bytes.WriteTo(0, (byte)KcpHeader.ReceiveData);
            bytes.WriteTo(1, Id);
            _socket.SendTo(bytes, 0, count + 5, SocketFlags.None, RemoteEndPoint);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
#endif