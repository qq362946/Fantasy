using System;
using System.IO;
using System.Runtime.CompilerServices;
using MessagePack;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy
{
    public abstract class ReadOnlyMemoryPacketParser : APacketParser
    {
        protected APackInfo PackInfo;

        protected int Offset;
        protected int MessageHeadOffset;
        protected int MessageBodyOffset;
        protected int MessagePacketLength;
        protected bool IsUnPackHead = true;
        protected readonly byte[] MessageHead = new byte[20];
        public ReadOnlyMemoryPacketParser() { }

        public abstract bool UnPack(ref ReadOnlyMemory<byte> buffer, out APackInfo packInfo);

        public override void Dispose()
        {
            Offset = 0;
            MessageHeadOffset = 0;
            MessageBodyOffset = 0;
            MessagePacketLength = 0;
            IsUnPackHead = true;
            PackInfo = null;
            Array.Clear(MessageHead, 0, 20);
            base.Dispose();
        }
    }

#if FANTASY_NET
    public sealed class InnerReadOnlyMemoryPacketParser : ReadOnlyMemoryPacketParser
    {
        public override unsafe bool UnPack(ref ReadOnlyMemory<byte> buffer, out APackInfo packInfo)
        {
            packInfo = null;
            var readOnlySpan = buffer.Span;
            var bufferLength = buffer.Length - Offset;
            
            if (bufferLength == 0)
            {
                // 没有剩余的数据需要处理、等待下一个包再处理。
                Offset = 0;
                return false;
            }
            
            if (IsUnPackHead)
            {
                fixed (byte* bufferPtr = readOnlySpan)
                fixed (byte* messagePtr = MessageHead)
                {
                    // 在当前buffer中拿到包头的数据
                    var innerPacketHeadLength = Packet.InnerPacketHeadLength - MessageHeadOffset;
                    var copyLength = Math.Min(bufferLength, innerPacketHeadLength);
                    Buffer.MemoryCopy(bufferPtr + Offset, messagePtr + MessageHeadOffset, innerPacketHeadLength, copyLength);
                    Offset += copyLength;
                    MessageHeadOffset += copyLength;
                    // 检查是否有完整包头
                    if (MessageHeadOffset == Packet.InnerPacketHeadLength)
                    {
                        // 通过指针直接读取协议编号、messagePacketLength protocolCode rpcId routeId
                        MessagePacketLength = *(int*)messagePtr;
                        // 检查消息体长度是否超出限制
                        if (MessagePacketLength > Packet.PacketBodyMaxLength)
                        {
                            throw new ScanException($"The received information exceeds the maximum limit = {MessagePacketLength}");
                        }

                        PackInfo = InnerPackInfo.Create(Network);
                        var memoryStream = PackInfo.RentMemoryStream(Packet.InnerPacketHeadLength + MessagePacketLength);
                        PackInfo.RpcId = *(uint*)(messagePtr + Packet.InnerPacketRpcIdLocation);
                        PackInfo.ProtocolCode = *(uint*)(messagePtr + Packet.PacketLength);
                        PackInfo.RouteId = *(long*)(messagePtr + Packet.InnerPacketRouteRouteIdLocation);
                        PackInfo.MessagePacketLength = MessagePacketLength;
                        memoryStream.Write(MessageHead);
                        IsUnPackHead = false;
                        bufferLength -= copyLength;
                        MessageHeadOffset = 0;
                    }
                    else
                    {
                        Offset = 0;
                        return false;
                    }
                }
            }

            if (bufferLength == 0)
            {
                // 没有剩余的数据需要处理、等待下一个包再处理。
                Offset = 0;
                return false;
            }

            // 处理包消息体
            var innerPacketBodyLength = MessagePacketLength - MessageBodyOffset;
            var copyBodyLength = Math.Min(bufferLength, innerPacketBodyLength);
            // 写入数据到消息体中
            PackInfo.MemoryStream.Write(readOnlySpan.Slice(Offset, copyBodyLength));
            Offset += copyBodyLength;
            MessageBodyOffset += copyBodyLength;
            // 检查是否是完整的消息体
            if (MessageBodyOffset == MessagePacketLength)
            {
                packInfo = PackInfo;
                PackInfo = null;
                IsUnPackHead = true;
                MessageBodyOffset = 0;
                return true;
            }
            Offset = 0;
            return false;
        }

        public override MemoryStreamBuffer Pack(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStreamBuffer memoryStream, object message)
        {
            return memoryStream == null
                ? Pack(ref rpcId, ref routeId, message)
                : Pack(ref rpcId, ref routeId, memoryStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStreamBuffer Pack(ref uint rpcId, ref long routeId, MemoryStreamBuffer memoryStream)
        {
            memoryStream.Seek(Packet.InnerPacketRpcIdLocation, SeekOrigin.Begin);
            rpcId.GetBytes(RpcIdBuffer);
            routeId.GetBytes(RouteIdBuffer);
            memoryStream.Write(RpcIdBuffer);
            memoryStream.Write(RouteIdBuffer);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStreamBuffer Pack(ref uint rpcId, ref long routeId, object message)
        {
            var messageType = message.GetType();
            var memoryStream = Network.RentMemoryStream();
            memoryStream.Seek(Packet.InnerPacketHeadLength, SeekOrigin.Begin);

            switch (message)
            {
                case IBsonMessage:
                {
                    BsonPackHelper.Serialize(messageType, message, memoryStream);
                    break;
                }
                default:
                {
                    MessagePackHelper.Serialize(messageType, message, memoryStream);
                    break;
                }
            }

            var opCode = Scene.MessageDispatcherComponent.GetOpCode(messageType);
            var packetBodyCount = (int)(memoryStream.Length - Packet.InnerPacketHeadLength);
            
            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
                // 检查消息体长度是否超出限制
                throw new Exception($"Message content exceeds {Packet.PacketBodyMaxLength} bytes");
            }

            rpcId.GetBytes(RpcIdBuffer);
            opCode.GetBytes(OpCodeBuffer);
            routeId.GetBytes(RouteIdBuffer);
            packetBodyCount.GetBytes(BodyBuffer);
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Write(BodyBuffer);
            memoryStream.Write(OpCodeBuffer);
            memoryStream.Write(RpcIdBuffer);
            memoryStream.Write(RouteIdBuffer);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
    }
#endif
    public sealed class OuterReadOnlyMemoryPacketParser : ReadOnlyMemoryPacketParser
    {
        public override unsafe bool UnPack(ref ReadOnlyMemory<byte> buffer, out APackInfo packInfo)
        {
            packInfo = null;
            var readOnlySpan = buffer.Span;
            var bufferLength = buffer.Length - Offset;
            
            if (bufferLength == 0)
            {
                // 没有剩余的数据需要处理、等待下一个包再处理。
                Offset = 0;
                return false;
            }
            
            if (IsUnPackHead)
            {
                fixed (byte* bufferPtr = readOnlySpan)
                fixed (byte* messagePtr = MessageHead)
                {
                    // 在当前buffer中拿到包头的数据
                    var outerPacketHeadLength = Packet.OuterPacketHeadLength - MessageHeadOffset;
                    var copyLength = Math.Min(bufferLength, outerPacketHeadLength);
                    Buffer.MemoryCopy(bufferPtr + Offset, messagePtr + MessageHeadOffset, outerPacketHeadLength, copyLength);
                    Offset += copyLength;
                    MessageHeadOffset += copyLength;
                    // 检查是否有完整包头
                    if (MessageHeadOffset == Packet.OuterPacketHeadLength)
                    {
                        // 通过指针直接读取协议编号、messagePacketLength protocolCode rpcId routeId
                        MessagePacketLength = *(int*)messagePtr;
                        // 检查消息体长度是否超出限制
                        if (MessagePacketLength > Packet.PacketBodyMaxLength)
                        {
                            throw new ScanException($"The received information exceeds the maximum limit = {MessagePacketLength}");
                        }

                        PackInfo = OuterPackInfo.Create(Network);
                        var memoryStream = PackInfo.RentMemoryStream(Packet.OuterPacketHeadLength + MessagePacketLength);
                        PackInfo.MessagePacketLength = MessagePacketLength;
                        PackInfo.ProtocolCode = *(uint*)(messagePtr + Packet.PacketLength);
                        PackInfo.RpcId = *(uint*)(messagePtr + Packet.OuterPacketRpcIdLocation);
                        PackInfo.RouteTypeCode = *(long*)(messagePtr + Packet.OuterPacketRouteTypeOpCodeLocation);
                        memoryStream.Write(MessageHead);
                        IsUnPackHead = false;
                        bufferLength -= copyLength;
                        MessageHeadOffset = 0;
                    }
                    else
                    {
                        Offset = 0;
                        return false;
                    }
                }
            }

            if (bufferLength == 0)
            {
                // 没有剩余的数据需要处理、等待下一个包再处理。
                Offset = 0;
                return false;
            }
            // 处理包消息体
            var outerPacketBodyLength = MessagePacketLength - MessageBodyOffset;
            var copyBodyLength = Math.Min(bufferLength, outerPacketBodyLength);
            // 写入数据到消息体中
            PackInfo.MemoryStream.Write(readOnlySpan.Slice(Offset, copyBodyLength));
            Offset += copyBodyLength;
            MessageBodyOffset += copyBodyLength;
            // 检查是否是完整的消息体
            if (MessageBodyOffset == MessagePacketLength)
            {
                packInfo = PackInfo;
                PackInfo = null;
                IsUnPackHead = true;
                MessageBodyOffset = 0;
                return true;
            }

            Offset = 0;
            return false;
        }

        public override MemoryStreamBuffer Pack(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStreamBuffer memoryStream, object message)
        {
            return memoryStream == null
                ? Pack(ref rpcId, ref routeTypeOpCode, message)
                : Pack(ref rpcId, ref routeTypeOpCode, memoryStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStreamBuffer Pack(ref uint rpcId, ref long routeTypeOpCode, MemoryStreamBuffer memoryStream)
        {
            memoryStream.Seek(Packet.OuterPacketRpcIdLocation, SeekOrigin.Begin);
            rpcId.GetBytes(RpcIdBuffer);
            routeTypeOpCode.GetBytes(PackRouteTypeOpCode);
            memoryStream.Write(RpcIdBuffer);
            memoryStream.Write(PackRouteTypeOpCode);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStreamBuffer Pack(ref uint rpcId, ref long routeTypeOpCode, object message)
        {
            var messageType = message.GetType();
            var memoryStream = Network.RentMemoryStream();
            memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
            MessagePackHelper.Serialize(messageType, message, memoryStream);
            var opCode = Scene.MessageDispatcherComponent.GetOpCode(messageType);
            var packetBodyCount = (int)(memoryStream.Length - Packet.OuterPacketHeadLength);
            
            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
                // 检查消息体长度是否超出限制
                throw new Exception($"Message content exceeds {Packet.PacketBodyMaxLength} bytes");
            }

            rpcId.GetBytes(RpcIdBuffer);
            opCode.GetBytes(OpCodeBuffer);
            packetBodyCount.GetBytes(BodyBuffer);
            routeTypeOpCode.GetBytes(PackRouteTypeOpCode);
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Write(BodyBuffer);
            memoryStream.Write(OpCodeBuffer);
            memoryStream.Write(RpcIdBuffer);
            memoryStream.Write(PackRouteTypeOpCode);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
    }
}