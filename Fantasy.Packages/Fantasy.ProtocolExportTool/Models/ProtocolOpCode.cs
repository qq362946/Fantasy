namespace Fantasy.ProtocolExportTool.Models;

public class ProtocolOpCode
{
    private const int Start = 10001;
    
    public uint Message;
    public uint Request;
    public uint Response;
    public uint AddressMessage;
    public uint AddressRequest;
    public uint AddressResponse;
    public uint AddressableMessage;
    public uint AddressableRequest;
    public uint AddressableResponse;
    public uint CustomRouteMessage;
    public uint CustomRouteRequest;
    public uint CustomRouteResponse;
    public uint RoamingMessage;
    public uint RoamingRequest;
    public uint RoamingResponse;

    public uint AMessage = Start;
    public uint ARequest = Start;
    public uint AResponse = Start;
    public uint AAddressMessage = Start;
    public uint AAddressRequest = Start;
    public uint AAddressResponse = Start;
    public uint AAddressableMessage = Start;
    public uint AAddressableRequest = Start;
    public uint AAddressableResponse = Start;
    public uint ACustomRouteMessage = Start;
    public uint ACustomRouteRequest = Start;
    public uint ACustomRouteResponse = Start;
    public uint ARoamingMessage = Start;
    public uint ARoamingRequest = Start;
    public uint ARoamingResponse = Start;
}