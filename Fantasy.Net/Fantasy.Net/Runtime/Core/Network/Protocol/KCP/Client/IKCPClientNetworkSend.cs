using System.Collections.Generic;
using System.IO;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    internal interface IKCPClientNetworkSend
    {
        void Send(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message);
        void Send(MemoryStream memoryStream);
    }
    
    /// <summary>
    /// KCP客户端等待连接状态的处理
    /// </summary>
    internal class KCPClientNetworkSendPending : IKCPClientNetworkSend
    {
        private readonly APacketParser _packetParser;
        private readonly KCPClientSocket _kcpClientSocket;
        private readonly Queue<MessageQueue> _messageCache = new Queue<MessageQueue>();

        public KCPClientNetworkSendPending(KCPClientSocket kcpClientSocket, APacketParser packetParser)
        {
            _packetParser = packetParser;
            _kcpClientSocket = kcpClientSocket;
        }

        public void Send(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message)
        {
            _messageCache.Enqueue(new MessageQueue()
            {
                RpcId = rpcId,
                RouteId = routeId,
                RouteTypeOpCode = routeTypeOpCode,
                Message = message,
                MemoryStream = memoryStream
            });
        }

        public void Send(MemoryStream memoryStream)
        {
            _messageCache.Enqueue(new MessageQueue()
            {
                MemoryStream = memoryStream
            });
        }

        public void SendCache()
        {
            while (_messageCache.TryDequeue(out var messageQueue))
            {
                if (messageQueue.RpcId != 0)
                {
                    var stream = _packetParser.Pack(messageQueue.RpcId, messageQueue.RouteTypeOpCode,
                        messageQueue.RouteId, messageQueue.MemoryStream, messageQueue.Message);
                    _kcpClientSocket.Send(stream);
                    continue;
                }

                _kcpClientSocket.Send(messageQueue.MemoryStream);
            }
        }
    }

    internal class KCPClientNetworkSend : IKCPClientNetworkSend
    {
        private readonly APacketParser _packetParser;
        private readonly KCPClientSocket _kcpClientSocket;

        public KCPClientNetworkSend(KCPClientSocket kcpClientSocket, APacketParser packetParser)
        {
            _packetParser = packetParser;
            _kcpClientSocket = kcpClientSocket;
        }

        public void Send(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message)
        {
            _kcpClientSocket.Send(_packetParser.Pack(rpcId, routeTypeOpCode, routeId, memoryStream, message));
        }

        public void Send(MemoryStream memoryStream)
        {
            _kcpClientSocket.Send(memoryStream);
        }
    }
}