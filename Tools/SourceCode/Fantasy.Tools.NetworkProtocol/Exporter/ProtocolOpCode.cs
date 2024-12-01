namespace Fantasy.Tools.ProtocalExporter;

public class ProtocolOpCode
{
    private const int Start = 10001;

    public uint Message;
    public uint Request;
    public uint Response;
    public uint RouteMessage;
    public uint RouteRequest;
    public uint RouteResponse;
    public uint AddressableMessage;
    public uint AddressableRequest;
    public uint AddressableResponse;
    public uint CustomRouteMessage;
    public uint CustomRouteRequest;
    public uint CustomRouteResponse;

    public uint AMessage = Start;
    public uint ARequest = Start;
    public uint AResponse = Start;
    public uint ARouteMessage = Start;
    public uint ARouteRequest = Start;
    public uint ARouteResponse = Start;
    public uint AAddressableMessage = Start;
    public uint AAddressableRequest = Start;
    public uint AAddressableResponse = Start;
    public uint ACustomRouteMessage = Start;
    public uint ACustomRouteRequest = Start;
    public uint ACustomRouteResponse = Start;
}