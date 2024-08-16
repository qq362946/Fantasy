#if FANTASY_NET && FANTASY_KCPUNSAFE
using System.Net;
using System.Net.Sockets;
using KCP;
// ReSharper disable ParameterHidesMember
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy;

/// <summary>
/// KCP 服务器网络通道，用于处理服务器与客户端之间的数据通信。
/// </summary>
public class KCPServerNetworkChannel : ANetworkServerChannel
{
    private uint _channelId;
    private bool _isInnerDispose;
    private readonly int _maxSndWnd;
    private KCPServerNetwork _kcpServerNetwork;
    private readonly BufferPacketParser _packetParser;
    private readonly byte[] _channelIdBytes = new byte[sizeof(uint)];
    private readonly byte[] _receiveBuffer = new byte[Packet.PacketBodyMaxLength + 20];
    
    public Kcp Kcp { get; private set; }
    public uint ChannelId 
    {
        get => _channelId;
        private set
        {
            _channelId = value;
            _channelId.GetBytes(_channelIdBytes);
        }
    }
    public KCPServerNetworkChannel(KCPServerNetwork network, uint channelId, IPEndPoint ipEndPoint) : base(network, channelId, ipEndPoint)
    {
        _kcpServerNetwork = network;
        ChannelId = channelId;
        _maxSndWnd = network.Settings.MaxSendWindowSize;
        Kcp = KCPFactory.Create(network.Settings,ChannelId, KcpSpanCallback);
        _packetParser = PacketParserFactory.CreateServerBufferPacket(network);
    }
    
    public override void Dispose()
    {
        if (IsDisposed || _isInnerDispose)
        {
            return;
        }

        _isInnerDispose = true;
        _kcpServerNetwork.RemoveChannel(Id);
        base.Dispose();
        IsDisposed = true;
        Kcp.Dispose();
        Kcp = null;
        ChannelId = 0;
        _kcpServerNetwork = null;
    }
    
    public void Input(ReadOnlyMemory<byte> buffer)
    {
        Kcp.Input(buffer);
        _kcpServerNetwork.AddUpdateChannel(ChannelId, 0);
            
        while (!IsDisposed)
        {
            try
            {
                var peekSize = Kcp.PeekSize();

                switch (peekSize)
                {
                    case < 0:
                    {
                        return;
                    }
                    case 0:
                    {
                        Log.Error($"SocketError.NetworkReset peekSize:{peekSize}");
                        return;
                    }
                }

                var receiveCount = Kcp.Receive(_receiveBuffer, peekSize);
                
                if (receiveCount != peekSize)
                {
                    Log.Error($"receiveCount != peekSize receiveCount:{receiveCount} peekSize:{peekSize}");
                    return;
                }
                
                if (!_packetParser.UnPack(_receiveBuffer, ref receiveCount, out var packInfo))
                {
                    continue;
                }
        
                Session.Receive(packInfo);
            }
            catch (ScanException e)
            {
                Log.Debug($"RemoteAddress:{RemoteEndPoint} \n{e}");
                Dispose();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
    
    public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStreamBuffer memoryStream, object message)
    {
        if (IsDisposed)
        {
            return;
        }
        
        if (Kcp.WaitSendCount > _maxSndWnd)
        {
            // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
            Log.Warning($"ERR_KcpWaitSendSizeTooLarge {Kcp.WaitSendCount} > {_maxSndWnd}");
            Dispose();
            return;
        }

        var buffer = _packetParser.Pack(ref rpcId, ref routeTypeOpCode, ref routeId, memoryStream, message);
        Kcp.Send(buffer.GetBuffer(), 0, (int)buffer.Length);
        _kcpServerNetwork.ReturnMemoryStream(buffer);
        _kcpServerNetwork.AddUpdateChannel(ChannelId, 0);
    }

    private const byte KcpHeaderReceiveData = (byte)KcpHeader.ReceiveData;
    
    private unsafe void KcpSpanCallback(byte[] buffer, ref int count)
    {
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

            fixed (byte* p = buffer)
            {
                p[0] = KcpHeaderReceiveData;
                p[1] = _channelIdBytes[0];
                p[2] = _channelIdBytes[1];
                p[3] = _channelIdBytes[2];
                p[4] = _channelIdBytes[3];
            }
            
            _kcpServerNetwork.SendAsync(buffer, 0, count + 5, RemoteEndPoint);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
#endif