using System;
using System.IO;
using System.Runtime.CompilerServices;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy
{
    // 这个不会用在TCP协议中、因此不用考虑分包和粘包的问题。
    // 目前这个只会用在KCP协议中、因为KCP出来的就是一个完整的包、所以可以一次性全部解析出来。
    // 如果是用在其他协议上可能会出现问题。
    public abstract class BufferPacketParser : APacketParser
    {
        protected uint RpcId;
        protected long RouteId;
        protected uint ProtocolCode;
        protected long RouteTypeCode;
        protected int MessagePacketLength;
        public override void Dispose()
        {
            RpcId = 0;
            RouteId = 0;
            ProtocolCode = 0;
            RouteTypeCode = 0;
            MessagePacketLength = 0;
            base.Dispose();
        }

        public abstract bool UnPack(byte[] buffer, ref int count, out APackInfo packInfo);
    }
#if FANTASY_NET
    public sealed class InnerBufferPacketParser : BufferPacketParser
    {
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
            var memoryStream = packInfo.RentMemoryStream(count);
            memoryStream.Write(buffer, 0, count);
            packInfo.RpcId = RpcId;
            packInfo.RouteId = RouteId;
            packInfo.ProtocolCode = ProtocolCode;
            packInfo.MessagePacketLength = MessagePacketLength;
            return true;
        }
        
        public override MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message)
        {
            return memoryStream == null ? Pack(ref rpcId, ref routeId, message) : Pack(ref rpcId, ref routeId, memoryStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStream Pack(ref uint rpcId, ref long routeId, MemoryStream memoryStream)
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
        private MemoryStream Pack(ref uint rpcId, ref long routeId, object message)
        {
            var memoryStream = Network.RentMemoryStream();
            memoryStream.Seek(Packet.InnerPacketHeadLength, SeekOrigin.Begin);

            switch (message)
            {
                case IBsonMessage:
                {
                    MongoHelper.SerializeTo(message, memoryStream);
                    break;
                }
                default:
                {
                    ProtoBuffHelper.ToStream(message, memoryStream);
                    break;
                }
            }

            var opCode = Scene.MessageDispatcherComponent.GetOpCode(message.GetType());
            var packetBodyCount = (int)(memoryStream.Position - Packet.InnerPacketHeadLength);
            
            if (packetBodyCount == 0)
            {
                // protoBuf做了一个优化、就是当序列化的对象里的属性和字段都为默认值的时候就不会序列化任何东西。
                // 为了TCP的分包和粘包、需要判定下是当前包数据不完整还是本应该如此、所以用-1代表。
                packetBodyCount = -1;
            }

            // 检查消息体长度是否超出限制
            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
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
    public sealed class OuterBufferPacketParser : BufferPacketParser
    {
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
                MessagePacketLength = *(int*)bufferPtr;

                if (MessagePacketLength > Packet.PacketBodyMaxLength || count < MessagePacketLength)
                {
                    // 检查消息体长度是否超出限制
                    throw new ScanException($"The received information exceeds the maximum limit = {MessagePacketLength}");
                }
                
                ProtocolCode = *(uint*)(bufferPtr + Packet.PacketLength);
                RpcId = *(uint*)(bufferPtr + Packet.OuterPacketRpcIdLocation);
                RouteTypeCode = *(long*)(bufferPtr + Packet.OuterPacketRouteTypeOpCodeLocation);
            }
            
            packInfo = OuterPackInfo.Create(Network);
            var memoryStream = packInfo.RentMemoryStream(count);
            memoryStream.Write(buffer, 0, count);
            packInfo.RpcId = RpcId;
            packInfo.ProtocolCode = ProtocolCode;
            packInfo.RouteTypeCode = RouteTypeCode;
            packInfo.MessagePacketLength = MessagePacketLength;
            return true;
        }
        
        public override MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message)
        {
            return memoryStream == null ? Pack(ref rpcId, ref routeTypeOpCode, message) : Pack(ref rpcId, ref routeTypeOpCode, memoryStream);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, MemoryStream memoryStream)
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
        private MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, object message)
        {
            var memoryStream = Network.RentMemoryStream();
            memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
            ProtoBuffHelper.ToStream(message, memoryStream);
            var opCode = Scene.MessageDispatcherComponent.GetOpCode(message.GetType());
            var packetBodyCount = (int)(memoryStream.Position - Packet.OuterPacketHeadLength);

            if (packetBodyCount == 0)
            {
                // protoBuf做了一个优化、就是当序列化的对象里的属性和字段都为默认值的时候就不会序列化任何东西。
                // 为了TCP的分包和粘包、需要判定下是当前包数据不完整还是本应该如此、所以用-1代表。
                packetBodyCount = -1;
            }

            // 检查消息体长度是否超出限制
            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
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
    
    public sealed class OuterWebglBufferPacketParser : BufferPacketParser
    {
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
            RouteTypeCode = BitConverter.ToUInt16(buffer, Packet.OuterPacketRouteTypeOpCodeLocation);
            
            packInfo = OuterPackInfo.Create(Network);
            var memoryStream = packInfo.RentMemoryStream(count);
            memoryStream.Write(buffer, 0, count);
            packInfo.RpcId = RpcId;
            packInfo.ProtocolCode = ProtocolCode;
            packInfo.RouteTypeCode = RouteTypeCode;
            packInfo.MessagePacketLength = MessagePacketLength;
            return true;
        }
        
        public override MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message)
        {
            return memoryStream == null ? Pack(ref rpcId, ref routeTypeOpCode, message) : Pack(ref rpcId, ref routeTypeOpCode, memoryStream);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, MemoryStream memoryStream)
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
        private MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, object message)
        {
            var memoryStream = Network.RentMemoryStream();
            memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
            ProtoBuffHelper.ToStream(message, memoryStream);
            var opCode = Scene.MessageDispatcherComponent.GetOpCode(message.GetType());
            var packetBodyCount = (int)(memoryStream.Position - Packet.OuterPacketHeadLength);

            if (packetBodyCount == 0)
            {
                // protoBuf做了一个优化、就是当序列化的对象里的属性和字段都为默认值的时候就不会序列化任何东西。
                // 为了TCP的分包和粘包、需要判定下是当前包数据不完整还是本应该如此、所以用-1代表。
                packetBodyCount = -1;
            }

            // 检查消息体长度是否超出限制
            if (packetBodyCount > Packet.PacketBodyMaxLength)
            {
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