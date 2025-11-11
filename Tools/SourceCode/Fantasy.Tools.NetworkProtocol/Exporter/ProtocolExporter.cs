using System.Text;
using Fantasy.Exporter;
using Fantasy.Helper;
using Fantasy.Network;
using OpCode = Fantasy.Network.OpCode;
using OpCodeType = Fantasy.Network.OpCodeType;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ConditionIsAlwaysTrueOrFalse
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8604 // Possible null reference argument.

namespace Fantasy.Tools.ProtocalExporter;

// 自定义异常，用于协议格式错误
public class ProtocolFormatException : Exception
{
    public ProtocolFormatException(string message) : base(message) { }
}

public enum NetworkProtocolOpCodeType
{
    None = 0,
    Outer = 1,
    Inner = 2,
}
public sealed class OpcodeInfo
{
    public uint Code;
    public string Name;
}

public sealed class MessageFieldInfo
{
    public string FieldName;
    public string FieldType;
}

public sealed class MessageHelperInfo
{
    public string MessageName;
    public string MessageType; // "IMessage" or "IRequest"
    public string ResponseType; // Only for IRequest
    public List<MessageFieldInfo> Fields = new(); // 消息的属性字段
}

public sealed class ProtocolExporter
{
    private string _serverTemplate;
    private string _clientTemplate;
    private readonly List<OpcodeInfo> _opcodes = new();
    private static readonly char[] SplitChars = [' ', '\t'];
    private readonly string _networkProtocolDirectory;
    private readonly string _networkProtocolClientDirectory;
    private readonly string _networkProtocolServerDirectory;
    private readonly string _networkProtocolDirectoryOuter;
    private readonly string _networkProtocolDirectoryInner;
    
    public ProtocolExporter()
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        if (ExporterSettingsHelper.NetworkProtocolDirectory == null || ExporterSettingsHelper.NetworkProtocolDirectory.Trim() == "")
        {
            Log.Info($"NetworkProtocolDirectory Can not be empty!");
            return;
        }

        _networkProtocolDirectory = ExporterSettingsHelper.NetworkProtocolDirectory;
        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
        {
            if (ExporterSettingsHelper.NetworkProtocolClientDirectory?.Trim() == "")
            {
                Log.Info($"NetworkProtocolClientDirectory Can not be empty!");
                return;
            }

            _networkProtocolClientDirectory = ExporterSettingsHelper.NetworkProtocolClientDirectory ?? string.Empty;
            if (!Directory.Exists(_networkProtocolClientDirectory))
            {
                Directory.CreateDirectory(_networkProtocolClientDirectory);
            }
        }

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
        {
            if (ExporterSettingsHelper.NetworkProtocolServerDirectory?.Trim() == "")
            {
                Log.Info($"NetworkProtocolServerDirectory Can not be empty!");
                return;
            }

            _networkProtocolServerDirectory = ExporterSettingsHelper.NetworkProtocolServerDirectory ?? string.Empty;
            if (!Directory.Exists(_networkProtocolServerDirectory))
            {
                Directory.CreateDirectory(_networkProtocolServerDirectory);
            }

            _networkProtocolDirectoryInner = $"{_networkProtocolDirectory}Inner";

            if (!Directory.Exists(_networkProtocolDirectoryInner))
            {
                Directory.CreateDirectory(_networkProtocolDirectoryInner);
            }
        }

        _networkProtocolDirectoryOuter = $"{_networkProtocolDirectory}Outer";

