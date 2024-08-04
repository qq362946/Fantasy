#if FANTASY_NET
using System.Runtime.CompilerServices;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
namespace Fantasy;

public static class InnerPacketParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe MemoryStream MessagePack(uint rpcId, long routeId, MemoryStream memoryStream)
    {
        var buffer = memoryStream.GetBuffer();
        fixed (byte* bufferPtr = buffer)
        {
            var rpcIdPtr = bufferPtr + Packet.InnerPacketRpcIdLocation;
            var routeIdPtr = bufferPtr + Packet.InnerPacketRouteRouteIdLocation;
            *(uint*)rpcIdPtr = rpcId;
            *(long*)routeIdPtr = routeId;
        }
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe MemoryStream MessagePack(Scene scene, uint rpcId, long routeId, object message)
    {
        var memoryStream = new MemoryStream();
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
        
        var opCode = scene.MessageDispatcherComponent.GetOpCode(message.GetType());
        var packetBodyCount = (int)(memoryStream.Position - Packet.InnerPacketHeadLength);
        
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
        
        var buffer = memoryStream.GetBuffer();

        fixed (byte* bufferPtr = buffer)
        {
            var opCodePtr = bufferPtr + Packet.PacketLength;
            var rpcIdPtr = bufferPtr + Packet.InnerPacketRpcIdLocation;
            var routeIdPtr = bufferPtr + Packet.InnerPacketRouteRouteIdLocation;
            *(int*)bufferPtr = packetBodyCount;
            *(uint*)opCodePtr = opCode;
            *(uint*)rpcIdPtr = rpcId;
            *(long*)routeIdPtr = routeId;
        }
        
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}
#endif
