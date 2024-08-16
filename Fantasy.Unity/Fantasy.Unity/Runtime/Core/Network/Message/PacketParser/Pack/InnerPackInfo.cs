// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#if FANTASY_NET
namespace Fantasy;

public sealed class InnerPackInfo : APackInfo
{
    private readonly Dictionary<Type, Func<object>> _createInstances = new Dictionary<Type, Func<object>>();

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }
        var network = Network;
        base.Dispose();
        network.ReturnInnerPackInfo(this);
    }

    public static InnerPackInfo Create(ANetwork network)
    {
        var innerPackInfo = network.RentInnerPackInfo();
        innerPackInfo.Network = network;
        innerPackInfo.IsDisposed = false;
        return innerPackInfo;
    }

    public override MemoryStreamBuffer RentMemoryStream(int size = 0)
    {
        if (MemoryStream == null)
        {
            MemoryStream = Network.RentMemoryStream(size);
        }

        return MemoryStream;
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
            _createInstances.Add(messageType, createInstance);
            return createInstance();
        }

        switch (ProtocolCode)
        {
            case >= OpCode.InnerBsonRouteResponse:
            {
                obj = BsonPackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.InnerRouteResponse:
            {
                obj = MessagePackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.OuterRouteResponse:
            {
                obj = MessagePackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.InnerBsonRouteMessage:
            {
                obj = BsonPackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.InnerRouteMessage:
            case >= OpCode.OuterRouteMessage:
            {
                obj = MessagePackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.InnerBsonResponse:
            {
                obj = BsonPackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.InnerResponse:
            {
                obj = MessagePackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.OuterResponse:
            {
                MemoryStream.Seek(0, SeekOrigin.Begin);
                Log.Error($"protocolCode:{ProtocolCode} Does not support processing protocol");
                return null;
            }
            case >= OpCode.InnerBsonMessage:
            {
                obj = BsonPackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            case >= OpCode.InnerMessage:
            {
                obj = MessagePackHelper.Deserialize(messageType, MemoryStream);
                break;
            }
            default:
            {
                MemoryStream.Seek(0, SeekOrigin.Begin);
                Log.Error($"protocolCode:{ProtocolCode} Does not support processing protocol");
                return null;
            }
        }

        MemoryStream.Seek(0, SeekOrigin.Begin);
        return obj;
    }
}
#endif