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
    public static MemoryStream Pack(Scene scene, uint rpcId, long routeTypeOpCode, object message)
    {
        var memoryStream = new MemoryStream();
        memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
        ProtoBuffHelper.ToStream(message, memoryStream);
        var opCode = scene.MessageDispatcherComponent.GetOpCode(message.GetType());
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