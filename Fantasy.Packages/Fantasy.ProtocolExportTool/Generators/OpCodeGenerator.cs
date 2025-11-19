using System;
using Fantasy.Network;
using Fantasy.ProtocolExportTool.Models;

namespace Fantasy.ProtocolExportTool.Generators;

/// <summary>
/// OpCode 生成器 - 为消息生成唯一的 OpCode
/// </summary>
public sealed class OpCodeGenerator(bool isOuter)
{
    private readonly ProtocolOpCode _opCode = new();

    /// <summary>
    /// 为消息生成 OpCode
    /// </summary>
    public OpcodeInfo Generate(MessageDefinition message)
    {
        var opcodeInfo = new OpcodeInfo
        {
            Name = message.Name,
            ProtocolType = message.Protocol.OpCodeType
        };

        var (protocolType, counter) = GetProtocolTypeAndCounter(message.InterfaceType);
        opcodeInfo.Code = OpCode.Create(message.Protocol.OpCodeType, protocolType, counter);
        
        return opcodeInfo;
    }

    /// <summary>
    /// 获取协议类型和计数器引用
    /// </summary>
    private (uint protocolType, uint counter) GetProtocolTypeAndCounter(string interfaceType)
    {
        return interfaceType switch
        {
            "IMessage" when isOuter => (_opCode.Message = OpCodeType.OuterMessage, _opCode.AMessage++),
            "IMessage" when !isOuter => (_opCode.Message = OpCodeType.InnerMessage, _opCode.AMessage++),

            "IRequest" when isOuter => (_opCode.Request = OpCodeType.OuterRequest, _opCode.ARequest++),
            "IRequest" when !isOuter => (_opCode.Request = OpCodeType.InnerRequest, _opCode.ARequest++),

            "IResponse" when isOuter => (_opCode.Response = OpCodeType.OuterResponse, _opCode.AResponse++),
            "IResponse" when !isOuter => (_opCode.Response = OpCodeType.InnerResponse, _opCode.AResponse++),

            "IAddressMessage" when !isOuter => (_opCode.AddressMessage = OpCodeType.InnerRouteMessage, _opCode.AAddressMessage++),
            "IAddressRequest" when !isOuter => (_opCode.AddressRequest = OpCodeType.InnerRouteRequest, _opCode.AAddressRequest++),
            "IAddressResponse" when !isOuter => (_opCode.AddressResponse = OpCodeType.InnerRouteResponse, _opCode.AAddressResponse++),

            "IAddressableMessage" when isOuter => (_opCode.AddressableMessage = OpCodeType.OuterAddressableMessage, _opCode.AAddressableMessage++),
            "IAddressableMessage" when !isOuter => (_opCode.AddressableMessage = OpCodeType.InnerAddressableMessage, _opCode.AAddressableMessage++),

            "IAddressableRequest" when isOuter => (_opCode.AddressableRequest = OpCodeType.OuterAddressableRequest, _opCode.AAddressableRequest++),
            "IAddressableRequest" when !isOuter => (_opCode.AddressableRequest = OpCodeType.InnerAddressableRequest, _opCode.AAddressableRequest++),

            "IAddressableResponse" when isOuter => (_opCode.AddressableResponse = OpCodeType.OuterAddressableResponse, _opCode.AAddressableResponse++),
            "IAddressableResponse" when !isOuter => (_opCode.AddressableResponse = OpCodeType.InnerAddressableResponse, _opCode.AAddressableResponse++),

            "ICustomRouteMessage" when isOuter => (_opCode.CustomRouteMessage = OpCodeType.OuterCustomRouteMessage, _opCode.ACustomRouteMessage++),
            "ICustomRouteRequest" when isOuter => (_opCode.CustomRouteRequest = OpCodeType.OuterCustomRouteRequest, _opCode.ACustomRouteRequest++),
            "ICustomRouteResponse" when isOuter => (_opCode.CustomRouteResponse = OpCodeType.OuterCustomRouteResponse, _opCode.ACustomRouteResponse++),

            "IRoamingMessage" when isOuter => (_opCode.RoamingMessage = OpCodeType.OuterRoamingMessage, _opCode.ARoamingMessage++),
            "IRoamingMessage" when !isOuter => (_opCode.RoamingMessage = OpCodeType.InnerRoamingMessage, _opCode.ARoamingMessage++),

            "IRoamingRequest" when isOuter => (_opCode.RoamingRequest = OpCodeType.OuterRoamingRequest, _opCode.ARoamingRequest++),
            "IRoamingRequest" when !isOuter => (_opCode.RoamingRequest = OpCodeType.InnerRoamingRequest, _opCode.ARoamingRequest++),

            "IRoamingResponse" when isOuter => (_opCode.RoamingResponse = OpCodeType.OuterRoamingResponse, _opCode.ARoamingResponse++),
            "IRoamingResponse" when !isOuter => (_opCode.RoamingResponse = OpCodeType.InnerRoamingResponse, _opCode.ARoamingResponse++),

            _ => throw new InvalidOperationException($"Unsupported interface type: {interfaceType}")
        };
    }
}
