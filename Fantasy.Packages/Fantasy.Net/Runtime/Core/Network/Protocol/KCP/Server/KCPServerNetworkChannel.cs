#if FANTASY_NET
using System;
using System.Buffers;
using System.Net;
using System.Runtime.InteropServices;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using Fantasy.Serialize;
using KCP;
// ReSharper disable ParameterHidesMember
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Network.KCP
{
    /// <summary>
    /// KCP 服务器网络通道，用于处理服务器与客户端之间的数据通信。
    /// </summary>
    internal class KCPServerNetworkChannel : ANetworkServerChannel
    {
        private bool _isInnerDispose;
        private readonly int _maxSndWnd;
        private KCPServerNetwork _kcpServerNetwork;
        private readonly BufferPacketParser _packetParser;
        private readonly int _packetHeadLength;
        
        public Kcp Kcp { get; private set; }
        public uint ChannelId { get; private set; }

        public KCPServerNetworkChannel(KCPServerNetwork network, uint channelId, IPEndPoint ipEndPoint) : base(network, channelId, ipEndPoint)
        {
            _kcpServerNetwork = network;
            _packetHeadLength =
                _kcpServerNetwork.NetworkTarget == NetworkTarget.Inner
                    ? Packet.InnerPacketHeadLength
                    : Packet.OuterPacketHeadLength;
            ChannelId = channelId;
            _maxSndWnd = network.Settings.MaxSendWindowSize;
            Kcp = KCPFactory.Create(network.Settings, ChannelId, KcpSpanCallback);
            _packetParser = PacketParserFactory.CreateBufferPacketParser(network);
        }

        public override void Dispose()
        {
            if (IsDisposed || _isInnerDispose)
            {
                return;
            }

            try
            {
                _isInnerDispose = true;
                _kcpServerNetwork.RemoveChannel(Id);
                IsDisposed = true;
                _packetParser.Dispose();
                Kcp.Dispose();
                Kcp = null;
                ChannelId = 0;
                _kcpServerNetwork = null;
            }
            catch (Exception e)
            {
               Log.Error(e);
            }
            finally
            {
                base.Dispose();
            }
        }

        public void Input(ReadOnlyMemory<byte> buffer)
        {
            if (Kcp.Input(buffer.Span) < 0)
            {
                Dispose();
                return;
            }
            
            _kcpServerNetwork.AddUpdateChannel(ChannelId, 0);

            while (!IsDisposed)
            {
                var peekSize = Kcp.PeekSize();

                if (peekSize < 0)
                {
                    return;
                }
                    
                // 使用减法，避免 MaxMessageSize + packetHeadLength 整数溢出。
                if (peekSize < _packetHeadLength ||
                    peekSize - _packetHeadLength > ProgramDefine.MaxMessageSize)
                {
                    Dispose();
                    return;
                }
                
                var receiveBuffer = ArrayPool<byte>.Shared.Rent(peekSize);
                
                try
                {
                    var receiveCount = Kcp.Receive(receiveBuffer.AsSpan(0, peekSize));

                    if (receiveCount != peekSize || 
                        !_packetParser.UnPack(receiveBuffer, ref receiveCount, out var packInfo))
                    {
                        Dispose();
                        return;
                    }

                    Session.Receive(packInfo);
                }
                catch (ScanException e)
                {
                    Log.Debug($"RemoteAddress:{RemoteEndPoint} \n{e}");
                    Dispose();
                    return;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Dispose();
                    return;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(receiveBuffer);
                }
            }
        }

        public override void Send(uint rpcId, long address, MemoryStreamBuffer memoryStream, IMessage message, Type messageType)
        {
            if (IsDisposed)
            {
                return;
            }

            if (Kcp.WaitSendCount > _maxSndWnd)
            {
                // 检查等待发送的消息，如果超出两倍窗口大小，KCP作者给的建议是要断开连接
                Log.Warning($"ERR_KcpWaitSendSizeTooLarge {Kcp.WaitSendCount} > {_maxSndWnd}");
                
                message?.Dispose();
                
                if (memoryStream?.MemoryStreamBufferSource == MemoryStreamBufferSource.Pack)
                {
                    _kcpServerNetwork.MemoryStreamBufferPool.ReturnMemoryStream(memoryStream);
                }
                
                Dispose();
                return;
            }

            MemoryStreamBuffer buffer = null;
            var packetLength = 0;
            var sendCount = -1;

            try
            {
                buffer = _packetParser.Pack(ref rpcId, ref address, memoryStream, message, messageType);
                packetLength = (int)buffer.Position;
                sendCount = Kcp.Send(buffer.GetBuffer().AsSpan(0, packetLength));

                if (sendCount == packetLength)
                {
                    _kcpServerNetwork.AddUpdateChannel(ChannelId, 0);
                }
            }
            finally
            {
                if (buffer != null && MemoryStreamBufferSource.Return.HasFlag(buffer.MemoryStreamBufferSource))
                {
                    _kcpServerNetwork.MemoryStreamBufferPool.ReturnMemoryStream(buffer);
                }
            }
            
            if (sendCount != packetLength)
            {
                Log.Error($"ERR_KcpSendFailed {sendCount} != {packetLength} RemoteAddress:{RemoteEndPoint}");
                Dispose();
            }
        }

        private const byte KcpHeaderReceiveData = (byte)KcpHeader.ReceiveData;

        private void KcpSpanCallback(byte[] buffer, int count)
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
                
                var channelId = ChannelId;
                var bufferSpan = buffer.AsSpan();
                bufferSpan[0] = KcpHeaderReceiveData;
                MemoryMarshal.Write(bufferSpan[1..], in channelId);
                
                _kcpServerNetwork.SendAsync(buffer, 0, count + 5, RemoteEndPoint);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
#endif