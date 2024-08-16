#if FANTASY_NET
using System.Runtime.CompilerServices;
namespace Fantasy;

public static class OuterBufferPacketParserHelper
{
    private static readonly byte[] StaticBodyBuffer = new byte[sizeof(int)];
    private static readonly byte[] StaticRpcIdBuffer = new byte[sizeof(uint)];
    private static readonly byte[] StaticOpCodeBuffer = new byte[sizeof(uint)];
    private static readonly byte[] StaticPackRouteTypeOpCode = new byte[sizeof(long)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryStreamBuffer Pack(Scene scene, uint rpcId, long routeTypeOpCode, object message)
    {
        var messageType = message.GetType();
        var memoryStream = new MemoryStreamBuffer();
        memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
        MessagePackHelper.Serialize(messageType, message, memoryStream);
        var opCode = scene.MessageDispatcherComponent.GetOpCode(messageType);
        var packetBodyCount = (int)(memoryStream.Length - Packet.OuterPacketHeadLength);
        
        if (packetBodyCount > Packet.PacketBodyMaxLength)
        {
            // 检查消息体长度是否超出限制
            throw new Exception($"Message content exceeds {Packet.PacketBodyMaxLength} bytes");
        }

        rpcId.GetBytes(StaticRpcIdBuffer);
        opCode.GetBytes(StaticOpCodeBuffer);
        packetBodyCount.GetBytes(StaticBodyBuffer);
        routeTypeOpCode.GetBytes(StaticPackRouteTypeOpCode);
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.Write(StaticBodyBuffer);
        memoryStream.Write(StaticOpCodeBuffer);
        memoryStream.Write(StaticRpcIdBuffer);
        memoryStream.Write(StaticPackRouteTypeOpCode);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}
#endif