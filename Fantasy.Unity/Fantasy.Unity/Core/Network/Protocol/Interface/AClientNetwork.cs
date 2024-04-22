using System;
using System.IO;
using System.Net;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 抽象客户端网络基类。
    /// </summary>
    public abstract class AClientNetwork : ANetwork, INetworkChannel
    {
        public uint ChannelId { get; protected set; }
        public Session Session { get; protected set; }
        public abstract event Action OnConnectFail;
        public abstract event Action OnConnectComplete;
        public abstract event Action OnConnectDisconnect;
        protected AClientNetwork(Scene scene, NetworkType networkType, NetworkProtocolType networkProtocolType, NetworkTarget networkTarget) : base(scene, networkType, networkProtocolType, networkTarget) { }
        public abstract Session Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000);
        public abstract void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message);

        public abstract void Send(MemoryStream memoryStream);
        public override void Dispose()
        {
            ChannelId = 0;
            
            if (Session != null)
            {
                Session.Dispose();
                Session = null;
            }
            
            base.Dispose();
        }
    }
}