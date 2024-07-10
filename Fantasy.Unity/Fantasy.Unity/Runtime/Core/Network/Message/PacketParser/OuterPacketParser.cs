using System;
using System.Buffers;
using System.IO;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 用于解析外部网络消息的数据包信息。
    /// </summary>
    public sealed class OuterPackInfo : APackInfo
    {
        /// <summary>
        /// 释放当前 <see cref="OuterPackInfo"/> 实例及其关联的资源。
        /// </summary>
        public override void Dispose()
        {
            // 将当前的 OuterPackInfo 实例归还到对象池，以便重复利用
            MultiThreadPool.Return(this);
            base.Dispose();
        }

        /// <summary>
        /// 创建一个 <see cref="OuterPackInfo"/> 实例，并将其与给定的内存资源关联。
        /// </summary>
        /// <param name="memoryStream">内存资源的所有者。</param>
        /// <returns>创建的 <see cref="OuterPackInfo"/> 实例。</returns>
        public static OuterPackInfo Create(MemoryStream memoryStream)
        {
            var outerPackInfo = MultiThreadPool.Rent<OuterPackInfo>();
            outerPackInfo.IsDisposed = false;
            outerPackInfo.MemoryStream = memoryStream;
            return outerPackInfo;
        }

        /// <summary>
        /// 创建一个 <see cref="MemoryStream"/> 实例，用于存储内存数据，并返回该实例。
        /// </summary>
        /// <returns>创建的 <see cref="MemoryStream"/> 实例。</returns>
        public override MemoryStream CreateMemoryStream()
        {
            // // 创建可回收的内存流，用于存储消息数据
            // var recyclableMemoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
            // // 将内存资源中的消息数据写入内存流
            // // 写入从内存起始位置到消息头长度+消息体长度的数据
            // recyclableMemoryStream.Write(MemoryStream.GetBuffer().AsSpan().Slice(0, Packet.InnerPacketHeadLength + MessagePacketLength));
            // 将内存流的指针定位到起始位置
            MemoryStream.Seek(0, SeekOrigin.Begin);
            // 返回创建的内存流
            return MemoryStream;
        }

        /// <summary>
        /// 将消息数据从内存反序列化为指定的消息类型实例。
        /// </summary>
        /// <param name="messageType">目标消息类型。</param>
        /// <returns>反序列化后的消息类型实例。</returns>
        public override object Deserialize(Type messageType)
        {
            MemoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
            var @object = ProtoBuffHelper.FromStream(messageType, MemoryStream);
            MemoryStream.Seek(0, SeekOrigin.Begin);
            return @object;
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
        private readonly byte[] _packRpcId = new byte[4];
        private readonly byte[] _packOpCode = new byte[4];
        private readonly byte[] _packPacketBodyCount = new byte[4];
        private readonly byte[] _packRouteTypeOpCode = new byte[8];
        private readonly byte[] _messageHead = new byte[Packet.OuterPacketHeadLength];

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
                    if (_messagePacketLength < 0 || buffer.Length < _messagePacketLength)
                    {
                        return false;
                    }
                    
                    _isUnPackHead = true;
                    // 创建消息包
                    var memoryStream = new MemoryStream(_messagePacketLength);
                    // 写入消息体的信息到内存中
                    buffer.Read(memoryStream, _messagePacketLength);
                    packInfo = OuterPackInfo.Create(memoryStream);
                    packInfo.RpcId = _rpcId;
                    packInfo.ProtocolCode = _protocolCode;
                    packInfo.RouteTypeCode = _routeTypeCode;
                    packInfo.MessagePacketLength = _messagePacketLength;
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return true;
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
        /// <param name="buffer">需要解包的buffer。</param>
        /// <param name="count">解包的总长度。</param>
        /// <param name="packInfo">解析得到的数据包信息。</param>
        /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
        public override bool UnPack(byte[] buffer, ref int count, out APackInfo packInfo)
        {
            packInfo = null;

            try
            {
                if (count < Packet.OuterPacketHeadLength)
                {
                    return false;
                }

                _messagePacketLength = BitConverter.ToInt32(buffer);
#if FANTASY_NET
                if (_messagePacketLength > Packet.PacketBodyMaxLength)
                {
                    throw new ScanException(
                        $"The received information exceeds the maximum limit = {_messagePacketLength}");
                }
#endif
                if (_messagePacketLength < 0 || count < _messagePacketLength)
                {
                    return false;
                }

                var newMemoryStream = new MemoryStream(count);
                newMemoryStream.Write(buffer, 0, count);
                packInfo = OuterPackInfo.Create(newMemoryStream);
                packInfo.MessagePacketLength = _messagePacketLength;
                packInfo.ProtocolCode = BitConverter.ToUInt32(buffer, Packet.PacketLength);
                packInfo.RpcId = BitConverter.ToUInt32(buffer, Packet.OuterPacketRpcIdLocation);
                packInfo.RouteTypeCode = BitConverter.ToUInt16(buffer, Packet.OuterPacketRouteTypeOpCodeLocation);
                newMemoryStream.Seek(0, SeekOrigin.Begin);
                return true;
            }
            catch (Exception e)
            {
                packInfo?.Dispose();
                Log.Error(e);
                return false;
            }
        }

        public override MemoryStream Pack(uint rpcId, long routeTypeOpCode, long routeId, MemoryStream memoryStream, object message)
        {
            // 根据是否提供内存流打包消息
            return memoryStream == null
                ? Pack(rpcId, routeTypeOpCode, message)
                : Pack(rpcId, routeTypeOpCode, memoryStream);
        }

        /// <summary>
        /// 封装数据包。
        /// </summary>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeTypeOpCode">路由类型和操作码。</param>
        /// <param name="memoryStream">要封装的内存流。</param>
        /// <returns>封装后的内存流。</returns>
        public MemoryStream Pack(uint rpcId, long routeTypeOpCode, MemoryStream memoryStream)
        {
            rpcId.GetBytes(_packRpcId);
            routeTypeOpCode.GetBytes(_packRouteTypeOpCode);

            memoryStream.Seek(Packet.OuterPacketRpcIdLocation, SeekOrigin.Begin);
            memoryStream.Write(_packRpcId);
            memoryStream.Write(_packRouteTypeOpCode);
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
        public MemoryStream Pack(uint rpcId, long routeTypeOpCode, object message)
        {
            var opCode = Opcode.PingRequest;
            var packetBodyCount = 0;
            var memoryStream = new MemoryStream();
            memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);

            if (message != null)
            {
                ProtoBuffHelper.ToStream(message, memoryStream);
                opCode = MessageDispatcherComponent.GetOpCode(message.GetType());
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

            rpcId.GetBytes(_packRpcId);
            opCode.GetBytes(_packOpCode);
            packetBodyCount.GetBytes(_packPacketBodyCount);
            routeTypeOpCode.GetBytes(_packRouteTypeOpCode);
            
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Write(_packPacketBodyCount);
            memoryStream.Write(_packOpCode);
            memoryStream.Write(_packRpcId);
            memoryStream.Write(_packRouteTypeOpCode);
            memoryStream.Seek(0, SeekOrigin.Begin);
            
            return memoryStream;
        }

        /// <summary>
        /// 释放资源并重置状态。
        /// </summary>
        public override void Dispose()
        {
            _rpcId = 0;
            _protocolCode = 0;
            _routeTypeCode = 0;
            _isUnPackHead = true;
            _messagePacketLength = 0;
            Array.Clear(_messageHead, 0, _messageHead.Length);
            base.Dispose();
        }
    }
}