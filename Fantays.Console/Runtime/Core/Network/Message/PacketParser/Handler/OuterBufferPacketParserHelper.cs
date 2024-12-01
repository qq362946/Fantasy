#if FANTASY_NET
using System.Runtime.CompilerServices;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

namespace Fantasy.PacketParser
{
    /// <summary>
    /// 打包Outer消息的帮助类
    /// </summary>
    public static class OuterBufferPacketParserHelper
    {
        /// <summary>
        /// 打包一个网络消息
        /// </summary>
        /// <param name="scene">scene</param>
        /// <param name="rpcId">如果是RPC消息需要传递一个rpcId</param>
        /// <param name="message">打包的网络消息</param>
        /// <param name="memoryStreamLength">序列化后流的长度</param>
        /// <returns>打包完成会返回一个MemoryStreamBuffer</returns>
        /// <exception cref="Exception"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe MemoryStreamBuffer Pack(Scene scene, uint rpcId, IMessage message, out int memoryStreamLength)
        {
            memoryStreamLength = 0;
            var messageType = message.GetType();
            var memoryStream = new MemoryStreamBuffer();
            memoryStream.MemoryStreamBufferSource = MemoryStreamBufferSource.Pack;
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

            var opCode = scene.MessageDispatcherComponent.GetOpCode(messageType);
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

#endif