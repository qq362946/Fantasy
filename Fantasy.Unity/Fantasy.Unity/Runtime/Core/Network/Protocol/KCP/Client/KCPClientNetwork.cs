using System;
using System.IO;
using System.Net;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// KCP协议客户端网络类，用于管理KCP客户端网络连接。
    /// </summary>
    public class KCPClientNetwork : AClientNetwork
    {
        private readonly APacketParser _packetParser;
        private readonly KCPClientSocket _kcpClientSocket;
        private IKCPClientNetworkSend _kcpClientNetworkSend;
        private readonly ThreadSynchronizationContext _threadSynchronizationContext;
        
        public KCPClientNetwork(Scene scene, NetworkTarget networkTarget) : base(scene, NetworkType.Client, NetworkProtocolType.KCP, networkTarget)
        {
            _kcpClientSocket = new KCPClientSocket();
            _threadSynchronizationContext = scene.ThreadSynchronizationContext;
            _packetParser = APacketParser.CreatePacketParser(Scene, NetworkTarget);
            _kcpClientNetworkSend = new KCPClientNetworkSendPending(_kcpClientSocket, _packetParser);
        }
        
        public override Session Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail, Action onConnectDisconnect, int connectTimeout = 5000)
        {
            if (IsInit)
            {
                throw new NotSupportedException($"KCPClientNetwork Id:{Id} Has already been initialized. If you want to call Connect again, please re instantiate it.");
            }
            
            IsInit = true;
            _kcpClientSocket.OnConnectTimeout += onConnectFail;
            _kcpClientSocket.OnConnectComplete += () =>
            {
                // 当KCP连接建立成功后、会首先把缓存中的消息发送给目标。
                ((KCPClientNetworkSendPending)_kcpClientNetworkSend).SendCache();
                // 发送完成后开始正常的发送流程。
                _kcpClientNetworkSend = new KCPClientNetworkSend(_kcpClientSocket, _packetParser);
                onConnectComplete?.Invoke();
            };
            _kcpClientSocket.OnConnectDisconnect += onConnectDisconnect;
            _kcpClientSocket.OnReceiveCompleted += OnReceiveCompleted;
            _kcpClientSocket.Connect(Scene, NetworkTarget, remoteEndPoint, connectTimeout);
            Session = Session.Create(this, remoteEndPoint);
            return Session;
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            _kcpClientSocket.Dispose();
            _packetParser.Dispose();

            if (Session != null && !Session.IsDisposed)
            {
                Session.Dispose();
            }
            
            base.Dispose();
        }

        public override void RemoveChannel(uint channelId)
        {
            Dispose();
        }

        public override void Send(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
        {
            if (IsDisposed)
            {
                return;
            }

            _kcpClientNetworkSend.Send(ref rpcId, ref routeTypeOpCode, ref routeId, memoryStream, message);
        }

        public override void Send(MemoryStream memoryStream)
        {
            if (IsDisposed)
            {
                return;
            }
            
            _kcpClientNetworkSend.Send(memoryStream);
        }

        private void OnReceiveCompleted(byte[] buffer, ref int count)
        {
            if (!_packetParser.UnPack(buffer, ref count, out var packInfo))
            {
                return;
            }

            _threadSynchronizationContext.Post(() =>
            {
                if (IsDisposed)
                {
                    return;
                }
            
                Session.Receive(packInfo);
            });
        }
    }
}