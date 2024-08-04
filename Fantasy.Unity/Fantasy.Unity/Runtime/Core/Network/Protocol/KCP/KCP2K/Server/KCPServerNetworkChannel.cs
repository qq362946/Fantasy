#if FANTASY_NET && FANTASY_KCP2K
using System.Net;
using System.Net.Sockets;
using kcp2k;
// ReSharper disable ParameterHidesMember
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy;

public sealed class KCPServerNetworkChannel : ANetworkServerChannel
{
    private Kcp _kcp;
    private uint _channelId;
    private KCPServerNetwork _kcpServerNetwork;
    private readonly byte[] _channelIdBytes = new byte[sizeof(uint)];
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
        ChannelId = channelId;
        _kcpServerNetwork = network;
        _kcp = KCPFactory.Create(network.Settings,ChannelId, KcpSpanCallback);
    }
}
#endif