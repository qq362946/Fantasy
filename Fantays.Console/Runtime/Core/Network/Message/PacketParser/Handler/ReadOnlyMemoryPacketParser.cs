using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.PacketParser.Interface;
using Fantasy.Serialize;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.PacketParser
{
    internal abstract class ReadOnlyMemoryPacketParser : APacketParser
    {
        /// <summary>
        /// 一个网络消息包
        /// </summary>
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
    internal sealed class InnerReadOnlyMemoryPacketParser : ReadOnlyMemoryPacketParser
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
                        var memoryStream = PackInfo.RentMemoryStream(MemoryStreamBufferSource.UnPack, Packet.InnerPacketHeadLength + MessagePacketLength);
                        PackInfo.RpcId = *(uint*)(messagePtr + Packet.InnerPacketRpcIdLocation);
                        PackInfo.ProtocolCode = *(uint*)(messagePtr + Packet.PacketLength);
                        PackInfo.RouteId = *(long*)(messagePtr + Packet.InnerPacketRouteRouteIdLocation);
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

            if (MessagePacketLength == -1)
            {
                // protoBuf做了一个优化、就是当序列化的对象里的属性和字段都为默认值的时候就不会序列化任何东西。
                // 为了TCP的分包和粘包、需要判定下是当前包数据不完整还是本应该如此、所以用-1代表。
                packInfo = PackInfo;
                PackInfo = null;
                IsUnPackHead = true;
                return true;
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

        public override MemoryStreamBuffer Pack(ref uint rpcId, ref long routeId, MemoryStreamBuffer memoryStream, IMessage message)
        {
            return memoryStream == null ? Pack(ref rpcId, ref routeId, message) : Pack(ref rpcId, ref routeId, memoryStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe MemoryStreamBuffer Pack(ref uint rpcId, ref long routeId, MemoryStreamBuffer memoryStream)
        {
            fixed (byte* bufferPtr = memoryStream.GetBuffer())
            {
                *(uint*)(bufferPtr + Packet.InnerPacketRpcIdLocation) = rpcId;
                *(long*)(bufferPtr + Packet.InnerPacketRouteRouteIdLocation) = routeId;
            }
            
            return memoryStream;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe MemoryStreamBuffer Pack(ref uint rpcId, ref long routeId, IMessage message)
        {
            var memoryStreamLength = 0;
            var messageType = message.GetType();
            var memoryStream = Network.MemoryStreamBufferPool.RentMemoryStream(MemoryStreamBufferSource.Pack);
            OpCodeIdStruct opCodeIdStruct = message.OpCode();
            memoryStream.Seek(Packet.InnerPacketHeadLength, SeekOrigin.Begin);

            if (SerializerManager.TryGetSerializer(opCodeIdStruct.OpCodeProtocolType, out var serializer))
            {
                serializer.Serialize(messageType, message, memoryStream);
                memoryStreamLength = (int)memoryStream.Position;
            }
            else
            {
                Log.Error($"type:{messageType} Does not support processing protocol");
            }

            var opCode = Scene.MessageDispatcherComponent.GetOpCode(messageType);
            var packetBodyCount = memoryStreamLength - Packet.InnerPacketHeadLength;
            
            if (packetBodyCount == 0)
            {
                // protoBuf做了一个优化、就是当序列化的对象里的属性和字段都为默认值的时候就不会序列化任何东西。
                // 为了TCP的分包和粘包、需要判定下是当前包数据不完整还是本应该如此、所以用-1代表。
                // 其实可以不用设置-1、解包的时候判断如果是0也可以、但我仔细想了下，还是用-1代表更加清晰。
                packetBodyCount = -1;
            }
            
            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
                // 检查消息体长度是否超出限制
                throw new Exception($"Message content exceeds {Packet.PacketBodyMaxLength} bytes");
            }
            
            fixed (byte* bufferPtr = memoryStream.GetBuffer())
            {
                *(int*)bufferPtr = packetBodyCount;
                *(uint*)(bufferPtr + Packet.PacketLength) = opCode;
                *(uint*)(bufferPtr + Packet.InnerPacketRpcIdLocation) = rpcId;
                *(long*)(bufferPtr + Packet.InnerPacketRouteRouteIdLocation) = routeId;
            }
            
            return memoryStream;
        }
    }
#endif
    internal sealed class OuterReadOnlyMemoryPacketParser : ReadOnlyMemoryPacketParser
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
                        PackInfo.ProtocolCode = *(uint*)(messagePtr + Packet.PacketLength);
                        PackInfo.RpcId = *(uint*)(messagePtr + Packet.OuterPacketRpcIdLocation);
                        var memoryStream = PackInfo.RentMemoryStream(MemoryStreamBufferSource.UnPack, Packet.OuterPacketHeadLength + MessagePacketLength);
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
            
            if (MessagePacketLength == -1)
            {
                // protoBuf做了一个优化、就是当序列化的对象里的属性和字段都为默认值的时候就不会序列化任何东西。
                // 为了TCP的分包和粘包、需要判定下是当前包数据不完整还是本应该如此、所以用-1代表。
                packInfo = PackInfo;
                PackInfo = null;
                IsUnPackHead = true;
                return true;
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

        public override MemoryStreamBuffer Pack(ref uint rpcId, ref long routeId, MemoryStreamBuffer memoryStream, IMessage message)
        {
            return memoryStream == null ? Pack(ref rpcId, message) : Pack(ref rpcId, memoryStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe MemoryStreamBuffer Pack(ref uint rpcId, MemoryStreamBuffer memoryStream)
        {
            fixed (byte* bufferPtr = memoryStream.GetBuffer())
            {
                *(uint*)(bufferPtr + Packet.OuterPacketRpcIdLocation) = rpcId;
            }
            
            return memoryStream;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe MemoryStreamBuffer Pack(ref uint rpcId, IMessage message)
        {
            var memoryStreamLength = 0;
            var messageType = message.GetType();
            var memoryStream = Network.MemoryStreamBufferPool.RentMemoryStream(MemoryStreamBufferSource.Pack);
            OpCodeIdStruct opCodeIdStruct = message.OpCode();
            memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
            
            if (SerializerManager.TryGetSerializer(opCodeIdStruct.OpCodeProtocolType, out var serializer))
            {
                serializer.Serialize(messageType, message, memoryStream);
                memoryStreamLength = (int)memoryStream.Position;
            }
            else
            {
                Log.Error($"type:{messageType} Does not support processing protocol");
            }
            
            var opCode = Scene.MessageDispatcherComponent.GetOpCode(messageType);
            var packetBodyCount = memoryStreamLength - Packet.OuterPacketHeadLength;

            if (packetBodyCount == 0)
            {
                // protoBuf做了一个优化、就是当序列化的对象里的属性和字段都为默认值的时候就不会序列化任何东西。
                // 为了TCP的分包和粘包、需要判定下是当前包数据不完整还是本应该如此、所以用-1代表。
                packetBodyCount = -1;
            }
            
            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
                // 检查消息体长度是否超出限制
                throw new Exception($"Message content exceeds {Packet.PacketBodyMaxLength} bytes");
            }
            
            fixed (byte* bufferPtr = memoryStream.GetBuffer())
            {
                *(int*)bufferPtr = packetBodyCount;
                *(uint*)(bufferPtr + Packet.PacketLength) = opCode;
                *(uint*)(bufferPtr + Packet.OuterPacketRpcIdLocation) = rpcId;
            }
            
            return memoryStream;
        }
    }
}