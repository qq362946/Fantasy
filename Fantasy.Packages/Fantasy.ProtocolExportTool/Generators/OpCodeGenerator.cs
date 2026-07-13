using System;
using System.Collections.Generic;
using Fantasy.Network;
using Fantasy.ProtocolExportTool.Models;

namespace Fantasy.ProtocolExportTool.Generators;

/// <summary>
/// OpCode 生成器 - 为消息生成唯一的 OpCode
/// </summary>
public sealed class OpCodeGenerator(bool isOuter, IReadOnlyDictionary<string, uint> existingCodes, IReadOnlyDictionary<uint, string> existingCodeOwners)
{
    private readonly ProtocolOpCode _opCode = new();
    private readonly HashSet<uint> _usedCodes = new(existingCodeOwners.Keys);
    private readonly IReadOnlyDictionary<string, uint> _existingCodes = existingCodes;
    private readonly IReadOnlyDictionary<uint, string> _existingCodeOwners = existingCodeOwners;
    private readonly Dictionary<string, uint> _assignedCodes = new(StringComparer.Ordinal);

    public OpCodeGenerator(bool isOuter)
        : this(isOuter, new Dictionary<string, uint>(StringComparer.Ordinal), new Dictionary<uint, string>())
    {
    }

    public IReadOnlyDictionary<string, uint> AssignedCodes => _assignedCodes;

    public OpcodeInfo Generate(MessageDefinition message)
    {
        var protocolType = GetProtocolType(message.InterfaceType);
        var opcodeInfo = new OpcodeInfo
        {
            Name = message.Name,
            ProtocolType = message.Protocol.OpCodeType
        };

        if (_existingCodes.TryGetValue(message.Name, out var existingCode) &&
            _existingCodeOwners.TryGetValue(existingCode, out var owner) &&
            string.Equals(owner, message.Name, StringComparison.Ordinal) &&
            IsMatchingProtocol(existingCode, message.Protocol.OpCodeType, protocolType))
        {
            opcodeInfo.Code = existingCode;
            _assignedCodes[message.Name] = existingCode;
            return opcodeInfo;
        }

        opcodeInfo.Code = GenerateNewCode(message, protocolType);
        _assignedCodes[message.Name] = opcodeInfo.Code;
        return opcodeInfo;
    }

    private static bool IsMatchingProtocol(uint code, uint opCodeProtocolType, uint protocolType)
    {
        OpCodeIdStruct opCodeId = code;
        return opCodeId.OpCodeProtocolType == opCodeProtocolType && opCodeId.Protocol == protocolType;
    }

    private uint GenerateNewCode(MessageDefinition message, uint protocolType)
    {
        var counter = GetCounterValue(message.InterfaceType);

        while (true)
        {
            var code = OpCode.Create(message.Protocol.OpCodeType, protocolType, counter);
            counter++;
            if (_usedCodes.Add(code))
            {
                SetCounterValue(message.InterfaceType, counter);
                return code;
            }
        }
    }

