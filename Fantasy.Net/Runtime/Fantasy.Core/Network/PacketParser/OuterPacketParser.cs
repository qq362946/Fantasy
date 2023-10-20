using System;
using System.Buffers;
using System.IO;
using Fantasy.DataStructure;
using Fantasy.Helper;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.Core.Network
{
    /// <summary>
    /// 用于解析外部网络消息的数据包信息。
    /// </summary>
    public sealed class OuterPackInfo : APackInfo
    {
        /// <summary>
        /// 创建一个 <see cref="OuterPackInfo"/> 实例，并将其与给定的内存资源关联。
        /// </summary>
        /// <param name="memoryOwner">内存资源的所有者。</param>
        /// <returns>创建的 <see cref="OuterPackInfo"/> 实例。</returns>
        public static OuterPackInfo Create(IMemoryOwner<byte> memoryOwner)
        {
            var outerPackInfo = Rent<OuterPackInfo>();;
            outerPackInfo.MemoryOwner = memoryOwner;
            return outerPackInfo;
        }

        /// <summary>
        /// 创建一个 <see cref="MemoryStream"/> 实例，用于存储内存数据，并返回该实例。
        /// </summary>
        /// <returns>创建的 <see cref="MemoryStream"/> 实例。</returns>
        public override MemoryStream CreateMemoryStream()
        {
            // 创建可回收的内存流，用于存储消息数据
            var recyclableMemoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
            // 将内存资源中的消息数据写入内存流
            // 写入从内存起始位置到消息头长度+消息体长度的数据
            recyclableMemoryStream.Write(MemoryOwner.Memory.Span.Slice(0, Packet.InnerPacketHeadLength + MessagePacketLength));
            // 将内存流的指针定位到起始位置
            recyclableMemoryStream.Seek(0, SeekOrigin.Begin);
            // 返回创建的内存流
            return recyclableMemoryStream;
        }

        /// <summary>
        /// 释放当前 <see cref="OuterPackInfo"/> 实例及其关联的资源。
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            // 将当前的 OuterPackInfo 实例归还到对象池，以便重复利用
            Pool<OuterPackInfo>.Return(this);
        }

        /// <summary>
        /// 将消息数据从内存反序列化为指定的消息类型实例。
        /// </summary>
        /// <param name="messageType">目标消息类型。</param>
        /// <returns>反序列化后的消息类型实例。</returns>
        public override object Deserialize(Type messageType)
        {
            // 获取内存资源的引用
            var memoryOwnerMemory = MemoryOwner.Memory;
            // 获取消息体数据的切片
            var memory = memoryOwnerMemory.Slice(Packet.OuterPacketHeadLength, MessagePacketLength);
            // 使用 ProtoBufHelper 解析内存中的消息数据为指定的消息类型
            return ProtoBufHelper.FromMemory(messageType, memory);
        }
    }

    /// <summary>
    /// 用于解析外部网络消息的数据包解析器。
    /// </summary>
    public sealed class OuterPacketParser : APacketParser
    {
        private uint _rpcId;
        private uint _protocolCode;
        private long _routeTypeCode;
        private int _messagePacketLength;
        private bool _isUnPackHead = true;
        private readonly byte[] _messageHead = new byte[Packet.OuterPacketHeadLength];

        /// <summary>
        /// 创建一个新的 <see cref="OuterPacketParser"/> 实例。
        /// </summary>
        public OuterPacketParser()
        {
            MemoryPool = MemoryPool<byte>.Shared;
        }

        /// <summary>
        /// 用于解析外部网络消息的数据包解析器。
        /// </summary>
        /// <param name="buffer">循环缓冲区，用于存储接收到的数据。</param>
        /// <param name="packInfo">解析后的数据包信息。</param>
        /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
        public override bool UnPack(CircularBuffer buffer, out APackInfo packInfo)
        {
            packInfo = null;

            while (!IsDisposed)
            {
                if (_isUnPackHead)
                {
                    if (buffer.Length < Packet.OuterPacketHeadLength)
                    {
                        return false;
                    }

                    _ = buffer.Read(_messageHead, 0, Packet.OuterPacketHeadLength);
                    _messagePacketLength = BitConverter.ToInt32(_messageHead, 0);
#if FANTASY_NET
                    if (_messagePacketLength > Packet.PacketBodyMaxLength)
                    {
                        throw new ScanException($"The received information exceeds the maximum limit = {_messagePacketLength}");
                    }
#endif
                    _protocolCode = BitConverter.ToUInt32(_messageHead, Packet.PacketLength);
                    _rpcId = BitConverter.ToUInt32(_messageHead, Packet.OuterPacketRpcIdLocation);
                    _routeTypeCode = BitConverter.ToUInt16(_messageHead, Packet.OuterPacketRouteTypeOpCodeLocation);
                    _isUnPackHead = false;
                }

                try
                {
                    if (buffer.Length < _messagePacketLength)
                    {
                        return false;
                    }
                
                    _isUnPackHead = true;
                    // 创建消息包
                    var memoryOwner = MemoryPool.Rent(Packet.OuterPacketMaxLength);
                    packInfo = OuterPackInfo.Create(memoryOwner);
                    packInfo.RpcId = _rpcId;
                    packInfo.ProtocolCode = _protocolCode;
                    packInfo.RouteTypeCode = _routeTypeCode;
                    packInfo.MessagePacketLength = _messagePacketLength;
                    // 写入消息体的信息到内存中
                    buffer.Read(memoryOwner.Memory.Slice(Packet.OuterPacketHeadLength), _messagePacketLength);
                    // 写入消息头的信息到内存中
                    _messageHead.AsMemory().CopyTo(memoryOwner.Memory.Slice(0, Packet.OuterPacketHeadLength));
                    return _messagePacketLength > 0;
                }
                catch (Exception e)
                {
                    packInfo?.Dispose();
                    Log.Error(e);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 从内存中解析数据包。
        /// </summary>
        /// <param name="memoryOwner">内存块所有者。</param>
        /// <param name="packInfo">解析后的数据包信息。</param>
        /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
        public override bool UnPack(IMemoryOwner<byte> memoryOwner, out APackInfo packInfo)
        {
            packInfo = null;
            var memory = memoryOwner.Memory;

            try
            {
                if (memory.Length < Packet.OuterPacketHeadLength)
                {
                    return false;
                }

                var memorySpan = memory.Span;
                _messagePacketLength = BitConverter.ToInt32(memorySpan);
#if FANTASY_NET
                if (_messagePacketLength > Packet.PacketBodyMaxLength)
                {
                    throw new ScanException($"The received information exceeds the maximum limit = {_messagePacketLength}");
                }
#endif
                packInfo = OuterPackInfo.Create(memoryOwner);
                packInfo.MessagePacketLength = _messagePacketLength;
                packInfo.ProtocolCode = BitConverter.ToUInt32(memorySpan.Slice(Packet.PacketLength));
                packInfo.RpcId = BitConverter.ToUInt32(memorySpan.Slice(Packet.OuterPacketRpcIdLocation));
                packInfo.RouteTypeCode = BitConverter.ToUInt16(memorySpan.Slice(Packet.OuterPacketRouteTypeOpCodeLocation));

                if (memory.Length < _messagePacketLength)
                {
                    return false;
                }
                // Log.Debug($"ProtocolCode:{packInfo.ProtocolCode} RpcId:{packInfo.RpcId} RouteTypeCode:{packInfo.RouteTypeCode} MessagePacketLength:{packInfo.MessagePacketLength}");
                return _messagePacketLength >= 0;
            }
            catch (Exception e)
            {
                packInfo?.Dispose();
                Log.Error(e);
                return false;
            }
        }

        /// <summary>
        /// 封装数据包。
        /// </summary>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="memoryStream">要封装的内存流。</param>
        /// <returns>封装后的内存流。</returns>
        public static MemoryStream Pack(uint rpcId, long routeTypeOpCode, MemoryStream memoryStream)
        {
            memoryStream.Seek(Packet.OuterPacketRpcIdLocation, SeekOrigin.Begin);
            memoryStream.Write(BitConverter.GetBytes(rpcId));
            memoryStream.Write(BitConverter.GetBytes(routeTypeOpCode));
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// 封装数据包。
        /// </summary>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="message">要封装的消息对象。</param>
        /// <returns>封装后的内存流。</returns>
        public static MemoryStream Pack(uint rpcId, long routeTypeOpCode, object message)
        {
            var opCode = Opcode.PingRequest;
            var packetBodyCount = 0;
            var memoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
            memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);

            if (message != null)
            {
                ProtoBufHelper.ToStream(message, memoryStream);
                opCode = MessageDispatcherSystem.Instance.GetOpCode(message.GetType());
                packetBodyCount = (int)(memoryStream.Position - Packet.OuterPacketHeadLength);
                // 如果消息是对象池的需要执行Dispose
                if (message is IPoolMessage iPoolMessage)
                {
                    iPoolMessage.Dispose();
                }
            }

            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
                throw new Exception($"Message content exceeds {Packet.PacketBodyMaxLength} bytes");
            }
            
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Write(BitConverter.GetBytes(packetBodyCount));
            memoryStream.Write(BitConverter.GetBytes(opCode));
            memoryStream.Write(BitConverter.GetBytes(rpcId));
            memoryStream.Write(BitConverter.GetBytes(routeTypeOpCode));
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// 释放资源并重置状态。
        /// </summary>
        public override void Dispose()
        {
            _messagePacketLength = 0;
            Array.Clear(_messageHead, 0, _messageHead.Length);
            base.Dispose();
        }
    }
}