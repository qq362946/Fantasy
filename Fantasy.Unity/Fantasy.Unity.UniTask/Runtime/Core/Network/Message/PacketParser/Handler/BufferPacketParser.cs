using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.PacketParser.Interface;
using Fantasy.Serialize;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy.PacketParser
{
    /// <summary>
    /// BufferPacketParser消息格式化器抽象类
    /// 这个不会用在TCP协议中、因此不用考虑分包和粘包的问题。
    /// 目前这个只会用在KCP协议中、因为KCP出来的就是一个完整的包、所以可以一次性全部解析出来。
    /// 如果是用在其他协议上可能会出现问题。
    /// </summary>
    public abstract class BufferPacketParser : APacketParser
    {
        protected uint RpcId;
        protected long RouteId;
        protected uint ProtocolCode;
        protected int MessagePacketLength;
        public override void Dispose()
        {
            RpcId = 0;
            RouteId = 0;
            ProtocolCode = 0;
            MessagePacketLength = 0;
            base.Dispose();
        }
        /// <summary>
        /// 解包方法
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="count">count</param>
        /// <param name="packInfo">packInfo</param>
        /// <returns></returns>
        public abstract bool UnPack(byte[] buffer, ref int count, out APackInfo packInfo);
    }
#if FANTASY_NET
    /// <summary>
    /// 服务器之间专用的BufferPacketParser消息格式化器
    /// </summary>
    public sealed class InnerBufferPacketParser : BufferPacketParser
    {
        /// <summary>
        /// <see cref="BufferPacketParser.UnPack"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"></param>
        /// <param name="packInfo"></param>
        /// <returns></returns>
        /// <exception cref="ScanException"></exception>
        public override unsafe bool UnPack(byte[] buffer, ref int count, out APackInfo packInfo)
        {
            packInfo = null;

            if (buffer.Length < count)
            {
                throw new ScanException($"The buffer length is less than the specified count. buffer.Length={buffer.Length} count={count}");
            }
            
            if (count < Packet.InnerPacketHeadLength)
            {
                // 如果内存资源中的数据长度小于内部消息头的长度，无法解析
                return false;
            }
            
            fixed (byte* bufferPtr = buffer)
            {
                MessagePacketLength = *(int*)bufferPtr;

                if (MessagePacketLength > Packet.PacketBodyMaxLength || count < MessagePacketLength)
                {
                    // 检查消息体长度是否超出限制
                    throw new ScanException($"The received information exceeds the maximum limit = {MessagePacketLength}");
                }
                
                ProtocolCode = *(uint*)(bufferPtr + Packet.PacketLength);
                RpcId = *(uint*)(bufferPtr + Packet.InnerPacketRpcIdLocation);
                RouteId = *(long*)(bufferPtr + Packet.InnerPacketRouteRouteIdLocation);
            }
            
            packInfo = InnerPackInfo.Create(Network);
            packInfo.RpcId = RpcId;
            packInfo.RouteId = RouteId;
            packInfo.ProtocolCode = ProtocolCode;
            packInfo.RentMemoryStream(MemoryStreamBufferSource.UnPack, count).Write(buffer, 0, count);
            return true;
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
    /// <summary>
    /// 客户端和服务器之间专用的BufferPacketParser消息格式化器
    /// </summary>
    public sealed class OuterBufferPacketParser : BufferPacketParser
    {
        /// <summary>
        /// <see cref="BufferPacketParser.UnPack"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"></param>
        /// <param name="packInfo"></param>
        /// <returns></returns>
        /// <exception cref="ScanException"></exception>
        public override unsafe bool UnPack(byte[] buffer, ref int count, out APackInfo packInfo)
        {
            packInfo = null;

            if (buffer.Length < count)
            {
                throw new ScanException($"The buffer length is less than the specified count. buffer.Length={buffer.Length} count={count}");
            }
            
            if (count < Packet.OuterPacketHeadLength)
            {
                // 如果内存资源中的数据长度小于内部消息头的长度，无法解析
                return false;
            }
            
            fixed (byte* bufferPtr = buffer)
            {
                MessagePacketLength =  *(int*)bufferPtr;

                if (MessagePacketLength > Packet.PacketBodyMaxLength || count < MessagePacketLength)
                {
                    // 检查消息体长度是否超出限制
                    throw new ScanException($"The received information exceeds the maximum limit = {MessagePacketLength}");
                }

                ProtocolCode = *(uint*)(bufferPtr + Packet.PacketLength);
                RpcId = *(uint*)(bufferPtr + Packet.OuterPacketRpcIdLocation);
            }
            
            packInfo = OuterPackInfo.Create(Network);
            packInfo.RpcId = RpcId;
            packInfo.ProtocolCode = ProtocolCode;
            packInfo.RentMemoryStream(MemoryStreamBufferSource.UnPack, count).Write(buffer, 0, count);
            return true;
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
                *(uint*)(bufferPtr + Packet.InnerPacketRpcIdLocation) = rpcId;
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
    /// <summary>
    /// Webgl专用的客户端和服务器之间专用的BufferPacketParser消息格式化器
    /// </summary>
    public sealed class OuterWebglBufferPacketParser : BufferPacketParser
    {
        /// <summary>
        /// <see cref="BufferPacketParser.UnPack"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"></param>
        /// <param name="packInfo"></param>
        /// <returns></returns>
        /// <exception cref="ScanException"></exception>
        public override bool UnPack(byte[] buffer, ref int count, out APackInfo packInfo)
        {
            packInfo = null;

            if (buffer.Length < count)
            {
                throw new ScanException($"The buffer length is less than the specified count. buffer.Length={buffer.Length} count={count}");
            }
            
            if (count < Packet.OuterPacketHeadLength)
            {
                // 如果内存资源中的数据长度小于内部消息头的长度，无法解析
                return false;
            }
            
            MessagePacketLength = BitConverter.ToInt32(buffer, 0);
            
            if (MessagePacketLength > Packet.PacketBodyMaxLength || count < MessagePacketLength)
            {
                // 检查消息体长度是否超出限制
                throw new ScanException($"The received information exceeds the maximum limit = {MessagePacketLength}");
            }
            
            ProtocolCode = BitConverter.ToUInt32(buffer, Packet.PacketLength);
            RpcId = BitConverter.ToUInt32(buffer, Packet.OuterPacketRpcIdLocation);
            
            packInfo = OuterPackInfo.Create(Network);
            packInfo.RpcId = RpcId;
            packInfo.ProtocolCode = ProtocolCode;
            packInfo.RentMemoryStream(MemoryStreamBufferSource.UnPack, count).Write(buffer, 0, count);
            return true;
        }
        
        public override MemoryStreamBuffer Pack(ref uint rpcId, ref long routeId, MemoryStreamBuffer memoryStream, IMessage message)
        {
            return memoryStream == null ? Pack(ref rpcId, message) : Pack(ref rpcId, memoryStream);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStreamBuffer Pack(ref uint rpcId, MemoryStreamBuffer memoryStream)
        {
            var buffer = memoryStream.GetBuffer().AsSpan();
#if FANTASY_NET
            MemoryMarshal.Write(buffer.Slice(Packet.OuterPacketRpcIdLocation, sizeof(uint)), in rpcId);
#endif
#if FANTASY_UNITY
            MemoryMarshal.Write(buffer.Slice(Packet.OuterPacketRpcIdLocation, sizeof(uint)), ref rpcId);
#endif
            return memoryStream;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStreamBuffer Pack(ref uint rpcId, IMessage message)
        {
            var memoryStreamLength = 0;
            var messageType = message.GetType();
            var memoryStream = Network.MemoryStreamBufferPool.RentMemoryStream(MemoryStreamBufferSource.UnPack);
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
            
            var buffer = memoryStream.GetBuffer().AsSpan();
#if FANTASY_NET
            MemoryMarshal.Write(buffer, in packetBodyCount);
            MemoryMarshal.Write(buffer.Slice(Packet.PacketLength, sizeof(uint)), in opCode);
            MemoryMarshal.Write(buffer.Slice(Packet.OuterPacketRpcIdLocation, sizeof(uint)), in rpcId);
#endif
#if FANTASY_UNITY
            MemoryMarshal.Write(buffer, ref packetBodyCount);
            MemoryMarshal.Write(buffer.Slice(Packet.PacketLength, sizeof(uint)), ref opCode);
            MemoryMarshal.Write(buffer.Slice(Packet.OuterPacketRpcIdLocation, sizeof(uint)), ref rpcId);
#endif
            return memoryStream;
        }
    }
}