    private uint GetProtocolType(string interfaceType)
    {
        return interfaceType switch
        {
            "IMessage" when isOuter => _opCode.Message = OpCodeType.OuterMessage,
            "IMessage" => _opCode.Message = OpCodeType.InnerMessage,
            "IRequest" when isOuter => _opCode.Request = OpCodeType.OuterRequest,
            "IRequest" => _opCode.Request = OpCodeType.InnerRequest,
            "IResponse" when isOuter => _opCode.Response = OpCodeType.OuterResponse,
            "IResponse" => _opCode.Response = OpCodeType.InnerResponse,
            "IAddressMessage" when !isOuter => _opCode.AddressMessage = OpCodeType.InnerAddressMessage,
            "IAddressRequest" when !isOuter => _opCode.AddressRequest = OpCodeType.InnerAddressRequest,
            "IAddressResponse" when !isOuter => _opCode.AddressResponse = OpCodeType.InnerAddressResponse,
            "IAddressableMessage" when isOuter => _opCode.AddressableMessage = OpCodeType.OuterAddressableMessage,
            "IAddressableMessage" when !isOuter => _opCode.AddressableMessage = OpCodeType.InnerAddressableMessage,
            "IAddressableRequest" when isOuter => _opCode.AddressableRequest = OpCodeType.OuterAddressableRequest,
            "IAddressableRequest" when !isOuter => _opCode.AddressableRequest = OpCodeType.InnerAddressableRequest,
            "IAddressableResponse" when isOuter => _opCode.AddressableResponse = OpCodeType.OuterAddressableResponse,
            "IAddressableResponse" when !isOuter => _opCode.AddressableResponse = OpCodeType.InnerAddressableResponse,
            "ICustomRouteMessage" when isOuter => _opCode.CustomRouteMessage = OpCodeType.OuterCustomRouteMessage,
            "ICustomRouteRequest" when isOuter => _opCode.CustomRouteRequest = OpCodeType.OuterCustomRouteRequest,
            "ICustomRouteResponse" when isOuter => _opCode.CustomRouteResponse = OpCodeType.OuterCustomRouteResponse,
            "IRoamingMessage" when isOuter => _opCode.RoamingMessage = OpCodeType.OuterRoamingMessage,
            "IRoamingMessage" when !isOuter => _opCode.RoamingMessage = OpCodeType.InnerRoamingMessage,
            "IRoamingRequest" when isOuter => _opCode.RoamingRequest = OpCodeType.OuterRoamingRequest,
            "IRoamingRequest" when !isOuter => _opCode.RoamingRequest = OpCodeType.InnerRoamingRequest,
            "IRoamingResponse" when isOuter => _opCode.RoamingResponse = OpCodeType.OuterRoamingResponse,
            "IRoamingResponse" when !isOuter => _opCode.RoamingResponse = OpCodeType.InnerRoamingResponse,
            _ => throw new InvalidOperationException($"Unsupported interface type: {interfaceType}")
        };
    }

    private uint GetCounterValue(string interfaceType)
    {
        return interfaceType switch
        {
            "IMessage" => _opCode.AMessage,
            "IRequest" => _opCode.ARequest,
            "IResponse" => _opCode.AResponse,
            "IAddressMessage" when !isOuter => _opCode.AAddressMessage,
            "IAddressRequest" when !isOuter => _opCode.AAddressRequest,
            "IAddressResponse" when !isOuter => _opCode.AAddressResponse,
            "IAddressableMessage" => _opCode.AAddressableMessage,
            "IAddressableRequest" => _opCode.AAddressableRequest,
            "IAddressableResponse" => _opCode.AAddressableResponse,
            "ICustomRouteMessage" when isOuter => _opCode.ACustomRouteMessage,
            "ICustomRouteRequest" when isOuter => _opCode.ACustomRouteRequest,
            "ICustomRouteResponse" when isOuter => _opCode.ACustomRouteResponse,
            "IRoamingMessage" => _opCode.ARoamingMessage,
            "IRoamingRequest" => _opCode.ARoamingRequest,
            "IRoamingResponse" => _opCode.ARoamingResponse,
            _ => throw new InvalidOperationException($"Unsupported interface type: {interfaceType}")
        };
    }

    private void SetCounterValue(string interfaceType, uint value)
    {
        switch (interfaceType)
        {
            case "IMessage":
                _opCode.AMessage = value;
                return;
            case "IRequest":
                _opCode.ARequest = value;
                return;
            case "IResponse":
                _opCode.AResponse = value;
                return;
            case "IAddressMessage" when !isOuter:
                _opCode.AAddressMessage = value;
                return;
            case "IAddressRequest" when !isOuter:
                _opCode.AAddressRequest = value;
                return;
            case "IAddressResponse" when !isOuter:
                _opCode.AAddressResponse = value;
                return;
            case "IAddressableMessage":
                _opCode.AAddressableMessage = value;
                return;
            case "IAddressableRequest":
                _opCode.AAddressableRequest = value;
                return;
            case "IAddressableResponse":
                _opCode.AAddressableResponse = value;
                return;
            case "ICustomRouteMessage" when isOuter:
                _opCode.ACustomRouteMessage = value;
                return;
            case "ICustomRouteRequest" when isOuter:
                _opCode.ACustomRouteRequest = value;
                return;
            case "ICustomRouteResponse" when isOuter:
                _opCode.ACustomRouteResponse = value;
                return;
            case "IRoamingMessage":
                _opCode.ARoamingMessage = value;
                return;
            case "IRoamingRequest":
                _opCode.ARoamingRequest = value;
                return;
            case "IRoamingResponse":
                _opCode.ARoamingResponse = value;
                return;
            default:
                throw new InvalidOperationException($"Unsupported interface type: {interfaceType}");
        }
    }
}
