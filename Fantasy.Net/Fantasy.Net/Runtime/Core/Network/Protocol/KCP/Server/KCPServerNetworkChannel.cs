#if FANTASY_NET
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy;

/// <summary>
/// KCP 服务器网络通道，用于处理服务器与客户端之间的数据通信。
/// </summary>
public class KCPServerNetworkChannel : ANetworkServerChannel
{
    private KCPServerNetwork _kcpServerNetwork;
    private KCPServerSSocketAsyncEventArgs _kcpServerSSocketAsyncEventArgs;
    
    /// <summary>
    /// 构造函数，创建 KCP 服务器网络通道实例。
    /// </summary>
    /// <param name="network"></param>
    /// <param name="kcpServerSSocketAsyncEventArgs"></param>
    public KCPServerNetworkChannel(KCPServerNetwork network, KCPServerSSocketAsyncEventArgs kcpServerSSocketAsyncEventArgs) : base(network, kcpServerSSocketAsyncEventArgs.ChannelId, kcpServerSSocketAsyncEventArgs.IPEndPoint)
    {
        _kcpServerNetwork = network;
        _kcpServerSSocketAsyncEventArgs = kcpServerSSocketAsyncEventArgs;
        _kcpServerSSocketAsyncEventArgs.OnReceiveCompleted = OnReceiveCompleted;
        _kcpServerSSocketAsyncEventArgs.OnDisconnectCompleted += OnDisconnectCompleted;
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
        
        _kcpServerSSocketAsyncEventArgs.Send(memoryStream);
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
        _kcpServerSSocketAsyncEventArgs.Send(PacketParser.Pack(rpcId, routeTypeOpCode, routeId, memoryStream, message));
    }

    private void OnReceiveCompleted(byte[] buffer, ref int count)
    {
        if (!PacketParser.UnPack(buffer, ref count, out var packInfo))
        {
            return;
        }

        ChannelSynchronizationContext.Post(() => { Session.Receive(packInfo); });
    }

    private void OnDisconnectCompleted(object? sender, EventArgs e)
    {
        ChannelSynchronizationContext.Post(Dispose);
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
        
        IsDisposed = true;
        _kcpServerNetwork.RemoveChannel(Id);
        _kcpServerSSocketAsyncEventArgs.Dispose();
        _kcpServerNetwork = null;
        _kcpServerSSocketAsyncEventArgs = null;
    }
}
#endif