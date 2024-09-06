#if FANTASY_NET
using System.Runtime.CompilerServices;
namespace Fantasy;

public static class OuterBufferPacketParserHelper
{
    private static readonly byte[] StaticBodyBuffer = new byte[sizeof(int)];
    private static readonly byte[] StaticRpcIdBuffer = new byte[sizeof(uint)];
    private static readonly byte[] StaticOpCodeBuffer = new byte[sizeof(uint)];
    private static readonly byte[] StaticPackRouteIdBuffer = new byte[sizeof(long)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryStreamBuffer Pack(Scene scene, uint rpcId, IMessage message)
    {
        var memoryStreamLength = 0;
        var messageType = message.GetType();
        var memoryStream = new MemoryStreamBuffer();
        OpCodeIdStruct opCodeIdStruct = message.OpCode();
        memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
        MemoryPackHelper.Serialize(messageType, message, memoryStream);
        
        switch (opCodeIdStruct.OpCodeProtocolType)
        {
            case OpCodeProtocolType.ProtoBuf:
            {
                ProtoBufPackHelper.Serialize(messageType, message, memoryStream);
                memoryStreamLength = (int)memoryStream.Position;
                break;
            }
            case OpCodeProtocolType.MemoryPack:
            {
                MemoryPackHelper.Serialize(messageType, message, memoryStream);
                memoryStreamLength = (int)memoryStream.Length;
                break;
            }
            case OpCodeProtocolType.Bson:
            {
                BsonPackHelper.Serialize(messageType, message, memoryStream);
                memoryStreamLength = (int)memoryStream.Length;
                break;
            }
        }
        
        var opCode = scene.MessageDispatcherComponent.GetOpCode(messageType);
        var packetBodyCount = memoryStreamLength- Packet.OuterPacketHeadLength;
        
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

        rpcId.GetBytes(StaticRpcIdBuffer);
        opCode.GetBytes(StaticOpCodeBuffer);
        packetBodyCount.GetBytes(StaticBodyBuffer);
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.Write(StaticBodyBuffer);
        memoryStream.Write(StaticOpCodeBuffer);
        memoryStream.Write(StaticRpcIdBuffer);
        memoryStream.Write(StaticPackRouteIdBuffer);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}
#endif