        if (!Directory.Exists(_networkProtocolDirectoryOuter))
        {
            Directory.CreateDirectory(_networkProtocolDirectoryOuter);
        }
    }

    public void Run()
    {
        var tasks = new Task[3];
        tasks[0] = Task.Run(RouteType);
        tasks[1] = Task.Run(RoamingType);
        tasks[2] = Task.Run(async () =>
        {
            LoadTemplate();
            await Start(NetworkProtocolOpCodeType.Outer);
            if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
            {
                await Start(NetworkProtocolOpCodeType.Inner);
            }
        });
        Task.WaitAll(tasks);
    }

    private async Task Start(NetworkProtocolOpCodeType opCodeType)
    {
        var className = "";
        var opCodeName = "";
        var file = new StringBuilder();
        var messageStr = new StringBuilder();
        var disposeStr = new StringBuilder();
        var errorCodeStr = new StringBuilder();
        var usingNamespace = new HashSet<string>();
        var saveDirectory = new Dictionary<string, string>();
        var helperInfos = new List<MessageHelperInfo>();

        OpcodeInfo opcodeInfo = null;
        ProtocolOpCode protocolOpCode = null;
        string[] protocolFiles = null;
        MessageHelperInfo currentHelperInfo = null; // 当前正在处理的消息Helper信息

        // 格式检测相关
        var allMessageNames = new HashSet<string>(); // 所有消息名称
        var currentFieldNames = new HashSet<string>(); // 当前消息的字段名
        var currentFieldNumbers = new HashSet<int>(); // 当前消息的字段编号
        var currentFilePath = ""; // 当前处理的文件路径

        _opcodes.Clear();
        
        switch (opCodeType)
        {
            case NetworkProtocolOpCodeType.Outer:
            {
                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
                {
                    saveDirectory.Add(_networkProtocolServerDirectory, _serverTemplate);
                }
                
                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
                {
                    saveDirectory.Add(_networkProtocolClientDirectory, _clientTemplate);
                }
                
                protocolOpCode = new ProtocolOpCode()
                {
                    Message = OpCodeType.OuterMessage,
                    Request = OpCodeType.OuterRequest,
                    Response = OpCodeType.OuterResponse,
                    AddressMessage = 0,
                    AddressRequest = 0,
                    AddressResponse = 0,
                    AddressableMessage = OpCodeType.OuterAddressableMessage,
                    AddressableRequest = OpCodeType.OuterAddressableRequest,
                    AddressableResponse = OpCodeType.OuterAddressableResponse,
                    CustomRouteMessage = OpCodeType.OuterCustomRouteMessage,
                    CustomRouteRequest = OpCodeType.OuterCustomRouteRequest,
                    CustomRouteResponse = OpCodeType.OuterCustomRouteResponse,
                    RoamingMessage = OpCodeType.OuterRoamingMessage,
                    RoamingRequest = OpCodeType.OuterRoamingRequest,
                    RoamingResponse = OpCodeType.OuterRoamingResponse,
                };
                opCodeName = "OuterOpcode";
                protocolFiles = FileHelper.GetDirectoryFile(_networkProtocolDirectoryOuter, "*.proto", SearchOption.AllDirectories);
                break;
            }
            case NetworkProtocolOpCodeType.Inner:
            {
                protocolOpCode = new ProtocolOpCode()
                {
                    Message = OpCodeType.InnerMessage,
                    Request = OpCodeType.InnerRequest,
                    Response = OpCodeType.InnerResponse,
                    AddressMessage = OpCodeType.InnerRouteMessage,
                    AddressRequest = OpCodeType.InnerRouteRequest,
                    AddressResponse = OpCodeType.InnerRouteResponse,
                    AddressableMessage = OpCodeType.InnerAddressableMessage,
                    AddressableRequest = OpCodeType.InnerAddressableRequest,
                    AddressableResponse = OpCodeType.InnerAddressableResponse,
                    CustomRouteMessage = 0,
                    CustomRouteRequest = 0,
                    CustomRouteResponse = 0,
                    RoamingMessage = OpCodeType.InnerRoamingMessage,
                    RoamingRequest = OpCodeType.InnerRoamingRequest,
                    RoamingResponse = OpCodeType.InnerRoamingResponse,
                };
                opCodeName = "InnerOpcode";
                saveDirectory.Add(_networkProtocolServerDirectory, _serverTemplate);
                protocolFiles = FileHelper.GetDirectoryFile(_networkProtocolDirectoryInner, "*.proto", SearchOption.AllDirectories);
                break;
            }
        }

        if (protocolFiles == null || protocolFiles.Length == 0)
        {
            return;
        }
        
        #region GenerateFiles

        foreach (var filePath in protocolFiles)
        {
            currentFilePath = filePath; // 记录当前文件路径
            var keyIndex = 1;
            var parameter = "";
            var hasOpCode = false;
            var isMsgHead = false;
            var isSetProtocol = false;
            string responseTypeStr = null;
            string customRouteType = null;
            string protocolMember = "ProtoMember";
            string protocolType = "\t[ProtoContract]";
            string protocolIgnore = "\t\t[ProtoIgnore]";
            var protocolOpCodeType = OpCodeProtocolType.ProtoBuf;
            var fileText = await File.ReadAllTextAsync(filePath);

            foreach (var line in fileText.Split('\n'))
            {
                var currentLine = line.Trim();

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    continue;
                }

                if (currentLine.StartsWith("///"))
                {
                    messageStr.AppendFormat("	/// <summary>\r\n" + "	/// {0}\r\n" + "	/// </summary>\r\n", currentLine.Substring("///".Length));
                    continue;
                }

                if (currentLine.StartsWith("// Protocol"))
                {
                    isSetProtocol = true;
                    var protocol = currentLine.Substring("// Protocol".Length).Trim();

                    switch (protocol)
                    {
                        case "ProtoBuf":
                        {
                            protocolType = "\t[ProtoContract]";
                            protocolIgnore = "\t\t[ProtoIgnore]";
                            protocolMember = "ProtoMember";
                            protocolOpCodeType = OpCodeProtocolType.ProtoBuf;
                            break;
                        }
                        case "Bson":
                        {
                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                            {
                                Log.Error("Under Outer, /// does not support the Bson protocol!");
                                return;
                            }
                            protocolType = null;
                            protocolIgnore = "\t\t[BsonIgnore]";
                            protocolMember = null;
                            protocolOpCodeType = OpCodeProtocolType.Bson;
                            break;
                        }
                        default:
                        {
                            if (!ExporterSettingsHelper.CustomSerializes.TryGetValue(protocol, out var customSerialize))
                            {
                                Log.Error($"// Protocol {protocol} is not supported!");
                                return;
                            }

                            usingNamespace.Add(customSerialize.NameSpace);
                            keyIndex = customSerialize.KeyIndex;
                            protocolType = customSerialize.Attribute;
                            protocolIgnore = customSerialize.Ignore;
                            protocolMember = customSerialize.Member;
                            protocolOpCodeType = customSerialize.OpCodeType;
                            break;
                        }
                    }
                }

                if (currentLine.StartsWith("message"))
                {
                    isMsgHead = true;
                    className = currentLine.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries)[1];

                    // 检测消息名称是否重复
                    if (!allMessageNames.Add(className))
                    {
                        var errorMsg = $"协议格式错误！\n文件: {currentFilePath}\n消息名称重复: {className}";
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(errorMsg);
                        Console.ResetColor();
                        throw new ProtocolFormatException(errorMsg);
                    }

                    // 清空当前消息的字段集合
                    currentFieldNames.Clear();
                    currentFieldNumbers.Clear();

                    var splits = currentLine.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

                    if (isSetProtocol)
                    {
                        if (protocolType != null)
                        {
                            messageStr.AppendLine(protocolType);
                        }
                    }
                    else
                    {
                        messageStr.AppendLine("\t[ProtoContract]");
                    }
                    
                    if (splits.Length > 1)
                    {
                        hasOpCode = true;
                        var parameterArray = currentLine.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim().Split(',');
                        parameter = parameterArray[0].Trim();
                        opcodeInfo = new OpcodeInfo()
                        {
                            Name = className
                        };
                        switch (parameterArray.Length)
                        {
                            case 2:
                            {
                                switch (parameter)
                                {
                                    case "ICustomRouteMessage":
                                    {
                                        customRouteType = $"Fantasy.RouteType.{parameterArray[1].Trim()}";
                                        break;
                                    }
                                    case "IRoamingMessage":
                                    {
                                        customRouteType = $"Fantasy.RoamingType.{parameterArray[1].Trim()}";
                                        break;
                                    }
                                    default:
                                    {
                                        responseTypeStr = parameterArray[1].Trim();
                                        break;
                                    }
                                }
                                break;
                            }
                            case 3:
                            {
                                responseTypeStr = parameterArray[1].Trim();
                                customRouteType = parameter.Contains("IRoaming") ? $"Fantasy.RoamingType.{parameterArray[2].Trim()}" : $"Fantasy.RouteType.{parameterArray[2].Trim()}";
                                break;
                            }
                        }
                    }
                    else
                    {
                        parameter = "";
                        hasOpCode = false;
                    }

                    messageStr.Append(string.IsNullOrWhiteSpace(parameter)
                        ? $"\tpublic partial class {className} : AMessage"
                        : $"\tpublic partial class {className} : AMessage, {parameter}");
                    continue;
                }

                if (!isMsgHead)
                {
                    continue;
                }

                switch (currentLine)
                {
                    case "{":
                    {
                        messageStr.AppendLine("\n\t{");
                        messageStr.AppendLine($"\t\tpublic static {className} Create(Scene scene)");
                        messageStr.AppendLine($"\t\t{{\n\t\t\treturn scene.MessagePoolComponent.Rent<{className}>();\n\t\t}}");
                        messageStr.AppendLine($"\t\tpublic override void Dispose()");
                        messageStr.AppendLine($"\t\t{{");
                        messageStr.AppendLine($"<<<<Dispose>>>#if FANTASY_NET || FANTASY_UNITY\n\t\t\tGetScene().MessagePoolComponent.Return<{className}>(this);\n#endif");
                        messageStr.AppendLine($"\t\t}}");

                        if (parameter == "IMessage")
                        {
                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.Message, protocolOpCode.AMessage++);
                            messageStr.AppendLine($"\t\tpublic uint OpCode() {{ return {opCodeName}.{className}; }}");

                            // 收集客户端Helper信息
                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                            {
                                currentHelperInfo = new MessageHelperInfo
                                {
                                    MessageName = className,
                                    MessageType = "IMessage"
                                };
                                helperInfos.Add(currentHelperInfo);
                            }
                        }
                        else
                        {
                            // 保存 responseTypeStr 用于后续收集Helper信息
                            var savedResponseType = responseTypeStr;

                            if (responseTypeStr != null)
                            {
                                messageStr.AppendLine(protocolIgnore);
                                messageStr.AppendLine($"\t\tpublic {responseTypeStr} ResponseType {{ get; set; }}");
                                responseTypeStr = null;
                            }
                            else
                            {
                                if (parameter.Contains("RouteRequest"))
                                {
                                    Log.Error($"{opcodeInfo.Name} 没指定ResponseType");
                                    return;
                                }
                            }

                            if (hasOpCode)
                            {
                                messageStr.AppendLine($"\t\tpublic uint OpCode() {{ return {opCodeName}.{className}; }}");
                            }

                            if (customRouteType != null)
                            {
                                messageStr.AppendLine(protocolIgnore);
                                messageStr.AppendLine($"\t\tpublic int RouteType => {customRouteType};");
                                customRouteType = null;
                            }

                            switch (parameter)
                            {
                                case "IRequest":
                                {
                                    opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.Request, protocolOpCode.ARequest++);

                                    // 收集客户端Helper信息
                                    if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                    {
                                        currentHelperInfo = new MessageHelperInfo
                                        {
                                            MessageName = className,
                                            MessageType = "IRequest",
                                            ResponseType = savedResponseType
                                        };
                                        helperInfos.Add(currentHelperInfo);
                                    }
                                    break;
                                }
                                case "IResponse":
                                {
                                    opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.Response, protocolOpCode.AResponse++);
                                    if (!string.IsNullOrEmpty(protocolMember))
                                    {
                                        errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                    }
                                    errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                    disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                    break;
                                }
                                default:
                                {
                                    switch (parameter)
                                    {
                                        case "IAddressableMessage":
                                        {
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressableMessage, protocolOpCode.AAddressableMessage++);

                                            // 收集客户端Helper信息
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                currentHelperInfo = new MessageHelperInfo
                                                {
                                                    MessageName = className,
                                                    MessageType = "IMessage" // IAddressableMessage也是Send方式
                                                };
                                                helperInfos.Add(currentHelperInfo);
                                            }
                                            break;
                                        }
                                        case "IAddressableRequest":
                                        {
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressableRequest, protocolOpCode.AAddressableRequest++);

                                            // 收集客户端Helper信息
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                currentHelperInfo = new MessageHelperInfo
                                                {
                                                    MessageName = className,
                                                    MessageType = "IRequest",
                                                    ResponseType = savedResponseType
                                                };
                                                helperInfos.Add(currentHelperInfo);
                                            }
                                            break;
                                        }
                                        case "IAddressableResponse":
                                        {
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressableResponse, protocolOpCode.AAddressableResponse++);
                                            if (!string.IsNullOrEmpty(protocolMember))
                                            {
                                                errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                            }
                                            errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                            disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                            break;
                                        }
                                        case "ICustomRouteMessage":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.CustomRouteMessage, protocolOpCode.ACustomRouteMessage++);

                                            // 收集客户端Helper信息
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                currentHelperInfo = new MessageHelperInfo
                                                {
                                                    MessageName = className,
                                                    MessageType = "IMessage" // ICustomRouteMessage也是Send方式
                                                };
                                                helperInfos.Add(currentHelperInfo);
                                            }
                                            break;
                                        }
                                        case "ICustomRouteRequest":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.CustomRouteRequest, protocolOpCode.ACustomRouteRequest++);

                                            // 收集客户端Helper信息
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                currentHelperInfo = new MessageHelperInfo
                                                {
                                                    MessageName = className,
                                                    MessageType = "IRequest",
                                                    ResponseType = savedResponseType
                                                };
                                                helperInfos.Add(currentHelperInfo);
                                            }
                                            break;
                                        }
                                        case "ICustomRouteResponse":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.CustomRouteResponse, protocolOpCode.ACustomRouteResponse++);
                                            if (!string.IsNullOrEmpty(protocolMember))
                                            {
                                                errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                            }
                                            errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                            disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                            break;
                                        }
                                        case "IRoamingMessage":
                                        {
                                            // if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            // {
                                            //     throw new NotSupportedException("Under Inner, /// does not support the IRoamingMessage!");
                                            // }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.RoamingMessage, protocolOpCode.ARoamingMessage++);

                                            // 收集客户端Helper信息
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                currentHelperInfo = new MessageHelperInfo
                                                {
                                                    MessageName = className,
                                                    MessageType = "IMessage" // IRoamingMessage也是Send方式
                                                };
                                                helperInfos.Add(currentHelperInfo);
                                            }
                                            break;
                                        }
                                        case "IRoamingRequest":
                                        {
                                            // if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            // {
                                            //     throw new NotSupportedException("Under Inner, /// does not support the IRoamingRequest!");
                                            // }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.RoamingRequest, protocolOpCode.ARoamingRequest++);

                                            // 收集客户端Helper信息
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                currentHelperInfo = new MessageHelperInfo
                                                {
                                                    MessageName = className,
                                                    MessageType = "IRequest",
                                                    ResponseType = savedResponseType
                                                };
                                                helperInfos.Add(currentHelperInfo);
                                            }
                                            break;
                                        }
                                        case "IRoamingResponse":
                                        {
                                            // if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            // {
                                            //     throw new NotSupportedException("Under Inner, /// does not support the IRoamingResponse!");
                                            // }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.RoamingResponse, protocolOpCode.ARoamingResponse++);
                                            if (!string.IsNullOrEmpty(protocolMember))
                                            {
                                                errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                            }
                                            errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                            disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                            break;
                                        }
                                        case "IAddressMessage":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the IAddressMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressMessage, protocolOpCode.AAddressMessage++);
                                            break;
                                        }
                                        case "IAddressRequest":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the IAddressRequest!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressRequest, protocolOpCode.AAddressRequest++);
                                            break;
                                        }
                                        case "IAddressResponse":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the IAddressResponse!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressResponse, protocolOpCode.AAddressResponse++);
                                            if (!string.IsNullOrEmpty(protocolMember))
                                            {
                                                errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                            }
                                            errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                            disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }
                        }

                        if (hasOpCode)
                        {
                            _opcodes.Add(opcodeInfo);
                        }

                        continue;
                    }
                    case "}":
                    {
                        isMsgHead = false;
                        currentHelperInfo = null; // 清空当前Helper信息
                        errorCodeStr = errorCodeStr.Replace("ErrorCodeKeyIndex", keyIndex.ToString());
                        messageStr = messageStr.Replace("<<<<Dispose>>>", disposeStr.ToString());
                        messageStr.Append(errorCodeStr);
                        messageStr.AppendLine("\t}");
                        file.Append(messageStr);
                        messageStr.Clear();
                        disposeStr.Clear();
                        errorCodeStr.Clear();
                        keyIndex = 1;
                        protocolType = "\t[ProtoContract]";
                        protocolIgnore = "\t\t[ProtoIgnore]";
                        protocolMember = "ProtoMember";
                        protocolOpCodeType = OpCodeProtocolType.ProtoBuf;
                        continue;
                    }
                    case "":
                    {
                        continue;
                    }
                }

                if (currentLine.StartsWith("//"))
                {
                    messageStr.AppendFormat("\t\t///<summary>\r\n" + "\t\t/// {0}\r\n" + "\t\t///</summary>\r\n", currentLine.TrimStart('/', '/'));
                    continue;
                }

                if (currentLine.StartsWith("repeatedArray"))
                {
                    Repeated(messageStr, disposeStr, currentLine, protocolMember, ref keyIndex, currentHelperInfo, "repeatedArray", className, currentFilePath, currentFieldNames, currentFieldNumbers);
                }
                else if (currentLine.StartsWith("repeatedList"))
                {
                    Repeated(messageStr, disposeStr, currentLine, protocolMember, ref keyIndex, currentHelperInfo, "repeatedList", className, currentFilePath, currentFieldNames, currentFieldNumbers);
                }
                else if (currentLine.StartsWith("repeated"))
                {
                    Repeated(messageStr, disposeStr, currentLine, protocolMember, ref keyIndex, currentHelperInfo, "repeated", className, currentFilePath, currentFieldNames, currentFieldNumbers);
                }
                else
                {
                    Members(messageStr, disposeStr, currentLine, protocolMember, ref keyIndex, currentHelperInfo, className, currentFilePath, currentFieldNames, currentFieldNumbers);
                }
            }
            
            var namespaceBuilder = new StringBuilder();
            
            foreach (var @namespace in usingNamespace)
            {
                namespaceBuilder.Append($"using {@namespace};\n");
            }
            
            var csName = $"{Path.GetFileNameWithoutExtension(filePath)}.cs";
            foreach (var (directory, template) in saveDirectory)
            {
                var csFile = Path.Combine(directory, csName);
                var content = template.Replace("(Content)", file.ToString());
                content = content.Replace("(UsingNamespace)", namespaceBuilder.ToString());
                await File.WriteAllTextAsync(csFile, content);
            }
            file.Clear();
        }

        #endregion

        #region GenerateNetworkProtocolHelper

        // 为客户端生成NetworkProtocolHelper
        if (opCodeType == NetworkProtocolOpCodeType.Outer &&
            ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client) &&
            helperInfos.Count > 0)
        {
            var helperContent = GenerateNetworkProtocolHelperFile(helperInfos);
            var helperFilePath = Path.Combine(_networkProtocolClientDirectory, "NetworkProtocolHelper.cs");
            await File.WriteAllTextAsync(helperFilePath, helperContent);
        }

        #endregion

        #region GenerateOpCode

        file.Clear();
        file.AppendLine("namespace Fantasy");
        file.AppendLine("{");
        file.AppendLine($"\tpublic static partial class {opCodeName}");
        file.AppendLine("\t{");

        foreach (var opcode in _opcodes)
        {
            file.AppendLine($"\t\t public const uint {opcode.Name} = {opcode.Code};");
        }
        
        _opcodes.Clear();
        file.AppendLine("\t}");
        file.AppendLine("}");

        foreach (var (directory, _) in saveDirectory)
        {
            var csFile = Path.Combine(directory, $"{opCodeName}.cs");
            await File.WriteAllTextAsync(csFile, file.ToString());
        }
        #endregion
    }
    
    private void Repeated(StringBuilder file, StringBuilder disposeStr, string newline, string protocolMember, ref int keyIndex, MessageHelperInfo currentHelperInfo, string repeatedType, string messageName, string filePath, HashSet<string> fieldNames, HashSet<int> fieldNumbers)
    {
        try
        {
            var index = newline.IndexOf(";", StringComparison.Ordinal);
            newline = newline.Remove(index);
            var property = newline.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
            var type = property[1];
            var name = property[2];
            var fieldNumber = int.Parse(property[4]);
            type = ConvertType(type);

            // 检测字段名重复
            if (!fieldNames.Add(name))
            {
                var errorMsg = $"协议格式错误！\n文件: {filePath}\n消息: {messageName}\n字段名重复: {name}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMsg);
                Console.ResetColor();
                throw new ProtocolFormatException(errorMsg);
            }

            // 检测字段编号重复
            if (!fieldNumbers.Add(fieldNumber))
            {
                var errorMsg = $"协议格式错误！\n文件: {filePath}\n消息: {messageName}\n字段编号重复: {fieldNumber} (字段: {name})";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMsg);
                Console.ResetColor();
                throw new ProtocolFormatException(errorMsg);
            }

            file.AppendLine($"\t\t[{protocolMember}({keyIndex++})]");

            switch (repeatedType)
            {
                case "repeated":
                    // public List<string> List = new List<string>();
                    file.AppendLine($"\t\tpublic List<{type}> {name} = new List<{type}>();");
                    disposeStr.AppendLine($"\t\t\t{name}.Clear();");

                    // 收集字段信息到Helper
                    if (currentHelperInfo != null)
                    {
                        currentHelperInfo.Fields.Add(new MessageFieldInfo
                        {
                            FieldName = name,
                            FieldType = $"List<{type}>"
                        });
                    }
                    break;

                case "repeatedArray":
                    // public string[] List;
                    file.AppendLine($"\t\tpublic {type}[] {name};");
                    disposeStr.AppendLine($"\t\t\t{name} = default;");

                    // 收集字段信息到Helper
                    if (currentHelperInfo != null)
                    {
                        currentHelperInfo.Fields.Add(new MessageFieldInfo
                        {
                            FieldName = name,
                            FieldType = $"{type}[]"
                        });
                    }
                    break;

                case "repeatedList":
                    // public List<string> List;
                    file.AppendLine($"\t\tpublic List<{type}> {name};");
                    disposeStr.AppendLine($"\t\t\t{name} = default;");

                    // 收集字段信息到Helper
                    if (currentHelperInfo != null)
                    {
                        currentHelperInfo.Fields.Add(new MessageFieldInfo
                        {
                            FieldName = name,
                            FieldType = $"List<{type}>"
                        });
                    }
                    break;
            }
        }
        catch (ProtocolFormatException)
        {
            // 格式错误已经打印过了，直接重新抛出
            throw;
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"解析字段时出错: {newline}\n错误: {e.Message}");
            Console.ResetColor();
            throw;
        }
    }

    private void Members(StringBuilder file, StringBuilder disposeStr, string currentLine, string protocolMember, ref int keyIndex, MessageHelperInfo currentHelperInfo, string messageName, string filePath, HashSet<string> fieldNames, HashSet<int> fieldNumbers)
    {
        try
        {
            var index = currentLine.IndexOf(";", StringComparison.Ordinal);
            currentLine = currentLine.Remove(index);
            var property = currentLine.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
            var type = property[0];
            var name = property[1];
            var fieldNumber = int.Parse(property[3]);
            var typeCs = ConvertType(type);

            // 检测字段名重复
            if (!fieldNames.Add(name))
            {
                var errorMsg = $"协议格式错误！\n文件: {filePath}\n消息: {messageName}\n字段名重复: {name}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMsg);
                Console.ResetColor();
                throw new ProtocolFormatException(errorMsg);
            }

            // 检测字段编号重复
            if (!fieldNumbers.Add(fieldNumber))
            {
                var errorMsg = $"协议格式错误！\n文件: {filePath}\n消息: {messageName}\n字段编号重复: {fieldNumber} (字段: {name})";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMsg);
                Console.ResetColor();
                throw new ProtocolFormatException(errorMsg);
            }
            if (protocolMember != null)
            {
                file.AppendLine($"\t\t[{protocolMember}({keyIndex++})]");
            }
            file.AppendLine($"\t\tpublic {typeCs} {name} {{ get; set; }}");
            disposeStr.AppendLine($"\t\t\t{name} = default;");

            // 收集字段信息到Helper
            if (currentHelperInfo != null)
            {
                currentHelperInfo.Fields.Add(new MessageFieldInfo
                {
                    FieldName = name,
                    FieldType = typeCs
                });
            }
        }
        catch (ProtocolFormatException)
        {
            // 格式错误已经打印过了，直接重新抛出
            throw;
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"解析字段时出错: {currentLine}\n错误: {e.Message}");
            Console.ResetColor();
            throw;
        }
    }

    private string ConvertType(string type)
    {
        return type switch
        {
            "int[]" => "int[] { }",
            "int32[]" => "int[] { }",
            "int64[]" => "long[] { }",
            "int32" => "int",
            "uint32" => "uint",
            "int64" => "long",
            "uint64" => "ulong",
            _ => type
        };
    }

    private async Task RoamingType()
    {
        var routeTypeFile = $"{_networkProtocolDirectory}RoamingType.Config";
        var protoFileText = "";
        if (!File.Exists(routeTypeFile))
        {
            protoFileText = "// Roaming协议定义(需要定义10000以上、因为10000以内的框架预留)\n";
            await File.WriteAllTextAsync(routeTypeFile, protoFileText);
        }
        else
        {
            protoFileText = await File.ReadAllTextAsync(routeTypeFile);
        }

        var roamingTypes = new HashSet<int>();
        var roamingTypeFileSb = new StringBuilder();
        roamingTypeFileSb.AppendLine("using System.Collections.Generic;");
        roamingTypeFileSb.AppendLine("namespace Fantasy\n{");
        roamingTypeFileSb.AppendLine("\t// Roaming协议定义(需要定义10000以上、因为10000以内的框架预留)\t");
        roamingTypeFileSb.AppendLine("\tpublic static class RoamingType\n\t{");

        foreach (var line in protoFileText.Split('\n'))
        {
            var currentLine = line.Trim();

            if (currentLine == "" || currentLine.StartsWith("//"))
            {
                continue;
            }

            var splits = currentLine.Split(["//"], StringSplitOptions.RemoveEmptyEntries);
            var routeTypeStr = splits[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var roamingType = routeTypeStr[1].Trim();
            roamingTypes.Add(int.Parse(roamingType));
            roamingTypeFileSb.Append($"\t\tpublic const int {routeTypeStr[0].Trim()} = {roamingType};");

            if (splits.Length > 1)
            {
                roamingTypeFileSb.Append($" // {splits[1].Trim()}\n");
            }
            else
            {
                roamingTypeFileSb.Append('\n');
            }
        }

        if (roamingTypes.Count > 0)
        {
            roamingTypeFileSb.AppendLine("\t\tpublic static IEnumerable<int> RoamingTypes");
            roamingTypeFileSb.AppendLine("\t\t{\n\t\t\tget\n\t\t\t{");
            foreach (var roamingType in roamingTypes)
            {
                roamingTypeFileSb.AppendLine($"\t\t\t\tyield return {roamingType};");
            }
            roamingTypeFileSb.AppendLine("\t\t\t}\n\t\t}");
        }
        

        roamingTypeFileSb.AppendLine("\t}\n}");
        var file = roamingTypeFileSb.ToString();

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
        {
            await File.WriteAllTextAsync($"{_networkProtocolServerDirectory}RoamingType.cs", file);
        }

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
        {
            await File.WriteAllTextAsync($"{_networkProtocolClientDirectory}RoamingType.cs", file);
        }
    }
    
    private async Task RouteType()
    {
        var routeTypeFile = $"{_networkProtocolDirectory}RouteType.Config";
        var protoFileText = "";
        if (!File.Exists(routeTypeFile))
        {
            protoFileText = "// Route协议定义(需要定义1000以上、因为1000以内的框架预留)\n";
            await File.WriteAllTextAsync(routeTypeFile, protoFileText);
        }
        else
        {
            protoFileText = await File.ReadAllTextAsync(routeTypeFile);
        }
        var routeTypeFileSb = new StringBuilder();
        routeTypeFileSb.AppendLine("namespace Fantasy\n{");
        routeTypeFileSb.AppendLine("\t// Route协议定义(需要定义1000以上、因为1000以内的框架预留)\t");
        routeTypeFileSb.AppendLine("\tpublic static class RouteType\n\t{");

        foreach (var line in protoFileText.Split('\n'))
        {
            var currentLine = line.Trim();

            if (currentLine.StartsWith("//"))
            {
                continue;
            }

            var splits = currentLine.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
            var routeTypeStr = splits[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            routeTypeFileSb.Append($"\t\tpublic const int {routeTypeStr[0].Trim()} = {routeTypeStr[1].Trim()};");

            if (splits.Length > 1)
            {
                routeTypeFileSb.Append($" // {splits[1].Trim()}\n");
            }
            else
            {
                routeTypeFileSb.Append('\n');
            }
        }

        routeTypeFileSb.AppendLine("\t}\n}");
        var file = routeTypeFileSb.ToString();

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
        {
            await File.WriteAllTextAsync($"{_networkProtocolServerDirectory}RouteType.cs", file);
        }

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
        {
            await File.WriteAllTextAsync($"{_networkProtocolClientDirectory}RouteType.cs", file);
        }
    }
    
    private void LoadTemplate()
    {
        string[] lines = NetworkProtocolTemplate.Template.Split(["\r\n", "\n"], StringSplitOptions.None);

        StringBuilder serverSb = new StringBuilder();
        StringBuilder clientSb = new StringBuilder();

        int flag = 0;
        foreach (string line in lines)
        {
            string trim = line.Trim();

            if (trim.StartsWith("#if") && trim.Contains("SERVER"))
            {
                flag = 1;
                continue;
            }
            else if (trim.StartsWith("#else"))
            {
                flag = 2;
                continue;
            }
            else if (trim.StartsWith($"#endif"))
            {
                flag = 0;
                continue;
            }

            switch (flag)
            {
                case 1: // 服务端
                {
                    serverSb.AppendLine(line);
                    break;
                }
                case 2: // 客户端
                {
                    clientSb.AppendLine(line);
                    break;
                }
                default: // 双端
                {
                    serverSb.AppendLine(line);
                    clientSb.AppendLine(line);
                    break;
                }
            }
        }

        _serverTemplate = serverSb.Replace("(NetworkProtocol)", "ProtoBuf").ToString();
        _clientTemplate = clientSb.Replace("(NetworkProtocol)", "ProtoBuf").ToString();
    }

    private string GenerateNetworkProtocolHelperFile(List<MessageHelperInfo> helperInfos)
    {
        var helperSb = new StringBuilder();

        // 添加文件头和命名空间
        helperSb.AppendLine("using System.Runtime.CompilerServices;");
        helperSb.AppendLine("using Fantasy;");
        helperSb.AppendLine("using Fantasy.Async;");
        helperSb.AppendLine("using Fantasy.Network;");
        helperSb.AppendLine("using System.Collections.Generic;");
        helperSb.AppendLine("#pragma warning disable CS8618");
        helperSb.AppendLine();
        helperSb.AppendLine("namespace Fantasy");
        helperSb.AppendLine("{");
        helperSb.AppendLine("\tpublic static class NetworkProtocolHelper");
        helperSb.AppendLine("\t{");

        foreach (var info in helperInfos)
        {
            if (info.MessageType == "IMessage")
            {
                // 版本1: 生成接受消息对象的 Send 方法
                helperSb.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                helperSb.AppendLine($"\t\tpublic static void {info.MessageName}(this Session session, {info.MessageName} message)");
                helperSb.AppendLine("\t\t{");
                helperSb.AppendLine("\t\t\tsession.Send(message);");
                helperSb.AppendLine("\t\t}");
                helperSb.AppendLine();

                // 版本2: 生成接受属性参数的 Send 方法
                if (info.Fields.Count > 0)
                {
                    var parameters = string.Join(", ", info.Fields.Select(f => $"{f.FieldType} {char.ToLower(f.FieldName[0])}{f.FieldName.Substring(1)}"));
                    helperSb.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    helperSb.AppendLine($"\t\tpublic static void {info.MessageName}(this Session session, {parameters})");
                    helperSb.AppendLine("\t\t{");
                    helperSb.AppendLine($"\t\t\tusing var message = Fantasy.{info.MessageName}.Create(session.Scene);");
                    foreach (var field in info.Fields)
                    {
                        var paramName = $"{char.ToLower(field.FieldName[0])}{field.FieldName.Substring(1)}";
                        helperSb.AppendLine($"\t\t\tmessage.{field.FieldName} = {paramName};");
                    }
                    helperSb.AppendLine("\t\t\tsession.Send(message);");
                    helperSb.AppendLine("\t\t}");
                    helperSb.AppendLine();
                }
                else
                {
                    // 没有字段的消息，生成无参数版本
                    helperSb.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    helperSb.AppendLine($"\t\tpublic static void {info.MessageName}(this Session session)");
                    helperSb.AppendLine("\t\t{");
                    helperSb.AppendLine($"\t\t\tusing var message = Fantasy.{info.MessageName}.Create(session.Scene);");
                    helperSb.AppendLine("\t\t\tsession.Send(message);");
                    helperSb.AppendLine("\t\t}");
                    helperSb.AppendLine();
                }
            }
            else if (info.MessageType == "IRequest")
            {
                // 版本1: 生成接受请求对象的 Call 方法
                helperSb.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                helperSb.AppendLine($"\t\tpublic static async FTask<{info.ResponseType}> {info.MessageName}(this Session session, {info.MessageName} request)");
                helperSb.AppendLine("\t\t{");
                helperSb.AppendLine($"\t\t\treturn ({info.ResponseType})await session.Call(request);");
                helperSb.AppendLine("\t\t}");
                helperSb.AppendLine();

                // 版本2: 生成接受属性参数的 Call 方法
                if (info.Fields.Count > 0)
                {
                    var parameters = string.Join(", ", info.Fields.Select(f => $"{f.FieldType} {char.ToLower(f.FieldName[0])}{f.FieldName.Substring(1)}"));
                    helperSb.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    helperSb.AppendLine($"\t\tpublic static async FTask<{info.ResponseType}> {info.MessageName}(this Session session, {parameters})");
                    helperSb.AppendLine("\t\t{");
                    helperSb.AppendLine($"\t\t\tusing var request = Fantasy.{info.MessageName}.Create(session.Scene);");
                    foreach (var field in info.Fields)
                    {
                        var paramName = $"{char.ToLower(field.FieldName[0])}{field.FieldName.Substring(1)}";
                        helperSb.AppendLine($"\t\t\trequest.{field.FieldName} = {paramName};");
                    }
                    helperSb.AppendLine($"\t\t\treturn ({info.ResponseType})await session.Call(request);");
                    helperSb.AppendLine("\t\t}");
                    helperSb.AppendLine();
                }
                else
                {
                    // 没有字段的请求，生成无参数版本
                    helperSb.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    helperSb.AppendLine($"\t\tpublic static async FTask<{info.ResponseType}> {info.MessageName}(this Session session)");
                    helperSb.AppendLine("\t\t{");
                    helperSb.AppendLine($"\t\t\tusing var request = Fantasy.{info.MessageName}.Create(session.Scene);");
                    helperSb.AppendLine($"\t\t\treturn ({info.ResponseType})await session.Call(request);");
                    helperSb.AppendLine("\t\t}");
                    helperSb.AppendLine();
                }
            }
        }

        helperSb.AppendLine("\t}");
        helperSb.AppendLine("}");

        return helperSb.ToString();
    }
}