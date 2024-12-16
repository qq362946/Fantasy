#if FANTASY_NET
using System.Collections.Concurrent;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.PacketParser.Interface;
using Fantasy.Pool;
using Fantasy.Serialize;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
namespace Fantasy.PacketParser
{
    public sealed class ProcessPackInfo : APackInfo
    {
        private int _disposeCount;
        public Type MessageType { get; private set; }
        private static readonly ConcurrentQueue<ProcessPackInfo> Caches = new ConcurrentQueue<ProcessPackInfo>();
        private readonly ConcurrentDictionary<Type, Func<object>> _createInstances = new ConcurrentDictionary<Type, Func<object>>();

        public override void Dispose()
        {
            if (--_disposeCount > 0 || IsDisposed)
            {
                return;
            }

            _disposeCount = 0;
            MessageType = null;
            base.Dispose();

            if (Caches.Count > 2000)
            {
                return;
            }

            Caches.Enqueue(this);
        }

        public static unsafe ProcessPackInfo Create<T>(Scene scene, T message, int disposeCount, uint rpcId = 0, long routeId = 0) where T : IRouteMessage
        {
            if (!Caches.TryDequeue(out var packInfo))
            {
                packInfo = new ProcessPackInfo();
            }

            var type = typeof(T);
            var memoryStreamLength = 0;
            packInfo._disposeCount = disposeCount;
            packInfo.MessageType = type;
            packInfo.IsDisposed = false;
            var memoryStream = new MemoryStreamBuffer();
            memoryStream.MemoryStreamBufferSource = MemoryStreamBufferSource.Pack;
            OpCodeIdStruct opCodeIdStruct = message.OpCode();
            memoryStream.Seek(Packet.InnerPacketHeadLength, SeekOrigin.Begin);

            if (SerializerManager.TryGetSerializer(opCodeIdStruct.OpCodeProtocolType, out var serializer))
            {
                serializer.Serialize(type, message, memoryStream);
                memoryStreamLength = (int)memoryStream.Position;
            }
            else
            {
                Log.Error($"type:{type} Does not support processing protocol");
            }

            var opCode = scene.MessageDispatcherComponent.GetOpCode(packInfo.MessageType);
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
            packInfo.MemoryStream = memoryStream;
            return packInfo;
        }

        public unsafe void Set(uint rpcId, long routeId)
        {
            var buffer = MemoryStream.GetBuffer();

            fixed (byte* bufferPtr = buffer)
            {
                var rpcIdPtr = bufferPtr + Packet.InnerPacketRpcIdLocation;
                var routeIdPtr = bufferPtr + Packet.InnerPacketRouteRouteIdLocation;
                *(uint*)rpcIdPtr = rpcId;
                *(long*)routeIdPtr = routeId;
            }

            MemoryStream.Seek(0, SeekOrigin.Begin);
        }

        public override MemoryStreamBuffer RentMemoryStream(MemoryStreamBufferSource memoryStreamBufferSource, int size = 0)
        {
            throw new NotImplementedException();
        }

        public override object Deserialize(Type messageType)
        {
            if (MemoryStream == null)
            {
                Log.Debug("Deserialize MemoryStream is null");
                return null;
            }

            object obj = null;
            MemoryStream.Seek(Packet.InnerPacketHeadLength, SeekOrigin.Begin);

            if (MemoryStream.Length == 0)
            {
                if (_createInstances.TryGetValue(messageType, out var createInstance))
                {
                    return createInstance();
                }

                createInstance = CreateInstance.CreateObject(messageType);
                _createInstances.TryAdd(messageType, createInstance);
                return createInstance();
            }

            if (SerializerManager.TryGetSerializer(OpCodeIdStruct.OpCodeProtocolType, out var serializer))
            {
                obj = serializer.Deserialize(messageType, MemoryStream);
                MemoryStream.Seek(0, SeekOrigin.Begin);
                return obj;
            }
            
            MemoryStream.Seek(0, SeekOrigin.Begin);
            Log.Error($"protocolCode:{ProtocolCode} Does not support processing protocol");
            return null;
        }
    }
}
#endif