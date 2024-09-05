namespace Fantasy.Exporter;

public enum ProtocolOpCodeType
{
    None = 0,
    Bson = 1,
    ProtoBuf = 2,
    MemoryPack = 3
}

public class OpCodeModel
{
    public uint ABsonMessage;
    public uint AProtoBufMessage;
    public uint AMemoryPackMessage;
    public uint ABsonRequest;
    public uint AProtoBufRequest;
    public uint AMemoryPackRequest;
    public uint ABsonResponse;
    public uint AProtoBufResponse;
    public uint AMemoryPackResponse;
    public uint ABsonRouteMessage;
    public uint AProtoBufRouteMessage;
    public uint AMemoryPackRouteMessage;
    public uint ABsonRouteRequest;
    public uint AProtoBufRouteRequest;
    public uint AMemoryPackRouteRequest;
    public uint ABsonRouteResponse;
    public uint AProtoBufRouteResponse;
    public uint AMemoryPackRouteResponse;
}

public sealed class ProtocolOpCode
{
    public OpCodeModel Outer = new()
    {
        ABsonMessage = 0,
        AProtoBufMessage = OpCode.OuterProtoBufMessage + Start,
        AMemoryPackMessage = OpCode.OuterMemoryPackMessage + Start,
        ABsonRequest = 0,
        AProtoBufRequest = OpCode.OuterProtoBufRequest + Start,
        AMemoryPackRequest = OpCode.OuterMemoryPackRequest + Start,
        ABsonResponse = 0,
        AProtoBufResponse = OpCode.OuterProtoBufResponse + Start,
        AMemoryPackResponse = OpCode.OuterMemoryPackResponse + Start,
        ABsonRouteMessage = 0,
        AProtoBufRouteMessage = OpCode.OuterProtoBufRouteMessage + Start,
        AMemoryPackRouteMessage = OpCode.OuterMemoryPackRouteMessage + Start,
        ABsonRouteRequest = 0,
        AProtoBufRouteRequest = OpCode.OuterProtoBufRouteRequest + Start,
        AMemoryPackRouteRequest = OpCode.OuterMemoryPackRouteRequest + Start,
        ABsonRouteResponse = 0,
        AProtoBufRouteResponse = OpCode.OuterProtoBufRouteResponse + Start,
        AMemoryPackRouteResponse = OpCode.OuterMemoryPackRouteResponse + Start,
    };
    public OpCodeModel Inner = new()
    {
        ABsonMessage = OpCode.InnerBsonMessage + Start,
        AProtoBufMessage = OpCode.InnerProtoBufMessage + Start,
        AMemoryPackMessage = OpCode.InnerMemoryPackMessage + Start,
        ABsonRequest = OpCode.InnerBsonRequest + Start,
        AProtoBufRequest = OpCode.InnerProtoBufRequest + Start,
        AMemoryPackRequest = OpCode.InnerMemoryPackRequest + Start,
        ABsonResponse = OpCode.InnerBsonResponse + Start,
        AProtoBufResponse = OpCode.InnerProtoBufResponse + Start,
        AMemoryPackResponse = OpCode.InnerMemoryPackResponse + Start,
        ABsonRouteMessage = OpCode.InnerBsonRouteMessage + Start,
        AProtoBufRouteMessage = OpCode.InnerProtoBufRouteMessage + Start,
        AMemoryPackRouteMessage = OpCode.InnerMemoryPackRouteMessage + Start,
        ABsonRouteRequest = OpCode.InnerBsonRouteRequest + Start,
        AProtoBufRouteRequest = OpCode.InnerProtoBufRouteRequest + Start,
        AMemoryPackRouteRequest = OpCode.InnerMemoryPackRouteRequest + Start,
        ABsonRouteResponse = OpCode.InnerBsonRouteResponse + Start,
        AProtoBufRouteResponse = OpCode.InnerProtoBufRouteResponse + Start,
        AMemoryPackRouteResponse = OpCode.InnerMemoryPackRouteResponse + Start
    };
    private const int Start = 10000;
}